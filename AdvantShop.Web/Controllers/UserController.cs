using AdvantShop.Configuration;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Customers;
using AdvantShop.Handlers.User;
using AdvantShop.Helpers;
using AdvantShop.Models.User;
using AdvantShop.Security;
using AdvantShop.Security.OAuth;
using AdvantShop.ViewModel.User;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Extensions;
using AdvantShop.Web.Infrastructure.Filters;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using AdvantShop.Core;
using AdvantShop.Core.Services.Auth;
using AdvantShop.Core.Services.Configuration.Settings.Enums;
using AdvantShop.Core.Services.Customers;
using AdvantShop.Core.Services.Diagnostics;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using AdvantShop.Extensions;
using AdvantShop.Core.Services.Mails;

namespace AdvantShop.Controllers
{
    public sealed class UserController : BaseClientController
    {
        /// <summary>
        /// Эндпоинт для формирования дальнейших действий при авторизации. Необходимо для безопасности, чтобы явно
        /// не отвечать существует ли в системе пользователь с определенными данными.<br/><br/>
        /// Поддерживается только для входа через электронную почту или номер телефона.<br/><br/>
        /// Автоматически отправляет коды подтверждения.<br/><br/>
        /// </summary>
        /// <param name="model">Модель, которая содержит:<br/>
        /// Data — данные для обработки авторизации (номер телефона или адрес электронной почты).<br/>
        /// InputValue, CaptchaId, CaptchaInstanceId — данные капчи.</param>
        /// <returns>Стандартный CommandResult, где obj это строка состоящая из двух символов.<br/><br/>
        /// Первый символ обозначает, существует ли пользователь или нет и принимает значения:<br/>
        /// "y" — существует.<br/>
        /// "n" — не существует.<br/><br/>
        /// Второй символ обозначает текущий тип авторизации:<br/>
        /// "c" — подтверждение через код.<br/>
        /// "p" — подтверждение через пароль (доступно только для входа через email).</returns>
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AuthorizationData(AuthorizationDataModel model) =>
            ProcessJsonResult(new AuthorizationDataHandler(model));

        [HttpGet]
        public JsonResult IsNeedShowAuthCaptcha() =>
            JsonOk(AuthCaptchaHandler.NeedValidate());
        
        [HttpGet]
        public JsonResult IsNeedShowSendCodeCaptcha() => 
            JsonOk(SettingsMain.EnableCaptchaInSendCode || AuthCaptchaHandler.NeedValidate());
        
        #region Email Auth

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult EmailLogin(EmailLoginModel model) => 
            ProcessJsonResult(new EmailLoginHandler(model, Session));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SendEmailCode(SendEmailCodeModel model) =>
            ProcessJsonResult(new SendEmailCodeHandler(model));
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ConfirmEmailCode(ConfirmEmailCodeModel model) =>
            ProcessJsonResult(new ConfirmEmailCodeHandler(model));

        #endregion

        #region Code Auth
        
        [HttpGet]
        public JsonResult GetLoginCodeSettings() =>
            JsonOk(new GetLoginCodeSettingsHandler().Execute());

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SendCode(SendCodeModel model) => 
            ProcessJsonResult(new SendCodeHandler(model));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ConfirmCode(ConfirmCodeModel model) =>
            ProcessJsonResult(new ConfirmCodeHandler(model));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult IsPhoneConfirmed(string phone) =>
            ProcessJsonResult(new IsPhoneConfirmedHandler(phone));

        [HttpGet]
        public JsonResult InitCodeConfirmation() => 
            ProcessJsonResult(new InitCodeConfirmationHandler());

        #endregion
        
        #region Routes
        
        [HttpGet]
        public JsonResult GetAuthMethod() => 
            JsonOk(SettingsAuth.AuthMethod.ToString().ToLower());
        
        [HttpGet]
        public JsonResult GetAuthModuleId() => 
            JsonOk(SettingsAuth.DefaultAuthModuleId);

        [HttpGet]
        public JsonResult GetAuthRoutes() => 
            JsonOk(new AuthRoutesHandler().Execute());
        
        #endregion
        
        #region OAuth

        public ActionResult OpenId(string redirectTo) =>
            PartialView(new OpenIdHandler(redirectTo).Execute());

        public ActionResult LoginOpenId(string code, string state) =>
            Redirect(new LoginOpenIdHandler(code, state).Execute());

        public ActionResult LoginVk(string pageToRedirect) => 
            Redirect(VkOAuth.OpenDialog(pageToRedirect));

        public ActionResult LoginFacebook(string pageToRedirect) =>
            Redirect(FacebookOAuth.OpenDialog(pageToRedirect));

        public ActionResult LoginGoogle(string pageToRedirect) =>
            Redirect(GoogleOAuth.OpenDialog(pageToRedirect));

        public ActionResult LoginGoogleAnalytics(string pageToRedirect) =>
            Redirect(GoogleOAuth.OpenAnalyticsDialog(pageToRedirect));

        public ActionResult LoginOk(string pageToRedirect) => 
            Redirect(OkOAuth.OpenDialog(pageToRedirect));

        public ActionResult LoginMailRu(string pageToRedirect) => 
            Redirect(MailOAuth.OpenDialog(pageToRedirect));

        public ActionResult LoginYandex(string pageToRedirect) => 
            Redirect(YandexOAuth.OpenDialog(pageToRedirect));

        public ActionResult LoginOAuth(string provider, string pageToRedirect)
        {
            var returUrl = string.IsNullOrEmpty(pageToRedirect) ? this.Url.Action("LoginExternal", "User", null, this.Request.Url.Scheme) : pageToRedirect;
            return string.IsNullOrWhiteSpace(provider)
                ? Redirect(PassportAdvantService.GetLoginPage(returUrl, SettingsLic.LicKey))
                : Redirect(PassportAdvantService.GetLoginRequest(returUrl, provider, SettingsLic.LicKey));
        }

        public async Task<ActionResult> LoginExternal(string code, string state)
        {
            if (string.IsNullOrEmpty(state)) throw new Exception("state missing");
            var data = state.Split('|');
            if (data.Length != 3) throw new Exception("state missing");
            var provider = data[0];
            var pageToRedirect = data[1];
            var advClientId = data[2];

            pageToRedirect = pageToRedirect.TrimStart('/').ToLower();

            await PassportAdvantService.Login(code, state);

            return Redirect(pageToRedirect);
        }

        #endregion

        #region Registration

        [HttpGet]
        public JsonResult InitRegistration() =>
            JsonOk(new InitRegistrationHandler().Execute());
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult Registration(RegistrationModel model, ERegistrationMethod method) //=> GlorySoft_033
            //CustomerContext.CurrentCustomer.RegistredUser
            //    ? JsonOk()
            //    : ProcessJsonResult(new RegistrationHandler(model, method, TempData));
        {//GlorySoft_033
            if (CustomerContext.CurrentCustomer.RegistredUser)
                return JsonOk();

            var handler = new RegistrationHandler(model, method, TempData);

            var d = CustomerService.GetCustomerByEmail(model.Email);
            if (d != null && !d.Enabled)
            {
                handler.Update(d, model);
                return JsonOk(Url.RouteUrl("RegCodePhysicalEntity") + $"/{d.Id}");
            }

            try
            {
                handler.Execute();
                return JsonOk(Url.RouteUrl("RegCodePhysicalEntity") + $"/{CustomerContext.CustomerId}");
            }
            catch (BlException e)
            {
                ModelState.AddModelError(e.Property, e.Message);
                return JsonError();
            }
        }

        #endregion

        #region Recovery password

        public ActionResult RecoveryPassword(string email, string recoveryCode, int? lpId)
        {
            try
            {
                var model = new RecoveryPasswordHandler(email, recoveryCode, lpId).Execute();
                
                SetMetaInformation(T("User.ForgotPassword.PasswordRecovery"));
                SetNoFollowNoIndex();
                SetNgController(NgControllers.NgControllersTypes.RecoveryPasswordCtrl);
                
                return lpId != null 
                    ? View("~/Views/User/RecoveryPassword.cshtml", model) 
                    : View(model);
            }
            catch (BlException)
            {
                return RedirectToRoute("Home");
            }
        }
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SendRecoveryPassword(string email, int? lpId) =>
            ProcessJsonResult(new SendRecoveryPasswordHandler(email, lpId));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ChangePassword(string newPassword, string newPasswordConfirm, string email, string recoveryCode) => 
            ProcessJsonResult(new ChangePasswordHandler(newPassword, newPasswordConfirm, email, recoveryCode, Session));

        #endregion

        #region ClientCode

        [ChildActionOnly]
        public ActionResult ClientCode()
        {
            if (BrowsersHelper.IsBot())
                return new EmptyResult();
            
            if (!SettingsDesign.ShowClientId)
                // стили по ClientCode необходимо включить в CriticalCss
                if (!DebugMode.IsDebugMode(eDebugMode.CriticalCss)) 
                    return new EmptyResult();

            return PartialView(new ClientCodeViewModel());
        }

        public JsonResult GetClientCode()
        {
            if (BrowsersHelper.IsBot())
                return JsonOk();
            
            if (!SettingsDesign.ShowClientId)
                // стили по ClientCode необходимо включить в CriticalCss
                if (!DebugMode.IsDebugMode(eDebugMode.CriticalCss)) 
                    return JsonOk();
            
            var code = ClientCodeService.GetClientCode(CustomerContext.CustomerId);

            return JsonOk(new ClientCodeViewModel()
            {
                Code = code.ToString("##,##0").Replace(",", "-").Replace("\u00A0", "-")
            });
        }

        #endregion

        #region Phone confirmation

        [HttpGet]
        public JsonResult InitPhoneConfirmation() =>
            JsonOk(new InitPhoneConfirmationHandler().Execute());

        #endregion

        public ActionResult Login(string from, string state, string code)
        {
            if (!string.IsNullOrWhiteSpace(state))
            {
                if (state.Contains("googleanalytics"))
                {
                    GoogleOAuth.LoginAnalytics(code, "login");
                    return RedirectToRoute("Home", new { state = "googleanalytics" });
                }
                
                return RedirectToRoute("LoginOpenId", new { state, code });
            }

            if (CustomerContext.CurrentCustomer.CustomerRole != Role.Guest)
            {
                if (string.IsNullOrWhiteSpace(from))
                    return RedirectToRoute("Home");

                return Redirect(from);
            }

            if (string.IsNullOrWhiteSpace(from))
                from = Url.RouteUrl("Home");
            
            SetMetaInformation(T("User.Login.Header"));
            return View("Login", "_UserLayout", from);
        }
        
        [TechDomainGuard(Disable = true)]
        public ActionResult LoginToken(string email, string hash, string redirectTo, bool? showhelp)
        {
            SettingsLic.ShowAdvantshopJivoSiteForm = showhelp ?? false;
            var customer = CustomerService.GetCustomerByEmail(email);
            if (customer != null)
            {
                var hashComputed = SecurityHelper.EncodeWithHmac(customer.EMail, customer.Password);
                if (hash == hashComputed && AuthorizeService.SignIn(customer.EMail, customer.Password, true, true))
                {
                    var domain = UrlService.GetAbsoluteBaseLink();
                    
                    if (!string.IsNullOrEmpty(redirectTo) && redirectTo != "/")
                        return Redirect(
                            Uri.IsWellFormedUriString(redirectTo, UriKind.Absolute)
                                ? redirectTo
                                : Url.AbsoluteActionUrl("RedirectWithAuth", "Account", new
                                    {
                                        area = "AdminV2",
                                        domain = domain,
                                        path = redirectTo,
                                    })
                        );

                    if (!string.IsNullOrEmpty(SettingsMain.AdminHomeForceRedirectUrl))
                        return Redirect(
                            Uri.IsWellFormedUriString(SettingsMain.AdminHomeForceRedirectUrl, UriKind.Absolute)
                                ? SettingsMain.AdminHomeForceRedirectUrl
                                : Url.AbsoluteActionUrl("RedirectWithAuth", "Acc", new
                                {
                                    area = "AdminV2",
                                    domain = domain,
                                    path = SettingsMain.AdminHomeForceRedirectUrl,
                                })
                        );

                    if (string.IsNullOrEmpty(redirectTo))
                        return Redirect(Url.AbsoluteActionUrl("RedirectWithAuth", "Account", new
                        {
                            area = "AdminV2",
                            domain = domain,
                            path = "/adminv2",
                        }));
                }
            }

            return Redirect("~/");
        }
        
        [AdminMobileAppGuard(Disable = true)]
        public ActionResult Logout()
        {
            AuthorizeService.SignOut();

            if (MobileHelper.IsMobileAdminApp())
            {
                CustomerAdminPushNotificationService.UpdateFcmToken(CustomerContext.CustomerId, null);
                RedirectToAction("Login", "Account", new { area = "AdminV2" });
            }

            if (Request.GetUrlReferrer() != null)
            {
                var referrer = Request.GetUrlReferrer().ToString();

                if (!string.IsNullOrEmpty(referrer) && !(referrer.Contains("admin") 
                                                         || referrer.Contains("checkout") 
                                                         || referrer.Contains("advantshop.net")))
                    return Redirect(referrer);
            }

            return RedirectToRoute("Home");
        }
        
        public ActionResult RegistrationCustomerFields(
            string ngModelName,
            string cssParamName,
            string cssParamValue,
            bool checkFields,
            string ngVariableVisible
        ) => PartialView(
            "_CustomerFields", 
            new RegistrationCustomerFieldsHandler(
                ngModelName,
                cssParamName,
                cssParamValue,
                checkFields,
                ngVariableVisible
             ).Execute());

        [ChildActionOnly]
        public ActionResult CloseTrigger()
        {
            if (SettingsMain.StoreAccessMode != EStoreAccessMode.All)
                return new EmptyResult();
            
            var from = Request.QueryString["from"];

            if (string.IsNullOrWhiteSpace(from))
                from = Url.RouteUrl("Home");
            
            if (from.Equals(Url.RouteUrl("Checkout"), StringComparison.OrdinalIgnoreCase))
                from = Url.RouteUrl("Cart");
                
            return PartialView("CloseTrigger", from);
        }

        [HttpGet]
        public JsonResult GetConfirmPhone(string customerId)//GlorySoft_026
        {
            var c = CustomerService.GetConfirmPhone(customerId);
            return Json(c);
        }

        public ActionResult AccountForAdmin(string hash)//GlorySoft_028
        {
            if (hash.IsNullOrEmpty())
                return RedirectToRoute("Home");

            var lk = new string[] { };
            try
            {
                lk = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(hash)).Split("&&");
                if (lk.Length != 2)
                    return RedirectToRoute("Home");
            }
            catch (Exception e)
            {
                Debug.Log.Error(e);
                return RedirectToRoute("Home");
            }


            if (!AuthorizeService.SignIn(lk[0], lk[1], true, false))
                return RedirectToRoute("Home");

            SetMetaInformation(T("User.Registration.Registration"));
            SetNoFollowNoIndex();
            //SetNgController(NgControllers.NgControllersTypes.RegistrationPageCtrl);

            return Redirect(Url.RouteUrl("MyAccount") + "#tab=commoninf");
            //return View("~/Modules/" + OneSApi.ModuleStringId + "/Views/Client/User/RegistrationSuccess.cshtml");
        }

        [HttpGet]
        public JsonResult GetCustomerFields(string customerId, bool onlyFilled)//GlorySoft_031
        {
            var customer = CustomerService.GetCustomer(Guid.Parse(customerId));
            var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id)
                .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
                            (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All)).ToList();
            if (onlyFilled)
                customerFields = customerFields.Where(x => x.Value.IsNotEmpty() || x.ValueDateFormat.IsNotEmpty()).ToList();

            return Json(customerFields);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> SendRequestCommonInfo(string customerId, string lastname, string firstname, string patronymic, string phone, string email, string birthday, List<CustomerFieldWithValue> CustomerFields)//GlorySoft_031
        {
            var guid = Guid.Parse(customerId);
            var errors = new List<string> { };
            string title = "Некорректные данные";

            //check phone
            if (string.IsNullOrEmpty(phone) || phone.Contains('_'))
                errors.Add("Пустой номер телефона");
            var standardPhone = StringHelper.ConvertToStandardPhone(phone);
            if (!standardPhone.HasValue)
                errors.Add("Введите корректный номер телефона");
            var exist = CustomerService.GetCustomerByPhone(phone, standardPhone, CustomerType.PhysicalEntity);
            if (exist != null && exist.Id != guid)
                errors.Add("Указанный номер телефона уже используется");

            var customer = CustomerService.GetCustomer(guid);

            //check email
            if (!ValidationHelper.IsValidEmail(email))
                errors.Add("Введите корректный email");
            if (!string.IsNullOrWhiteSpace(email) && CustomerService.IsEmailExist(email) && (exist != null && exist.Id != guid))
                errors.Add("Указанный email уже используется");
            if (SettingsCheckout.IsShowLastName && SettingsCheckout.IsRequiredLastName && String.IsNullOrWhiteSpace(lastname))
                errors.Add("Поле \"Фамилия\" обязательно");
            if (String.IsNullOrWhiteSpace(firstname))
                errors.Add("Поле \"Имя\" обязательно");
            if (SettingsCheckout.IsShowPatronymic && SettingsCheckout.IsRequiredPatronymic && String.IsNullOrWhiteSpace(patronymic))
                errors.Add("Поле \"Отчество\" обязательно");
            if (customer.CustomerType != CustomerType.LegalEntity && SettingsCheckout.IsShowBirthDay && SettingsCheckout.IsRequiredBirthDay && birthday == null)
                errors.Add("Поле \"День рождения\" обязательно");

            int? task = null;
            if (errors.Count == 0)
            {
                try
                {
                    var oldFields = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id)
                        .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
                                    (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All)).ToList();
                    var body = "";//<p>ТЕКУЩИЕ УЧЕТНЫЕ ДАННЫЕ:</p>";
                    if (customer.CustomerType == CustomerType.LegalEntity)
                    {
                        foreach (var field in CustomerFields)
                        {
                            var old = oldFields.FirstOrDefault(x => x.Id == field.Id);
                            body += string.Format("{0}: {1}\r\n", field.Name, old != null ? old.Value : "");
                        }
                    }
                    body += string.Format("Фамилия: {0}\r\n", customer.LastName);
                    body += string.Format("Имя: {0}\r\n", customer.FirstName);
                    body += string.Format("Отчество: {0}\r\n", customer.Patronymic);
                    body += string.Format("Телефон: {0}\r\n", customer.Phone);
                    body += string.Format("Email: {0}\r\n", customer.EMail);
                    if (customer.CustomerType != CustomerType.LegalEntity)
                        body += string.Format("День рождения: {0}\r\n", customer.BirthDay != null ? customer.BirthDay.Value.ToString("dd.MM.yyyy") : "");
                    if (customer.CustomerType != CustomerType.LegalEntity && CustomerFields != null)
                    {
                        foreach (var field in CustomerFields)
                        {
                            var old = oldFields.FirstOrDefault(x => x.Id == field.Id);
                            body += string.Format("{0}: {1}\r\n", field.Name, old != null ? old.Value : "");
                        }
                    }
                    //body += "<p>&nbsp;</p><p>НОВЫЕ УЧЕТНЫЕ ДАННЫЕ:</p>";
                    //if (customer.CustomerType == CustomerType.LegalEntity && CustomerFields != null)
                    //{
                    //    foreach (var field in CustomerFields)
                    //    {
                    //        var old = oldFields.FirstOrDefault(x => x.Id == field.Id);
                    //        if (old == null || old.Value == field.Value)
                    //            body += string.Format("<p>{0}: {1}</p>", field.Name, field.Value);
                    //        else
                    //            body += string.Format("<p>{0}: <strong>{1}</strong></p>", field.Name, field.Value);
                    //    }
                    //}
                    //if (customer.LastName == lastname)
                    //    body += string.Format("<p>Фамилия: {0}</p>", lastname);
                    //else
                    //    body += string.Format("<p>Фамилия: <strong>{0}</strong></p>", lastname);
                    //if (customer.FirstName == firstname)
                    //    body += string.Format("<p>Имя: {0}</p>", firstname);
                    //else
                    //    body += string.Format("<p>Имя: <strong>{0}</strong></p>", firstname);
                    //if (customer.Patronymic == patronymic)
                    //    body += string.Format("<p>Отчество: {0}</p>", patronymic);
                    //else
                    //    body += string.Format("<p>Отчество: <strong>{0}</strong></p>", patronymic);
                    //if (customer.Phone == phone)
                    //    body += string.Format("<p>Телефон: {0}</p>", phone);
                    //else
                    //    body += string.Format("<p>Телефон: <strong>{0}</strong></p>", phone);
                    //if (customer.EMail == email)
                    //    body += string.Format("<p>Email: {0}</p>", email);
                    //else
                    //    body += string.Format("<p>Email: <strong>{0}</strong></p>", email);
                    //if (customer.CustomerType != CustomerType.LegalEntity)
                    //{
                    //    if ((customer.BirthDay == null && birthday.IsNullOrEmpty()) || (customer.BirthDay != null && customer.BirthDay.Value.ToString("dd.MM.yyyy") == birthday))
                    //        body += string.Format("<p>День рождения: {0}</p>", birthday);
                    //    else
                    //        body += string.Format("<p>День рождения: <strong>{0}</strong></p>", birthday);
                    //}
                    var fields = new Dictionary<string, string>();
                    if (customer.LastName != lastname)
                        fields.Add("LastName", lastname);
                    if (customer.FirstName != firstname)
                        fields.Add("FirstName", firstname);
                    if (customer.Patronymic != patronymic)
                        fields.Add("Patronymic", patronymic);
                    if (customer.Phone != phone)
                        fields.Add("Phone", phone);
                    if (customer.EMail != email)
                        fields.Add("EMail", email);
                    if (customer.CustomerType != CustomerType.LegalEntity)
                    {
                        if (!((customer.BirthDay == null && birthday.IsNullOrEmpty()) || (customer.BirthDay != null && customer.BirthDay.Value.ToString("dd.MM.yyyy") == birthday)))
                            fields.Add("BirthDay", birthday);
                    }
                    if (customer.CustomerType != CustomerType.LegalEntity && CustomerFields != null)
                    {
                        foreach (var field in CustomerFields)
                        {
                            var old = oldFields.FirstOrDefault(x => x.Id == field.Id);
                            if (old == null || (old.Value ?? "") == (field.Value ?? ""))
                            {
                                //body += string.Format("<p>{0}: {1}</p>", field.Name, field.Value);
                            }
                            else
                            {
                                //body += string.Format("<p>{0}: <strong>{1}</strong></p>", field.Name, field.Value);
                                fields.Add(field.Name, field.Value);
                            }
                        }
                    }
                    //MailService.SendMailNow(guid,
                    //    SettingsMail.EmailForRegReport + (customer.Manager != null ? ";" + customer.Manager.Email : ""),
                    //    "Запрос на изменение личных данных", body, true);
                    if (fields.Count > 0)
                    {
                        var pyrusResponse = await PyrusApiService.CreateFormTaskChangeCommonInfo(1534880, body, fields, customer.EMail);
                        if (pyrusResponse.Key == null)
                            errors.Add("Ошибка при отправке запроса");
                        else
                            task = pyrusResponse.Key?.Id;
                    }
                }
                catch (Exception E)
                {
                    errors.Add(E.Message);
                    title = "Ошибка при отправке запроса";
                }
            }

            return Json(new { result = errors.Count == 0, errors = errors, title = title, redirectTo = (task.HasValue ? $"feedback/success?task={task}" : null) });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult InplaceSaveCustomer(string value, string field, bool additional, bool? subscribe)//GlorySoft_031
        {
            Debug.Log.Info(Newtonsoft.Json.JsonConvert.SerializeObject(new { CustomerContext.CustomerId, value, field, additional }));
            if (value.IsNullOrEmpty())
                return JsonError("Значение не заполнено!");
            try
            {
                CustomerService.InplaceSave(CustomerContext.CurrentCustomer, field, value, additional, subscribe ?? false);
                return JsonOk();
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> SendRequestDeleteAccount(string customerId)//GlorySoft_031
        {
            var guid = Guid.Parse(customerId);
            var errors = new List<string> { };
            var customer = CustomerService.GetCustomer(guid);

            int? task = null;
            try
            {
                var pyrusResponse = await PyrusApiService.CreateFormTaskDeleteAccount(1534880, customer);
                if (pyrusResponse.Key == null)
                    errors.Add("Ошибка при отправке запроса");
                else
                    task = pyrusResponse.Key?.Id;
            }
            catch (Exception E)
            {
                errors.Add(E.Message);
            }

            return Json(new { result = errors.Count == 0, errors = errors, title = "Ошибка при отправке запроса", redirectTo = (task.HasValue ? $"feedback/success?task={task}" : null) });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SendRequestConfirmPhone(string customerId, string phone)//GlorySoft_031
        {
            var errors = new List<string> { };

            try
            {
                CustomerService.SetConfirmPhone(customerId, phone);
            }
            catch (Exception E)
            {
                errors.Add(E.Message);
            }

            return Json(new { result = errors.Count == 0, errors = errors, title = "Ошибка при отправке запроса" });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> RetrySendRegCode(string customerId, string email, bool? createTask)//GlorySoft_031
        {
            if (customerId.IsNullOrEmpty())
                return JsonError("Значение не заполнено: customerId");

            try
            {
                var customer = CustomerService.GetCustomer(Guid.Parse(customerId));
                if (customer == null)
                    return JsonError("Пользователь не найден");
                if (createTask == true)
                {
                    var pyrusResponse = await PyrusApiService.CreateFormTaskSendRegCodeError(1534880, customer);
                    if (pyrusResponse.Key == null)
                        return JsonError("Ошибка при отправке запроса");
                }
                else
                {
                    var hash = CustomerService.GetRegCode(Guid.Parse(customerId));
                    if (hash.IsNullOrEmpty())
                        return JsonError("Код потверждения не найден");
                    var href = UrlService.GetUrl() + $"confirmregistration/{hash}";
                    MailService.SendMailNow(Guid.Parse(customerId), email, "Подтверждение регистрации в магазине \"Сапфир\"",
                        $"<div>Для подтверждения регистрации перейдите по ссылке: <a href='{href}'>Подтвердить регистрацию</a></div><div>Ссылка действительна в течение 24 часов</div>",
                        true);
                }
                return JsonOk();
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

    }
}