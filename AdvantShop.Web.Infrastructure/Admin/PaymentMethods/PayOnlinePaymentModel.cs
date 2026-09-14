using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Payment;
using AdvantShop.Payment;

namespace AdvantShop.Web.Infrastructure.Admin.PaymentMethods
{
    [PaymentAdminModel("PayOnline")]
    public class PayOnlinePaymentModel : PaymentMethodAdminModel, IValidatableObject
    {
        public string MerchantId
        {
            get { return Parameters.ElementOrDefault(PayOnlineTemplate.MerchantId); }
            set { Parameters.TryAddValue(PayOnlineTemplate.MerchantId, value.DefaultOrEmpty()); }
        }

        public string SecretKey
        {
            get { return Parameters.ElementOrDefault(PayOnlineTemplate.SecretKey); }
            set { Parameters.TryAddValue(PayOnlineTemplate.SecretKey, value.DefaultOrEmpty()); }
        }

        public string PayType
        {
            get { return Parameters.ElementOrDefault(PayOnlineTemplate.PayType); }
            set { Parameters.TryAddValue(PayOnlineTemplate.PayType, value.DefaultOrEmpty()); }
        }

        public override Tuple<string, string> Instruction
        {
            get { return new Tuple<string, string>($"{LinkService.Internal.AccountPlatform}/help/pages/payonline", "Инструкция. Подключение платежного модуля PayOnline"); }
        }

        public List<SelectListItem> PayTypes
        {
            get
            {
                var types = new List<SelectListItem>()
                {
                    new SelectListItem() {Text = "Select", Value = "0"},
                    new SelectListItem() {Text = "WebMoney", Value = "1"},
                    new SelectListItem() {Text = "QIWI", Value = "2"},
                    new SelectListItem() {Text = "YandexMoney", Value = "3"},
                    new SelectListItem() {Text = "CreditCard_EN", Value = "4"},
                    new SelectListItem() {Text = "CreditCard_RU", Value = "5"},
                };

                var type = types.Find(x => x.Value == PayType);
                if (type != null)
                    type.Selected = true;

                return types;
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(MerchantId) ||
                string.IsNullOrWhiteSpace(SecretKey))
            {
                yield return new ValidationResult("Заполните обязательные поля");
            }
        }

        //GlorySoft_001
        public string ShowForNewUsers
        {
            get { return Parameters.ElementOrDefault(PayOnlineTemplate.ShowForNewUsers); }
            set { Parameters.TryAddValue(PayOnlineTemplate.ShowForNewUsers, value.DefaultOrEmpty()); }
        }
        public List<SelectListItem> ShowForNewUsersTypes
        {
            get
            {
                var types = new List<SelectListItem>()
                {
                    new SelectListItem() {Text = "Всем", Value = ""},
                    new SelectListItem() {Text = "Только старым", Value = "reg"},
                    new SelectListItem() {Text = "Только новым", Value = "new"},
                };

                var type = types.Find(x => x.Value == ShowForNewUsers);
                if (type != null)
                    type.Selected = true;

                return types;
            }
        }

    }
}
