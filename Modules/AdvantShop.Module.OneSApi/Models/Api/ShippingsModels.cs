using System.Collections.Generic;
using AdvantShop.Orders;
using AdvantShop.Shipping;

namespace AdvantShop.Module.OneSApi.Models.Api
{
    public class GetCheckoutShippingsResponse
    {
        public BaseShippingOption selectShipping { get; set; }
        public List<BaseShippingOption> option { get; set; }
        public string typeCalculationVariants { get; set; }
    }

    public class GetCheckoutShippingsRequest
    {
        public OrderData OrderData { get; set; }
        public List<PreOrderItemV8> PreOrderList { get; set; }
    }

    public class OrderData
    {
        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Zip { get; set; }
    }

    public class PreOrderItemV8
    {
        public string ExternalId { get; set; }
        public string ArtNo { get; set; }
        public float Price { get; set; }
        public float Amount { get; set; }
    }

}