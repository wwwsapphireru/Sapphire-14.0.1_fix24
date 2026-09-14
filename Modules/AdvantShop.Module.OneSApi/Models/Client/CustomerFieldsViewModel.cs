using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Customers;
using Newtonsoft.Json;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class CustomerFieldsViewModel
    {
        public CustomerFieldsViewModel() { }

        public CustomerFieldsViewModel(List<CustomerFieldWithValue> customerFields, string modelName = "",
                                        string ngModelName = "", string ngChangeFunc = "", string cssParamName = "col-xs-12 col-sm-8",
                                        string cssParamValue = "col-xs-12 col-sm-8", bool checkFields = false, 
                                        CustomerFieldShowMode? showMode = null, string ngVariableVisible = null, ISuggestions suggestions = null, string verticalIntervalCss = null)
        {
            CustomerFields = customerFields;
            ModelName = modelName;
            NgModelName = ngModelName;
            NgChangeFunc = ngChangeFunc;
            CssParamName = cssParamName;
            CssParamValue = cssParamValue;
            ShowMode = showMode ?? CustomerFieldShowMode.None;
            Suggestions = suggestions;
            VerticalIntervalCss = verticalIntervalCss;
            if (checkFields)
            {
                var modelFieldValues = CustomerFields != null
                    ? CustomerFields.ToDictionary(x => x.Id, x => x.Value)
                    : new Dictionary<int, string>();

                var fields = CustomerFieldService.GetCustomerFieldsWithValue(Guid.Empty);
                var isUseMultipleTypes = SettingsCustomers.IsRegistrationAsLegalEntity &&
                                         SettingsCustomers.IsRegistrationAsPhysicalEntity;

                if (isUseMultipleTypes && ngVariableVisible.IsNotEmpty())
                {
                    NgVariableVisible = ngVariableVisible;
                    CustomerFields = showMode != null && showMode == CustomerFieldShowMode.Checkout
                        ? fields.Where(x => x.ShowInCheckout).ToList()
                        : fields.Where(x => x.ShowInRegistration).ToList();
                }
                else
                {
                    NgVariableVisible = null;
                    var customer = CustomerContext.CurrentCustomer;
                    CustomerFields = showMode != null && showMode == CustomerFieldShowMode.Checkout
                        ? fields.Where(x => x.ShowInCheckout && (x.CustomerType == customer?.CustomerType || x.CustomerType == CustomerType.All)).ToList()
                        : fields.Where(x => x.ShowInRegistration && (x.CustomerType == customer?.CustomerType || x.CustomerType == CustomerType.All)).ToList();
                }
                
                foreach (var field in CustomerFields)
                {
                    if (modelFieldValues.ContainsKey(field.Id))
                        field.Value = modelFieldValues[field.Id];
                }
            }
        }

        
        public List<CustomerFieldWithValue> CustomerFields { get; set; }

        public string ModelName { get; set; }

        public string NgModelName { get; set; }

        public string NgChangeFunc { get; set; }

        public string NgVariableVisible  { get; set; }
        
        public string CssParamName { get; set; }

        public string CssParamValue { get; set; }
        public string VerticalIntervalCss { get; set; }

        public CustomerFieldShowMode ShowMode { get; set; }


        public string CustomerFieldsSerialized { get { return CustomerFields != null ? JsonConvert.SerializeObject(CustomerFields) : "[]"; } }

        public ISuggestions Suggestions { get; set; }
        public string GetName()
        {
            return GetName(null, string.Empty);
        }

        public string GetName(int? index, string field)
        {
            var prefix = NgModelName.IsNotEmpty() ? string.Format("{0}.", NgModelName) : string.Empty;
            var postfix = field.IsNotEmpty() ? string.Format(".{0}", field) : string.Empty;
            var indexPart = index.HasValue ? string.Format("[{0}]", index.Value) : string.Empty;
            return string.Format("{0}CustomerFields{1}{2}", prefix, indexPart, postfix);
        }
        public string GetPrefix()
        {
            var prefixLength = NgModelName.IndexOf('.');
            var prefix = prefixLength != -1 ? NgModelName.Substring(0, prefixLength) : NgModelName;

            return prefix;
        }
    }
}