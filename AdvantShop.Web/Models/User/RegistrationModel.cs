using System;
using System.Collections.Generic;
using AdvantShop.Customers;
using AdvantShop.Repository;

namespace AdvantShop.Models.User
{
    public sealed class RegistrationModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public DateTime? BirthDay { get; set; }
        public string Patronymic { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PasswordConfirm { get; set; }
        public bool WantBonusCard { get; set; }
        public List<CustomerFieldWithValue> CustomerFields { get; set; }
        public bool NewsSubscription { get; set; }
        public bool Agree { get; set; }
        public bool UserAgreementForPromotionalNewsletter { get; set; }
        public string CustomerType { get; set; }
        public string Captcha { get; set; }

        //GlorySoft_017
        public LocationModel Location { get; set; }
        public string City { get; set; }
    }
}