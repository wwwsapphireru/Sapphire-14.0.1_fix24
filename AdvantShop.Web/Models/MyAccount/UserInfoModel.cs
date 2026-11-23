using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Configuration;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Customers;

namespace AdvantShop.Models.MyAccount
{
    public class UserInfoModel
    {
        public UserInfoModel()
        {
            SuggestionsModule = AttachedModules.GetModules<ISuggestions>().Select(x => (ISuggestions)Activator.CreateInstance(x)).FirstOrDefault();
        }

        public string Email { get; set; }
        public string Organization { get; set; }
        public string RegistrationDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public string Phone { get; set; }
        public string CustomerType { get; set; }
        public string CustomerGroup { get; set; }
        public bool ShowCustomerRole { get; set; }
        public bool ShowCustomerGroup { get; set; }
        public bool IsEnabledBothTypesOfCustomers { get; set; }
        public CustomerType CustomerTypeEntity { get; set; }

        public List<CustomerFieldWithValue> CustomerFields { get; set; }
        public DateTime? BirthDay { get; set; }

        public ISuggestions SuggestionsModule { get; set; }
        
        public bool IsAgreeForPromotionalNewsletter { get; set; }
        public bool AllowChangePhone { get; set; }

        /// <summary>Vadim_Dyl_381073705: подтверждение телефона по SMS — только при включённом модуле SmsConfirmation</summary>
        public bool CanConfirmPhone { get; set; }
        public bool IsForbiddenAsChangingGeneralData { get; set; }
        public bool IsForbiddenAsChangingAddressBook { get; set; }
    }
}