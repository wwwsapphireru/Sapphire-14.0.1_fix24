using System;
using System.Web;
using System.Web.Mvc;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Auth;
using AdvantShop.Core.Services.Auth.Emails;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.Mails;
using AdvantShop.Models.User;
using AdvantShop.Security;
using AdvantShop.Web.Infrastructure.Handlers;

namespace AdvantShop.Handlers.User
{
    public sealed class RegistrationHandler : ICommandHandler
    {
        private readonly RegistrationModel _model;
        private readonly ERegistrationMethod _method;
        private readonly TempDataDictionary _tempData;

        private CustomerType _customerType;
        private Customer _customer;

        public RegistrationHandler(
            RegistrationModel model, 
            ERegistrationMethod method, 
            TempDataDictionary tempData
        )
        {
            _model = model;
            _method = method;
            _tempData = tempData;
        }

        public void Execute()
        {
            if (!SettingsMain.RegistrationIsProhibited)
            {
                Load();
                Validate();
                Process();
            }
            else
                throw new BlException(
                    LocalizationService.GetResource("User.Registration.ErrorRegistrationIsProhibited"));
        }

        private void Load()
        {
            _customerType = _model.CustomerType.TryParseEnum<CustomerType>();
        }

        private void Validate()
        {
            CustomerTypeValidate();
            EmailValidate();
            PasswordValidate();
            PhoneValidate();
            NameValidate();
            BirthDayValidate();
            AgreeValidate();
            BonusValidate();
            SpamValidate();
            LocationValidate();//GlorySoft_017
        }

        private void Process()
        {
            AddCustomer();
            //Authorize();GlorySoft_033
            AddBonusCart();
            //SendMails();GlorySoft_033
            SetTempData();
            
            ModulesExecuter.Registration(_customer);
        }

        #region Validations

        private void CustomerTypeValidate()
        {
            if (_customerType == CustomerType.PhysicalEntity && !SettingsCustomers.IsRegistrationAsPhysicalEntity
                || _customerType == CustomerType.LegalEntity && !SettingsCustomers.IsRegistrationAsLegalEntity)
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorCustomerType"));
        }

        private void EmailValidate()
        {
            if ((_method == ERegistrationMethod.Email 
                 || ((SettingsCheckout.IsShowEmail || !SettingsAuth.AuthByCodeActive) 
                     && (SettingsCheckout.IsRequiredEmail || !string.IsNullOrEmpty(_model.Email))))
                && !ValidationHelper.IsValidEmail(_model.Email))
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorCustomerEmailIsWrong"));
            
            if (((_method == ERegistrationMethod.Email && SettingsAuth.EmailAuthType == EEmailAuthType.Code)
                || (SettingsAuth.UseEmailConfirmation 
                    && (SettingsCheckout.IsRequiredEmail || !string.IsNullOrEmpty(_model.Email))))
                && SettingsMail.IsMailServiceEnabled
                && !EmailConfirmationService.IsConfirmed(_model.Email, CustomerContext.CustomerId))
                throw new BlException(LocalizationService.GetResource("User.Registration.ConfirmationError"));
            
            if (!string.IsNullOrWhiteSpace(_model.Email) && CustomerService.IsEmailExist(_model.Email))
                throw new BlException(LocalizationService.GetResource("User.Registration.IsExistCustomerError"));

            if (_model.Email.EndsWith(".com"))//GlorySoft_030
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorEmailDotCom"));
        }

        private void PasswordValidate()
        {
            if (_method == ERegistrationMethod.Code) return;
            
            if (string.IsNullOrWhiteSpace(_model.PasswordConfirm)
                || string.IsNullOrWhiteSpace(_model.Password)
                || _model.Password != _model.PasswordConfirm)
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorPasswordNotMatch"));

            if (_model.Password.Length < 6)
                throw new BlException(LocalizationService.GetResource("User.Registration.PasswordLenght"));
        }

        private void PhoneValidate()
        {
            if (((SettingsCheckout.IsShowPhone && SettingsCheckout.IsRequiredPhone) 
                 || _method == ERegistrationMethod.Code)
                && string.IsNullOrWhiteSpace(_model.Phone))
                throw new BlException(LocalizationService.GetResource("User.Registration.Error"));

            if (!SettingsCheckout.IsShowPhone || string.IsNullOrWhiteSpace(_model.Phone)) return;

            var standardPhone = StringHelper.ConvertToStandardPhone(HttpUtility.HtmlEncode(_model.Phone));

            if (CustomerService.IsPhoneExist(_model.Phone, standardPhone,/*GlorySoft_030*/ CustomerType.PhysicalEntity) &&/*GlorySoft_030*/ _model.CustomerType.TryParseEnum<CustomerType>() != CustomerType.LegalEntity)
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorCustomerPhoneExist"));

            if (SettingsAuth.UsePhoneConfirmation 
                && (SettingsAuth.AuthByCodeActive || ModulesPhoneConfirmationService.IsExistsPhoneConfirmedModules()) 
                && !(new PhoneConfirmationService().IsPhoneConfirmed(standardPhone ?? 0, CustomerContext.CustomerId) 
                     || ModulesPhoneConfirmationService.IsPhoneConfirmed(standardPhone ?? 0, CustomerContext.CustomerId)
                )
            )
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorPhoneNotConfirmed"));
        }

        private void NameValidate()
        {
            if (string.IsNullOrWhiteSpace(_model.FirstName)
                || (SettingsCheckout.IsShowLastName
                    && SettingsCheckout.IsRequiredLastName
                    && string.IsNullOrWhiteSpace(_model.LastName))
                || (SettingsCheckout.IsShowPatronymic
                    && SettingsCheckout.IsRequiredPatronymic
                    && string.IsNullOrWhiteSpace(_model.Patronymic)))
                throw new BlException(LocalizationService.GetResource("User.Registration.Error"));
        }

        private void BirthDayValidate()
        {
            if (SettingsCheckout.IsShowBirthDay
                && SettingsCheckout.IsRequiredBirthDay
                && _model.BirthDay == null)
                throw new BlException(LocalizationService.GetResource("User.Registration.Error"));
        }

        private void AgreeValidate()
        {
            if (SettingsCheckout.IsShowUserAgreementText
                && !_model.Agree)
                throw new BlException(LocalizationService.GetResource("User.Registration.ErrorAgreement"));
        }

        private void BonusValidate()
        {
            if (!BonusSystem.IsActive || !_model.WantBonusCard) return;

            var bonusCard = BonusSystem.GetCard(CustomerContext.CurrentCustomer);
            if (bonusCard != null)
                throw new BlException(LocalizationService.GetResource("User.Registration.BonusCardError"));
        }

        private void SpamValidate()
        {
            if (!ModulesExecuter.CheckInfo(
                    HttpContext.Current,
                    ECheckType.Registration,
                    _model.Email,
                    _model.FirstName,
                    phone: _model.Phone))
                throw new BlException(LocalizationService.GetResource("Common.SpamCheckFailed"));
        }

        private void LocationValidate()//GlorySoft_017
        {
            if (_model.Location == null)
                throw new BlException(LocalizationService.GetResource("Не указан населенный пункт"));
            if (_model.Location.CountryId != SettingsMain.SellerCountryId)
                throw new BlException(LocalizationService.GetResource("Указан некорректный населенный пункт"));
        }

        #endregion

        #region Processes

        private void AddCustomer()
        {
            _customer = new Customer(CustomerGroupService.DefaultCustomerGroup)
            {
                Id = CustomerContext.CustomerId,
                Password = HttpUtility.HtmlEncode(_method != ERegistrationMethod.Code 
                    ? _model.Password
                    : StringHelper.GeneratePassword(8)),
                FirstName = HttpUtility.HtmlEncode(_model.FirstName),
                LastName = SettingsCheckout.IsShowLastName
                    ? HttpUtility.HtmlEncode(_model.LastName)
                    : string.Empty,
                Patronymic = SettingsCheckout.IsShowPatronymic
                    ? HttpUtility.HtmlEncode(_model.Patronymic)
                    : string.Empty,
                Phone = SettingsCheckout.IsShowPhone || _method == ERegistrationMethod.Code
                    ? HttpUtility.HtmlEncode(_model.Phone)
                    : string.Empty,
                StandardPhone = StringHelper.ConvertToStandardPhone(
                    SettingsCheckout.IsShowPhone
                        ? HttpUtility.HtmlEncode(_model.Phone)
                        : ""),
                SubscribedForNews = _model.NewsSubscription,
                EMail = HttpUtility.HtmlEncode(_model.Email),
                BirthDay = SettingsCheckout.IsShowBirthDay
                    ? _model.BirthDay
                    : null,
                CustomerRole = Role.User,
                CustomerType = _customerType,
                IsAgreeForPromotionalNewsletter = 
                    SettingsDesign.ShowUserAgreementForPromotionalNewsletter
                        ? _model.UserAgreementForPromotionalNewsletter
                        : SettingsDesign.SetUserAgreementForPromotionalNewsletterChecked,

                Enabled = false//GlorySoft_033
            };

            var newGuid /*GlorySoft_074*/= CustomerService.InsertNewCustomer(_customer, _model.CustomerFields);

            //GlorySoft_033
            CustomerService.AddContact(new CustomerContact
            {
                CustomerGuid = newGuid,
                Name = new string[] { _customer.LastName, _customer.FirstName, _customer.Patronymic }.AggregateString(" "),
                IsMain = true,
                CountryId = SettingsMain.SellerCountryId,
                Country = Repository.CountryService.GetCountry(SettingsMain.SellerCountryId)?.Name,
                RegionId = _model.Location.RegionId,
                Region = _model.Location.Region,
                City = _model.Location.Name,
            },
            newGuid, false);
            if (_model.CustomerType.TryParseEnum<CustomerType>() == CustomerType.LegalEntity)
                CustomerService.SetConfirmPhone(CustomerContext.CustomerId.ToString(), _customer.Phone);
            var regCode = CommonHelper.GenerateRandomString(100);
            CustomerService.SetRegCode(CustomerContext.CustomerId, regCode);
        }

        private void Authorize()
        {
            if (!string.IsNullOrWhiteSpace(_customer.EMail))
                AuthorizeService.SignIn(_customer.EMail, _customer.Password, false, true);
            else
                AuthorizeService.SignInByPhone(_customer.StandardPhone, _customer.Password, false, true);
            
            AuthCaptchaHandler.ClearAttempts();
        }

        private void AddBonusCart()
        {
            if (!BonusSystem.IsActive || !_model.WantBonusCard) return;
            
            try
            {
                BonusSystem.CreateCard(_customer);
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        private void SendMails()
        {
            if (CustomerContext.CurrentCustomer.IsVirtual) return;
            
            var mail = new RegistrationMailTemplate(_customer);
            
            MailService.SendMailNow(_customer.Id, _customer.EMail, mail);
            MailService.SendMailNow(SettingsMail.EmailForRegReport, mail, replyTo: _customer.EMail);
        }

        private void SetTempData()
        {
            _tempData["IsRegisteredNow"] = "true";
        }

        public void Update(Customer customer, RegistrationModel model)//GlorySoft_033
        {
            customer.FirstName = HttpUtility.HtmlEncode(model.FirstName);
            customer.LastName = HttpUtility.HtmlEncode(model.LastName);
            customer.Patronymic = HttpUtility.HtmlEncode(model.Patronymic);
            customer.Phone = HttpUtility.HtmlEncode(model.Phone);
            customer.StandardPhone = StringHelper.ConvertToStandardPhone(SettingsCheckout.IsShowPhone ? HttpUtility.HtmlEncode(model.Phone) : "");

            CustomerService.UpdateCustomer(customer);

            CustomerService.ChangePassword(customer.Id, model.Password, false);

            var regCode = CustomerService.GetRegCode(customer.Id);
            if (regCode.IsNullOrEmpty())
                regCode = CommonHelper.GenerateRandomString(100);
            CustomerService.SetRegCode(customer.Id, regCode);
        }

        #endregion
    }
}