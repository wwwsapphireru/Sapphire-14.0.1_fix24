using AdvantShop.Customers;
using AdvantShop.Orders;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class BillingViewModel
    {
        public Order Order { get; set; }
        public bool IsMobile { get; set; }
        public string Header { get; set; }
    }
}