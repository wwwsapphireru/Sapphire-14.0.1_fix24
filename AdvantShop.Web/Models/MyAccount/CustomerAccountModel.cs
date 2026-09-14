namespace AdvantShop.Models.MyAccount
{
    public partial class CustomerAccountModel : BaseModel
    {
        public string ContactId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }

        public int CountryId { get; set; }

        public string Country { get; set; }

        public string Region { get; set; }

        public string City { get; set; }
        public string District { get; set; }
        
        public string Zip { get; set; }


        public string Street { get; set; }
        public string House { get; set; }
        public string Apartment { get; set; }
        public string Structure { get; set; }
        public string Entrance { get; set; }
        public string Floor { get; set; }
        
        public bool IsShowName { get; set; }
        public bool IsMain { get; set; }

        public bool? IsRequiredAddress { get; set; }//GlorySoft_027
    }
}