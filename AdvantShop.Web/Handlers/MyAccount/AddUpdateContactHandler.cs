using System.Web;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Customers;
using AdvantShop.GeoModes;
using AdvantShop.Helpers;
using AdvantShop.Models.MyAccount;
using AdvantShop.Repository;

namespace AdvantShop.Handlers.MyAccount
{
    public class AddUpdateContactHandler
    {
        private bool IsValidCustomerContactModel(CustomerAccountModel account)
        {
            var valid = true;

            if (SettingsCheckout.IsShowCountry && SettingsCheckout.IsRequiredCountry)
            {
                valid &= account.CountryId != 0;
                valid &= account.Country.IsNotEmpty();
            }

            if (SettingsCheckout.IsShowState && SettingsCheckout.IsRequiredState)
            {
                valid &= account.Region.IsNotEmpty();
            }

            if (SettingsCheckout.IsShowCity && SettingsCheckout.IsRequiredCity)
            {
                valid &= account.City.IsNotEmpty();
            }

            if (SettingsCheckout.IsShowDistrict && SettingsCheckout.IsRequiredDistrict)
            {
                valid &= account.District.IsNotEmpty();
            }

            if (SettingsCheckout.IsShowAddress && SettingsCheckout.IsRequiredAddress &&/*GlorySoft_027*/ account.IsRequiredAddress != false)
            {
                valid &= account.Street.IsNotEmpty();
            }

            return valid;
        }

        public CustomerContact Execute(CustomerAccountModel account)
        {
            if (!IsValidCustomerContactModel(account) || !CustomerContext.CurrentCustomer.RegistredUser)
                return null;

            if (account.IsShowName && CustomerContext.CurrentCustomer != null)
            {
                var customer = CustomerContext.CurrentCustomer;
                var updateCustomer = false;

                if(customer.FirstName != account.FirstName && !string.IsNullOrWhiteSpace(account.FirstName))
                {
                    customer.FirstName = account.FirstName;
                    updateCustomer = true;
                }
                if (customer.LastName != account.LastName && 
                    SettingsCheckout.IsShowLastName && 
                    (!SettingsCheckout.IsRequiredLastName || 
                     SettingsCheckout.IsRequiredLastName && !string.IsNullOrWhiteSpace(account.LastName)))
                {
                    customer.LastName = account.LastName;
                    updateCustomer = true;
                }
                if (customer.Patronymic != account.Patronymic  && 
                    SettingsCheckout.IsShowPatronymic && 
                    (!SettingsCheckout.IsRequiredPatronymic || 
                     SettingsCheckout.IsRequiredPatronymic && !string.IsNullOrWhiteSpace(account.Patronymic)))
                {
                    customer.Patronymic = account.Patronymic;
                    updateCustomer = true;
                }

                if(updateCustomer)
                    CustomerService.UpdateCustomer(customer);
            }

            var ipZone = IpZoneContext.CurrentZone;

            var contact = account.ContactId.IsNullOrEmpty()
                                ? new CustomerContact()
                                : CustomerService.GetCustomerContact(account.ContactId);

            contact.Name = CustomerContext.CurrentCustomer != null 
                ? CustomerContext.CurrentCustomer.GetFullName() 
                : StringHelper.AggregateStrings(" ", account.LastName, account.FirstName, account.Patronymic);
            contact.City = account.City.IsNotEmpty() ? account.City : ipZone.City;
            contact.District = account.District.IsNotEmpty() ? account.District : ipZone.District;
            contact.Zip = account.Zip ?? string.Empty;

            var country = CountryService.GetCountry(account.CountryId);
            
            var countryId = country?.CountryId ?? ipZone.CountryId;
            var countryName = country?.Name ?? ipZone.CountryName;
            
            if (countryId != 0)
                contact.CountryId = countryId;
            
            if (!string.IsNullOrEmpty(countryName))
                contact.Country = countryName;

            contact.Street = account.Street ?? string.Empty;
            contact.House = account.House ?? string.Empty;
            contact.Apartment = account.Apartment ?? string.Empty;
            contact.Structure = account.Structure ?? string.Empty;
            contact.Entrance = account.Entrance ?? string.Empty;
            contact.Floor = account.Floor ?? string.Empty;
            contact.IsMain = account.IsMain;
            
            if (!string.IsNullOrEmpty(account.Region))
            {
                var regionId = RegionService.GetRegionIdByName(HttpUtility.HtmlDecode(account.Region));
                contact.RegionId = regionId != 0 ? regionId : ipZone.RegionId;
                contact.Region = account.Region.IsNotEmpty() ? HttpUtility.HtmlDecode(account.Region) : ipZone.Region;
            }
            else if (SettingsCheckout.IsShowState && SettingsCheckout.IsRequiredState == false)
            {
                contact.RegionId = null;
                contact.Region = string.Empty;
            }

            if (account.ContactId.IsNullOrEmpty())
            {
                contact.IsMain = true;
                CustomerService.AddContact(contact, CustomerContext.CurrentCustomer.Id);

                var geoModeService = new GeoModeService();
                if (geoModeService.ShowGeoMode())
                    geoModeService.SetCurrentContactId(contact.ContactId);
            }
            else
            {
                CustomerService.UpdateContact(contact);
            }

            return contact;
        }

    }
}