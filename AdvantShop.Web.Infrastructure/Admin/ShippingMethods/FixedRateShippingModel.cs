using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Shipping.FixedRate;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdvantShop.Web.Infrastructure.Admin.ShippingMethods
{
    [ShippingAdminModel("FixedRate")]
    public class FixedRateShippingAdminModel : ShippingMethodAdminModel, IValidatableObject
    {
        public string ShippingPrice
        {
            get { return Params.ElementOrDefault(FixeRateShippingTemplate.ShippingPrice, "0"); }
            set { Params.TryAddValue(FixeRateShippingTemplate.ShippingPrice, value.TryParseFloat().ToString()); }
        }

        public string DeliveryTime
        {
            get { return Params.ElementOrDefault(FixeRateShippingTemplate.DeliveryTime); }
            set { Params.TryAddValue(FixeRateShippingTemplate.DeliveryTime, value.DefaultOrEmpty()); }
        }

        public bool ShippingForTK//GlorySoft_030
        {
            get { return Params.ElementOrDefault(FixeRateShippingTemplate.ShippingForTK).TryParseBool(); }
            set { Params.TryAddValue(FixeRateShippingTemplate.ShippingForTK, value.ToString()); }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ShippingPrice))
                yield return new ValidationResult("Укажите стоимость доставки");
            
            if (!ShippingPrice.IsDecimal())
                yield return new ValidationResult("Стоимость доставки дожна быть числом");
        }
    }
}
