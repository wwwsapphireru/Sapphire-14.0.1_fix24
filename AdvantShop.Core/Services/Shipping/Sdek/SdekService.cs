using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AdvantShop.Configuration;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Helpers;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Orders;
using AdvantShop.Shipping.Sdek.Api;
using Pyrus.ApiClient.Requests.Builders;
using PyrusApiClient;

namespace AdvantShop.Shipping.Sdek
{
    public class SdekService
    {
        public static int? GetSdekCityId(string cityName, string district, string regionName, string country, SdekApiService20 service20, out CityInfo city)
        {
            city = null;
            int cityId = 0;
            string cacheCityIdKey = "sdek_CityIdByName_" + (cityName + "_" + district + "_" + regionName + "_" + country).GetHashCode();
            if (!CacheManager.TryGetValue<int>(cacheCityIdKey, out cityId))
            {
                var countryIso2 = country.IsNotEmpty() ? Repository.CountryService.GetIso2(country) : null;

                // новый алгоритм только по городу плохо ищет, нужен регион для точности
                if (regionName.IsNotEmpty() && service20 != null)
                    cityId = GetCityByNameV20(service20, cityName, district, regionName, country, countryIso2, out city) ?? 0;

                if (cityId == 0 && regionName.IsNotEmpty())
                    cityId = GetCityByNameV15(cityName, district, regionName, country, countryIso2) ?? 0;

                CacheManager.Insert<int>(cacheCityIdKey, cityId, 1440);
            }

            return cityId == 0 ? null : (int?)cityId;
        }

        private static string CityCrutch(string cityName)
        {
            return cityName.Replace("ё", "е", StringComparison.OrdinalIgnoreCase);
        }

        private static string RegionCrutch(string regionName)
        {
            return regionName.Replace("ё", "е", StringComparison.OrdinalIgnoreCase);
        }

        private static int? GetCityByNameV20(SdekApiService20 sdekApiService20, string cityName, string district,
            string regionName, string country, string countryIso2, out CityInfo city)
        {
            int cityId = 0;

            var newRegionName = regionName.RemoveTypeFromRegion() ?? string.Empty;
            var newCountry = country ?? string.Empty;
            var newDistrict = district ?? string.Empty;

            var response = sdekApiService20.GetCities(
                new CitiesFilter
                {
                    City = cityName,
                    CountryCodes = countryIso2.IsNotEmpty()
                        ? new List<string>() {countryIso2}
                        : null
                });
            if (response != null && response.Count > 0)
            {
                var cities = response
                    .OrderBy(x => x.Code)
                    .ToList();

                cities.ForEach(x => {
                    x.Region = (x.Region.RemoveTypeFromRegion() ?? string.Empty);
                    x.SubRegion = x.SubRegion ?? string.Empty;
                });

                // в регионе тоже проблемы с буквой Ё
                var regionNameCrutch = RegionCrutch(newRegionName);

                city =
                    FindCity(cities, newDistrict, newRegionName, regionNameCrutch, newCountry) // по всем данным
                    ?? FindCity(cities, null, newRegionName, regionNameCrutch, newCountry) // без района
                    ?? FindCity(cities, null, newRegionName, regionNameCrutch, null); // без района и страны
                    //?? FindCity(cities, null, null, null, null); // берем первый (не стоит, может считать не туда)

                if (city != null)
                {
                    cityId = city.Code;
                    return cityId;
                }
            }

            city = null;
            return null;        
        }

        private static CityInfo FindCity(IEnumerable<CityInfo> cities, string district, string region, string regionCrutch, string country)
        {
            if (district.IsNotEmpty())
                cities = cities.Where(x => x.SubRegion.Contains(district, StringComparison.OrdinalIgnoreCase));
            if (region.IsNotEmpty() || regionCrutch.IsNotEmpty())
                cities = cities.Where(x => x.Region.Equals(region, StringComparison.OrdinalIgnoreCase) || x.Region.Equals(regionCrutch, StringComparison.OrdinalIgnoreCase));
            if (country.IsNotEmpty())
                cities = cities.Where(x => x.Country.Contains(country, StringComparison.OrdinalIgnoreCase));

            return cities.FirstOrDefault();
        }

        private static int? GetCityByNameV15(string cityName, string district, string regionName, string country, string countryIso2)
        {
            int cityId = 0;

            var newRegionName = regionName.RemoveTypeFromRegion() ?? string.Empty;
            var newCountry = country ?? string.Empty;
            var newDistrict = district ?? string.Empty;

            var response = RequestHelper.MakeRequest<List<SdekCity>>(
                $"{LinkService.ShippingMethod.Sdek.GetCity}?cityName={HttpUtility.UrlEncode(cityName)}&countryCode={HttpUtility.UrlEncode(countryIso2)}",
                method: ERequestMethod.GET,
                contentType: ERequestContentType.TextJson,
                timeoutSeconds: 5);
            if (response != null && response.Count > 0)
            {
                var cities = response
                    .OrderBy(x => x.CityCode.Length)
                    .ThenBy(x => x.CityCode)
                    .ToList();

                cities.ForEach(x => {
                    x.Region = (x.Region.RemoveTypeFromRegion() ?? string.Empty);
                    x.SubRegion = x.SubRegion ?? string.Empty;
                });

                // в регионе тоже проблемы с буквой Ё
                var regionNameCrutch = RegionCrutch(newRegionName);

                var city =
                    FindCity(cities, newDistrict, newRegionName, regionNameCrutch, newCountry) // по всем данным
                    ?? FindCity(cities, null, newRegionName, regionNameCrutch, newCountry) // без района
                    ?? FindCity(cities, null, newRegionName, regionNameCrutch, null); // без района и страны
                    //?? FindCity(cities, null, null, null, null); // берем первый (не стоит, может считать не туда)

                if (city != null)
                {
                    cityId = city.CityCode.TryParseInt();
                    return cityId;
                }
            }
            return null;
        }

        private static SdekCity FindCity(IEnumerable<SdekCity> cities, string district, string region, string regionCrutch, string country)
        {
            if (district.IsNotEmpty())
                cities = cities.Where(x => x.SubRegion.Contains(district, StringComparison.OrdinalIgnoreCase));
            if (region.IsNotEmpty() || regionCrutch.IsNotEmpty())
                cities = cities.Where(x => x.Region.Equals(region, StringComparison.OrdinalIgnoreCase) || x.Region.Equals(regionCrutch, StringComparison.OrdinalIgnoreCase));
            if (country.IsNotEmpty())
                cities = cities.Where(x => x.Country.Contains(country, StringComparison.OrdinalIgnoreCase));

            return cities.FirstOrDefault();
        }

        private static SdekCityByTerm FindCity(IEnumerable<SdekCityByTerm> cities, string district, string region, string regionCrutch, string country)
        {
            if (district.IsNotEmpty())
                cities = cities.Where(x => x.Name.Contains(district, StringComparison.OrdinalIgnoreCase));
            if (region.IsNotEmpty() || regionCrutch.IsNotEmpty())
                cities = cities.Where(x => x.RegionName.Equals(region, StringComparison.OrdinalIgnoreCase) || x.RegionName.Equals(regionCrutch, StringComparison.OrdinalIgnoreCase));
            if (country.IsNotEmpty())
                cities = cities.Where(x => x.CountryName.Contains(country, StringComparison.OrdinalIgnoreCase));

            return cities.FirstOrDefault();
        }

        #region GlorySoft_001

        private const string _pyrusLogin = "advanta@sapphire.ru";//"info@glorysoft.ru";
        private const string _pyrusApiKey = "LBj52WHzQzhCVku4BQD7wsktEY-10WQDwZTb6cmpIEtGkTtWbiBn~Ett5npr~lBwKQTpmJvRd~Fhvdm1zvtsknbQaOl2QX2l";//"m-gbdygnNy3-91s6jAJXECU~go1uxcn9fGvHDBJaqGrlgLGVDnzbJBw9h5E0478cMw1uY-iu8bYdhp3E0xGwyuBEenBfZwnk";

        private static async Task<KeyValuePair<PyrusClient, string>> Auth()
        {
            var pyrusClient = new PyrusClient();
            var response = await pyrusClient.Auth(_pyrusLogin, _pyrusApiKey);
            if (response.Success)
                return new KeyValuePair<PyrusClient, string>(pyrusClient, null);
            else
                return new KeyValuePair<PyrusClient, string>(null, response.Error);
        }

        public static async void /*Task<KeyValuePair<TaskWithComments, string>>*/ CreateFormTaskWrongTrack(int formId, string orderNumber, string trackNumber, int orderId)
        {
            if (OrderService.GetOrderAdditionalData(orderId, SdekTemplate.KeyNameWrongTrackFormTaskAdditionalData).IsNotEmpty())
                return;

            var authResponse = await Auth();
            if (authResponse.Key == null)
                return;// new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            //var catalogResponse = await RequestBuilder.GetCatalog(253192).Process(pyrusClient);
            //var items = catalogResponse.Items;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "номер заказа" || field.Info?.Code == "OrderNumber")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, orderNumber));
                else if (field.Name == "метод доставки" || field.Info?.Code == "ShippingType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 2, "СДЭК"));
                else if (field.Name == "ошибочный трек-номер" || field.Info?.Code == "WrongTrack")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, trackNumber));
            }
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
            {
                OrderService.AddUpdateOrderAdditionalData(
                    orderId,
                    SdekTemplate.KeyNameWrongTrackFormTaskAdditionalData,
                    taskResponse.Task.Id.ToString());
                //return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            }
            else
            {
                MailService.SendMailNow(Guid.Empty, SettingsMail.EmailForProductDiscuss, "Некорректный трек-номер", $"Некорректный трек-номер по заказу {orderNumber} - <a href='https://www.pochta.ru/tracking?barcode={trackNumber}'>{trackNumber}</a>", true);
                //return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
            }
        }

        private static FormFieldText SetFormFieldText(FormFieldText field, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        private static FormFieldMultipleChoice SetFormFieldMultipleChoice(FormFieldMultipleChoice field, int id, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = new MultipleChoice { ChoiceIds = new int[] { id }, ChoiceNames = new string[] { value } };
            return f;
        }

        #endregion

    }
}
