using System;
using System.Linq;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Orders;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class CheckoutShippingAddressViewModel
    {
        public CheckoutShippingAddressViewModel()
        {
            SuggestionsModule = AttachedModules.GetModules<ISuggestions>().Select(x => (ISuggestions)Activator.CreateInstance(x)).FirstOrDefault();
        }

        public CheckoutAddress AddressContact { get; set; }

        public bool HasAddresses { get; set; }
        public bool HasCustomShippingFields { get; set; }

        public ISuggestions SuggestionsModule { get; private set; }
    }
}