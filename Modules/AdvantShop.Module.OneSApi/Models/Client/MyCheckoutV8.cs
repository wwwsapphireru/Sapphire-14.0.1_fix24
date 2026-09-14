using AdvantShop.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class CheckoutDataV8 : CheckoutData
    {
        new public CheckoutAddressV8 Contact { get; set; }
    }

    public class CheckoutAddressV8 : CheckoutAddress
    {
        public string Name { get; set; }
    }
}
