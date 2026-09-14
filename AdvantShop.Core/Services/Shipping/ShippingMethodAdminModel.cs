using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Scheduler;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.Services.Taxes;
using AdvantShop.Repository.Currencies;
using AdvantShop.Shipping;
using AdvantShop.Shipping.ShippingWithInterval;
using AdvantShop.Taxes;
using Newtonsoft.Json;

namespace AdvantShop.Core.Services.Shipping
{
    public class ShippingMethodAdminModel
    {
        public ShippingMethodAdminModel()
        {
            Params = new Dictionary<string, string>();

            Taxes = new List<SelectListItem>() { new SelectListItem() { Text = "Не выбран", Value = "0" } };
            Taxes.AddRange(TaxService.GetTaxes()
                .Select(x => new SelectListItem()
                {
                    Text = x.Name,
                    Value = x.TaxId.ToString()
                }));

            BaseExtrachargeTypes =
                Enum.GetValues(typeof(AdvantShop.Payment.ExtrachargeType))
                    .Cast<AdvantShop.Payment.ExtrachargeType>()
                    .Select(x => new SelectListItem()
                    {
                        Text = x.Localize(),
                        Value = x.ToString()
                    })
                    .ToList();

            PaymentMethodTypes = Enum.GetValues(typeof(ePaymentMethodType))
                .Cast<ePaymentMethodType>()
                .Select(x => new SelectListItem()
                {
                    Text = x.Localize(),
                    Value = x.ConvertIntString()
                })
                .ToList();

            PaymentSubjectTypes = Enum.GetValues(typeof(ePaymentSubjectType))
                .Cast<ePaymentSubjectType>()
                .Select(x => new SelectListItem()
                {
                    Text = x.Localize(),
                    Value = x.ConvertIntString()
                })
                .ToList();
        }

        public int ShippingMethodId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public int SortOrder { get; set; }
        public string ShippingType { get; set; }

        public string ShippingTypeLocalized
        {
            get
            {
                var list = AdvantshopConfigService.GetDropdownShippings();
                var type = list.FirstOrDefault(x => x.Value.Equals(ShippingType, StringComparison.OrdinalIgnoreCase));

                if (type == null)
                    type = ModulesExecuter.GetDropdownShippings().FirstOrDefault(x => x.Value.Equals(ShippingType, StringComparison.OrdinalIgnoreCase));

                return type != null ? type.Text : ShippingType;
            }
        }
        public bool DisplayCustomFields { get; set; }
        public bool DisplayIndex { get; set; }
        public bool ShowInDetails { get; set; }
        public bool MoveToEnd { get; set; }
        public bool ShowIfNoOtherShippings { get; set; }
        public string ZeroPriceMessage { get; set; }
        public string Icon { get; set; }

        public Dictionary<string, string> Params { get; set; }

        public virtual string ModelType => this.GetType().AssemblyQualifiedName;

        public virtual string ShippingViewPath => "~/Areas/Admin/Views/ShippingMethods/_" + ShippingType + ".cshtml";

        public string Payments { get; set; }
        public bool UseTax { get; set; }
        public int? TaxId { get; set; }
        public List<SelectListItem> Taxes { get; set; }
        public bool UsePaymentMethodAndSubjectTypes { get; set; }
        public ePaymentMethodType PaymentMethodType { get; set; } = ePaymentMethodType.full_prepayment;
        public List<SelectListItem> PaymentMethodTypes { get; set; }
        public ePaymentSubjectType PaymentSubjectType { get; set; } = ePaymentSubjectType.payment;
        public List<SelectListItem> PaymentSubjectTypes { get; set; }

        public bool UseExtracharge { get; set; }
        public List<SelectListItem> BaseExtrachargeTypes { get; set; }
        public float ExtrachargeInNumbers { get; set; }
        public float ExtrachargeInPercents { get; set; }
        public bool ExtrachargeFromOrder { get; set; }

        public bool UseWeight { get; set; }
        public float BaseDefaultWeight { get; set; }
        public AdvantShop.Payment.ExtrachargeType WeightExtrachargeType { get; set; }
        public float WeightExtracharge { get; set; }

        public bool UseCargo { get; set; }
        public float BaseDefaultHeight { get; set; }
        public float BaseDefaultWidth { get; set; }
        public float BaseDefaultLength { get; set; }
        public AdvantShop.Payment.ExtrachargeType CargoExtrachargeType { get; set; }
        public float CargoExtracharge { get; set; }

        public int ExtraDeliveryTime { get; set; }
        public bool UseExtraDeliveryTime { get; set; }

        public bool UseCurrency { get; set; }
        public int? CurrencyId { get; set; }
        public bool CurrencyAllAvailable { get; set; }
        public string[] CurrencyIso3Available { get; set; }
        public string CurrencySymbol { get; set; }

        public virtual string StatusesReference { get; set; }
        public virtual Dictionary<string, string> Statuses { get; set; }
        public virtual bool StatusesSync { get; set; }
        public virtual string AdditionalSettingsStatusesViewPath { get; }

        public List<SelectListItem> Currencies
        {
            get
            {
                var currencies = new List<SelectListItem>();

                if (UseCurrency)
                {
                    var currencyIso3Available = CurrencyIso3Available ?? new string[] { };
                    foreach (var currency in CurrencyService.GetAllCurrencies()
                        .Where(x => CurrencyAllAvailable || currencyIso3Available.Contains(x.Iso3, StringComparer.OrdinalIgnoreCase)))
                    {
                        currencies.Add(new SelectListItem() { Text = currency.Name, Value = currency.CurrencyId.ToString() });
                    }

                    var selected = currencies.Find(x => x.Value == CurrencyId.ToString());
                    if (selected != null)
                    {
                        selected.Selected = true;
                        CurrencySymbol = CurrencyService.GetAllCurrencies().First(x => x.CurrencyId == CurrencyId).Symbol;
                    }
                    else
                    {
                        CurrencyId = null;
                        currencies.Insert(0, new SelectListItem() { Text = "Не указана валюта", Value = "", Selected = true });
                    }
                }

                return currencies;
            }
        }

        public bool RequiresSpecifyingTypeOfDelivery { get; set; }
        public EnTypeOfDelivery? TypeOfDelivery { get; set; }

        public List<SelectListItem> TypesOfDelivery
        {
            get
            {
                var list = Enum.GetValues(typeof(EnTypeOfDelivery))
                               .Cast<EnTypeOfDelivery>()
                               .Select(x => new SelectListItem()
                                {
                                    Text = x.Localize(),
                                    Value = x.ConvertIntString()
                                })
                               .ToList();
                
                if (TypeOfDelivery is null)
                    list.Insert(0, new SelectListItem() { Text = LocalizationService.GetResource("AdvantShop.Shipping.AdminModel.TypesOfDelivery.NotSet"), Value = "", Selected = true });
                return list;
            }
        }


        public Currency CurrentCurrency { get; set; }

        public bool UseDeliveryInterval { get; set; }
        // пример "1!11:00-12:00&12:00-15:00|2!11:00-12:00&12:00-15:00" 
        // ! - разделитель дня недели((int)DayOfWeek) и интервалов, & - разделитель интервалов, | - разделитель дней недели с интервалами
        public string DeliveryIntervalsStr
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.DeliveryIntervals, string.Empty);
            set => Params.TryAddValue(ShippingWithIntervalTemplate.DeliveryIntervals, value ?? string.Empty);
        }

        public string TimeZoneId
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.TimezoneId);
            set => Params.TryAddValue(ShippingWithIntervalTemplate.TimezoneId, value);
        }

        private string _timeZones;
        public string TimeZones => _timeZones ?? (_timeZones = JsonConvert.SerializeObject(TimeZoneInfo.GetSystemTimeZones()
            .Select(x => new SelectListItem
            {
                Text = x.DisplayName,
                Value = x.Id
            })));

        /// <summary>
        /// Минимальная разница между текущей датой и первым интервалом, в минутах
        /// </summary>
        public int? MinDeliveryTime
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.MinDeliveryTime).TryParseInt(true);
            set => Params.TryAddValue(ShippingWithIntervalTemplate.MinDeliveryTime, value.ToString());
        }

        public int? CountVisibleDeliveryDay
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.CountVisibleDeliveryDay).TryParseInt(true);
            set => Params.TryAddValue(ShippingWithIntervalTemplate.CountVisibleDeliveryDay, value.ToString());
        }

        public int? CountHiddenDeliveryDay
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.CountHiddenDeliveryDay).TryParseInt(true);
            set => Params.TryAddValue(ShippingWithIntervalTemplate.CountHiddenDeliveryDay, value.ToString());
        }

        public bool ShowSoonest
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.ShowSoonest).TryParseBool();
            set => Params.TryAddValue(ShippingWithIntervalTemplate.ShowSoonest, value.ToString());
        }

        public TimeSpan? OrderProcessingDeadline
        {
            get => Params.ElementOrDefault(ShippingWithIntervalTemplate.OrderProcessingDeadline).TryParseTimeSpan(true);
            set => Params.TryAddValue(ShippingWithIntervalTemplate.OrderProcessingDeadline, value.ToString());
        }

        public string TrackingUrl { get; set; } //GlorySoft_027
        //public bool ShippingForTK { get; set; } //GlorySoft_030
    }
}
