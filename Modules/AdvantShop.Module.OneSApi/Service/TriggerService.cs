using AdvantShop.Catalog;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Bonuses.Model;
using AdvantShop.Core.Services.Bonuses.Model.Enums;
using AdvantShop.Core.Services.Bonuses.Service;
using AdvantShop.Core.Services.ChangeHistories;
using AdvantShop.Core.Services.Crm;
using AdvantShop.Core.Services.Crm.BusinessProcesses;
using AdvantShop.Core.Services.Crm.BusinessProcesses.Customers;
using AdvantShop.Core.Services.Crm.DealStatuses;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Core.Services.Messengers;
using AdvantShop.Core.Services.Smses;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Core.Services.Triggers.DeferredDatas;
using AdvantShop.Core.Services.Triggers.Orders;
using AdvantShop.Core.SQL;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.MobileApp;
using AdvantShop.Orders;
using AdvantShop.Payment;
using AdvantShop.Shipping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class TriggerServiceV8
    {
        public static void ProcessEvent(ETriggerEventType eventType, ITriggerObject triggerObject)
        {
            try
            {
                var categoryId = ModuleService.GetImportExportSettings().ImportOrder.TriggerCategoryId;
                if (categoryId == 0)
                    return;

                var data = triggerObject.GetTriggerProcessObject();
                if (data == null)
                    return;

                var triggers =
                    GetTriggersByType(eventType, categoryId)
                        .Where(x => CheckTrigger(x, data.EventObjId, triggerObject))
                        .ToList();

                foreach (var trigger in triggers)
                {
                    if (!CheckFilter(trigger, triggerObject)
                        || trigger.WorksOnlyOnce && TriggerSendOnceDataService.IsExist(trigger.Id, data.EntityId, data.CustomerId))
                        continue;

                    var sendedEmails = new List<string>();
                    var sendedPhones = new List<long>();

                    foreach (var action in trigger.Actions)
                    {
                        var sendNow = action.TimeDelay == null;
                        if (sendNow)
                        {
                            ExecuteAction(data, action, trigger, triggerObject, sendedEmails, sendedPhones);

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

        private static bool CheckTrigger(TriggerRule trigger, int eventObjId, ITriggerObject triggerObject)
        {
            return /*trigger.Enabled &&*/ (trigger.EventObjId == null || trigger.EventObjId == eventObjId);
        }

        private static bool CheckFilter(TriggerRule trigger, ITriggerObject triggerObject)
        {
            return trigger.Filter == null || trigger.Filter.Check(triggerObject);
        }

        private static bool ExecuteAction(TriggerProcessObject data, TriggerAction action, TriggerRule trigger, ITriggerObject triggerObject, List<string> sendedEmails, List<long> sendedPhones)
        {
            var result = false;

            try
            {
                switch (action.ActionType)
                {
                    case ETriggerActionType.Email:
                        result = SendMail(data.Email, trigger, action, triggerObject, data.CustomerId, sendedEmails, data);
                        break;

                    case ETriggerActionType.Sms:
                        result = SendSms(data.Phone, trigger, action, triggerObject, data.CustomerId, sendedPhones, data);
                        break;

                    case ETriggerActionType.Edit:
                        result = EditField(trigger, action, triggerObject);
                        break;

                    case ETriggerActionType.SendRequest:
                        result = SendRequest(trigger, action, triggerObject);
                        break;

                    case ETriggerActionType.Message:
                        result = SendMessage(trigger, action, triggerObject, data);
                        break;

                    case ETriggerActionType.PushNotification:
                        result = SendPush(trigger, action, triggerObject, data);
                        break;
                    default:
                        throw new NotImplementedException("TriggerProccessService ExecuteAction " + action.ActionType);
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

            if (trigger.WorksOnlyOnce)
            {
                var lastAction = trigger.Actions.OrderByDescending(x => x.TimeDelay == null).ThenBy(x => x.Id).Last();
                if (lastAction != null && action.Id == lastAction.Id)
                {
                    TriggerSendOnceDataService.Add(new TriggerSendOnceData()
                    {
                        TriggerId = trigger.Id,
                        EntityId = data.EntityId,
                        CustomerId = data.CustomerId
                    });
                }
            }

            return result;
        }

        private static bool SendMail(string email, TriggerRule trigger, TriggerAction action, ITriggerObject triggerObject, Guid customerId, List<string> sendedEmails, TriggerProcessObject data)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (sendedEmails.Contains(email.ToLower()))
                return false;

            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, customerId, data.EntityId);

            var subject = trigger.ReplaceVariables(action.EmailSubject, triggerObject, couponByAction, triggerCouponCode);
            var body = trigger.ReplaceVariables(action.EmailBody, triggerObject, couponByAction, triggerCouponCode);

            MailService.SendMailNow(customerId, email, subject, body, true, action.EmailingId);

            sendedEmails.Add(email.ToLower());

            return true;
        }

        private static bool SendSms(long phone, TriggerRule trigger, TriggerAction action, ITriggerObject triggerObject, Guid customerId, List<long> sendedPhones, TriggerProcessObject data)
        {
            if (phone == 0)
                return false;

            if (sendedPhones.Contains(phone))
                return false;

            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, customerId, data.EntityId);

            var sms = trigger.ReplaceVariables(action.SmsText, triggerObject, couponByAction, triggerCouponCode);

            SmsNotifier.SendSms(phone, sms, customerId, isInternal: true);

            sendedPhones.Add(phone);

            return true;
        }

        private static bool SendMessage(TriggerRule trigger, TriggerAction action, ITriggerObject triggerObject, TriggerProcessObject data)
        {
            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, data.CustomerId, data.EntityId);

            var message = trigger.ReplaceVariables(action.MessageText, triggerObject, couponByAction, triggerCouponCode);
            MessengerServices.SendMessage(new Message
            {
                CustomerId = data.CustomerId,
                Email = data.Email,
                Phone = data.Phone,
                Text = message
            });

            return true;
        }

        private static bool SendPush(TriggerRule trigger, TriggerAction action, ITriggerObject triggerObject, TriggerProcessObject data)
        {
            if (action.NotificationBody.IsNullOrEmpty() && action.NotificationTitle.IsNullOrEmpty())
                return false;

            var couponByAction = action.Coupons.FirstOrDefault();
            var triggerCouponCode = GetTriggerCouponCode(trigger, data.CustomerId, data.EntityId);

            var body = trigger.ReplaceVariables(action.NotificationBody, triggerObject, couponByAction, triggerCouponCode);
            var title = trigger.ReplaceVariables(action.NotificationTitle, triggerObject, couponByAction, triggerCouponCode);
            if (action.NotificationRequestParams != null)
                foreach (var param in action.NotificationRequestParams)
                    param.Value = trigger.ReplaceVariables(param.Value, triggerObject, couponByAction, triggerCouponCode);

            NotificationService.SendNotification(new Notification
            {
                CustomerId = data.CustomerId,
                Body = body,
                Title = title,
                RequestParams = action.NotificationRequestParams?.ToDictionary(k => k.Key, v => v.Value)
            });

            return true;
        }

        private static bool SendRequest(TriggerRule trigger, TriggerAction action, ITriggerObject triggerObject)
        {
            return TriggerSendRequestService.Send(trigger, action, triggerObject);
        }

        private static bool EditField(TriggerRule trigger, TriggerAction action, ITriggerObject triggerObject)
        {
            if (!action.EditField.Type.HasValue)
                return false;

            switch (trigger.EventType)
            {
                case ETriggerEventType.OrderCreated:
                case ETriggerEventType.OrderStatusChanged:
                case ETriggerEventType.OrderPaied:
                    {
                        var orderField = (EOrderFieldType)action.EditField.Type;
                        var order = (Order)triggerObject;
                        var customer = CustomerService.GetCustomer(order.OrderCustomer.CustomerID);
                        var customerExist = customer != null;

                        if (!customerExist)
                            customer = (Customer)order.OrderCustomer;

                        var orderContact = CustomerService.GetCustomerContacts(order.OrderCustomer.CustomerID).FirstOrDefault() ??
                                           new CustomerContact() { CustomerGuid = order.OrderCustomer.CustomerID };

                        EditFieldOrder(action, orderField, order, customer, orderContact, trigger);

                        OrderService.UpdateOrderMain(order, true, new OrderChangedBy("Триггер " + trigger.Name));
                        OrderService.UpdateOrderCustomer(order.OrderCustomer, new OrderChangedBy("Триггер " + trigger.Name));

                        if (customerExist)
                        {
                            CustomerService.UpdateCustomer(customer);

                            if (orderContact.ContactId == Guid.Empty)
                                CustomerService.AddContact(orderContact, customer.Id);
                            else
                                CustomerService.UpdateContact(orderContact, trackChanges: true, changedBy: new ChangedBy("Триггер " + trigger.Name));
                        }
                        break;
                    }

                case ETriggerEventType.LeadCreated:
                case ETriggerEventType.LeadStatusChanged:
                    {
                        var leadField = (ELeadFieldType)action.EditField.Type;
                        var lead = (Lead)triggerObject;
                        var leadCustomer = lead.Customer ?? new Customer();
                        var leadContact = leadCustomer.Contacts.FirstOrDefault();

                        if (leadContact == null)
                        {
                            leadContact = new CustomerContact() { CustomerGuid = lead.CustomerId ?? leadCustomer.Id };
                            leadCustomer.Contacts.Add(leadContact);
                        }

                        EditFieldLead(action, leadField, lead, leadCustomer, leadContact, trigger);

                        LeadService.UpdateLead(lead, true, new ChangedBy("Триггер " + trigger.Name));
                        break;
                    }
                case ETriggerEventType.CustomerCreated:
                case ETriggerEventType.TimeFromLastOrder:
                case ETriggerEventType.SignificantDate:
                case ETriggerEventType.SignificantCustomerDate:
                case ETriggerEventType.InstallMobileApp:
                    {
                        var customerField = (ECustomerFieldType)action.EditField.Type;
                        var customer = (Customer)triggerObject;
                        var customerContact = CustomerService.GetCustomerContacts(customer.Id).FirstOrDefault() ??
                                              new CustomerContact() { CustomerGuid = customer.Id };

                        EditFieldCustomer(action, customerField, customer, customerContact, trigger);

                        CustomerService.UpdateCustomer(customer);

                        if (customerContact.ContactId == Guid.Empty)
                            CustomerService.AddContact(customerContact, customer.Id);
                        else
                            CustomerService.UpdateContact(customerContact, trackChanges: true, changedBy: new ChangedBy("Триггер " + trigger.Name));

                        break;
                    }
                default:
                    throw new BlException("Wrong type " + trigger.EventType);
            }

            return true;
        }

        private static string GetTriggerCouponCode(TriggerRule trigger, Guid customerId, int entityId)
        {
            if (trigger.Coupon == null)
                return null;

            var customerCoupon = CouponService.GetGeneratedTriggerCouponByCustomerId(trigger.Id, customerId, entityId);
            if (customerCoupon != null)
                return customerCoupon.Code;

            var coupon = CouponService.GenerateCoupon(trigger.Coupon, customerId, entityId);
            return coupon != null ? coupon.Code : null;
        }

        private static void EditFieldOrder(TriggerAction action, EOrderFieldType orderField, Order order, Customer orderCustomer, CustomerContact orderContact, TriggerRule trigger)
        {
            switch (orderField)
            {
                case EOrderFieldType.LastName:
                    order.OrderCustomer.LastName = action.EditField.EditFieldValue;
                    orderCustomer.LastName = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.FirstName:
                    order.OrderCustomer.FirstName = action.EditField.EditFieldValue;
                    orderCustomer.FirstName = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.Patronymic:
                    order.OrderCustomer.Patronymic = action.EditField.EditFieldValue;
                    orderCustomer.Patronymic = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.Phone:
                    order.OrderCustomer.Phone = action.EditField.EditFieldValue;
                    orderCustomer.Phone = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.Email:
                    order.OrderCustomer.Email = action.EditField.EditFieldValue;
                    orderCustomer.EMail = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.Country:
                    order.OrderCustomer.Country = action.EditField.EditFieldValue;
                    orderContact.Country = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.Region:
                    order.OrderCustomer.Region = action.EditField.EditFieldValue;
                    orderContact.Region = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.City:
                    order.OrderCustomer.City = action.EditField.EditFieldValue;
                    orderContact.City = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.Organization:
                    order.OrderCustomer.Organization = action.EditField.EditFieldValue;
                    orderCustomer.Organization = action.EditField.EditFieldValue;
                    break;
                case EOrderFieldType.CustomerGroup:
                    var groupId = action.EditField.EditFieldValue.TryParseInt(true);
                    var group = groupId.HasValue ? CustomerGroupService.GetCustomerGroup(groupId.Value) : null;
                    orderCustomer.CustomerGroupId = @group != null ? @group.CustomerGroupId : orderCustomer.CustomerGroupId;
                    break;
                case EOrderFieldType.CustomerType:
                    var customerType = (CustomerType)action.EditField.EditFieldValue.TryParseInt(true);
                    order.OrderCustomer.CustomerType = customerType;
                    orderCustomer.CustomerType = customerType;
                    break;
                case EOrderFieldType.OrderSource:
                    var orderSourceId = action.EditField.EditFieldValue.TryParseInt(true);
                    var orderSource = orderSourceId.HasValue ? OrderSourceService.GetOrderSource(orderSourceId.Value) : null;
                    order.OrderSourceId = orderSource != null ? orderSource.Id : order.OrderSourceId;
                    break;
                case EOrderFieldType.OrderStatus:
                    var orderStatusId = action.EditField.EditFieldValue.TryParseInt(true);
                    var orderStatus = orderStatusId.HasValue ? OrderStatusService.GetOrderStatus(orderStatusId.Value) : null;
                    order.OrderStatusId = orderStatus != null ? orderStatus.StatusID : order.OrderStatusId;
                    order.OrderStatus = orderStatus ?? order.OrderStatus;

                    OrderStatusService.ChangeOrderStatus(order.OrderID, order.OrderStatusId, "Триггер " + trigger.Name, false);
                    break;
                case EOrderFieldType.IsPaid:
                    var pay = action.EditField.EditFieldValue.TryParseBool();
                    OrderService.PayOrder(order.OrderID, pay, changedBy: new OrderChangedBy("Триггер " + trigger.Name));
                    break;

                case EOrderFieldType.PaymentMethod:
                    var paymentId = action.EditField.EditFieldValue.TryParseInt(true);
                    var payment = paymentId.HasValue ? PaymentService.GetPaymentMethod(paymentId.Value) : null;
                    order.PaymentMethodId = payment != null ? payment.PaymentMethodId : order.PaymentMethodId;
                    order.ArchivedPaymentName = payment != null ? payment.Name : order.ArchivedPaymentName;
                    break;
                case EOrderFieldType.ShippingMethod:
                    var shippingId = action.EditField.EditFieldValue.TryParseInt(true);
                    var shipping = shippingId.HasValue ? ShippingMethodService.GetShippingMethod(shippingId.Value) : null;
                    order.ShippingMethodId = shipping != null ? shipping.ShippingMethodId : order.ShippingMethodId;
                    order.ArchivedShippingName = shipping != null ? shipping.Name : order.ArchivedShippingName;
                    break;
                case EOrderFieldType.CustomerField:
                    if (action.EditField.ObjId.HasValue)
                    {
                        CustomerFieldService.AddUpdateMap(orderCustomer.Id, action.EditField.ObjId.Value,
                            action.EditField.EditFieldValue ?? "", true);
                    }
                    break;
                case EOrderFieldType.Manager:
                    var managerId = action.EditField.EditFieldValue.TryParseInt(true);
                    var manager = managerId.HasValue ? ManagerService.GetManager(managerId.Value) : null;
                    order.ManagerId = manager != null ? manager.ManagerId : order.ManagerId;
                    break;
                case EOrderFieldType.CustomerManager:
                    var customerManagerId = action.EditField.EditFieldValue.TryParseInt(true);
                    var customerManager = customerManagerId.HasValue ? ManagerService.GetManager(customerManagerId.Value) : null;
                    orderCustomer.ManagerId = customerManager != null ? customerManager.ManagerId : orderCustomer.ManagerId;
                    break;

                case EOrderFieldType.UseIn1C:
                    order.UseIn1C = action.EditField.EditFieldValue.TryParseBool();
                    break;
                case EOrderFieldType.BonusAccount:
                    if (!BonusSystem.IsActive)
                        break;
                    var bonusMultiple = 0;
                    if (action.EditField.AddBonusesByItemComparers is true && trigger.Filter is OrderFilter filter)
                    {
                        foreach (var comparer in filter.Comparers)
                        {
                            if (comparer.CompareType != BizObjectFieldCompareType.Equal)
                                continue;
                            if (comparer.FieldComparer.Type == EFieldComparerType.Categories && comparer.FieldComparer is FieldsCategoriesComparer categoriesComparer)
                            {
                                bonusMultiple += order.OrderItems
                                                    .Where(item => item.ProductID.HasValue
                                                                    && ProductService.GetProduct(item.ProductID.Value).Multiplicity == 1
                                                                    && categoriesComparer.Categories.Any(category => ProductService.GetCategoriesIDsByProductId(item.ProductID.Value, false).Contains(category.Id)))
                                                    .Sum(x => (int)Math.Ceiling(x.Amount));
                            }
                            else if (comparer.FieldComparer.Type == EFieldComparerType.Products && comparer.FieldComparer is FieldsProductsComparer productsComparer)
                            {
                                bonusMultiple += order.OrderItems
                                                    .Where(item => item.ProductID.HasValue
                                                                    && ProductService.GetProduct(item.ProductID.Value).Multiplicity == 1
                                                                    && productsComparer.Products.Any(product => product.Id == item.ProductID.Value))
                                                    .Sum(x => (int)Math.Ceiling(x.Amount));
                            }
                        }
                        if (bonusMultiple == 0)
                            break;
                    }
                    else
                    {
                        bonusMultiple = 1;
                    }
                    var sum = action.EditField.EditFieldValue.TryParseInt() * action.EditField.ObjValue.TryParseInt(1) * bonusMultiple;
                    var operation = sum < 0 ? EOperationType.SubtractMainBonus : EOperationType.AddMainBonus;
                    if (orderCustomer.BonusCardNumber.HasValue)
                    {
                        var card = BonusSystemService.GetCard(orderCustomer.BonusCardNumber);
                        card.BonusAmount += sum;
                        CardService.Update(card);
                        var transLog = Transaction.Factory(orderCustomer.Id, Math.Abs(sum), $"{trigger.EventType.DescriptionKey()} - {trigger.Name}", operation, card.BonusAmount);
                        TransactionService.Create(transLog);
                    }
                    else
                    {
                        orderCustomer.BonusCardNumber = BonusSystemService.AddCard(new Card { CardId = orderCustomer.Id, BonusAmount = sum });
                        var transLog = Transaction.Factory(orderCustomer.Id, Math.Abs(sum), $"{trigger.EventType.DescriptionKey()} - {trigger.Name}", operation, sum);
                        TransactionService.Create(transLog);
                    }
                    break;
            }
        }

        private static void EditFieldLead(TriggerAction action, ELeadFieldType leadField, Lead lead, Customer leadCustomer,
            CustomerContact leadContact, TriggerRule trigger)
        {
            switch (leadField)
            {
                case ELeadFieldType.LastName:
                    lead.LastName = action.EditField.EditFieldValue;
                    leadCustomer.LastName = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.FirstName:
                    lead.FirstName = action.EditField.EditFieldValue;
                    leadCustomer.FirstName = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Patronymic:
                    lead.Patronymic = action.EditField.EditFieldValue;
                    leadCustomer.Patronymic = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Phone:
                    lead.Phone = action.EditField.EditFieldValue;
                    leadCustomer.Phone = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Email:
                    lead.Email = action.EditField.EditFieldValue;
                    leadCustomer.EMail = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Country:
                    lead.Country = action.EditField.EditFieldValue;
                    leadContact.Country = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Region:
                    lead.Region = action.EditField.EditFieldValue;
                    leadContact.Region = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.City:
                    lead.City = action.EditField.EditFieldValue;
                    leadContact.City = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.CustomerGroup:
                    var groupId = action.EditField.EditFieldValue.TryParseInt(true);
                    var group = groupId.HasValue ? CustomerGroupService.GetCustomerGroup(groupId.Value) : null;
                    leadCustomer.CustomerGroupId = @group != null ? @group.CustomerGroupId : leadCustomer.CustomerGroupId;
                    break;
                case ELeadFieldType.SalesFunnel:
                    var funnelId = action.EditField.EditFieldValue.TryParseInt(true);
                    var funnel = funnelId.HasValue ? Core.Services.Crm.SalesFunnels.SalesFunnelService.Get(funnelId.Value) : null;

                    if (funnel != null)
                    {
                        DealStatus status = null;
                        var statuses = DealStatusService.GetList(funnel.Id).OrderBy(x => x.SortOrder);

                        if (action.EditField.DealStatusId.HasValue)
                        {
                            status = statuses.FirstOrDefault(x => x.Id == action.EditField.DealStatusId.Value);
                        }
                        else
                        {
                            status = statuses.FirstOrDefault(x => x.Status != SalesFunnelStatusType.Canceled
                                                               && x.Status != SalesFunnelStatusType.FinalSuccess)
                                  ?? statuses.FirstOrDefault(x => x.Status != SalesFunnelStatusType.Canceled)
                                  ?? statuses.FirstOrDefault();
                        }

                        if (status != null)
                        {
                            lead.SalesFunnelId = funnel.Id;
                            lead.DealStatusId = status.Id;
                        }
                    }
                    break;
                case ELeadFieldType.Source:
                    var orderSourceId = action.EditField.EditFieldValue.TryParseInt(true);
                    var orderSource = orderSourceId.HasValue ? OrderSourceService.GetOrderSource(orderSourceId.Value) : null;
                    lead.OrderSourceId = orderSource != null ? orderSource.Id : lead.OrderSourceId;
                    break;
                case ELeadFieldType.Organization:
                    lead.Organization = action.EditField.EditFieldValue;
                    leadCustomer.Organization = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Title:
                    lead.Title = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.Description:
                    lead.Description = action.EditField.EditFieldValue;
                    break;
                case ELeadFieldType.CustomerField:
                    if (action.EditField.ObjId.HasValue)
                    {
                        CustomerFieldService.AddUpdateMap(leadCustomer.Id, action.EditField.ObjId.Value,
                            action.EditField.EditFieldValue ?? "", true);
                    }
                    break;
                case ELeadFieldType.Manager:
                    var managerId = action.EditField.EditFieldValue.TryParseInt(true);
                    var manager = managerId.HasValue ? ManagerService.GetManager(managerId.Value) : null;
                    lead.ManagerId = manager != null ? manager.ManagerId : lead.ManagerId;
                    break;
                case ELeadFieldType.CustomerManager:
                    var customerManagerId = action.EditField.EditFieldValue.TryParseInt(true);
                    var customerManager = customerManagerId.HasValue ? ManagerService.GetManager(customerManagerId.Value) : null;
                    leadCustomer.ManagerId = customerManager != null ? customerManager.ManagerId : leadCustomer.ManagerId;
                    break;
                case ELeadFieldType.CustomerType:
                    var customerType = (CustomerType)action.EditField.EditFieldValue.TryParseInt(true);
                    lead.CustomerType = customerType;
                    leadCustomer.CustomerType = customerType;
                    break;
                case ELeadFieldType.BonusAccount:
                    if (!BonusSystem.IsActive)
                        break;
                    var bonusMultiple = 0;
                    if (action.EditField.AddBonusesByItemComparers is true && trigger.Filter is LeadFilter filter)
                    {
                        foreach (var comparer in filter.Comparers)
                        {
                            if (comparer.CompareType != BizObjectFieldCompareType.Equal)
                                continue;
                            if (comparer.FieldComparer.Type == EFieldComparerType.Categories && comparer.FieldComparer is FieldsCategoriesComparer categoriesComparer)
                            {
                                bonusMultiple += lead.LeadItems
                                                    .Where(item => item.ProductId.HasValue
                                                                    && ProductService.GetProduct(item.ProductId.Value).Multiplicity == 1
                                                                    && categoriesComparer.Categories.Any(category => ProductService.GetCategoriesIDsByProductId(item.ProductId.Value, false).Contains(category.Id)))
                                                    .Sum(x => (int)Math.Ceiling(x.Amount));
                            }
                            else if (comparer.FieldComparer.Type == EFieldComparerType.Products && comparer.FieldComparer is FieldsProductsComparer productsComparer)
                            {
                                bonusMultiple += lead.LeadItems
                                                    .Where(item => item.ProductId.HasValue
                                                                    && ProductService.GetProduct(item.ProductId.Value).Multiplicity == 1
                                                                    && productsComparer.Products.Any(product => product.Id == item.ProductId.Value))
                                                    .Sum(x => (int)Math.Ceiling(x.Amount));
                            }
                        }
                        if (bonusMultiple == 0)
                            break;
                    }
                    else
                    {
                        bonusMultiple = 1;
                    }
                    var sum = action.EditField.EditFieldValue.TryParseInt() * action.EditField.ObjValue.TryParseInt(1) * bonusMultiple;
                    var operation = sum < 0 ? EOperationType.SubtractMainBonus : EOperationType.AddMainBonus;
                    if (leadCustomer.BonusCardNumber.HasValue)
                    {
                        var card = BonusSystemService.GetCard(leadCustomer.BonusCardNumber);
                        card.BonusAmount += sum;
                        CardService.Update(card);
                        var transLog = Transaction.Factory(leadCustomer.Id, Math.Abs(sum), $"{trigger.EventType.DescriptionKey()} - {trigger.Name}", operation, card.BonusAmount);
                        TransactionService.Create(transLog);
                    }
                    else
                    {
                        leadCustomer.BonusCardNumber = BonusSystemService.AddCard(new Card { CardId = leadCustomer.Id, BonusAmount = sum });
                        var transLog = Transaction.Factory(leadCustomer.Id, Math.Abs(sum), $"{trigger.EventType.DescriptionKey()} - {trigger.Name}", operation, sum);
                        TransactionService.Create(transLog);
                    }
                    break;
            }
        }

        private static void EditFieldCustomer(TriggerAction action, ECustomerFieldType customerField, Customer customer,
            CustomerContact customerContact, TriggerRule trigger)
        {
            switch (customerField)
            {
                case ECustomerFieldType.LastName:
                    customer.LastName = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.FirstName:
                    customer.FirstName = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.Patronymic:
                    customer.Patronymic = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.Email:
                    customer.EMail = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.Phone:
                    customer.Phone = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.Country:
                    customerContact.Country = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.Region:
                    customerContact.Region = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.City:
                    customerContact.City = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.CustomerGroup:
                    var groupId = action.EditField.EditFieldValue.TryParseInt(true);
                    var group = groupId.HasValue ? CustomerGroupService.GetCustomerGroup(groupId.Value) : null;
                    customer.CustomerGroupId = @group != null ? @group.CustomerGroupId : customer.CustomerGroupId;
                    break;
                case ECustomerFieldType.CustomerField:
                    if (action.EditField.ObjId.HasValue)
                    {
                        CustomerFieldService.AddUpdateMap(customer.Id, action.EditField.ObjId.Value, action.EditField.EditFieldValue ?? "",
                            true);
                    }
                    break;
                case ECustomerFieldType.Organization:
                    customer.Organization = action.EditField.EditFieldValue;
                    break;
                case ECustomerFieldType.CustomerType:
                    customer.CustomerType = (CustomerType)action.EditField.EditFieldValue.TryParseInt(true);
                    break;
                case ECustomerFieldType.Manager:
                    var managerId = action.EditField.EditFieldValue.TryParseInt(true);
                    var manager = managerId.HasValue ? ManagerService.GetManager(managerId.Value) : null;
                    customer.ManagerId = manager != null ? manager.ManagerId : customer.ManagerId;
                    break;
                case ECustomerFieldType.BonusAccount:
                    if (!BonusSystem.IsActive)
                        break;
                    var sum = action.EditField.EditFieldValue.TryParseInt() * action.EditField.ObjValue.TryParseInt(1);
                    var operation = sum < 0 ? EOperationType.SubtractMainBonus : EOperationType.AddMainBonus;
                    if (customer.BonusCardNumber.HasValue)
                    {
                        var card = BonusSystemService.GetCard(customer.BonusCardNumber);
                        card.BonusAmount += sum;
                        CardService.Update(card);
                        var transLog = Transaction.Factory(customer.Id, Math.Abs(sum), $"{trigger.EventType.DescriptionKey()} - {trigger.Name}", operation, card.BonusAmount);
                        TransactionService.Create(transLog);
                    }
                    else
                    {
                        customer.BonusCardNumber = BonusSystemService.AddCard(new Card { CardId = customer.Id, BonusAmount = sum });
                        var transLog = Transaction.Factory(customer.Id, Math.Abs(sum), $"{trigger.EventType.DescriptionKey()} - {trigger.Name}", operation, sum);
                        TransactionService.Create(transLog);
                    }
                    break;
            }
        }

        public static List<TriggerRule> GetTriggersByType(ETriggerEventType eventType, int categoryId)
        {
            return SQLDataAccess.ExecuteReadList("SELECT * FROM CRM.TriggerRule WHERE EventType = @EventType and CategoryId = @categoryId",
                CommandType.Text, GetTriggerRuleFromReaderByType,
                new SqlParameter("@EventType", (int)eventType), new SqlParameter("@categoryId", categoryId));
        }

        private static TriggerRule GetTriggerRuleFromReaderByType(SqlDataReader reader)
        {
            var eventType = (ETriggerEventType)SQLDataHelper.GetInt(reader, "EventType");
            switch (eventType)
            {
                case ETriggerEventType.OrderCreated:
                    return GetTriggerRuleFromReader<OrderCreatedTriggerRuleV8>(reader);

                case ETriggerEventType.OrderStatusChanged:
                    return GetTriggerRuleFromReader<OrderStatusChangedTriggerRuleV8>(reader);

                case ETriggerEventType.OrderPaied:
                    return GetTriggerRuleFromReader<OrderPayTriggerRuleV8>(reader);

                //case ETriggerEventType.LeadCreated:
                //    return GetTriggerRuleFromReader<LeadCreatedTriggerRule>(reader);

                //case ETriggerEventType.LeadStatusChanged:
                //    return GetTriggerRuleFromReader<LeadStatusChangedTriggerRule>(reader);

                //case ETriggerEventType.CustomerCreated:
                //    return GetTriggerRuleFromReader<CustomerCreatedTriggerRule>(reader);

                //case ETriggerEventType.TimeFromLastOrder:
                //    return GetTriggerRuleFromReader<TimeFromLastOrderTriggerRule>(reader);

                //case ETriggerEventType.SignificantDate:
                //    return GetTriggerRuleFromReader<SignificantDateTriggerRule>(reader);

                //case ETriggerEventType.SignificantCustomerDate:
                //    return GetTriggerRuleFromReader<SignificantCustomerDateTriggerRule>(reader);

                //case ETriggerEventType.InstallMobileApp:
                //    return GetTriggerRuleFromReader<InstallMobileAppTriggerRule>(reader);

                default:
                    throw new NotImplementedException();
            }
        }

        private static T GetTriggerRuleFromReader<T>(SqlDataReader reader) where T : TriggerRule, new()
        {
            var rule = new T
            {
                Id = SQLDataHelper.GetInt(reader, "Id"),
                EventObjId = SQLDataHelper.GetNullableInt(reader, "EventObjId"),
                EventObjValue = SQLDataHelper.GetNullableInt(reader, "EventObjValue"),
                CategoryId = SQLDataHelper.GetNullableInt(reader, "CategoryId"),
                Name = SQLDataHelper.GetString(reader, "Name"),
                Enabled = SQLDataHelper.GetBoolean(reader, "Enabled"),
                WorksOnlyOnce = SQLDataHelper.GetBoolean(reader, "WorksOnlyOnce"),
                DateCreated = SQLDataHelper.GetDateTime(reader, "DateCreated"),
                DateModified = SQLDataHelper.GetDateTime(reader, "DateModified"),
                PreferredHour = SQLDataHelper.GetNullableInt(reader, "PreferredHour"),

                Filter = TriggerRuleService.GetTriggerFilterFromJson<T>(SQLDataHelper.GetString(reader, "Filter"))
            };

            var triggerParams = SQLDataHelper.GetString(reader, "TriggerParams");

            if (!string.IsNullOrEmpty(triggerParams))
                rule.TriggerParams = TriggerRuleService.GetTriggerParams(triggerParams, rule.EventType);

            return rule;
        }

    }
}
