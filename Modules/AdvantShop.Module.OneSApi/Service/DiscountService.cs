using AdvantShop.Customers;
using AdvantShop.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class DiscountService
    {
        public static float GetDiscount(ShoppingCart cart)
        {
            var sum = cart.Sum(x => x.Price);
            if (sum == 0)
                return 0;
            return (1f - cart.Sum(x => x.PriceWithDiscount) / sum) * 100f;
        }

        public static void RegisterCustomer(Customer customer)
        {
            var settings = ModuleService.GetImportExportSettings();
            if (settings.ImportCustomer.CustomerGroupId > 0 && customer.CustomerGroupId != settings.ImportCustomer.CustomerGroupId)
                CustomerService.ChangeCustomerGroup(customer.Id, settings.ImportCustomer.CustomerGroupId);
        }


    }
}
