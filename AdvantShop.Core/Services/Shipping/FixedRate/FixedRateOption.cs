using AdvantShop.Core.Common.Extensions;
using AdvantShop.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
//GlorySoft_001
namespace AdvantShop.Shipping.FixedRate
{
    public class FixedRateOption : BaseShippingOption
    {
        public string NeedPassport { get; set; }

        public FixedRateOption()
        {
        }

        public FixedRateOption(ShippingMethod method, float preCost)
            : base(method, preCost)
        {
        }

        public override string TemplateName
        {
            get { return "FixedRateOption.html"; }
        }

        public override void Update(BaseShippingOption option)
        {
            var opt = option as FixedRateOption;
            if (opt != null && opt.Id == this.Id)
            {
                NeedPassport = "no";
                var customer = CustomerContext.CurrentCustomer;
                if (customer.CustomerType != CustomerType.LegalEntity)
                {
                    var needs = new List<string> { "Серия паспорта", "Номер паспорта", "Дата выдачи паспорта"/*, "Кем выдан паспорт"*/ };
                    var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id)
                        .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
                                    (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All)).ToList();
                    foreach (var need in needs)
                    {
                        var field = customerFields.SingleOrDefault(x => x.Name == need);
                        if (field == null || field.Value.IsNullOrEmpty())//(!needs.Contains(field.Name) || field.Value.IsNullOrEmpty())
                        {
                            NeedPassport = null;
                            break;
                        }
                    }
                }
            }
        }

    }
}
