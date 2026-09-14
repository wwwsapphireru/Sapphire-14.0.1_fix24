//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System.Collections.Generic;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Shipping;

namespace AdvantShop.Shipping.FixedRate
{
    [ShippingKey("FixedRate")]
    public class FixeRateShipping : BaseShipping, IShippingNoUseExtraDeliveryTime, IShippingUseDeliveryInterval, IShippingRequiresSpecifyingTypeOfDelivery
    {
        private readonly float _shippingPrice;

        public override bool CurrencyAllAvailable { get { return true;} }

        public FixeRateShipping(ShippingMethod method, ShippingCalculationParameters calculationParameters)
            : base(method, calculationParameters)
        {
            _shippingPrice = method.Params.ElementOrDefault(FixeRateShippingTemplate.ShippingPrice).TryParseFloat();
        }

        protected override IEnumerable<BaseShippingOption> CalcOptions(CalculationVariants calculationVariants)
        {
            var option = new FixedRateOption/*GlorySoft_001 BaseShippingOption*/(_method, _totalPrice)
            {
                Rate = _shippingPrice,
                DeliveryTime = _method.Params.ElementOrDefault(FixeRateShippingTemplate.DeliveryTime)
            };
            return new List<BaseShippingOption> { option };
        }
    }
}