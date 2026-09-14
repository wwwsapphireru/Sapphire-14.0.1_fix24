using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AdvantShop.Catalog;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Loging;
using AdvantShop.Core.Services.Loging.Triggers;
using AdvantShop.Core.Services.Loging.Triggers.Logs;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Core.Services.Messengers;
using AdvantShop.Core.Services.Smses;
using AdvantShop.Core.Services.Triggers.DeferredDatas;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.MobileApp;
using Newtonsoft.Json;

namespace AdvantShop.Core.Services.Triggers
{
    /// <summary>
    /// Сервис обработки событий, на которые срабатывает триггер
    /// </summary>
    public class TriggerProcessService
    {
        public static void ProcessEvent(ETriggerEventType eventType, ITriggerObject triggerObject)
        {
            try
            {
                var data = triggerObject.GetTriggerProcessObject();
                if (data == null)
                    return;

                var triggers =
                    TriggerRuleService.GetTriggersByType(eventType)
                        .Where(x => CheckTrigger(x, data.EventObjId, triggerObject))
                        .ToList();

                foreach (var trigger in triggers)
                {
                    if (!CheckFilter(trigger, triggerObject)
                        || (trigger.WorksOnlyOnceForCustomer && TriggerSendOnceDataService.IsExistForCustomer(trigger, data))
                        || (trigger.WorksOnlyOnce && TriggerSendOnceDataService.IsExist(trigger, data)))
                        continue;

                    var sendedEmails = new List<string>();
                    var sendedPhones = new List<long>();
                    var logger = LoggingManager.GetTriggerLogger(trigger.Id);
                    
                    logger.Info(TriggerLogEventType.CallTrigger);

                    foreach (var action in trigger.Actions)
                    {
                        var sendNow = action.TimeDelay == null;
                        if (sendNow)
                        {
                            ExecuteAction(data, action, trigger, triggerObject, sendedEmails, sendedPhones, logger);
                            continue;
                        }

                        TriggerDeferredDataService.Add(new TriggerDeferredData()
                        {
                            EntityId = data.EntityId,
                            TriggerActionId = action.Id,
                            TriggerObjectType = trigger.ObjectType
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        public static void ProcessDeferredData(TriggerDeferredData deferredData)
        {
            try
            {
                var action = TriggerActionService.GetTriggerAction(deferredData.TriggerActionId);
                if (action == null)
                {
                    TriggerDeferredDataService.Delete(deferredData.Id);
                    return;
                }

                if (!CheckTime(action, deferredData.DateCreated))
                    return;
                
                var trigger = TriggerRuleService.GetTrigger(action.TriggerRuleId);
                if (trigger == null || !trigger.Enabled)
                {
                    TriggerDeferredDataService.Delete(deferredData.Id);
                    return;
                }
                
                var logger = LoggingManager.GetTriggerLogger(trigger.Id);

                var triggerObject = TriggerRuleService.GetTriggerObject(trigger.ObjectType, deferredData.EntityId);
                if (triggerObject == null)
                {
                    TriggerDeferredDataService.Delete(deferredData.Id);

                    logger.Warning(TriggerLogEventType.CallTrigger,
                        $"Отложенный триггер отменен т.к. не найден объект триггера {trigger.ObjectType.ToString()} №{deferredData.EntityId}",
                        action.Id);
                    return;
                }

                var data = triggerObject.GetTriggerProcessObject();
                if (data == null)
                {
                    TriggerDeferredDataService.Delete(deferredData.Id);
                    
                    logger.Warning(TriggerLogEventType.CallTrigger, 
                        "Отложенный триггер отменен т.к. не найдены данные объекта триггера", 
                        action.Id);
                    return;
                }

                if (!CheckTrigger(trigger, data.EventObjId, triggerObject))
                    return;
                
                if (!CheckFilter(trigger, triggerObject))
                {
                    TriggerDeferredDataService.Delete(deferredData.Id);

                    logger.Warning(TriggerLogEventType.CallTrigger, 
                        "Отложенный триггер отменен т.к. не пройдена проверка условий отбора по фильтру", 
                        action.Id, data);
                    
                    return;
                }

                if ((trigger.WorksOnlyOnceForCustomer && TriggerSendOnceDataService.IsExistForCustomer(trigger, data))
                    || (trigger.WorksOnlyOnce && TriggerSendOnceDataService.IsExist(trigger, data)))
                {
                    TriggerDeferredDataService.Delete(deferredData.Id);

                    logger.Warning(TriggerLogEventType.CallTrigger, 
                        "Отложенный триггер отменен т.к. стоит срабатывать 1 раз", action.Id, data);
                    
                    return;
                }

                var sendedEmails = new List<string>();
                var sendedPhones = new List<long>();
                
                logger.Info(TriggerLogEventType.CallTrigger);

                ExecuteAction(data, action, trigger, triggerObject, sendedEmails, sendedPhones, logger);
                
                TriggerDeferredDataService.Delete(deferredData.Id);
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        public static void ProcessTriggersByDatetime()
        {
            try
            {
                var triggers = TriggerRuleService.GetTriggersByProcessType(ETriggerProcessType.Datetime).ToList();
                var nowHour = DateTime.Now.Hour;

                foreach (var trigger in triggers)
                {
                    if (trigger.PreferredHour != null && trigger.PreferredHour != nowHour)
                        continue;

                    var logger = LoggingManager.GetTriggerLogger(trigger.Id);
                    logger.Info(TriggerLogEventType.CallTrigger);
                    
                    try
                    {
                        foreach (var action in trigger.Actions)
                        {
                            var triggerObjects = trigger.GeTriggerObjects();
                            if (triggerObjects == null)
                            {
                                logger.Warning(TriggerLogEventType.CallTrigger, "Нет объектов для выполнения", action.Id);
                                continue;
                            }

                            var count = 0;
                            var sendedEmails = new List<string>();
                            var sendedPhones = new List<long>();

                            foreach (var triggerObject in triggerObjects)
                            {
                                try
                                {
                                    var data = triggerObject.GetTriggerProcessObject();

                                    if (data == null || !CheckFilter(trigger, triggerObject))
                                        continue;

                                    if ((trigger.WorksOnlyOnceForCustomer && TriggerSendOnceDataService.IsExistForCustomer(trigger, data))
                                        || (trigger.WorksOnlyOnce && TriggerSendOnceDataService.IsExist(trigger, data)))
                                    {
                                        logger.Warning(TriggerLogEventType.CallTrigger, 
                                            "Действие не выполнено для объекта т.к. стоит \"Срабатывает только 1 раз\"", 
                                            action.Id, data);
                                        
                                        continue;
                                    }

                                    ExecuteAction(data, action, trigger, triggerObject, sendedEmails, sendedPhones, logger);
                                    
                                    Thread.Sleep(count % 20 != 0 ? 100 : 500);
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log.Error(ex);
                                }
                                count++;
                            }
                            
                            if (count == 0)
                                logger.Warning(TriggerLogEventType.CallTrigger, "Нет объектов для выполнения", action.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log.Error(ex);
                        logger.Error(TriggerLogEventType.CallTrigger, ex.Message, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        /// <summary>
        /// Проверка тригера на активность, совпадение статуса и тд
        /// </summary>
        private static bool CheckTrigger(TriggerRule trigger, int eventObjId, ITriggerObject triggerObject)
        {
            return /*GlorySoft_003 trigger.Enabled &&*/ (trigger.EventObjId == null || trigger.EventObjId == eventObjId);
        }

        /// <summary>
        /// Проверка условий отбора по фильтру
        /// </summary>
        private static bool CheckFilter(TriggerRule trigger, ITriggerObject triggerObject)
        {
            return trigger.Filter == null || trigger.Filter.Check(triggerObject);
        }

        /// <summary>
        /// Проверка по времени
        /// </summary>
        private static bool CheckTime(TriggerAction action, DateTime eventDate)
        {
            if (action.TimeDelay == null)
                return true;

            return DateTime.Now >= action.TimeDelay.GetDateTime(eventDate);
        }

        private static bool ExecuteAction(
            TriggerProcessObject data, 
            TriggerAction action, 
            TriggerRule trigger, 
            ITriggerObject triggerObject, 
            List<string> sendedEmails, 
            List<long> sendedPhones,
            ITriggerLogger logger)
        {
            var result = false;
            
            try
            {
                switch (action.ActionType)
                {
                    case ETriggerActionType.Email:
                        result = SendMail(data.Email, trigger, action, triggerObject, data.CustomerId, sendedEmails, data, logger);
                        break;

                    case ETriggerActionType.Sms:
                        result = SendSms(data.Phone, trigger, action, triggerObject, data.CustomerId, sendedPhones, data, logger);
                        break;

                    case ETriggerActionType.Edit:
                        result = EditField(trigger, action, triggerObject, logger);
                        break;

                    case ETriggerActionType.SendRequest:
                        result = SendRequest(trigger, action, triggerObject, logger);
                        break;

                    case ETriggerActionType.Message:
                        result = SendMessage(trigger, action, triggerObject, data, logger);
                        break;

                    case ETriggerActionType.PushNotification:
                        result = SendPush(trigger, action, triggerObject, data, logger);
                        break;

                    case ETriggerActionType.SendToShippingService:
                        result = SendToShippingService(trigger, action, triggerObject, logger);
                        break;

                    case ETriggerActionType.CloseReceipt:
                        result = CloseReceipt(action, triggerObject, logger);
                        break;
                    
                    default:
                        throw new NotImplementedException("TriggerProcessService ExecuteAction " + action.ActionType);
                }
            }
            catch (NotImplementedException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                Debug.Log.Warn(ex);
            }

            if (trigger.WorksOnlyOnceForCustomer || trigger.WorksOnlyOnce)
            {
                var lastAction = trigger.Actions.OrderByDescending(x => x.TimeDelay == null).ThenBy(x => x.Id).Last();
                if (action.Id == lastAction.Id 
                    && !TriggerSendOnceDataService.IsExist(trigger, data))
                {
                    TriggerSendOnceDataService.Add(new TriggerSendOnceData()
                    {
                        TriggerId = trigger.Id,
                        EntityId = data.EntityId,
                        CustomerId = data.CustomerId,
                        TriggerType = (int)trigger.EventType,
                        CustomerMail = data.Email,
                        CustomerPhone = data.Phone
                    });
                }
            }

            return result;
        }

        private static bool SendMail(
            string customerEmail, 
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject,
            Guid customerId, List<string> sendedEmails, 
            TriggerProcessObject data, 
            ITriggerLogger logger)
        {
            var emails = new List<string>();

            if (action.SendEmailData.Params?.RecipientIsAnother is true
                && action.SendEmailData.Params.EmailToReceive.IsNotEmpty()
                && !sendedEmails.Any(x => x.Equals(action.SendEmailData.Params.EmailToReceive, StringComparison.OrdinalIgnoreCase)))
            {
                emails.Add(action.SendEmailData.Params.EmailToReceive);
            }

            if ((action.SendEmailData.Params == null || action.SendEmailData.Params.RecipientIsCustomer)
                && !string.IsNullOrWhiteSpace(customerEmail)
                && !sendedEmails.Any(x => x.Equals(customerEmail, StringComparison.OrdinalIgnoreCase)))
            {
                emails.Add(customerEmail);
            }

            if (emails.Count == 0)
            {
                logger.Error(TriggerLogEventType.Email, $"Нет почты, на которую отослать письмо", action.Id);
                
                return false;
            }

            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, customerId, data.EntityId);

            var subject = trigger.ReplaceVariables(action.SendEmailData.EmailSubject, triggerObject, couponByAction, triggerCouponCode);
            var body = trigger.ReplaceVariables(action.SendEmailData.EmailBody, triggerObject, couponByAction, triggerCouponCode);

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                logger.Error(TriggerLogEventType.Email, $"Не указаны заголовок '{subject}' или тело '{body}' письма", action.Id);
                
                return false;
            }

            foreach (var email in emails)
            {
                MailService.SendMailNow(
                    customerId,
                    email,
                    subject,
                    body,
                    true,
                    action.EmailingId,
                    onSuccess: () => logger.Success(TriggerLogEventType.Email, action.Id, new { email }),
                    onError: error => logger.Error(TriggerLogEventType.Email, error, action.Id, new { email }));
                
                sendedEmails.Add(email.ToLower());
            }
            
            return true;
        }

        private static bool SendSms(
            long customerPhone, 
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject, 
            Guid customerId, 
            List<long> sendedPhones, 
            TriggerProcessObject data,
            ITriggerLogger logger)
        {
            var phones = new List<long>();

            if (action.SendSmsData.Params != null 
                && action.SendSmsData.Params.RecipientIsAnother
                && action.SendSmsData.Params.PhoneToReceive.IsNotEmpty())
            {
                var standardPhone = StringHelper.ConvertToStandardPhone(action.SendSmsData.Params.PhoneToReceive);
                if (standardPhone.HasValue && !sendedPhones.Contains(standardPhone.Value))
                    phones.Add(standardPhone.Value);
            }

            if (customerPhone != 0
                && (action.SendSmsData.Params == null || action.SendSmsData.Params.RecipientIsCustomer)
                && !sendedPhones.Contains(customerPhone))
                phones.Add(customerPhone);

            if (phones.Count == 0)
            {
                logger.Error(TriggerLogEventType.Sms, "Нет телефона для отправки SMS", action.Id);
                return false;
            }

            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, customerId, data.EntityId);

            int? templateId = null;
            Dictionary<string, string> templateParameters = null;
            
            if (action.SendSmsData.SmsTemplateId != null && action.SendSmsData.SmsTemplateId != 0)
            {
                templateId = action.SendSmsData.SmsTemplateId;
                templateParameters = 
                    trigger.GetFormattedParameters(action.SendSmsData.SmsText, triggerObject, couponByAction, triggerCouponCode);
            }
            
            var sms = trigger.ReplaceVariables(action.SendSmsData.SmsText, triggerObject, couponByAction, triggerCouponCode);

            if (string.IsNullOrWhiteSpace(sms))
            {
                logger.Error(TriggerLogEventType.Sms, "Нет текста SMS", action.Id, new { phones });
                return false;
            }
            
            foreach (var phone in phones)
            {
                SmsNotifier.SendSms(
                    phone, 
                    sms, 
                    customerId, 
                    inBackground: false, 
                    isInternal: true, 
                    templateId: templateId, 
                    templateParameters: templateParameters,
                    onSuccess: () => logger.Success(TriggerLogEventType.Sms, action.Id, new { phone }),
                    onError: error => logger.Error(TriggerLogEventType.Sms, error, action.Id, new { phone, sms }));
                
                sendedPhones.Add(phone);
            }

            return true;
        }

        private static bool SendMessage(
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject, 
            TriggerProcessObject data,
            ITriggerLogger logger)
        {
            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, data.CustomerId, data.EntityId);

            var messageText = trigger.ReplaceVariables(action.MessageText, triggerObject, couponByAction, triggerCouponCode);

            var message = new Message
            {
                CustomerId = data.CustomerId,
                Email = data.Email,
                Phone = data.Phone,
                Text = messageText
            };

            MessengerServices.SendMessage(
                message,
                action.SendMessageData.ModuleNames,
                onSuccess: moduleName => logger.Success(TriggerLogEventType.Message, action.Id, new { moduleName }),
                onError: error => logger.Error(TriggerLogEventType.Message, error, action.Id, message));

            return true;
        }

        private static bool SendPush(
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject, 
            TriggerProcessObject data,
            ITriggerLogger logger)
        {
            if (action.NotificationBody.IsNullOrEmpty() 
                && action.NotificationTitle.IsNullOrEmpty())
            {
                logger.Error(TriggerLogEventType.PushNotification, $"Не указан заголовок и тело пуша", action.Id);
                return false;
            }

            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, data.CustomerId, data.EntityId);

            var body = trigger.ReplaceVariables(action.NotificationBody, triggerObject, couponByAction, triggerCouponCode);
            var title = trigger.ReplaceVariables(action.NotificationTitle, triggerObject, couponByAction, triggerCouponCode);
            
            if (action.NotificationRequestParams != null)
                foreach (var param in action.NotificationRequestParams)
                    param.Value = trigger.ReplaceVariables(param.Value, triggerObject, couponByAction, triggerCouponCode);

            var notification = new Notification
            {
                CustomerId = data.CustomerId,
                Body = body,
                Title = title,
                RequestParams = action.NotificationRequestParams?.ToDictionary(k => k.Key, v => v.Value),
                Source = new NotificationSource()
                {
                    Type = NotificationSourceType.Trigger,
                    SourceId = trigger.Id
                }
            };
            
            var error = NotificationService.SendNotification(notification);

            if (!string.IsNullOrEmpty(error))
            {
                Debug.Log.Error($"SendPush error: {error}. Trigger {trigger.Id}, action {action.Id}, notification: {JsonConvert.SerializeObject(notification)}");
                
                logger.Error(TriggerLogEventType.PushNotification, error, action.Id, notification);
                return false;
            }
            
            logger.Success(TriggerLogEventType.PushNotification, action.Id);
            
            return true;
        }

        private static bool EditField(
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject,
            ITriggerLogger logger)
        {
            if (!action.EditField.Type.HasValue)
            {
                logger.Error(TriggerLogEventType.Edit, $"Не выбрано поле", action.Id);
                return false;
            }
            
            return new TriggerProcessEditFieldService().EditField(trigger, action, triggerObject, logger);
        }

        private static bool SendToShippingService(
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject, 
            ITriggerLogger logger)
        {
            return new TriggerSendToShippingService(trigger, action, triggerObject, logger).SendToShippingService();
        }

        private static bool CloseReceipt(
            TriggerAction action, 
            ITriggerObject triggerObject, 
            ITriggerLogger logger)
        {
            return new TriggerCloseReceipt().CloseReceipt(action, triggerObject, logger);
        }

        private static bool SendRequest(
            TriggerRule trigger, 
            TriggerAction action, 
            ITriggerObject triggerObject, 
            ITriggerLogger logger)
        {
            return new TriggerSendRequestService().Send(trigger, action, triggerObject, logger);
        }
        
        private static string GetTriggerCouponCode(TriggerRule trigger, Guid customerId, int entityId)
        {
            if (trigger.Coupon == null)
                return null;

            var customerCoupon = CouponService.GetGeneratedTriggerCouponByCustomerId(trigger.Id, customerId, entityId);
            if (customerCoupon != null)
                return customerCoupon.Code;

            var coupon = CouponService.GenerateCoupon(trigger.Coupon, customerId, entityId);
            return coupon?.Code;
        }
    }
}
