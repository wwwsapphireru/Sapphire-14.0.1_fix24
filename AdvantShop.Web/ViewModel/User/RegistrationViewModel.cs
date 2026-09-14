using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Configuration;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Customers;
using AdvantShop.Repository;

namespace AdvantShop.ViewModel.User
{
    public class RegistrationViewModel
    {
        public RegistrationViewModel()
        {
            SuggestionsModule = AttachedModules.GetModules<ISuggestions>().Select(x => (ISuggestions)Activator.CreateInstance(x)).FirstOrDefault();
            AllCustomerTypes = SettingsCustomers.IsRegistrationAsLegalEntity && SettingsCustomers.IsRegistrationAsPhysicalEntity;
            CustomerTypeByDefault = SettingsCustomers.IsRegistrationAsPhysicalEntity
                ? CustomerType.PhysicalEntity.ToString()
                : CustomerType.LegalEntity.ToString();
        }

        public bool IsBonusSystemActive { get; set; }
        
        public string BonusesForNewCard { get; set; }

        public bool WantBonusCard { get; set; }

        public bool IsDemo { get; set; }
        
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public DateTime? BirthDay { get; set; }

        public string Patronymic { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string PasswordConfirm { get; set; }

        public bool Agree { get; set; }
        
        public bool UserAgreementForPromotionalNewsletter { get; set; }
        

        private List<CustomerFieldWithValue> _customerFields;
        public List<CustomerFieldWithValue> CustomerFields
        {
            get
            {
                if (_customerFields != null)
                    return _customerFields;
                _customerFields = CustomerFieldService.GetCustomerFieldsWithValue(Guid.Empty).Where(x => x.ShowInRegistration).ToList();
                return _customerFields;
            }
            set { _customerFields = value; }
        }

        public int? LpId { get; set; }

        public ISuggestions SuggestionsModule { get; private set; }

        public bool AllCustomerTypes { get; set; }
        public string CustomerTypeByDefault { get; set; }
        public CustomerType CustomerType { get; set; }
    }
}