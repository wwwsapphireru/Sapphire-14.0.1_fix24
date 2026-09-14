using AdvantShop.Customers;
using AdvantShop.Orders;
using AdvantShop.Repository.Currencies;
using AdvantShop.Taxes;
using System.Collections.Generic;
//GlorySoft_028
namespace AdvantShop.ViewModel.Checkout
{
    public class PrintCartViewModel
    {
        public ShoppingCart OrderItems { get; set; }

        //public bool ShowStatusInfo { get; set; }

        public bool ShowContacts { get; set; }

        public string MapAdress { get; set; }

        public string MapType { get; set; }

        public bool ShowMap { get; set; }

        public Currency Currency { get; set; }

        public string OrderDiscount { get; set; }

        //public string OrderBonus { get; set; }

        public string Certificate { get; set; }

        public string Coupon { get; set; }

        //public string ShippingPrice { get; set; }

        //public string PaymentPrice { get; set; }

        //public string PaymentPriceTitle { get; set; }

        public List<OrderTax> Taxes { get; set; }

        public string ProductsPrice { get; set; }

        public string TotalPrice { get; set; }

        //public string ShippingMethodName { get; set; }
        //public string PaymentMethodName { get; set; }
        //public string ShippingDeliveryTime { get; set; }

        //public float[] TotalDimensions { get; set; }

        public float TotalWeight { get; set; }

        public Customer Customer { get; set; }
        public CustomerContact Contact { get; set; }
    }
}