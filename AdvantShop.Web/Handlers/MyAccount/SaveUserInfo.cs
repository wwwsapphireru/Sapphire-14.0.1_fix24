using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Extensions;
using AdvantShop.Helpers;
using AdvantShop.Models.MyAccount;
using AdvantShop.Web.Infrastructure.Handlers;

namespace AdvantShop.Handlers.MyAccount
{
    public class SaveUserInfo : AbstractCommandHandler
    {
        private readonly UserInfoModel _userInfo;

        public SaveUserInfo(UserInfoModel userInfo)
        {
            _userInfo = userInfo;
        }

        protected override void Validate()
        {
            var valid = !string.IsNullOrWhiteSpace(_userInfo.FirstName);

            if (SettingsCheckout.IsRequiredLastName && string.IsNullOrWhiteSpace(_userInfo.LastName))
                valid = false;
            
            if (SettingsCheckout.IsRequiredPatronymic && string.IsNullOrWhiteSpace(_userInfo.Patronymic))
                valid = false;
            
            if (SettingsCheckout.IsRequiredPhone && string.IsNullOrWhiteSpace(_userInfo.Phone))
                valid = false;
            
            if (SettingsCheckout.IsRequiredBirthDay && _userInfo.BirthDay == null)
                valid = false;

            if (!valid)
                throw new BlException(LocalizationService.GetResource("MyAccount.SaveUserInfo.ErrorRequiredFields"));
            
            // Vadim_Dyl_381073705: паспорт — если заполнять начали, нужны все 4 поля и корректная длина серии/номера
            ValidatePassportFields();

            // Vadim_Dyl_381073705: реквизиты юрлица — формат ИНН/КПП/ОГРН/ОКПО.
            // Проверяем только те поля, которые реально сохраняем (прямое сохранение лишь заполняет пустые),
            // чтобы уже сохранённые значения не блокировали правку других данных.
            if (CustomerContext.CurrentCustomer.CustomerType == CustomerType.LegalEntity && _userInfo.CustomerFields != null)
            {
                var customerId = CustomerContext.CurrentCustomer.Id;
                var newFields = _userInfo.CustomerFields
                    .Where(x => string.IsNullOrEmpty(CustomerFieldService.GetCustomerFieldsWithValue(customerId, x.Id)?.Value))
                    .ToList();
                var fieldErrors = CustomerFieldsValidation.ValidateLegalEntityFields(newFields);
                if (fieldErrors.Count > 0)
                    throw new BlException(string.Join("; ", fieldErrors));
            }

            var customer = CustomerContext.CurrentCustomer;

            if (!string.IsNullOrWhiteSpace(_userInfo.Phone) && customer.Phone != _userInfo.Phone)
            {
                var standardPhone = StringHelper.ConvertToStandardPhone(_userInfo.Phone, true, true);

                if (standardPhone != null && CustomerService.IsPhoneExist(_userInfo.Phone, standardPhone))
                    throw new BlException("Пользователь с телефоном \"" + _userInfo.Phone + "\" уже существует");
            }
        }

        private void ValidatePassportFields()
        {
            const int serialId = 13, numberId = 14;
            var passportFieldIds = new[] { serialId, numberId, 15, 16 };
            var fields = _userInfo.CustomerFields?
                .Where(x => passportFieldIds.Contains(x.Id))
                .ToList() ?? new List<CustomerFieldWithValue>();

            if (fields.Count == 0)
                return;

            Func<CustomerFieldWithValue, bool> isFilled = x =>
                !string.IsNullOrWhiteSpace(x.Value) && x.Value.Trim() != "0";

            var started = fields.Any(isFilled);
            if (!started)
                return;

            if (fields.Count != passportFieldIds.Length || fields.Any(x => !isFilled(x)))
                throw new BlException("Заполните все паспортные данные");

            foreach (var field in fields)
            {
                var expectedLength = field.Id == serialId ? 4 : field.Id == numberId ? 6 : 0;
                if (expectedLength > 0 && !Regex.IsMatch(field.Value.Trim(), "^\\d{" + expectedLength + "}$"))
                    throw new BlException(field.Id == serialId
                        ? "Серия паспорта должна содержать 4 цифры"
                        : "Номер паспорта должен содержать 6 цифр");
            }
        }

        protected override void Handle()
        {
            try
            {
                var customer = CustomerContext.CurrentCustomer;
            
                customer.FirstName = StringHelper.HtmlEncode(_userInfo.FirstName);
                customer.LastName = StringHelper.HtmlEncode(_userInfo.LastName);
                customer.Patronymic = StringHelper.HtmlEncode(_userInfo.Patronymic);
                customer.Phone = StringHelper.HtmlEncode(_userInfo.Phone);
                customer.StandardPhone = StringHelper.ConvertToStandardPhone(_userInfo.Phone);
                customer.BirthDay = _userInfo.BirthDay;
                customer.IsAgreeForPromotionalNewsletter =
                    SettingsDesign.ShowUserAgreementForPromotionalNewsletter
                        ? _userInfo.IsAgreeForPromotionalNewsletter
                        : SettingsDesign.SetUserAgreementForPromotionalNewsletterChecked;

                if (_userInfo.CustomerFields != null)
                {
                    var isLegalEntity = customer.CustomerType == CustomerType.LegalEntity;

                    foreach (var customerField in _userInfo.CustomerFields)
                    {
                        var field = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id, customerField.Id);
                        if (field == null || !field.Enabled || (field.DisableCustomerEditing && !string.IsNullOrEmpty(field.Value)))
                            continue;

                        // Vadim_Dyl_381073705: реквизиты юрлица напрямую только заполняются;
                        // изменение уже заполненных — через заявку (Pyrus).
                        if (isLegalEntity && !string.IsNullOrEmpty(field.Value))
                            continue;

                        CustomerFieldService.AddUpdateMap(customer.Id, customerField.Id, customerField.Value ?? "");
                    }
                }

                CustomerService.UpdateCustomer(customer);

                //в адресной книге редактировать имя нельзя, при оформлении данные берутся именно из контакта
                if (customer.Contacts.Any())
                {
                    foreach (var contact in customer.Contacts)
                    {
                        contact.Name = customer.GetFullName();
                        CustomerService.UpdateContact(contact);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                throw new BlException(LocalizationService.GetResource("MyAccount.SaveUserInfo.Error"));
            }
        }
    }
}