using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Controls;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Core.Services.Shipping.Rules;
using AdvantShop.Core.Services.Taxes;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Repository;
using AdvantShop.Repository.Currencies;
using AdvantShop.Shipping;
using AdvantShop.Shipping.Boxberry;
using AdvantShop.Shipping.Sdek;
using AdvantShop.Shipping.Sdek.Api;
using AdvantShop.Shipping.Yandex;
using AdvantShop.Trial;
using AdvantShop.Web.Admin.Attributes;
using AdvantShop.Web.Admin.Handlers.Settings.ShippingMethods;
using AdvantShop.Web.Admin.Handlers.Settings.ShippingMethods.Warehouse;
using AdvantShop.Web.Admin.Handlers.Settings.ShippingRules;
using AdvantShop.Web.Admin.Models.Settings.ShippingMethods;
using AdvantShop.Web.Admin.Models.Settings.ShippingRules;
using AdvantShop.Web.Infrastructure.Admin;
using AdvantShop.Web.Infrastructure.Admin.ModelBinders;
using AdvantShop.Web.Infrastructure.Admin.ShippingMethods;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;

namespace AdvantShop.Web.Admin.Controllers.Settings
{
    [Auth(RoleAction.Settings)]
    public partial class ShippingMethodsController : BaseAdminController
    {
        private static ConcurrentDictionary<Guid, object> _customersLocks = new ConcurrentDictionary<Guid, object>();
        
        #region List

        public JsonResult GetShippingMethods()
        {
            var listTypes = AdvantshopConfigService.GetDropdownShippings();
            var listTypesModules = ModulesExecuter.GetDropdownShippings();
            var modules = ModulesRepository.GetModulesFromDb();
            Module module;
            var result = new List<AdminShippingMethodItemModel>();

            foreach (var shipping in ShippingMethodService.GetAllShippingMethods())
            {
                var type = listTypes.FirstOrDefault(p => p.Value.Equals(shipping.ShippingType, StringComparison.OrdinalIgnoreCase));
                if (type == null)
                    type = listTypesModules.FirstOrDefault(x => x.Value.Equals(shipping.ShippingType, StringComparison.OrdinalIgnoreCase));

                result.Add(new AdminShippingMethodItemModel()
                {
                    ShippingMethodId = shipping.ShippingMethodId,
                    Name = shipping.Name,
                    ShippingType = type != null ? type.Text : shipping.ShippingType,
                    SortOrder = shipping.SortOrder,
                    Enabled = shipping.Enabled,
                    Icon = ShippingIcons.GetShippingIcon(shipping.ShippingType, shipping.IconFileName?.PhotoName, shipping.Name),
                    WarningMessage = shipping.ModuleStringId.IsNotEmpty()
                        ? (module = modules.FirstOrDefault(x => x.StringId.Equals(shipping.ModuleStringId, StringComparison.OrdinalIgnoreCase))) == null
                            ? "Модуль не установлен"
                            : module.Active == false && shipping.Enabled
                                ? "Модуль деактивирован"
                                : null
                        : type is null
                            ? "Метод не поддерживается"
                            : null
                });
            }

            return Json(result);
        }


        public JsonResult GetTypesList()
        {
            var list = AdvantshopConfigService.GetDropdownShippings().Select(x => 
                new InfoByShippingTypeModel{ Label = x.Text, Value = x.Value, Type = "adv-core"}).ToList();
            list.AddRange(ModulesExecuter.GetDropdownShippings().Select(x => 
                new InfoByShippingTypeModel{ Label = x.Text, Value = x.Value, Type = "exist-module" }));
            list.AddRange(
                ModulesService.GetModuleShippings()
                              .Where(shippingModule =>
                                   list.All(x =>
                                       !string.Equals(x.Value,
                                           shippingModule.ShippingKey,
                                           StringComparison.OrdinalIgnoreCase)))
                              .Select(x => new InfoByShippingTypeModel
                               {
                                   Label = x.Name,
                                   Value = x.ShippingKey,
                                   Type = "not-exist-module",
                                   ModuleId = x.ModuleId,
                                   ModuleStringId = x.ModuleStringId,
                                   ModuleVersion = x.ModuleVersion,
                                   PriceString = x.PriceString,
                                   Price = x.Price
                               })
            );
            return Json(list);
        }


        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ChangeSorting(int id, int? prevId, int? nextId)
        {
            var handler = new ChangeShippingSorting(id, prevId, nextId);
            var result = handler.Execute();

            return Json(new {result = result});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SetEnabled(int id, bool enabled)
        {
            var shipping = ShippingMethodService.GetShippingMethod(id);
            if (shipping == null)
                return Json(new {result = false});

            shipping.Enabled = enabled;

            ShippingMethodService.UpdateShippingMethod(shipping, false);

            return Json(new {result = true});
        }

        #endregion

        #region Add / Edit / Delete

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddShippingMethod(string name, string type, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { result = false });

            try
            {
                var method = new ShippingMethod
                {
                    ShippingType = type,
                    Name = name.Trim(),
                    Description = description ?? "",
                    Enabled = type == "FreeShipping",
                    DisplayCustomFields = true,
                    ZeroPriceMessage = T("Admin.ShippingMethods.ZeroPriceMessage"),
                    TaxId = null,
                    PaymentMethodType = ePaymentMethodType.full_prepayment,
                    PaymentSubjectType = ePaymentSubjectType.payment,
                    ShowInDetails = true
                };

                var moduleOfShipping = AttachedModules.GetModules<Core.Modules.Interfaces.IShippingMethod>(ignoreActive: true)
                        .Where(module => module != null)
                        .Select(module => (Core.Modules.Interfaces.IShippingMethod)Activator.CreateInstance(module))
                        .FirstOrDefault(module => module.ShippingKey.Equals(type, StringComparison.OrdinalIgnoreCase));

                var advShippingType = AdvantshopConfigService.GetDropdownShippings(onlyActive: false)
                    .FirstOrDefault(p => p.Value.Equals(type, StringComparison.OrdinalIgnoreCase));

                if (moduleOfShipping != null && advShippingType != null)
                    return Json(new { result = false, error = "Дублирующий тип доставки." });

                if (moduleOfShipping == null && advShippingType == null)
                    return Json(new { result = false, error = "Тип доставки не определен." });

                method.ModuleStringId = moduleOfShipping?.ModuleStringId;

                TrialService.TrackEvent(TrialEvents.AddShippingMethod, method.ShippingType);
                Track.TrackService.TrackEvent(Track.ETrackEvent.Core_Settings_ShippingMethodCreated, method.ShippingType);

                BaseShipping shipping = null;
                
                var shippingType = ReflectionExt.GetTypeByAttributeValue<ShippingKeyAttribute>(
                    typeof(BaseShipping),
                    atr => atr.Value, method.ShippingType);
                
                if (shippingType != null)
                    shipping = (BaseShipping) Activator.CreateInstance(shippingType, method, null);

                if (shipping != null)
                {
                    method.TypeOfDelivery = shipping.TypeOfDelivery;
                    
                    if (method.UseCurrency)
                    {
                        var currencyIso3Available = shipping.CurrencyIso3Available ?? new string[] { };
                        var selectCurrency =
                            CurrencyService.GetAllCurrencies()
                                // рубли в приоритете
                                .OrderBy(x => string.Equals(x.Iso3, "RUB", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                                .FirstOrDefault(x => shipping.CurrencyAllAvailable || currencyIso3Available.Contains(x.Iso3, StringComparer.OrdinalIgnoreCase));
                        if (selectCurrency != null)
                            method.CurrencyId = selectCurrency.CurrencyId;
                    }
                }

                var methodId = ShippingMethodService.InsertShippingMethod(method);

                if (method.ShippingType == AttributeHelper.GetAttributeValue<ShippingKeyAttribute, string>(typeof(Boxberry)))
                    Track.TrackService.TrackEvent(Track.ETrackEvent.Core_Settings_BoxberryShippingMethodCreated);

                return Json(new { result = true, id = methodId });
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }

            return Json(new { result = false });
        }

        public ActionResult Edit(int id)
        {
            var model = ShippingMethodService.GetShippingMethodAdminModel(id);
            if (model == null)
                return Error404();
            
            SetMetaInformation(T("Admin.ShippingMethods.Edit.Title") + " - " + model.Name);
            SetNgController(NgControllers.NgControllersTypes.ShippingMethodCtrl);

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([ModelBinder(typeof(ModelTypeBinder))] ShippingMethodAdminModel model)
        {
            var method = ShippingMethodService.GetShippingMethod(model.ShippingMethodId);
            if (method == null)
            {
                ShowMessage(NotifyType.Error, T("Admin.Settings.MethodNotFound"));
                return RedirectToAction("Edit", new { id = model.ShippingMethodId });
            }

            foreach (var key in new List<string>() {"BaseExtracharge", "ExtraDeliveryTime"})
            {
                if (ModelState.ContainsKey(key))
                    ModelState[key].Errors.Clear();
            }

            if (!ModelState.IsValid)
            {
                ShowErrorMessages();
                return RedirectToAction("Edit", new { id = model.ShippingMethodId });
            }
            
            var lockObj = _customersLocks.GetOrAdd(CustomerContext.CustomerId, key => new object());
            lock (lockObj)
            {
                try
                {
                    method.Name = model.Name.DefaultOrEmpty();
                    method.Description = model.Description.DefaultOrEmpty();
                    method.Enabled = model.Enabled;
                    method.SortOrder = model.SortOrder;
                    method.ShowInDetails = model.ShowInDetails;
                    method.DisplayCustomFields = model.DisplayCustomFields;
                    method.DisplayIndex = model.DisplayIndex;
                    method.MoveToEnd = model.MoveToEnd;
                    method.ShowIfNoOtherShippings = model.ShowIfNoOtherShippings;
                    method.ZeroPriceMessage = model.ZeroPriceMessage;
                    method.TaxId = !model.TaxId.HasValue || model.TaxId == 0 ? null : model.TaxId;
                    method.PaymentMethodType = model.PaymentMethodType;
                    method.PaymentSubjectType = model.PaymentSubjectType;
                    method.ExtrachargeInNumbers = model.ExtrachargeInNumbers;
                    method.ExtrachargeInPercents = model.ExtrachargeInPercents;
                    method.ExtrachargeFromOrder = model.ExtrachargeFromOrder;
                    method.ExtraDeliveryTime = model.ExtraDeliveryTime;
                    method.CurrencyId = model.CurrencyId;
                    method.TypeOfDelivery = model.TypeOfDelivery;

                    method.TrackingUrl = model.TrackingUrl;//GlorySoft_027
                    //method.ShippingForTK = model.ShippingForTK;//GlorySoft_030

                    var shippingType =
                        ReflectionExt.GetTypeByAttributeValue<ShippingKeyAttribute>(
                            typeof(BaseShipping), atr => atr.Value, method.ShippingType);
                    var derivedTypeWeight = typeof(BaseShippingWithWeight);
                    var derivedTypeCargo = typeof(BaseShippingWithCargo);
                    if (derivedTypeWeight.IsAssignableFrom(shippingType))
                    {
                        if (model.Params.ContainsKey(DefaultWeightParams.DefaultWeight))
                            model.Params[DefaultWeightParams.DefaultWeight] = model.BaseDefaultWeight.ToInvariantString();
                        else
                            model.Params.Add(DefaultWeightParams.DefaultWeight, model.BaseDefaultWeight.ToInvariantString());

                        if (model.Params.ContainsKey(DefaultWeightParams.ExtrachargeWeight))
                            model.Params[DefaultWeightParams.ExtrachargeWeight] = model.WeightExtracharge.ToInvariantString();
                        else
                            model.Params.Add(DefaultWeightParams.ExtrachargeWeight, model.WeightExtracharge.ToInvariantString());

                        if (model.Params.ContainsKey(DefaultWeightParams.ExtrachargeTypeWeight))
                            model.Params[DefaultWeightParams.ExtrachargeTypeWeight] = ((int) model.WeightExtrachargeType).ToString();
                        else
                            model.Params.Add(DefaultWeightParams.ExtrachargeTypeWeight, ((int) model.WeightExtrachargeType).ToString());
                    }

                    if (derivedTypeCargo.IsAssignableFrom(shippingType))
                    {
                        if (model.Params.ContainsKey(DefaultCargoParams.DefaultLength))
                            model.Params[DefaultCargoParams.DefaultLength] = model.BaseDefaultLength.ToInvariantString();
                        else
                            model.Params.Add(DefaultCargoParams.DefaultLength, model.BaseDefaultLength.ToInvariantString());

                        if (model.Params.ContainsKey(DefaultCargoParams.DefaultWidth))
                            model.Params[DefaultCargoParams.DefaultWidth] = model.BaseDefaultWidth.ToInvariantString();
                        else
                            model.Params.Add(DefaultCargoParams.DefaultWidth, model.BaseDefaultWidth.ToInvariantString());

                        if (model.Params.ContainsKey(DefaultCargoParams.DefaultHeight))
                            model.Params[DefaultCargoParams.DefaultHeight] = model.BaseDefaultHeight.ToInvariantString();
                        else
                            model.Params.Add(DefaultCargoParams.DefaultHeight, model.BaseDefaultHeight.ToInvariantString());

                        if (model.Params.ContainsKey(DefaultCargoParams.ExtrachargeCargo))
                            model.Params[DefaultCargoParams.ExtrachargeCargo] = model.CargoExtracharge.ToInvariantString();
                        else
                            model.Params.Add(DefaultCargoParams.ExtrachargeCargo, model.CargoExtracharge.ToInvariantString());

                        if (model.Params.ContainsKey(DefaultCargoParams.ExtrachargeTypeCargo))
                            model.Params[DefaultCargoParams.ExtrachargeTypeCargo] = ((int) model.CargoExtrachargeType).ToString();
                        else
                            model.Params.Add(DefaultCargoParams.ExtrachargeTypeCargo, ((int) model.CargoExtrachargeType).ToString());
                    }

                    method.Params = model.Params;

                    ShippingMethodService.UpdateShippingMethod(method);

                    var allPayments = 
                        ShippingMethodService.GetPayments(method.ShippingMethodId).Select(x => x.PaymentMethodId);
                    var selectedPayments = 
                        model.Payments.Trim(new[] {'[', ']'}).Split(',').Select(x => x.TryParseInt());

                    var payments = allPayments.Where(x => !selectedPayments.Contains(x)).ToList();

                    ShippingMethodService.UpdateShippingPayments(method.ShippingMethodId, payments);
                    
                    Track.TrackService.TrackEvent(Track.ETrackEvent.Core_Settings_ShippingMethodEdited, method.ShippingType);

                    ShowMessage(NotifyType.Success, T("Admin.Settings.ChangesSavedSuccessfully"));
                }
                catch (Exception ex)
                {
                    Debug.Log.Error(ex);
                }
            }
            
            return RedirectToAction("Edit", new {id = model.ShippingMethodId});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteMethod(int methodId)
        {
            var shipping = ShippingMethodService.GetShippingMethod(methodId);
            if (shipping != null)
                ShippingMethodService.DeleteShippingMethod(methodId);
            
            return Json(new {result = true});
        }


        #region Icon

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult UploadIcon(int methodId)
        {
            var handler = new UploadShippingMethodIcon(methodId);
            var result = handler.Execute();

            return Json(result);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteIcon(int methodId)
        {
            var method = ShippingMethodService.GetShippingMethod(methodId);
            if (method == null)
                return Json(new { result = false });

            PhotoService.DeletePhotos(method.ShippingMethodId, PhotoType.Shipping);

            return Json(new { result = true });
        }

        #endregion

        #region Countries and Cities

        [HttpGet]
        public JsonResult GetAvailableLocations(int methodId)
        {
            var countries = ShippingPaymentGeoMaping.GetCountryByShippingId(methodId);
            var regions = ShippingPaymentGeoMaping.GetRegionsByShippingId(methodId);
            var cities = ShippingPaymentGeoMaping.GetCityByShippingId(methodId);

            return Json(new
            {
                countries = countries.Select(x => new {x.CountryId, x.Name}),
                regions = regions.Select(x => new
                {
                    x.RegionId,
                    Name = x.Region + (x.CountryName.IsNotEmpty() ? string.Format(" ({0})", x.CountryName) : string.Empty)
                }),
                cities = cities.Select(x => new
                {
                    x.CityId,
                    Name = x.City + (x.District.IsNotEmpty() ? string.Format(", {0}", x.District) : string.Empty) + (x.Region.IsNotEmpty() ? string.Format(" ({0})", x.Region) : string.Empty)
                }),
            });
        }

        [HttpGet]
        public JsonResult GetExcludedLocations(int methodId)
        {
            var cities = ShippingPaymentGeoMaping.GetCityByShippingIdExcluded(methodId);
            var regions = ShippingPaymentGeoMaping.GetRegionsByShippingIdExcluded(methodId);
            var country = ShippingPaymentGeoMaping.GetCountryByShippingIdExcluded(methodId);

            return Json(new
            {
                country = country.Select(x => new { x.CountryId, x.Name }),
                regions = regions.Select(x => new
                {
                    x.RegionId,
                    Name = x.Region + (x.CountryName.IsNotEmpty() ? string.Format(" ({0})", x.CountryName) : string.Empty)
                }),
                cities = cities.Select(x => new
                {
                    x.CityId,
                    Name = x.City + (x.District.IsNotEmpty() ? string.Format(", {0}", x.District) : string.Empty) + (x.Region.IsNotEmpty() ? string.Format(" ({0})", x.Region) : string.Empty)
                }),
            });
        }

        [HttpGet]
        public JsonResult GetExcludedByCatalog(int methodId)
        {
            var countProducts = ShippingCatalogMaping.GetCountExcludedProductsByShipping(methodId);
            var categories = ShippingCatalogMaping.GetCategoriesIDsByShipping(methodId);

            return Json(new
            {
                countProducts = countProducts,
                categories = categories,
            });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SaveExcludedCategories(int methodId, List<int> excludedCategories)
        {
            ShippingCatalogMaping.DeleteAllCategoriesLinkFromShipping(methodId);

            if (excludedCategories != null && excludedCategories.Count > 0)
                excludedCategories.Distinct().ForEach(id => ShippingCatalogMaping.AddCategoryLinkToShipping(methodId, id));

            return Json(new {result = true});
        }


        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddAvailableCountry(int methodId, string countryName)
        {
            var country = CountryService.GetCountryByName(countryName);
            if (country == null)
                return Json(new { result = false });

            if (!ShippingPaymentGeoMaping.IsExistShippingCountry(methodId, country.CountryId))
                ShippingPaymentGeoMaping.AddShippingCountry(methodId, country.CountryId);

            return Json(new {result = true});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddAvailableRegion(int methodId, string regionName, int? regionId)
        {
            var region = regionId.HasValue ? RegionService.GetRegion(regionId.Value) : RegionService.GetRegionByName(regionName);
            if (region == null)
                return Json(new { result = false });

            if (!ShippingPaymentGeoMaping.IsExistShippingRegion(methodId, region.RegionId))
                ShippingPaymentGeoMaping.AddShippingRegion(methodId, region.RegionId);

            return Json(new {result = true});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddAvailableCity(int methodId, string cityName, int? cityId)
        {
            var city = cityId.HasValue ? CityService.GetCity(cityId.Value) : CityService.GetCityByName(cityName);
            if (city == null)
                return Json(new { result = false });

            if (!ShippingPaymentGeoMaping.IsExistShippingCity(methodId, city.CityId))
                ShippingPaymentGeoMaping.AddShippingCity(methodId, city.CityId);

            return Json(new {result = true});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddExcludedCity(int methodId, string cityName, int? cityId)
        {
            var city = cityId.HasValue ? CityService.GetCity(cityId.Value) : CityService.GetCityByName(cityName);
            if (city == null)
                return Json(new { result = false });

            if (!ShippingPaymentGeoMaping.IsExistShippingCityExcluded(methodId, city.CityId))
                ShippingPaymentGeoMaping.AddShippingCityExcluded(methodId, city.CityId);

            return Json(new {result = true});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddExcludedRegion(int methodId, string regionName, int? regionId)
        {
            var region = regionId.HasValue ? RegionService.GetRegion(regionId.Value) : RegionService.GetRegionByName(regionName);
            if (region == null)
                return Json(new { result = false });

            if (!ShippingPaymentGeoMaping.IsExistShippingRegionExcluded(methodId, region.RegionId))
                ShippingPaymentGeoMaping.AddShippingRegionExcluded(methodId, region.RegionId);

            return Json(new {result = true});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddExcludedCountry(int methodId, string countryName)
        {
            var country = CountryService.GetCountryByName(countryName);
            if (country == null)
                return Json(new { result = false });

            if (!ShippingPaymentGeoMaping.IsExistShippingCountryExcluded(methodId, country.CountryId))
                ShippingPaymentGeoMaping.AddShippingCountryExcluded(methodId, country.CountryId);

            return Json(new { result = true });
        }


        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteAvailableCountry(int methodId, int countryId)
        {
            ShippingPaymentGeoMaping.DeleteShippingCountry(methodId, countryId);
            return Json(new { result = true });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteAvailableRegion(int methodId, int regionId)
        {
            ShippingPaymentGeoMaping.DeleteShippingRegion(methodId, regionId);
            return Json(new { result = true });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteExcludedRegion(int methodId, int regionId)
        {
            ShippingPaymentGeoMaping.DeleteShippingRegionExcluded(methodId, regionId);
            return Json(new { result = true });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteAvailableCity(int methodId, int cityId)
        {
            ShippingPaymentGeoMaping.DeleteShippingCity(methodId, cityId);
            return Json(new { result = true });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteExcludedCity(int methodId, int cityId)
        {
            ShippingPaymentGeoMaping.DeleteShippingCityExcluded(methodId, cityId);
            return Json(new { result = true });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteExcludedCountry(int methodId, int CountryId)
        {
            ShippingPaymentGeoMaping.DeleteShippingCountryExcluded(methodId, CountryId);
            return Json(new { result = true });
        }

        #endregion

        [HttpGet]
        public JsonResult GetPayments(int methodId)
        {
            var items = ShippingMethodService.GetPayments(methodId);
            return Json(items);
        }

        #region Warehouse

        public JsonResult GetWarehouses(int methodId)
        {
            return ProcessJsonResult(new GetWarehousesHandler(methodId));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddWarehouses(int methodId, List<int> warehouseIds)
        {
            return ProcessJsonResult(new AddWarehousesHandler(methodId, warehouseIds));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteWarehouse(int methodId, int warehouseId)
        {
            return ProcessJsonResult(new DeleteWarehouseHandler(methodId, warehouseId));
        }

        #endregion

        #endregion

        #region Shippings
        
        #region Sdek

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CallSdekCourier(SdekCallCourierModel model)
        {
            if (ModelState.IsValid)
            {
                var dict = new Dictionary<string, string>
                {
                    {SdekTemplate.DefaultCourierCity, model.DefaultCourierCity},
                    {SdekTemplate.DefaultCourierPhone, model.DefaultCourierPhone},
                    {SdekTemplate.DefaultCourierStreet, model.DefaultCourierStreet},
                    {SdekTemplate.DefaultCourierHouse, model.DefaultCourierHouse},
                    {SdekTemplate.DefaultCourierFlat, model.DefaultCourierFlat},
                    {SdekTemplate.DefaultCourierNameContact, model.DefaultCourierNameContact},
                };

                ShippingMethodService.UpdateShippingParams(model.MethodId, dict);

                var date = Convert.ToDateTime(model.Date);
                if (date < DateTime.Now.Date)
                {
                    return Json(new { result = false, msg = new List<string> {"Неверная дата доставки" } });
                }

                var method = ShippingMethodService.GetShippingMethod(model.MethodId);

                var sdek = new Sdek(method, null);
                var result = sdek.SdekApiService20.Intake(new IntakeParams
                {
                    IntakeDate = date,
                    IntakeTimeFrom = model.TimeFrom,
                    IntakeTimeTo = model.TimeTo,
                    Name = model.Comment,
                    Weight = (long) model.Weight,
                    Sender = new SenderIntake
                    {
                        Name = model.DefaultCourierNameContact,
                        Phones = new List<Phone>()
                        {
                            new Phone() {Number = model.DefaultCourierPhone}
                        }
                    },
                    FromLocation = new OrderLocation()
                    {
                        City = model.DefaultCourierCity,
                        Address =
                            $"{model.DefaultCourierStreet} {model.DefaultCourierHouse}, {model.DefaultCourierFlat}"
                    }
                });

                if (result?.Requests?.First().State != "INVALID")
                    return Json(new
                    {
                        result = true,
                        msg = T("Admin.Settings.ApplicationSent")
                    });

                return Json(new
                {
                    result = false,
                    msg = T("Admin.Settings.ApplicationSent") +
                          (result?.Requests?.First().Errors != null
                              ? string.Join("\\ ", result.Requests.First().Errors.Select(x => x.Message))
                              : null)
                });
            }

            var errors = new List<string>();
            foreach (var modelState in ViewData.ModelState.Values)
                foreach (var error in modelState.Errors)
                    errors.Add(error.ErrorMessage);

            return Json(new { result = false, msg = errors });
        }

        public JsonResult FindCityForSdek(string q)
        {
            if (q.IsNullOrEmpty())
                return Json(null);

            List<SdekCity> response;
            if (q.Length <= 3)
            {
                response = GetSdekCities(q);
            }
            else
            {
                var cityCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                var advCities = IpZoneService.GetIpZonesByCity(q)
                                             .Select(x => x.City)
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .ToList();
                response = new List<SdekCity>();
                foreach (var advCity in advCities) 
                    FillResponse(GetSdekCities(advCity), cityCodes);
                
                FillResponse(GetSdekCities(q), cityCodes);
            }
            
            if (response != null && response.Count > 0)
            {
                response = response
                           // более крупные города перыми ставим (у них собственный район)
                          .OrderBy(x => x.SubRegion.IsNotEmpty() && x.SubRegion.Contains(x.CityName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                          .ThenBy(x => x.CityCode.Length)
                          .ThenBy(x => x.CityCode)
                          .ToList();
                
                // удаляем SubRegion, для городов отличающихся только по SubRegion
                foreach (var sdekCity in response)
                {
                    if (sdekCity.SubRegion.IsNullOrEmpty())
                        continue;
                    
                    if (!response.Any(x =>
                            x.CityCode != sdekCity.CityCode
                            && x.CityName == sdekCity.CityName
                            && x.RegionCode == sdekCity.RegionCode
                            && x.CountryCode == sdekCity.CountryCode)) 
                        sdekCity.SubRegion = null;
                }
            }
            return Json(response);

            List<SdekCity> GetSdekCities(string cityName)
            {
                var sdekCities = cityName.IsNotEmpty()
                    ? Core.Services.Helpers.RequestHelper.MakeRequest<List<SdekCity>>(
                        $"{LinkService.ExternalApi.IntegrationCdek}/v1/location/cities/json?countrycode=AZ,AM,BY,KZ,KG,MD,RU,TJ,UZ&cityName=" +
                        System.Web.HttpUtility.UrlEncode(cityName),
                        method: Core.Services.Helpers.ERequestMethod.GET,
                        contentType: Core.Services.Helpers.ERequestContentType.TextJson,
                        timeoutSeconds: 5)
                    : null;
                return sdekCities;
            }

            void FillResponse(List<SdekCity> list, HashSet<string> cityCodes)
            {
                if (list is null
                    || list.Count == 0)
                    return;

                foreach (var sdekCity in list)
                {
                    if (!cityCodes.Add(sdekCity.CityCode))
                        continue;
                    response.Add(sdekCity);
                }
            }
        }
        #endregion

        #region Pec

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult FindCityForPec(string q, string login, string apiKey)
        {
            if (login.IsNullOrEmpty())
                return JsonError("Укажите логин");
            if (apiKey.IsNullOrEmpty())
                return JsonError("Укажите ключ API");
            
            var pecApi = new Shipping.Pec.Api.PecApiService(login, apiKey);
            var findResult = q.IsNotEmpty()
                ? pecApi.FindBranchesByTitle(q)
                : null;
            
            return JsonOk(findResult?.Result?.Items);
        }


        #endregion Pec

        #region Yandex

        public JsonResult FindCityForYandex(int shippingMethodId, string q)
        {
            if (q.IsNullOrEmpty() || q.Length <= 2)
                return Json(null);

            var method = ShippingMethodService.GetShippingMethod(shippingMethodId);
            if (method == null)
                return null;

            var yandex = new YandexDelivery(method, null);
            var result = yandex.YandexDeliveryApiService.GetCityGeoId(q);
            return Json(result?.Variants?.Take(10).Select(x => new SelectListItem
            {
                Text = x.Address,
                Value = x.GeoId.ToString()
            }));
        }

        #endregion

        #endregion Shippings


        #region FilterProducts

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SaveExcludedProducts(int methodId, List<int> excludedProducts)
        {
            ShippingCatalogMaping.DeleteAllExcludedProductsLinkFromShipping(methodId);

            if (excludedProducts != null && excludedProducts.Count > 0)
                excludedProducts.Distinct().ForEach(id => ShippingCatalogMaping.AddExcludedProductLinkToShipping(methodId, id));

            return Json(new {result = true});
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ResetExcludedProducts(int methodId)
        {
            ShippingCatalogMaping.DeleteAllExcludedProductsLinkFromShipping(methodId);
            return Json(new {result = true});
        }
        
        public JsonResult GetShippingProducts(ShippingProductsFilterModel model)
        {
            return Json(new GetShippingProducts(model).Execute());
        }
        
        private void Command(ShippingProductsFilterModel command, Action<int, ShippingProductsFilterModel> func)
        {
            var exceptions = new ConcurrentQueue<Exception>();

            if (command.SelectMode == SelectModeCommand.None)
            {
                Parallel.ForEach(command.Ids, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (id) =>
                {
                    try
                    {
                        func(id, command);
                    }
                    catch (Exception e)
                    {
                        exceptions.Enqueue(e);
                    }
                });
            }
            else
            {
                var ids = new GetShippingProducts(command).GetItemsIds<int>("[Product].[ProductID]");

                Parallel.ForEach(ids, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (id) =>
                {
                    try
                    {
                        if (command.Ids == null || !command.Ids.Contains(id))
                            func(id, command);
                    }
                    catch (Exception e)
                    {
                        exceptions.Enqueue(e);
                    }
                });
            }

            if (exceptions.Any())
            {
                Debug.Log.Error(exceptions.AggregateString("<br/>^^^<br/>"));
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public JsonResult InplaceShippingExcludeProducts(ShippingLinkProductModel model)
        {
            if (model.LinkShipping)
            {
                ShippingCatalogMaping.AddExcludedProductLinkToShipping(model.ShippingId, model.ProductId);
            }
            else
            {
                ShippingCatalogMaping.DeleteExcludedProductLinkFromShipping(model.ShippingId, model.ProductId);
            }
            return JsonOk();
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public JsonResult ShippingExcludeProducts(ShippingProductsFilterModel command)
        {
            Command(command, (id, c) =>
            {
                ShippingCatalogMaping.AddExcludedProductLinkToShipping(command.ShippingId, id);
            });
            return JsonOk();
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public JsonResult ShippingRemoveExcludeProducts(ShippingProductsFilterModel command)
        {
            Command(command, (id, c) =>
            {
                ShippingCatalogMaping.DeleteExcludedProductLinkFromShipping(command.ShippingId, id);
            });
            return JsonOk();
        }

		#endregion FilterProducts

		#region Shipping
		[HttpPost, ValidateJsonAntiForgeryToken]
		public JsonResult TestShippingCalculate(ShippingCalculateModel model)
		{
            try
            {
                var handler = new TestShippingCalculate(model);
                var result = handler.Execute();
                return Json(result);
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                return JsonError(ex.Message);
            }
		}
        
        #endregion

        #region Rules

        public JsonResult GetRuleItems(RuleFilterModel model)
        {
            return Json(new GetRuleItems(model).Execute());
        }

        #region Commands

        private void Command(RuleFilterModel model, Action<int, RuleFilterModel> func)
        {
            if (model.SelectMode == SelectModeCommand.None)
            {
                foreach (var id in model.Ids) 
                    func(id, model);
            }
            else
            {
                var handler = new GetRuleItems(model);
                var ids = handler.GetItemsIds<int>();

                foreach (int id in ids)
                {
                    if (model.Ids == null || !model.Ids.Contains(id))
                        func(id, model);
                }
            }
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteRules(RuleFilterModel model) 
            => ProcessJsonResult(
                () => Command(model, (id, c) => RuleRepository.Delete(id)));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ActivateRules(RuleFilterModel model)
            => ProcessJsonResult(
                () => Command(model, (id, c) => RuleRepository.SetActive(id, true)));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DisableRules(RuleFilterModel model)
            => ProcessJsonResult(
                () => Command(model, (id, c) => RuleRepository.SetActive(id, false)));

        #endregion

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteRule(int id)
            => ProcessJsonResult(
                () => RuleRepository.Delete(id));

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult InplaceRule(RuleGridModel model)
        {
            return ProcessJsonResult(() =>
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                    throw new BlException(T("Admin.ShippingRules.Validation.NameIsNotFilledIn"), nameof(model.Name));

                var ruleDto = RuleRepository.Get(model.Id);

                ruleDto.Name = model.Name;
                ruleDto.Enabled = model.Enabled;
                ruleDto.SortOrder = model.SortOrder;

                RuleRepository.Update(ruleDto);
            });
        }

        public JsonResult GetRule(int id)
            => ProcessJsonResult(new GetRule(id));

        public JsonResult GetRuleFormData()
        {
            var shippingTypes = GetTypesList().Data;
            return ProcessJsonResult(new GetRuleFormData(shippingTypes));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddRule(RuleModel model)
        {
            if (!ModelState.IsValid)
                return JsonError();
            
            return ProcessJsonResult(new AddUpdateRule(model));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult UpdateRule(RuleModel model)
        {
            if (!ModelState.IsValid)
                return JsonError();
            
            return ProcessJsonResult(new AddUpdateRule(model));
        }

        #endregion Rules
    }
}
