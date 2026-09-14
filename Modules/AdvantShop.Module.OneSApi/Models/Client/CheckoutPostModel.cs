using AdvantShop.Core.Services.Orders;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class CheckoutPostModel
    {
        public string CaptchaCode { get; set; }
        public string CaptchaSource { get; set; }
        public string CustomData { get; set; }
        public OrderType? OrderType { get; set; }

        public bool IsLanding { get; set; }
        //public CheckoutShowMode ShowMode { get; set; }
    }
}