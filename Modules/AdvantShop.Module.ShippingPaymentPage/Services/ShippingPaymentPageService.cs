using AdvantShop.Configuration;
using AdvantShop.Core.Common;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Orders;
using AdvantShop.Repository;
using AdvantShop.Repository.Currencies;
using AdvantShop.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
//GlorySoft_008
namespace AdvantShop.Module.ShippingPaymentPage.Services
{
    public class ShippingPaymentPageService
    {
        public static List<BaseShippingOption> AvailableShippingOptions(CheckoutAddress contact, List<PreOrderItem> preorderList = null)
        {
            var Cart = ShoppingCartService.CurrentShoppingCart;
            var ignoreCartItems = ModulesExecuter.GetIgnoreShippingCartItems();
            if (ignoreCartItems.Count > 0)
            {
                foreach (var item in ignoreCartItems)
                    Cart.Remove(item);
            }

            var zone = GetZone(contact.City, null, null, contact.Region, contact.Country, contact.Zip, contact.District);

            MyCheckout my = new MyCheckout(Cart)
            {
                Data = new CheckoutData
                {
                    Contact = new CheckoutAddress
                    {
                        Country = zone.CountryName,
                        Region = zone.Region,
                        City = zone.City,
                        District = zone.District,
                        Zip = zone.Zip
                    }
                }
            };
            var configuratorShippingCalculation =
                ShippingCalculationConfigurator.Configure()
                                               .ByMyCheckout(my)
                                               .WithCurrency(CurrencyService.CurrentCurrency);
            if (preorderList != null)
                configuratorShippingCalculation
                   .ByShoppingCart(null)
                   .WithPreOrderItems(preorderList);

            var shippingManager = new ShippingManager(configuratorShippingCalculation.Build());
            return shippingManager.GetOptions();
        }

        private static IpZoneModel GetZone(string city, int? cityId, int? countryId, string regionName, string countryName, string zip, string district)
        {
            Country country = null;
            var zone = cityId.HasValue
                ? IpZoneService.GetZoneByCityId(cityId.Value)
                : IpZoneService.GetZoneByCity(city.Trim().ToLower(), countryId);

            if (zone == null)
            {
                var region = regionName.IsNotEmpty()
                    ? RegionService.GetRegionsListByName(regionName.Trim()).FirstOrDefault()
                    : null;
                country = (countryId.HasValue ? CountryService.GetCountry(countryId.Value) : null)
                    ?? (countryName.IsNotEmpty() ? CountryService.GetCountryByName(countryName) : null)
                    ?? (region != null ? CountryService.GetCountry(region.CountryId) : null)
                    ?? CountryService.GetCountry(SettingsMain.SellerCountryId);

                zone = new IpZone()
                {
                    CountryId = country != null ? country.CountryId : SettingsMain.SellerCountryId,
                    CountryName = country != null ? country.Name : countryName,
                    RegionId = region != null ? region.RegionId : 0,
                    Region = region != null ? region.Name : regionName,
                    City = HttpUtility.HtmlEncode(city.Trim()),
                    District = HttpUtility.HtmlEncode(district.DefaultOrEmpty().Trim()),
                    Zip = zip
                };
            }
            else if (!string.IsNullOrEmpty(zip))
            {
                zone.Zip = zip;
            }

            //ModulesExecuter.OnSetZone(zone);

            //IpZoneContext.SetCustomerCookie(zone);
            //if (zone.Region.IsNotEmpty() && zone.City.IsNotEmpty() &&
            //    (country != null || (country = CountryService.GetCountry(zone.CountryId)) != null))
            //{
            //    IpZoneContext.SendGeoIpData(new GeoIpData
            //    {
            //        Country = country.Iso2,
            //        State = zone.Region,
            //        City = zone.City
            //    });
            //}

            var cityObj = cityId.HasValue ? CityService.GetCity(cityId.Value) : CityService.GetCityByName(zone.City);
            var phoneNumber = cityObj != null && cityObj.PhoneNumber.IsNotEmpty() ? cityObj.PhoneNumber : SettingsMain.Phone;
            var mobilePhoneNumber = cityObj != null && cityObj.MobilePhoneNumber.IsNotEmpty() ? cityObj.MobilePhoneNumber : SettingsMain.MobilePhone;

            return new IpZoneModel()
            {
                CountryId = zone.CountryId,
                CountryName = zone.CountryName,
                RegionId = zone.RegionId,
                Region = zone.Region,
                CityId = zone.CityId,
                City = zone.City,
                District = zone.District,
                Phone = phoneNumber,
                MobilePhone = mobilePhoneNumber,
                Zip = zone.Zip
            };
        }

    }
}
