using System;
using System.Web.Mvc;
using System.Linq;
using System.Text;
using System.Web;
//using System.Collections.Generic;
using AdvantShop.Core.Services.Helpers;
using AdvantShop.Core.Services.Smses;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using AdvantShop.Module.SmsConfirmation.Service;
using AdvantShop.Module.SmsConfirmation.Models;
using AdvantShop.Module.SmsConfirmation.Handlers;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Security;
using AdvantShop.Security.OAuth;
using BotDetect.Web.Mvc;

namespace AdvantShop.Module.SmsConfirmation.Controllers
{
    [Module(Type = "SmsConfirmation")]
    public class SmsConfirmationClientController : ModuleController
    {
        string ModuleID = SmsConfirmation.ModuleStringId;

        public ActionResult RenderSmsConfirmation()
        {
            var customer = CustomerContext.CurrentCustomer;
            if (customer.CustomerRole != Role.Guest)
                return new EmptyResult();

            SmsConfirmationService.DeleteSmsConfirmationCodes(customer.Id);

            var activeSmsModule = SmsNotifier.GetActiveSmsModule();
            if (activeSmsModule == null)
                return new EmptyResult();

            var controllerName = Request.RequestContext.RouteData.Values["controller"].ToString().ToLower();
            var actionName = Request.RequestContext.RouteData.Values["action"].ToString().ToLower();

            var pageType = "login"; // Все страницы. Отображается кнопка "Войти по SMS"

            if(((controllerName == "checkoutmobile" && actionName == "index") || controllerName == "checkout" || controllerName == "cart") && SmsConfirmationSettings.CheckoutPageActive)
            {
                pageType = "checkout";
            }

            if(controllerName == "user" && actionName == "registration" && SmsConfirmationSettings.RegistrationPageActive)
            {
                pageType = "registration";
            }

            if (controllerName == "preorder" && actionName == "index" && SmsConfirmationSettings.RegistrationPageActive)//GlorySoft_009
            {
                pageType = "registration";
            }

            return PartialView("~/modules/" + ModuleID + "/Views/Client/_SmsConfirmation.cshtml", pageType);
        }

        [HttpGet]
        public JsonResult GetFormSettings(string pageToRedirect,/*GlorySoft_018*/ string pageType)
        {
            if (string.IsNullOrEmpty(pageToRedirect))
                pageToRedirect = "login";
            
            var settings = new SettingsModel
            {
                FormTitle = SmsConfirmationSettings.FormTitle,
                FormContent = SmsConfirmationSettings.FormContent,
                UseCaptcha = SmsConfirmationSettings.UseCaptcha
            };

            if (pageType == "registration" || pageType == "checkout")//GlorySoft_018
                settings.FormTitle = "Подтверждение телефона";

            string socialLinks = null;

            if (Configuration.SettingsOAuth.GoogleActive || Configuration.SettingsOAuth.YandexActive || Configuration.SettingsOAuth.FacebookActive ||
                Configuration.SettingsOAuth.VkontakteActive || Configuration.SettingsOAuth.MailActive || Configuration.SettingsOAuth.OdnoklassnikiActive)
            {
                var sb = new StringBuilder();
                if (Configuration.SettingsOAuth.GoogleActive)
                {
                    sb.AppendFormat(
                        "<a href=\"{0}\"><img src=\"modules/SmsConfirmation/content/images/gl.png\" alt=\"Google\" title=\"Google\" style=\"padding-right: 5px;\" /></a>",
                        GoogleOAuth.OpenDialog(pageToRedirect)
                    );
                }

                if (Configuration.SettingsOAuth.YandexActive)
                {
                    sb.AppendFormat(
                        "<a href=\"{0}\"><img src=\"modules/SmsConfirmation/content/images/ya.png\" alt=\"Yandex\" title=\"Yandex\" style=\"padding-right: 5px;\" /></a>",
                        YandexOAuth.OpenDialog(pageToRedirect)
                    );
                }

                if (Configuration.SettingsOAuth.FacebookActive)
                {
                    sb.AppendFormat(
                        "<a href=\"{0}\"><img src=\"modules/SmsConfirmation/content/images/fb.png\" alt=\"Facebook\" title=\"Facebook\" style=\"padding-right: 5px;\" /></a>",
                        FacebookOAuth.OpenDialog(pageToRedirect)
                    );
                }

                if (Configuration.SettingsOAuth.VkontakteActive)
                {
                    sb.AppendFormat(
                        "<a href=\"{0}\"><img src=\"modules/SmsConfirmation/content/images/vk.png\" alt=\"Вконтакте\" title=\"Вконтакте\" style=\"padding-right: 5px;\" /></a>",
                        VkOAuth.OpenDialog(pageToRedirect)
                    );
                }

                if (Configuration.SettingsOAuth.MailActive)
                {
                    sb.AppendFormat(
                        "<a href=\"{0}\"><img src=\"modules/SmsConfirmation/content/images/ml.png\" alt=\"Mail.ru\" title=\"Mail.ru\" style=\"padding-right: 5px;\" /></a>",
                        MailOAuth.OpenDialog(pageToRedirect)
                    );
                }

                if (Configuration.SettingsOAuth.OdnoklassnikiActive)
                {
                    sb.AppendFormat(
                        "<a href=\"{0}\"><img src=\"modules/SmsConfirmation/content/images/od.png\" alt=\"Одноклассники\" title=\"Одноклассники\" /></a>",
                        MailOAuth.OpenDialog(pageToRedirect)
                    );
                }

                socialLinks = sb.ToString();
            }

            return Json(new { settings, socialLinks });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SendSmsCode(string phone, string pageType, string captchaCode, string captchaSource)
        {
            if (string.IsNullOrEmpty(phone) || phone.Contains('_'))
                return JsonError("Пустой номер телефона");

            var standardPhone = SmsConfirmationService/*GlorySoft_004 Helpers.StringHelper*/.ConvertToStandardPhone(phone);
            if (!standardPhone.HasValue)
                return JsonError("Введите корректный номер телефона");

            //GlorySoft_004
            var exist = SmsConfirmationService.GetCustomersByPhone(phone, null, null).Count > 0;
            var enabled = SmsConfirmationService.GetCustomersByPhone(phone, true, null).Count > 0;
            var confirmed = SmsConfirmationService.GetCustomersByPhone(phone, true, true).Count > 0;
            if ((pageType == "registration" || pageType == "checkout") && confirmed)
                return JsonError("Указанный номер телефона уже используется");
            else if ((pageType == "login") && !exist)
                return JsonError(T("SmsConfirmation.Alert.NotFound"));
            else if ((pageType == "login") && !enabled)
                return JsonError(T("SmsConfirmation.Alert.EmailNotConfirmed"));
            else if ((pageType == "login") && !confirmed)
                return JsonError(T("SmsConfirmation.Alert.PhoneNotConfirmed"));

            var activeSmsModule = SmsNotifier.GetActiveSmsModule();
            if (activeSmsModule == null)
                return JsonError("Модуль для отправки SMS не установлен");

            if (pageType == "login" 
                && SmsConfirmationSettings.UseCaptcha 
                && !MvcCaptcha.Validate("CaptchaSourceCallback", captchaCode, captchaSource))
            {
                return Json(new { result = false, msg = "Неверно введен код", captchaError = true });
            }

            try
            {
                var ePageType = ESmsConfirmationPageType.Login;
                switch (pageType)
                {
                    case "login":
                        ePageType = ESmsConfirmationPageType.Login;
                        break;
                    case "registration":
                        ePageType = ESmsConfirmationPageType.Registration;
                        break;
                    case "checkout":
                        ePageType = ESmsConfirmationPageType.Checkout;
                        break;
                }

                var code = SmsConfirmationService.SendSmsCode(phone, standardPhone.Value, (byte)ePageType);
                
                return Json(new { result = true } );
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
            
            return Json(false);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ConfirmSmsCode(string phone, string smsCode, string pageType)
        {
            try
            {
                var customerId = CustomerContext.CustomerId;

                var ePageType = ESmsConfirmationPageType.Login;
                switch (pageType)
                {
                    case "login":
                        ePageType = ESmsConfirmationPageType.Login;
                        break;
                    case "registration":
                        ePageType = ESmsConfirmationPageType.Registration;
                        break;
                    case "checkout":
                        ePageType = ESmsConfirmationPageType.Checkout;
                        break;
                }

                var smsConfirmationCode = SmsConfirmationService.GetFullSmsConfirmationCode(customerId, phone, (byte)ePageType);

                if (smsConfirmationCode == null)
                    return JsonError("Неверный код подтверждения");

                // срок проверяем до самого кода: просроченный код не должен пускать даже при верных цифрах
                if (SmsConfirmationService.IsSmsCodeExpired(smsConfirmationCode))
                {
                    SmsConfirmationService.DeleteSmsConfirmationCode(customerId, phone, (byte)ePageType);

                    return JsonResetToPhone("Срок действия кода истёк. Запросите код заново");
                }

                if (smsCode.Trim() == smsConfirmationCode.SmsCode)
                {
                    SmsConfirmationService.DeleteSmsConfirmationCode(customerId, phone, (byte)ePageType);

                    if (pageType != "login")
                        return JsonOk();

                    var customersByPhone = SmsConfirmationService/*GlorySoft_004 CustomerService*/.GetCustomersByPhone(phone, /*GlorySoft_004*/null, null);
                    var customer = customersByPhone.FirstOrDefault(x => !string.IsNullOrEmpty(x.EMail)) 
                                   ?? customersByPhone.FirstOrDefault();

                    if (customer != null)
                    {
                        if (string.IsNullOrEmpty(customer.EMail))
                        {
                            customer.EMail = SmsConfirmationService.GetEmailByPhone(phone);
                            SmsConfirmationService.UpdateCustomerEmailByCustomerId(customer.Id, customer.EMail);
                        }

                        if (!AuthorizeService.SignIn(customer.EMail, customer.Password, true, true))/*GlorySoft_004*/
                            return JsonError("Ошибка при авторизации");//GlorySoft_004
                    }
                    else
                    {
                        smsConfirmationCode.CustomerId = CustomerContext.CustomerId;
                        
                        new SmsConfirmationRegistrationHandler().Register(smsConfirmationCode);
                        
                        TempData["IsRegisteredNow"] = "true";
                    }

                    return JsonOk();
                }

                var attempts = SmsConfirmationService.IncrementSmsConfirmationCodeAttempts(customerId, phone, (byte)ePageType);

                if (attempts >= SmsConfirmationService.MaxConfirmAttempts)
                {
                    SmsConfirmationService.DeleteSmsConfirmationCode(customerId, phone, (byte)ePageType);

                    Debug.Log.Warn(string.Format(
                        "SmsConfirmation: превышено количество попыток ввода кода подтверждения. Ip: {0}, телефон: {1}",
                        HttpContext.TryGetIp() ?? "NO_DATA", phone));

                    return JsonResetToPhone("Превышено количество попыток ввода кода. Запросите код заново");
                }

                return JsonError("Неверный код подтверждения");
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                return JsonError();
            }
        }

        /// <summary>
        /// Код больше не действителен: клиент по флагу возвращается к вводу номера телефона и показывает причину.
        /// </summary>
        private JsonResult JsonResetToPhone(string error)
        {
            return Json(new { result = false, errors = new[] { error }, resetToPhone = true });
        }

        #region my account tab
        public ActionResult MyAccountChangeEmail()
        {
            var email = CustomerContext.CurrentCustomer != null ? CustomerContext.CurrentCustomer.EMail : string.Empty;
            return PartialView("~/modules/" + ModuleID + "/Views/Client/_MyAccountChangeEmail.cshtml", email);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ChangeEmail(string email)
        {
            try
            {
                var customer = CustomerContext.CurrentCustomer;
                
                if (!customer.RegistredUser)
                    return JsonError("Пользователь не авторизован");
                
                if (string.IsNullOrEmpty(email))
                    return JsonError("Email не может быть пустым");
                
                if (email.Contains(SmsConfirmationService._emailDomain))
                    return JsonError("Введите существующий адрес Email");

                if (SmsConfirmationService.IsExistsCustomerEmail(email))
                    return JsonError("Пользователь с таким Email уже существует");
                
                AuthorizeService.SignOut();

                customer.EMail = email;

                SmsConfirmationService.UpdateCustomerEmailByCustomerId(customer.Id, email);
                AuthorizeService.SignIn(customer.EMail, customer.Password, true, true);

                return JsonOk();
            }
            catch(Exception ex)
            {
                Diagnostics.Debug.Log.Error(ex);
                return JsonError();
            }
        }

        #endregion
    }
}
