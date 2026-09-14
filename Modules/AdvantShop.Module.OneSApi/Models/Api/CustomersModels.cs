using AdvantShop.Core.Services.Api;
using AdvantShop.Customers;
using AdvantShop.Web.Infrastructure.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Api
{
    public class AddUpdateCustomerModel
    {
        public string/*Guid*/ Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public string Organization { get; set; }
        public bool? SubscribedForNews { get; set; }
        public DateTime? BirthDay { get; set; }
        public string AdminComment { get; set; }
        public int? ManagerCode { get; set; }
        public int? GroupId { get; set; }
        public string Password { get; set; }
        public CustomerContactModel Contact { get; set; }
        public List<CustomerFieldModel> Fields { get; set; }
        public DateTime RegistrationDateTime { get; set; }
        public string FIO { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? Birthday { get; set; }
        public bool Subscribed { get; set; }
        public string Salt { get; set; }
        public CustomerType CustomerType { get; set; }
        public string INN { get; set; }
        public string CompanyName { get; set; }
        public float DiscountPercent { get; set; }
        public CustomerClientStatus ClientStatus { get; set; }
        public List<CustomerFieldValueMapShort> CustomerFields { get; set; }

        //public AddUpdateCustomerModel()
        //{
        //    Contact = new CustomerContactModel();
        //}
    }

    public class CustomerContactModel
    {
        [JsonProperty("contactId")]
        public Guid ContactId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("district")]
        public string District { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("zip")]
        public string Zip { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("apartment")]
        public string Apartment { get; set; }

        [JsonProperty("structure")]
        public string Structure { get; set; }

        [JsonProperty("entrance")]
        public string Entrance { get; set; }

        [JsonProperty("floor")]
        public string Floor { get; set; }

        public CustomerContactModel()
        {
        }

        public CustomerContactModel(CustomerContact contact)
        {
            ContactId = contact.ContactId;
            Name = contact.Name;
            Country = contact.Country;
            City = contact.City;
            District = contact.District;
            Region = contact.Region;
            Zip = contact.Zip;
            Street = contact.Street;
            House = contact.House;
            Apartment = contact.Apartment;
            Structure = contact.Structure;
            Entrance = contact.Entrance;
            Floor = contact.Floor;
        }
    }

    public class CustomerFieldModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class CustomersImportModel
    {
        public List<AddUpdateManagerModel> Managers { get; set; }
        public List<AddUpdateCustomerModel> Customers { get; set; }
    }
    public class CustomersImportResultModel
    {
        public List<ImportResultModel> Managers { get; set; }
        public List<ImportResultModel> Customers { get; set; }
    }

    public class AddUpdateManagerModel
    {
        public string Email { get; set; }
        public string FIO { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public string Phone { get; set; }
        public int/*string*/ Department { get; set; }
        public bool Active { get; set; }
        public int Code { get; set; }
    }

    public class FilterCustomersModel : EntitiesFilterModel//, IValidatableObject
    {
        public FilterCustomersModel()
        {
        }

        public Guid? CustomerId { get; set; }

        private bool loadItems;
        public bool LoadItems
        {
            get
            {
                return loadItems;
            }
            set
            {
                loadItems = value;

                //if (loadItems)
                //{
                //    MaxItemsPerPage = 50;
                //    DefaultItemsPerPage = 50;
                //}
            }
        }

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    DateTime temp;
        //    if (!string.IsNullOrWhiteSpace(DateFrom) && !DateTime.TryParse(DateFrom, out temp))
        //        yield return new ValidationResult("Неудалось преобразовать дату параметра DateFrom");

        //    if (!string.IsNullOrWhiteSpace(DateTo) && !DateTime.TryParse(DateTo, out temp))
        //        yield return new ValidationResult("Неудалось преобразовать дату параметра DateTo");
        //}
    }

    public class ConfirmCustomerModel
    {
        public List<ConfirmCustomerModel_item> Items { get; set; }
    }
    public class ConfirmCustomerModel_item
    {
        public string CustomerId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

}
