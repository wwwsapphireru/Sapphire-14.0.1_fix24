using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Customers;
using AdvantShop.Orders;
using AdvantShop.Shipping.RussianPost;
using AdvantShop.Shipping.RussianPost.Api;
using AdvantShop.Shipping.RussianPost.PickPoints;

namespace AdvantShop.Web.Infrastructure.Admin.ShippingMethods
{
    [ShippingAdminModel("RussianPost")]
    public class RussianPostShippingAdminModel : ShippingMethodAdminModel, IValidatableObject
    {
        private RussianPostApiService _service;
        private RussianPostApiService Service
        {
            get
            {
                if (_service == null && !string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(Token))
                    _service = new RussianPostApiService(Login, Password, Token);

                return _service;
            }
        }

        private AccountSettings _settings;
        private AccountSettings AccountSettings
        {
            get
            {
                if (_settings == null && Service != null)
                    _settings =  Service.GetAccountSettings();

                return _settings;
            }
        }

        public bool IsLimitedExceeded
        {
            get
            {
                return Service != null && AccountSettings == null && Service.IsLimitedExceeded;
            }
        }

        public string Login
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.Login); }
            set { Params.TryAddValue(RussianPostTemplate.Login, value.DefaultOrEmpty()); }
        }

        public string Password
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.Password); }
            set { Params.TryAddValue(RussianPostTemplate.Password, value.DefaultOrEmpty()); }
        }

        public string Token
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.Token); }
            set
            {
                if (Params.ElementOrDefault(RussianPostTemplate.Token) != value.DefaultOrEmpty())
                    PointIndex = null;

                Params.TryAddValue(RussianPostTemplate.Token, value.DefaultOrEmpty());
            }
        }

        public string PointIndex
        {
            get
            {
                var value = Params.ElementOrDefault(RussianPostTemplate.PointIndex);
                
                if (string.IsNullOrEmpty(value) && AccountSettings != null && AccountSettings.ShippingPoints != null)
                {
                    value =
                        AccountSettings.ShippingPoints.Where(x => x.Enabled == true)
                            .Select(x => x.OperatorIndex).FirstOrDefault();

                    if (string.IsNullOrEmpty(value))
                        Params.TryAddValue(RussianPostTemplate.PointIndex, value.DefaultOrEmpty());
                }
                return value;
            }
            set { Params.TryAddValue(RussianPostTemplate.PointIndex, value.DefaultOrEmpty()); }
        }

        public List<SelectListItem> PointsIndex
        {
            get
            {
                var listPointIndex = new List<SelectListItem>()
                {
                    new SelectListItem
                    {
                        Text = "Не выбрано",
                        Value = string.Empty
                    }
                };

                if (AccountSettings != null && AccountSettings.ShippingPoints != null)
                {
                    listPointIndex =
                        AccountSettings.ShippingPoints.Where(x => x.Enabled == true).Select(x => new SelectListItem()
                        {
                            Text = x.OperatorIndex,
                            Value = x.OperatorIndex
                        }).ToList();
                }
                else
                {
                    // чтобы не слетала настройка, когда ичерпан лимит обращений и кто-то меняет настройки
                    var pointIndex = PointIndex;
                    if (!string.IsNullOrEmpty(pointIndex) && listPointIndex.Count == 0)
                        listPointIndex.Add(new SelectListItem()
                        {
                            Text = pointIndex,
                            Value = pointIndex
                        });
                }

                return listPointIndex;
            }
        }

        private string[] _localDeliveryTypes;
        public string[] LocalDeliveryTypes
        {
            get 
            {
                if (_localDeliveryTypes == null)
                {
                    if (Params.ContainsKey(RussianPostTemplate.LocalDeliveryTypes))
                        _localDeliveryTypes = GetDeliveryTypes(Params.ElementOrDefault(RussianPostTemplate.LocalDeliveryTypes), ",", "\\", RussianPostAvailableOption.LocalTransportTypeAvailable, false).ToArray();
                    else if (Params.ContainsKey(RussianPostTemplate.OldLocalDeliveryTypes))
                    {
                        // поддержка настроек ранних версий
                        _localDeliveryTypes = GetDeliveryTypes(Params.ElementOrDefault(RussianPostTemplate.OldLocalDeliveryTypes), ",", "_", RussianPostAvailableOption.LocalTransportTypeAvailable, true).ToArray();
                    }
                    else if (Params.ContainsKey(RussianPostTemplate.DeliveryTypes))
                    {
                        // поддержка настроек ранних версий
                        var deliveryTypesOld = (Params.ElementOrDefault(RussianPostTemplate.DeliveryTypes) ?? string.Empty).Split(",").Select(x => RussianPost.GetEnMailTypeByInt(x.TryParseInt())).ToList();
                        var typeInsure = (EnTypeInsure)Params.ElementOrDefault(RussianPostTemplate.TypeInsure).TryParseInt();

                        var returnValue = new List<string>();
                        if (!string.IsNullOrEmpty(PointIndex) && AccountSettings != null && AccountSettings.ShippingPoints != null)
                        {
                            var point = AccountSettings.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex.Equals(PointIndex, StringComparison.OrdinalIgnoreCase));
                            if (point != null)
                            {
                                foreach (var availableProduct in point.AvailableProducts
                                    .Where(x => !RussianPost.IsInternational(x) && deliveryTypesOld.Contains(x.MailType))
                                    .OrderBy(x => x.MailType.Value).ThenBy(x => x.MailCategory.Value))
                                {
                                    if (typeInsure == EnTypeInsure.WithDeclaredValue && !RussianPost.IsDeclareCategory(availableProduct.MailCategory))
                                        continue;
                                    else if (typeInsure == EnTypeInsure.WithoutDeclaredValue && RussianPost.IsDeclareCategory(availableProduct.MailCategory))
                                        continue;


                                    if (RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                                    {
                                        foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[availableProduct.MailType])
                                            returnValue.Add(GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, transportType));
                                    }
                                    else
                                        returnValue.Add(GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, null));
                                }
                            }
                        }

                        _localDeliveryTypes = returnValue.ToArray();
                    }
                    else
                        _localDeliveryTypes = new string[]{};
                }

                return _localDeliveryTypes;
            }
            set
            {
                Params.TryAddValue(RussianPostTemplate.LocalDeliveryTypes, value != null ? string.Join(",", value) : string.Empty);
                
                // заодно загружаем постоматы и др., если они ранее не грузились
                if (Service != null && value
                        .Any(x => x.Contains(EnMailType.OnlineParcel.Value, StringComparison.OrdinalIgnoreCase)
                                  && x.Contains("COMBINED_", StringComparison.OrdinalIgnoreCase))
                   )
                    if (!PickPointsServices.ExistsPickPoints())
                        RussianPost.SyncPickPoints(Service);
            }
        }

        private List<string> GetDeliveryTypes(string deliveryTypesStr, string separator, string subSeparator, 
            Dictionary<EnMailType, List<EnTransportType>> transportTypeAvailable, bool valueIsInt)
        {
            var returnValue = new List<string>();
            string[] tempArr;
            foreach (var strDelivery in (deliveryTypesStr ?? string.Empty).Split(separator))
                if ((tempArr = strDelivery.Split(subSeparator)).Length >= 2)
                {
                    var mailType = valueIsInt 
                        ? RussianPost.GetEnMailTypeByInt(tempArr[0].TryParseInt()) 
                        : EnMailType.Parse(tempArr[0]);
                    if (tempArr.Length > 2 || !transportTypeAvailable.ContainsKey(mailType))
                        returnValue.Add(GetValueDelivery(
                            mailType,
                            valueIsInt 
                                ? RussianPost.GetEnMailCategoryByInt(tempArr[1].TryParseInt()) 
                                : EnMailCategory.Parse(tempArr[1]),
                            tempArr.Length > 2 
                                ? valueIsInt 
                                    ? RussianPost.GetEnTransportTypeByInt(tempArr[2].TryParseInt()) 
                                    : EnTransportType.Parse(tempArr[2])
                                : null));
                    else
                        foreach (var transportType in transportTypeAvailable[mailType])
                            returnValue.Add(GetValueDelivery(
                                mailType,
                                valueIsInt 
                                    ? RussianPost.GetEnMailCategoryByInt(tempArr[1].TryParseInt())
                                    : EnMailCategory.Parse(tempArr[1]),
                                transportType
                                ));
                }

            return returnValue;
        }

        private string[] _internationalDeliveryTypes;
        public string[] InternationalDeliveryTypes
        {
            get 
            {
                if (_internationalDeliveryTypes == null)
                {
                    if (Params.ContainsKey(RussianPostTemplate.InternationalDeliveryTypes))
                        _internationalDeliveryTypes = GetDeliveryTypes(Params.ElementOrDefault(RussianPostTemplate.InternationalDeliveryTypes), ",", "\\", RussianPostAvailableOption.InternationalTransportTypeAvailable, false).ToArray();
                    else if (Params.ContainsKey(RussianPostTemplate.OldInternationalDeliveryTypes))
                    {
                        // поддержка настроек ранних версий
                        _internationalDeliveryTypes = GetDeliveryTypes(Params.ElementOrDefault(RussianPostTemplate.OldInternationalDeliveryTypes), ",", "_", RussianPostAvailableOption.InternationalTransportTypeAvailable, true).ToArray();
                    }
                    else if (Params.ContainsKey(RussianPostTemplate.DeliveryTypes))
                    {
                        // поддержка настроек ранних версий
                        var deliveryTypesOld = (Params.ElementOrDefault(RussianPostTemplate.DeliveryTypes) ?? string.Empty).Split(",").Select(x => RussianPost.GetEnMailTypeByInt(x.TryParseInt())).ToList();
                        var typeInsure = (EnTypeInsure)Params.ElementOrDefault(RussianPostTemplate.TypeInsure).TryParseInt();

                        var returnValue = new List<string>();
                        if (!string.IsNullOrEmpty(PointIndex) && AccountSettings != null && AccountSettings.ShippingPoints != null)
                        {
                            var point = AccountSettings.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex.Equals(PointIndex, StringComparison.OrdinalIgnoreCase));
                            if (point != null)
                            {
                                foreach (var availableProduct in point.AvailableProducts
                                    .Where(x => RussianPost.IsInternational(x) && deliveryTypesOld.Contains(x.MailType))
                                    .OrderBy(x => x.MailType).ThenBy(x => x.MailCategory))
                                {
                                    if (typeInsure == EnTypeInsure.WithDeclaredValue && !RussianPost.IsDeclareCategory(availableProduct.MailCategory))
                                        continue;
                                    else if (typeInsure == EnTypeInsure.WithoutDeclaredValue && RussianPost.IsDeclareCategory(availableProduct.MailCategory))
                                        continue;

                                    if (RussianPostAvailableOption.InternationalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                                    {
                                        foreach (var transportType in RussianPostAvailableOption.InternationalTransportTypeAvailable[availableProduct.MailType])
                                            returnValue.Add(GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, transportType));
                                    }
                                    else
                                        returnValue.Add(GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, null));
                                }
                            }
                        }

                        _internationalDeliveryTypes = returnValue.ToArray();
                    }
                    else
                        _internationalDeliveryTypes = new string[]{};
                }

                return _internationalDeliveryTypes;
            }
            set { Params.TryAddValue(RussianPostTemplate.InternationalDeliveryTypes, value != null ? string.Join(",", value) : string.Empty); }
        }

        public List<SelectListItem> ListLocalDeliveryTypes
        {
            get
            {
                var listDeliveryTypes = new List<SelectListItem>();

                if (!string.IsNullOrEmpty(PointIndex) && AccountSettings != null && AccountSettings.ShippingPoints != null)
                {
                    var point = AccountSettings.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex.Equals(PointIndex, StringComparison.OrdinalIgnoreCase));
                    if (point != null)
                    {
                        foreach (var availableProduct in point.AvailableProducts
                            .Where(x => !RussianPost.IsInternational(x))
                            //.Where(x => x.MailType.Localize() != x.MailType.Value)
                            //.Where(x => x.MailCategory.Localize() != x.MailCategory.Value)
                            .OrderBy(x => x.MailType.Localize())
                            .ThenBy(x => x.MailCategory.Value))
                        {
                            if (RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                                foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[availableProduct.MailType])
                                    listDeliveryTypes.Add(new SelectListItem()
                                    {
                                        Text = string.Format("{0} {1} {2}",
                                                    availableProduct.MailType.Localize(),
                                                    availableProduct.MailCategory.Localize().ToLower(),
                                                    transportType.Localize().ToLower()),
                                        Value = GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, transportType),
                                        Disabled = availableProduct.MailType.Localize() == availableProduct.MailType.Value ||
                                                   availableProduct.MailCategory.Localize() == availableProduct.MailCategory.Value ||
                                                   transportType.Localize() == transportType.Value
                                    });
                            else
                                listDeliveryTypes.Add(new SelectListItem()
                            {
                                Text = string.Format("{0} {1}",
                                    availableProduct.MailType.Localize(),
                                    availableProduct.MailCategory.Localize().ToLower()),
                                Value = GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, null),
                                Disabled = availableProduct.MailType.Localize() == availableProduct.MailType.Value ||
                                           availableProduct.MailCategory.Localize() == availableProduct.MailCategory.Value
                            });
                        }

                        listDeliveryTypes.Where(x => LocalDeliveryTypes.Contains(x.Value)).ForEach(x => x.Selected = true);
                    }
                }

                return listDeliveryTypes;
            }
        }

        public List<SelectListItem> ListInternationalDeliveryTypes
        {
            get
            {
                var listDeliveryTypes = new List<SelectListItem>();

                if (!string.IsNullOrEmpty(PointIndex) && AccountSettings != null && AccountSettings.ShippingPoints != null)
                {
                    var point = AccountSettings.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex.Equals(PointIndex, StringComparison.OrdinalIgnoreCase));
                    if (point != null)
                    {
                        foreach (var availableProduct in point.AvailableProducts
                            .Where(RussianPost.IsInternational)
                            //.Where(x => x.MailType.Localize() != x.MailType.Value)
                            //.Where(x => x.MailCategory.Localize() != x.MailCategory.Value)
                            .OrderBy(x => x.MailType.Localize())
                            .ThenBy(x => x.MailCategory.Value))
                        {
                            if (RussianPostAvailableOption.InternationalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                                foreach (var transportType in RussianPostAvailableOption.InternationalTransportTypeAvailable[availableProduct.MailType])
                                    listDeliveryTypes.Add(new SelectListItem()
                                    {
                                        Text = string.Format("{0} {1} {2}",
                                                    availableProduct.MailType.Localize(),
                                                    availableProduct.MailCategory.Localize().ToLower(),
                                                    transportType.Localize().ToLower()),
                                        Value = GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, transportType),
                                        Disabled = availableProduct.MailType.Localize() == availableProduct.MailType.Value ||
                                                   availableProduct.MailCategory.Localize() == availableProduct.MailCategory.Value ||
                                                   transportType.Localize() == transportType.Value
                                    });
                            else
                            {
                                listDeliveryTypes.Add(new SelectListItem()
                                {
                                    Text = string.Format("{0} {1}",
                                        availableProduct.MailType.Localize(),
                                        availableProduct.MailCategory.Localize().ToLower()),
                                    Value = GetValueDelivery(availableProduct.MailType, availableProduct.MailCategory, null),
                                    Disabled = availableProduct.MailType.Localize() == availableProduct.MailType.Value ||
                                               availableProduct.MailCategory.Localize() == availableProduct.MailCategory.Value
                                });
                            }
                        }

                        listDeliveryTypes.Where(x => InternationalDeliveryTypes.Contains(x.Value)).ForEach(x => x.Selected = true);
                    }
                }

                return listDeliveryTypes;
            }
        }

        private string GetValueDelivery(EnMailType mailType, EnMailCategory mailCategory, EnTransportType transportType)
        {
            return string.Format("{0}\\{1}{2}", mailType.Value, mailCategory.Value, transportType != null ? "\\" + transportType.Value : null);
        }

        public string TypeNotification
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.TypeNotification); }
            set { Params.TryAddValue(RussianPostTemplate.TypeNotification, value.DefaultOrEmpty()); }
        }

        public List<SelectListItem> TypeNotifications
        {
            get
            {
                var listTypeNotifications = new List<SelectListItem>()
                {
                    new SelectListItem() {Text = "без уведомления", Value = ""}
                };

                listTypeNotifications.AddRange( Enum.GetValues(typeof(EnTypeNotification))
                    .Cast<EnTypeNotification>()
                    .Select(x => new SelectListItem()
                    {
                        Text = x.Localize(),
                        Value = ((int)x).ToString()
                    })
                    .ToList());

                return listTypeNotifications;
            }
        }

        public string HelpTypeNotification
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.OrderOfNoticeOptionAvailable.Keys.Concat(RussianPostAvailableOption.SimpleNoticeOptionAvailable.Keys).Distinct().Select(x => x.Localize())));
            }
        }

        public bool Courier
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.Courier).TryParseBool(); }
            set { Params.TryAddValue(RussianPostTemplate.Courier, value.ToString()); }
        }

        public string HelpCourier
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.CourierOptionAvailable.Select(x => x.Localize())));
            }
        }

        public bool Fragile
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.Fragile).TryParseBool(); }
            set { Params.TryAddValue(RussianPostTemplate.Fragile, value.ToString()); }
        }

        public string HelpFragile
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.FragileOptionAvailable.Select(x => x.Localize())));
            }
        }

        public bool DeliveryWithCod
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.DeliveryWithCod).TryParseBool(); }
            set { Params.TryAddValue(RussianPostTemplate.DeliveryWithCod, value.ToString()); }
        }

        public string HelpDeliveryWithCod
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.DeliveryWithCodAvailable.Select(x => x.Localize())));
            }
        }

        public bool DeliveryToOps
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.DeliveryToOps).TryParseBool(); }
            set
            {
                Params.TryAddValue(RussianPostTemplate.DeliveryToOps, value.ToString());
                                
                // заодно загружаем постоматы и др., если они ранее не грузились
                if (Service != null && value is true)
                    if (!PickPointsServices.ExistsPickPoints())
                        RussianPost.SyncPickPoints(Service);

            }
        }

        public string HelpToOps
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.DeliveryToOpsAvailable.Select(x => x.Localize())));
            }
        }

        public bool DeliveryToPochtamat
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.DeliveryToPochtamat).TryParseBool(); }
            set
            {
                Params.TryAddValue(RussianPostTemplate.DeliveryToPochtamat, value.ToString());
                                
                // заодно загружаем постоматы и др., если они ранее не грузились
                if (Service != null && value is true)
                    if (!PickPointsServices.ExistsPickPoints())
                        RussianPost.SyncPickPoints(Service);

            }
        }

        public string HelpToPochtamat
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.DeliveryToPochtamatAvailable.Select(x => x.Localize())));
            }
        }

        public bool DeliveryToPvz
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.DeliveryToPvz).TryParseBool(); }
            set
            {
                Params.TryAddValue(RussianPostTemplate.DeliveryToPvz, value.ToString());
                                
                // заодно загружаем постоматы и др., если они ранее не грузились
                if (Service != null && value is true)
                    if (!PickPointsServices.ExistsPickPoints())
                        RussianPost.SyncPickPoints(Service);

            }
        }

        public string HelpToPvz
        {
            get
            {
                return string.Format("Доступно для видов РПО: {0}",
                    string.Join(", ", RussianPostAvailableOption.DeliveryToPvzAvailable.Select(x => x.Localize())));
            }
        }

        public bool SmsNotification
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.SmsNotification).TryParseBool(); }
            set { Params.TryAddValue(RussianPostTemplate.SmsNotification, value.ToString()); }
        }

        public bool MinimalApiRequests
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.MinimalApiRequests).TryParseBool(); }
            set { Params.TryAddValue(RussianPostTemplate.MinimalApiRequests, value.ToString()); }
        }
        
        public override bool StatusesSync
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.StatusesSync).TryParseBool(); }
            set { Params.TryAddValue(RussianPostTemplate.StatusesSync, value.ToString()); }
        }

        public override string StatusesReference
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.StatusesReference); }
            set { Params.TryAddValue(RussianPostTemplate.StatusesReference, value.DefaultOrEmpty()); }
        }

        private Dictionary<string, string> _statuses;
        public override Dictionary<string, string> Statuses => _statuses ?? (_statuses = RussianPost.Statuses);

        public override string AdditionalSettingsStatusesViewPath => "_RussianPostSettingsStatuses";

        public string TrackingLogin
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.TrackingLogin); }
            set { Params.TryAddValue(RussianPostTemplate.TrackingLogin, value.DefaultOrEmpty()); }
        }

        public string TrackingPassword
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.TrackingPassword); }
            set { Params.TryAddValue(RussianPostTemplate.TrackingPassword, value.DefaultOrEmpty()); }
        }

        public string YaMapsApiKey
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.YaMapsApiKey); }
            set { Params.TryAddValue(RussianPostTemplate.YaMapsApiKey, value.DefaultOrEmpty()); }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Login))
                yield return new ValidationResult("Введите логин", new[] { "Login" });
            if (string.IsNullOrWhiteSpace(Password))
                yield return new ValidationResult("Введите пароль", new[] { "Password" });
            if (string.IsNullOrWhiteSpace(Token))
                yield return new ValidationResult("Введите токен", new[] { "Token" });
        }

        //GlorySoft_001
        public int StatusForReady
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.StatusForReady).TryParseInt(0); }
            set { Params.TryAddValue(RussianPostTemplate.StatusForReady, value.ToString()); }
        }
        public List<SelectListItem> ListOfStatuses
        {
            get
            {
                var listOfStatuses = new List<SelectListItem>()
                {
                    new SelectListItem() {Text = "не выбран", Value = "0"}
                };

                listOfStatuses.AddRange(OrderStatusService.GetOrderStatuses().OrderBy(x => x.SortOrder).ToList()
                    .Select(x => new SelectListItem()
                    {
                        Text = x.StatusName,
                        Value = (x.StatusID).ToString()
                    })
                    .ToList());

                return listOfStatuses;
            }
        }
        public CustomerType CustomerType
        {
            get { return (CustomerType)Params.ElementOrDefault(RussianPostTemplate.CustomerType).TryParseInt((int)CustomerType.All); }
            set { Params.TryAddValue(RussianPostTemplate.CustomerType, ((int)value).ToString()); }
        }
        public List<SelectListItem> CustomerTypes
        {
            get
            {
                return Enum.GetValues(typeof(CustomerType)).Cast<CustomerType>().Select(x => new SelectListItem
                {
                    Text = x.Localize(),
                    Value = x.ToString()
                }).OrderBy(x => x.Text).ToList();
            }
        }
        public int MaxWeight
        {
            get { return Params.ElementOrDefault(RussianPostTemplate.MaxWeight).TryParseInt(0); }
            set { Params.TryAddValue(RussianPostTemplate.MaxWeight, (value).ToString()); }
        }

    }
}
