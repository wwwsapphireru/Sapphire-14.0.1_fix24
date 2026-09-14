using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Shipping.Sdek;
using AdvantShop.Shipping.Sdek.Api;
using AdvantShop.Shipping.Sdek.DeliveryPoints;


namespace AdvantShop.Web.Infrastructure.Admin.ShippingMethods
{
    [ShippingAdminModel("Sdek")]
    public class SdekShippingAdminModel : ShippingMethodAdminModel, IValidatableObject
    {
        public string AuthLogin
        {
            get => Params.ElementOrDefault(SdekTemplate.AuthLogin);
            set => Params.TryAddValue(SdekTemplate.AuthLogin, value.DefaultOrEmpty());
        }
        public string AuthPassword
        {
            get => Params.ElementOrDefault(SdekTemplate.AuthPassword);
            set
            {
                Params.TryAddValue(SdekTemplate.AuthPassword, value.DefaultOrEmpty());
                
                // заодно загружаем постоматы, если они ранее не грузились
                if (!DeliveryPointService.ExistsDeliveryPoints()
                    && AuthLogin.IsNotEmpty()
                    && AuthPassword.IsNotEmpty())
                    Sdek.SyncPickPoints(new SdekApiService20(AuthLogin, AuthPassword));

            }
        }

        public string CityFrom
        {
            get => Params.ElementOrDefault(SdekTemplate.CityFrom);
            set => Params.TryAddValue(SdekTemplate.CityFrom, value.DefaultOrEmpty());
            // var methodId = ShippingMethodId;
            //
            // if (methodId == 0 && HttpContext.Current != null && HttpContext.Current.Request["shippingmethodid"] != null)
            //     methodId = HttpContext.Current.Request["shippingmethodid"].TryParseInt();
            //
            // var method = Shipping.ShippingMethodService.GetShippingMethod(methodId);
            // var sdek = new Shipping.Sdek.Sdek(method, null, null);
            // Params.TryAddValue(SdekTemplate.CityFromId, SdekService.GetSdekCityId(value.DefaultOrEmpty(), string.Empty, string.Empty, string.Empty, sdek.SdekApiService20, out _, allowedObsoleteFindCity: true).ToString());
        }

        public string CityFromId
        {
            get => Params.ElementOrDefault(SdekTemplate.CityFromId);
            set => Params.TryAddValue(SdekTemplate.CityFromId, value.DefaultOrEmpty());
        }
        
        public string[] Tariff
        {
            get
            {
                return Params.ContainsKey(SdekTemplate.CalculateTariffs)
                  ? (Params.ElementOrDefault(SdekTemplate.CalculateTariffs) ?? string.Empty).Split(new[]{","}, StringSplitOptions.RemoveEmptyEntries)
                  : (Params.ElementOrDefault(SdekTemplate.TariffOldParam) ?? string.Empty).Split(new[]{","}, StringSplitOptions.RemoveEmptyEntries);
            }
            set => Params.TryAddValue(SdekTemplate.CalculateTariffs, value != null ? string.Join(",", value) : string.Empty);
        }

        public string DeliveryNote
        {
            get => Params.ElementOrDefault(SdekTemplate.DeliveryNote) ?? "1";
            set => Params.TryAddValue(SdekTemplate.DeliveryNote, value.TryParseInt().ToString());
        }

        public override bool StatusesSync
        {
            get => Params.ElementOrDefault(SdekTemplate.StatusesSync).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.StatusesSync, value.ToString());
        }

        public override string StatusesReference
        {
            get => Params.ElementOrDefault(SdekTemplate.StatusesReference);
            set => Params.TryAddValue(SdekTemplate.StatusesReference, value.DefaultOrEmpty());
        }

        private Dictionary<string, string> _statuses;
        public override Dictionary<string, string> Statuses => _statuses ?? (_statuses = Sdek.Statuses);

        public bool WithInsure
        {
            get => Params.ElementOrDefault(SdekTemplate.WithInsure).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.WithInsure, value.ToString());
        }

        public bool AllowInspection
        {
            get => Params.ElementOrDefault(SdekTemplate.AllowInspection).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.AllowInspection, value.ToString());
        }

        public bool PartialDelivery
        {
            get => Params.ElementOrDefault(SdekTemplate.PartialDelivery).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.PartialDelivery, value.ToString());
        }

        public bool TryingOn
        {
            get => Params.ElementOrDefault(SdekTemplate.TryingOn).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.TryingOn, value.ToString());
        }

        public bool ShowPointsAsList
        {
            get => Params.ElementOrDefault(SdekTemplate.ShowPointsAsList).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.ShowPointsAsList, value.ToString());
        }

        public bool ShowSdekWidjet
        {
            get => Params.ElementOrDefault(SdekTemplate.ShowSdekWidjet).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.ShowSdekWidjet, value.ToString());
        }

        public bool ShowAddressComment
        {
            get => Params.ElementOrDefault(SdekTemplate.ShowAddressComment).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.ShowAddressComment, value.ToString());
        }

        public string YaMapsApiKey
        {
            get => Params.ElementOrDefault(SdekTemplate.YaMapsApiKey);
            set => Params.TryAddValue(SdekTemplate.YaMapsApiKey, value.DefaultOrEmpty());
        }

        public bool UseSeller
        {
            get => Params.ElementOrDefault(SdekTemplate.UseSeller).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.UseSeller, value.ToString());
        }

        public string SellerAddress
        {
            get => Params.ElementOrDefault(SdekTemplate.SellerAddress);
            set => Params.TryAddValue(SdekTemplate.SellerAddress, value.DefaultOrEmpty());
        }

        public string SellerName
        {
            get => Params.ElementOrDefault(SdekTemplate.SellerName);
            set => Params.TryAddValue(SdekTemplate.SellerName, value.DefaultOrEmpty());
        }

        public string SellerINN
        {
            get => Params.ElementOrDefault(SdekTemplate.SellerINN);
            set => Params.TryAddValue(SdekTemplate.SellerINN, value.DefaultOrEmpty());
        }

        public string SellerPhone
        {
            get => Params.ElementOrDefault(SdekTemplate.SellerPhone);
            set => Params.TryAddValue(SdekTemplate.SellerPhone, value.DefaultOrEmpty());
        }

        public string SellerOwnershipForm
        {
            get => Params.ElementOrDefault(SdekTemplate.SellerOwnershipForm);
            set => Params.TryAddValue(SdekTemplate.SellerOwnershipForm, value.DefaultOrEmpty());
        }
        
        public List<SelectListItem> SellerOwnershipForms
        {
            get
            {
                var forms = new List<SelectListItem>
                {
                    new SelectListItem() { Text = "", Value = "" },
                    new SelectListItem() { Text = "Акционерное общество", Value = "9" },
                    new SelectListItem() { Text = "Закрытое акционерное общество", Value = "61" },
                    new SelectListItem() { Text = "Индивидуальный предприниматель", Value = "63" },
                    new SelectListItem() { Text = "Открытое акционерное общество", Value = "119" },
                    new SelectListItem() { Text = "Общество с ограниченной ответственностью", Value = "137" },
                    new SelectListItem() { Text = "Публичное акционерное общество", Value = "147" },
                };

                return forms;
            }
        }

        public List<SelectListItem> ListTariffs
        {
            get
            {
                var selectedTariff = Tariff.ToList();

                var tariffs = SdekTariffs.Tariffs.Select(x => new SelectListItem() { Text = x.Name, Value = x.TariffId.ToString(), Selected = selectedTariff.Contains(x.TariffId.ToString()) }).ToList();

                //if (!tariffs.Any(x => x.Selected))
                //{
                //    tariffs[0].Selected = true;
                //    Tariff = new[] { tariffs[0].Value };
                //}

                return tariffs;
            }
        }

        public string DefaultCourierCity
        {
            get => Params.ElementOrDefault(SdekTemplate.DefaultCourierCity) ?? SettingsMain.City;
            set => Params.TryAddValue(SdekTemplate.DefaultCourierCity, value.DefaultOrEmpty());
        }

        public string DefaultCourierStreet
        {
            get => Params.ElementOrDefault(SdekTemplate.DefaultCourierStreet);
            set => Params.TryAddValue(SdekTemplate.DefaultCourierStreet, value.DefaultOrEmpty());
        }

        public string DefaultCourierHouse
        {
            get => Params.ElementOrDefault(SdekTemplate.DefaultCourierHouse);
            set => Params.TryAddValue(SdekTemplate.DefaultCourierHouse, value.DefaultOrEmpty());
        }

        public string DefaultCourierFlat
        {
            get => Params.ElementOrDefault(SdekTemplate.DefaultCourierFlat);
            set => Params.TryAddValue(SdekTemplate.DefaultCourierFlat, value.DefaultOrEmpty());
        }

        public string DefaultCourierNameContact
        {
            get => Params.ElementOrDefault(SdekTemplate.DefaultCourierNameContact);
            set => Params.TryAddValue(SdekTemplate.DefaultCourierNameContact, value.DefaultOrEmpty());
        }

        public string DefaultCourierPhone
        {
            get => Params.ElementOrDefault(SdekTemplate.DefaultCourierPhone);
            set => Params.TryAddValue(SdekTemplate.DefaultCourierPhone, value.DefaultOrEmpty());
        }
        
        public bool UseSender
        {
            get => Params.ElementOrDefault(SdekTemplate.UseSender).TryParseBool();
            set => Params.TryAddValue(SdekTemplate.UseSender, value.ToString());
        }

        public string SenderCompany
        {
            get => Params.ElementOrDefault(SdekTemplate.SenderCompany);
            set => Params.TryAddValue(SdekTemplate.SenderCompany, value.DefaultOrEmpty().Reduce(255));
        }

        public string SenderName
        {
            get => Params.ElementOrDefault(SdekTemplate.SenderName);
            set => Params.TryAddValue(SdekTemplate.SenderName, value.DefaultOrEmpty().Reduce(255));
        }

        public string SenderEmail
        {
            get => Params.ElementOrDefault(SdekTemplate.SenderEmail);
            set => Params.TryAddValue(SdekTemplate.SenderEmail, value.DefaultOrEmpty().Reduce(255));
        }

        public string SenderPhone
        {
            get => Params.ElementOrDefault(SdekTemplate.SenderPhone);
            set => Params.TryAddValue(SdekTemplate.SenderPhone, value.DefaultOrEmpty());
        }
        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new List<ValidationResult>();
        }

        //GlorySoft_001
        public string AdditionalAccounts
        {
            get { return Params.ElementOrDefault("AdditionalAccounts"); }
            set { Params.TryAddValue("AdditionalAccounts", value.DefaultOrEmpty()); }
        }
        public int MaxWeight
        {
            get { return Params.ElementOrDefault(SdekTemplate.MaxWeight).TryParseInt(0); }
            set { Params.TryAddValue(SdekTemplate.MaxWeight, (value).ToString()); }
        }

    }
}
