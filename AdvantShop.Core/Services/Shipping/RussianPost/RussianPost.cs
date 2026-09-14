using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Core.Services.Shipping.RussianPost.TrackingApi;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Orders;
using AdvantShop.Repository;
using AdvantShop.Shipping.RussianPost.Api;
using AdvantShop.Shipping.RussianPost.PickPoints;
using Newtonsoft.Json;

namespace AdvantShop.Shipping.RussianPost
{
    [ShippingKey("RussianPost")]
    public partial class RussianPost : BaseShippingWithCargo, IShippingSupportingSyncOfOrderStatus, IShippingSupportingTheHistoryOfMovement, IShippingLazyData, IShippingSupportingPaymentCashOnDelivery, IShippingWithBackgroundMaintenance
    {
        #region Ctor

        private readonly string _login;
        private readonly string _password;
        private readonly string _token;
        private readonly string _pointIndex;
        private readonly List<EnMailType> _deliveryTypesOld;
        private readonly List<Tuple<EnMailType, EnMailCategory, EnTransportType>> _localDeliveryTypes;
        private readonly List<Tuple<EnMailType, EnMailCategory, EnTransportType>> _internationalDeliveryTypes;
        private readonly bool _courier;
        private readonly bool _fragile;
        private readonly EnTypeNotification? _typeNotification;
        private readonly bool _smsNotification;
        private readonly int _increaseDeliveryTime;
        private readonly string _yaMapsApiKey;
        private readonly bool _deliveryWithCod;
        private readonly bool _deliveryToOps;
        private readonly bool _deliveryToPochtamat;
        private readonly bool _deliveryToPvz;

        private readonly bool _statusesSync;
        private readonly string _trackingLogin;
        private readonly string _trackingPassword;

        private readonly RussianPostApiService _russianPostApiService;
        private readonly RussianPostTrackingApiService _russianPostTrackingApiService;

        public const string KeyNameOrderRussianPostIdInOrderAdditionalData = "OrderRussianPostId";

        public override string[] CurrencyIso3Available { get { return new[] { "RUB" }; } }

        //GlorySoft_001
        private readonly OrderStatus _statusForReady;
        private const string _basis = "Синхронизация статусов для Почты России";
        private readonly CustomerType _customerType;
        private readonly int _maxWeight;

        public RussianPost(ShippingMethod method, ShippingCalculationParameters calculationParameters) : base(method, calculationParameters)
        {
            _login = _method.Params.ElementOrDefault(RussianPostTemplate.Login);
            _password = _method.Params.ElementOrDefault(RussianPostTemplate.Password);
            _token = _method.Params.ElementOrDefault(RussianPostTemplate.Token);
            _russianPostApiService = new RussianPostApiService(_login, _password, _token);
            _pointIndex = _method.Params.ElementOrDefault(RussianPostTemplate.PointIndex);

            _localDeliveryTypes = new List<Tuple<EnMailType, EnMailCategory, EnTransportType>>();
            _internationalDeliveryTypes = new List<Tuple<EnMailType, EnMailCategory, EnTransportType>>();

            _deliveryTypesOld =
                method.Params.ContainsKey(RussianPostTemplate.DeliveryTypes)
                    ? (method.Params.ElementOrDefault(RussianPostTemplate.DeliveryTypes) ?? string.Empty)
                        .Split(",")
                        .Select(x => GetEnMailTypeByInt(x.TryParseInt()))
                        .ToList()
                    : null;
            var typeInsure = (EnTypeInsure)method.Params.ElementOrDefault(RussianPostTemplate.TypeInsure).TryParseInt();
            var mailCategories = EnMailCategory.AsList();

            if (method.Params.ContainsKey(RussianPostTemplate.LocalDeliveryTypes))
            {
                string[] tempArr;
                foreach (var strDelivery in (method.Params.ElementOrDefault(RussianPostTemplate.LocalDeliveryTypes) ?? string.Empty).Split(","))
                {
                    if ((tempArr = strDelivery.Split("\\")).Length >= 2)
                    {
                        var mailType = EnMailType.Parse(tempArr[0]);
                        if (tempArr.Length > 2 || !RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(mailType))
                            _localDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                mailType,
                                EnMailCategory.Parse(tempArr[1]),
                                tempArr.Length > 2 ? EnTransportType.Parse(tempArr[2]) : null
                                ));
                        else
                            foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[mailType])
                                _localDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                    mailType,
                                    EnMailCategory.Parse(tempArr[1]),
                                    transportType
                                    ));
                    }
                }
            }
            else if (method.Params.ContainsKey(RussianPostTemplate.OldLocalDeliveryTypes))
            {
                // поддержка настроек ранних версий
                string[] tempArr;
                foreach (var strDelivery in (method.Params.ElementOrDefault(RussianPostTemplate.OldLocalDeliveryTypes) ?? string.Empty).Split(","))
                {
                    if ((tempArr = strDelivery.Split("_")).Length >= 2)
                    {
                        var mailType = GetEnMailTypeByInt(tempArr[0].TryParseInt());
                        if (tempArr.Length > 2 || !RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(mailType))
                            _localDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                mailType,
                                GetEnMailCategoryByInt(tempArr[1].TryParseInt()),
                                tempArr.Length > 2 ? GetEnTransportTypeByInt(tempArr[2].TryParseInt()) : null
                                ));
                        else
                            foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[mailType])
                                _localDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                    mailType,
                                    GetEnMailCategoryByInt(tempArr[1].TryParseInt()),
                                    transportType
                                    ));
                    }
                }
            }
            else if (_deliveryTypesOld != null)
            {
                // поддержка настроек ранних версий
                foreach (var deliveryType in _deliveryTypesOld)
                {
                    foreach(var deliveryCategory in mailCategories)
                    {
                        if (typeInsure == EnTypeInsure.WithDeclaredValue && !IsDeclareCategory(deliveryCategory))
                            continue;
                        else if (typeInsure == EnTypeInsure.WithoutDeclaredValue && IsDeclareCategory(deliveryCategory))
                            continue;

                        if (RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(deliveryType))
                        {
                            foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[deliveryType])
                                _localDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                    deliveryType,
                                    deliveryCategory,
                                    transportType
                                    ));
                        }
                        else
                            _localDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                deliveryType,
                                deliveryCategory,
                                null
                                ));
                    }
                }
            }

            if (method.Params.ContainsKey(RussianPostTemplate.InternationalDeliveryTypes))
            {
                string[] tempArr;
                foreach (var strDelivery in (method.Params.ElementOrDefault(RussianPostTemplate.InternationalDeliveryTypes) ?? string.Empty).Split(","))
                {
                    if ((tempArr = strDelivery.Split("\\")).Length >= 2)
                    {
                        _internationalDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                            EnMailType.Parse(tempArr[0]),
                            EnMailCategory.Parse(tempArr[1]),
                            tempArr.Length > 2 ? EnTransportType.Parse(tempArr[2]) : null
                            ));
                    }
                }
            }
            else if (method.Params.ContainsKey(RussianPostTemplate.OldInternationalDeliveryTypes))
            {
                // поддержка настроек ранних версий
                string[] tempArr;
                foreach (var strDelivery in (method.Params.ElementOrDefault(RussianPostTemplate.OldInternationalDeliveryTypes) ?? string.Empty).Split(","))
                {
                    if ((tempArr = strDelivery.Split("_")).Length >= 2)
                    {
                        _internationalDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                            GetEnMailTypeByInt(tempArr[0].TryParseInt()),
                            GetEnMailCategoryByInt(tempArr[1].TryParseInt()),
                            tempArr.Length > 2 ? GetEnTransportTypeByInt(tempArr[2].TryParseInt()) : null
                            ));
                    }
                }
            }
            else if (_deliveryTypesOld != null)
            {
                // поддержка настроек ранних версий
                foreach (var deliveryType in _deliveryTypesOld)
                {
                    foreach (var deliveryCategory in mailCategories)
                    {
                        if (typeInsure == EnTypeInsure.WithDeclaredValue && !IsDeclareCategory(deliveryCategory))
                            continue;
                        else if (typeInsure == EnTypeInsure.WithoutDeclaredValue && IsDeclareCategory(deliveryCategory))
                            continue;

                        if (RussianPostAvailableOption.InternationalTransportTypeAvailable.ContainsKey(deliveryType))
                        {
                            foreach (var transportType in RussianPostAvailableOption.InternationalTransportTypeAvailable[deliveryType])
                                _internationalDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                    deliveryType,
                                    deliveryCategory,
                                    transportType
                                    ));
                        }
                        else
                            _internationalDeliveryTypes.Add(new Tuple<EnMailType, EnMailCategory, EnTransportType>(
                                deliveryType,
                                deliveryCategory,
                                null
                                ));

                    }
                }
            }


            _courier = method.Params.ElementOrDefault(RussianPostTemplate.Courier).TryParseBool();
            _fragile = method.Params.ElementOrDefault(RussianPostTemplate.Fragile).TryParseBool();
            _typeNotification = (EnTypeNotification?)method.Params.ElementOrDefault(RussianPostTemplate.TypeNotification).TryParseInt(true);
            _smsNotification = method.Params.ElementOrDefault(RussianPostTemplate.SmsNotification).TryParseBool();
            _statusesSync = method.Params.ElementOrDefault(RussianPostTemplate.StatusesSync).TryParseBool();
            _increaseDeliveryTime = _method.ExtraDeliveryTime;
            _yaMapsApiKey = _method.Params.ElementOrDefault(RussianPostTemplate.YaMapsApiKey);
            _deliveryWithCod = method.Params.ElementOrDefault(RussianPostTemplate.DeliveryWithCod).TryParseBool();
            _deliveryToOps = method.Params.ElementOrDefault(RussianPostTemplate.DeliveryToOps).TryParseBool();
            _deliveryToPochtamat = method.Params.ElementOrDefault(RussianPostTemplate.DeliveryToPochtamat).TryParseBool();
            _deliveryToPvz = method.Params.ElementOrDefault(RussianPostTemplate.DeliveryToPvz).TryParseBool();

            _trackingLogin = _method.Params.ElementOrDefault(RussianPostTemplate.TrackingLogin);
            _trackingPassword = _method.Params.ElementOrDefault(RussianPostTemplate.TrackingPassword);
            _russianPostTrackingApiService = new RussianPostTrackingApiService(_trackingLogin, _trackingPassword);

            var statusesReference = method.Params.ElementOrDefault(RussianPostTemplate.StatusesReference);
            if (!string.IsNullOrEmpty(statusesReference))
            {
                string[] arr = null;
                _statusesReference =
                    statusesReference
                       .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                       .ToDictionary(
                            x =>
                                (arr = x.Split(','))[0].StartsWith("s")
                                    ? arr[0]
                                    : $"s{arr[0]}",
                            x =>
                                arr.Length > 1
                                    ? arr[1].TryParseInt(true)
                                    : null);
            }
            else
                _statusesReference = new Dictionary<string, int?>();

            //GlorySoft_001
            _statusForReady = OrderStatusService.GetOrderStatus(_method.Params.ElementOrDefault(RussianPostTemplate.StatusForReady).TryParseInt(0));
            _customerType = (CustomerType)_method.Params.ElementOrDefault(RussianPostTemplate.CustomerType).TryParseInt((int)CustomerType.All);
            _maxWeight = _method.Params.ElementOrDefault(RussianPostTemplate.MaxWeight).TryParseInt(0);
        }

        public RussianPostApiService RussianPostApiService
        {
            get { return _russianPostApiService; }
        }

        public bool SmsNotification
        {
            get { return _smsNotification; }
        }

        public bool Courier
        {
            get { return _courier; }
        }

        public bool Fragile
        {
            get { return _fragile; }
        }

        public bool DeliveryWithCod
        {
            get { return _deliveryWithCod; }
        }

        public EnTypeNotification? TypeNotification
        {
            get { return _typeNotification; }
        }

        public float DefaultWeight
        {
            get { return _defaultWeight; }
        }

        public float DefaultHeight
        {
            get { return _defaultHeight; }
        }

        public float DefaultLength
        {
            get { return _defaultLength; }
        }

        public float DefaultWidth
        {
            get { return _defaultWidth; }
        }

        #endregion

        #region Statuses

        //public void SyncStatusOfOrder(Order order)GlorySoft_001
        //{
        //    if (!string.IsNullOrEmpty(order.TrackNumber))
        //    {
        //        var history = _russianPostTrackingApiService.GetBarcodeHistory(order.TrackNumber);
        //        if (history.Body != null && history.Body.Response != null &&
        //            history.Body.Response.OperationHistoryData != null &&
        //            history.Body.Response.OperationHistoryData.HistoryRecords != null &&
        //            history.Body.Response.OperationHistoryData.HistoryRecords.Count > 0)
        //        {
        //            var statusInfo = 
        //                history.Body.Response.OperationHistoryData.HistoryRecords
        //                    .Where(record =>
        //                    {
        //                        var tKey = "s" + record.OperationParameters.OperType.Id;
        //                        var tAttrKey =
        //                            record.OperationParameters.OperAttr != null
        //                                ? $"{tKey}_{record.OperationParameters.OperAttr.Id}"
        //                                : null;
        //                        return
        //                            (StatusesReference.ContainsKey(tKey) 
        //                                && StatusesReference[tKey].HasValue)
        //                            || (tAttrKey != null
        //                                && StatusesReference.ContainsKey(tAttrKey)
        //                                && StatusesReference[tAttrKey].HasValue);
        //                    })
        //                    .OrderByDescending(x => x.OperationParameters.OperDate)
        //                    .FirstOrDefault();

        //            if (statusInfo is null)
        //                return;

        //            var typeAndAttrKey = statusInfo.OperationParameters.OperAttr != null 
        //                ? string.Format("s{0}_{1}", statusInfo.OperationParameters.OperType.Id, statusInfo.OperationParameters.OperAttr.Id) 
        //                : null;
        //            var typeKey = "s" + statusInfo.OperationParameters.OperType.Id;

        //            var russianPostOrderStatus = typeAndAttrKey != null && StatusesReference.ContainsKey(typeAndAttrKey)
        //                ? StatusesReference[typeAndAttrKey]
        //                : StatusesReference.ContainsKey(typeKey) 
        //                    ? StatusesReference[typeKey]
        //                    : null;

        //            if (russianPostOrderStatus.HasValue &&
        //                order.OrderStatusId != russianPostOrderStatus.Value &&
        //                OrderStatusService.GetOrderStatus(russianPostOrderStatus.Value) != null)
        //            {
        //                var lastOrderStatusHistory =
        //                    OrderStatusService.GetOrderStatusHistory(order.OrderID)
        //                        .OrderByDescending(item => item.Date)
        //                        .FirstOrDefault();

        //                if (lastOrderStatusHistory == null ||
        //                    lastOrderStatusHistory.Date < statusInfo.OperationParameters.OperDate)
        //                {
        //                    OrderStatusService.ChangeOrderStatus(order.OrderID,
        //                        russianPostOrderStatus.Value, "Синхронизация статусов для Почты России");
        //                }
        //            }
        //        }
        //    }
        //}

        public void SyncStatusOfOrder(Order order)
        {
            if (!string.IsNullOrEmpty(order.TrackNumber))
            {
                var orderTrackNumber = order.TrackNumber;
                var p = orderTrackNumber.IndexOf("/");
                if (p >= 0)
                    orderTrackNumber = orderTrackNumber.Substring(0, p);
                var history = _russianPostTrackingApiService.GetBarcodeHistory(orderTrackNumber.Trim().ToUpper());

                if (order.OrderStatus.IsCanceled)
                    Debug.Log.Info("OneSApi CheckRefusing ShippingType RussianPost Entity = " + JsonConvert.SerializeObject(history));

                if (history.Body != null && history.Body.Response != null &&
                    history.Body.Response.OperationHistoryData != null &&
                    history.Body.Response.OperationHistoryData.HistoryRecords != null &&
                    history.Body.Response.OperationHistoryData.HistoryRecords.Count > 0)
                {
                    var statusInfo =
                        history.Body.Response.OperationHistoryData.HistoryRecords
                            .Where(record =>
                            {
                                var tKey = "s" + record.OperationParameters.OperType.Id.ToString();
                                var tAttrKey =
                                    record.OperationParameters.OperAttr != null
                                        ? $"{tKey}_{record.OperationParameters.OperAttr.Id}"
                                        : null;
                                return
                                    (StatusesReference.ContainsKey(tKey)
                                        && StatusesReference[tKey].HasValue)
                                    || (tAttrKey != null
                                        && StatusesReference.ContainsKey(tAttrKey)
                                        && StatusesReference[tAttrKey].HasValue);
                            })
                            .OrderByDescending(x => x.OperationParameters.OperDate)
                            .FirstOrDefault();

                    if (statusInfo is null)
                        return;

                    var typeAndAttrKey = statusInfo.OperationParameters.OperAttr != null
                        ? string.Format("s{0}_{1}", statusInfo.OperationParameters.OperType.Id, statusInfo.OperationParameters.OperAttr.Id)
                        : null;
                    var typeKey = "s" + statusInfo.OperationParameters.OperType.Id.ToString();

                    var russianPostOrderStatus = typeAndAttrKey != null && StatusesReference.ContainsKey(typeAndAttrKey)
                        ? StatusesReference[typeAndAttrKey]
                        : StatusesReference.ContainsKey(typeKey)
                            ? StatusesReference[typeKey]
                            : null;

                    if (russianPostOrderStatus.HasValue &&
                        order.OrderStatusId != russianPostOrderStatus.Value &&
                        OrderStatusService.GetOrderStatus(russianPostOrderStatus.Value) != null)
                    {
                        var lastOrderStatusHistory =
                            OrderStatusService.GetOrderStatusHistory(order.OrderID)
                                .OrderByDescending(item => item.Date)
                                .FirstOrDefault();

                        //if (lastOrderStatusHistory == null ||
                        //    lastOrderStatusHistory.Date < statusInfo.OperationParameters.OperDate)
                        if (order.OrderStatus == null ||
                            (!order.OrderStatus.IsCanceled && !order.OrderStatus.IsCompleted))
                        {
                            OrderStatusService.ChangeOrderStatus(order.OrderID,
                                russianPostOrderStatus.Value, _basis);

                            if (_statusForReady != null && russianPostOrderStatus.Value == _statusForReady.StatusID)
                            {
                                var keepFreeUntilOld = OrderService.GetOrderAdditionalData(order.OrderID, "KeepFreeUntil");
                                var keepFreeUntilNew = (DateTime.Today.AddDays(15).AddSeconds(-1)).ToString();
                                if (keepFreeUntilNew != keepFreeUntilOld)
                                {
                                    OrderService.AddUpdateOrderAdditionalData(order.OrderID, "KeepFreeUntil", keepFreeUntilNew);
                                    var comment = "Срок бесплатного хранения: " + keepFreeUntilNew;
                                    var changedBy = new OrderChangedBy(_basis);
                                    OrderService.UpdateAdminOrderComment(order.OrderID, order.AdminOrderComment + "\n" + comment, changedBy);
                                    OrderService.UpdateStatusComment(order.OrderID, comment, changedBy);
                                }
                            }
                        }
                    }
                }
                else if (history.Body == null)
                {
                    Debug.Log.Warn(string.Format("RussianPostSync SyncStatusOfOrder Fault: OrderId={0} Track={1} Body == null", order.OrderID, order.TrackNumber));
                    RussianPostService.CreateFormTaskWrongTrack(2304147, order.Number, order.TrackNumber, order.OrderID);
                    //MailService.SendMailNow(Guid.Empty, SettingsMail.EmailForProductDiscuss, "Некорректный трек-номер", $"Некорректный трек-номер по заказу {order.Number} - <a href='https://www.pochta.ru/tracking?barcode={order.TrackNumber}'>{order.TrackNumber}</a>", true);
                }
                else if (history.Body.Fault != null)
                {
                    Debug.Log.Warn(string.Format("RussianPostSync SyncStatusOfOrder Fault: OrderId={0} Track={1} {2}: {3}", order.OrderID, order.TrackNumber, history.Body.Fault.Code.Value, history.Body.Fault.Reason.Text));
                    if (!(history.Body.Fault.Code.Value == "S:Receiver" && history.Body.Fault.Reason.Text == "Внутренняя ошибка сервиса"))
                        /*var pyrusResponse = */
                        RussianPostService.CreateFormTaskWrongTrack(2304147, order.Number, order.TrackNumber, order.OrderID);
                    //if (pyrusResponse.Key == null)
                    //    errors.Add("Ошибка при отправке запроса");
                    //MailService.SendMailNow(Guid.Empty, SettingsMail.EmailForProductDiscuss, "Некорректный трек-номер", $"Некорректный трек-номер по заказу {order.Number} - <a href='https://www.pochta.ru/tracking?barcode={order.TrackNumber}'>{order.TrackNumber}</a>", true);
                }
            }
        }

        public bool SyncByAllOrders => false;
        public void SyncStatusOfOrders(IEnumerable<Order> orders) => throw new NotImplementedException();

        public bool StatusesSync
        {
            get => _statusesSync;
        }

        private Dictionary<string, int?> _statusesReference;
        public Dictionary<string, int?> StatusesReference
        {
            get => _statusesReference;
        }

        public static Dictionary<string, string> Statuses => new Dictionary<string, string>
        {
            { "s1", "Прием" },
            { "s1_1", "Прием - Единичный" },
            { "s1_2", "Прием - Партионный" },
            { "s1_3", "Прием - Партионный электронно" },
            { "s1_4", "Прием - Упрощенный единичный" },
            { "s1_5", "Прием - По ведомости в РЦ (распределительном центре)" },
            { "s1_6", "Прием - По ведомости в СЦ (сортировочном центре" },
            { "s1_7", "Прием - Упрощенный предзаполненный" },
            { "s1_8", "Прием - Упрощенный предоплаченный" },
            { "s2", "Вручение" },
            { "s2_1", "Вручение - Вручение адресату" },
            { "s2_2", "Вручение - Вручение отправителю" },
            { "s2_3", "Вручение - Выдано адресату через почтомат" },
            { "s2_4", "Вручение - Выдано отправителю через почтомат" },
            { "s2_5", "Вручение - Адресату электронно" },
            { "s2_6", "Вручение - Адресату почтальоном" },
            { "s2_7", "Вручение - Отправителю почтальоном" },
            { "s2_8", "Вручение - Адресату курьером" },
            { "s2_9", "Вручение - Отправителю курьером" },
            { "s2_10", "Вручение - Адресату с контролем ответа" },
            { "s2_11", "Вручение - Адресату с контролем ответа почтальоном" },
            { "s2_12", "Вручение - Адресату с контролем ответа курьером" },
            { "s2_13", "Вручение - Вручение адресату по ПЭП" },
            { "s2_14", "Вручение - Для передачи на оцифровку" },
            { "s3", "Возврат" },
            { "s3_1", "Возврат - Истек срок хранения" },
            { "s3_2", "Возврат - Заявление отправителя" },
            { "s3_3", "Возврат - Отсутствие адресата по указанному адресу" },
            { "s3_4", "Возврат - Отказ адресата" },
            { "s3_5", "Возврат - Смерть адресата" },
            { "s3_6", "Возврат - Невозможно прочесть адрес адресата" },
            { "s3_7", "Возврат - Отказ в выпуске таможней" },
            { "s3_8", "Возврат - Адресат, абонирующий абонементный почтовый шкаф, не указан или указан неправильно" },
            { "s3_9", "Возврат - Иные обстоятельства" },
            { "s3_10", "Возврат - Неверный адрес" },
            { "s3_11", "Возврат - Несоответствие комплектности" },
            { "s3_12", "Возврат - Запрещено САБ" },
            { "s3_13", "Возврат - Для проведения таможенных операций" },
            { "s3_14", "Возврат - Распоряжение ЭТП" },
            { "s3_15", "Возврат - Частичный выкуп" },
            { "s4", "Досылка почты" },
            { "s4_1", "Досылка почты - По заявлению пользователя" },
            { "s4_2", "Досылка почты - Выбытие адресата по новому адресу" },
            { "s4_3", "Досылка почты - Засылка" },
            { "s4_4", "Досылка почты - Запрещено САБ" },
            { "s4_5", "Досылка почты - Передача на временное хранение" },
            { "s4_6", "Досылка почты - Передача в невостребованные" },
            { "s4_7", "Досылка почты - По техническим причинам" },
            { "s5", "Невручение" },
            { "s5_1", "Невручение - Утрачено" },
            { "s5_2", "Невручение - Изъято" },
            { "s5_3", "Невручение - Засылка" },
            { "s5_8", "Невручение - Решение таможни" },
            //{ "s5_9", "Невручение - (зарезервировано)" },
            { "s6", "Хранение" },
            { "s6_1", "Хранение - До востребования" },
            { "s6_2", "Хранение - На абонементный ящик" },
            { "s6_3", "Хранение - Установленный срок хранения" },
            { "s6_4", "Хранение - Продление срока хранения по заявлению адресата" },
            { "s6_5", "Хранение - Продление срока хранения по заявлению отправителя" },
            { "s7", "Временное хранение" },
            { "s7_1", "Временное хранение - Нероздано" },
            { "s7_2", "Временное хранение - Невостребовано" },
            { "s7_3", "Временное хранение - Содержимое запрещено к пересылке" },
            { "s7_4", "Временное хранение - Ожидает результаты экспертизы" },
            { "s8", "Обработка" },
            { "s8_0", "Обработка - Сортировка" },
            { "s8_1", "Обработка - Покинуло место приёма" },
            { "s8_2", "Обработка - Прибыло в место вручения" },
            { "s8_3", "Обработка - Прибыло в сортировочный центр" },
            { "s8_4", "Обработка - Покинуло сортировочный центр" },
            { "s8_5", "Обработка - Прибыло в место международного обмена" },
            { "s8_6", "Обработка - Покинуло место международного обмена" },
            { "s8_7", "Обработка - Прибыло в место транзита" },
            { "s8_8", "Обработка - Покинуло место транзита" },
            { "s8_9", "Обработка - Прибыло в почтомат" },
            { "s8_10", "Обработка - Истекает срок хранения в почтомате" },
            { "s8_11", "Обработка - Переадресовано в почтомат" },
            { "s8_12", "Обработка - Изъято из почтомата" },
            { "s8_13", "Обработка - Прибыло на территорию РФ" },
            { "s8_14", "Обработка - Прибыло в Центр выдачи посылок" },
            { "s8_15", "Обработка - Передано курьеру (водителю)" },
            { "s8_16", "Обработка - Доставлено для вручения электронно" },
            { "s8_17", "Обработка - Направлено в ЦГП" },
            { "s8_18", "Обработка - Передано почтальону" },
            { "s8_19", "Обработка - Передача в кладовую хранения" },
            { "s8_20", "Обработка - Покинуло место возврата/досылки" },
            { "s8_21", "Обработка - Уточнение адреса" },
            { "s8_22", "Обработка - Ожидает курьерской доставки" },
            { "s8_23", "Обработка - Продление срока хранения" },
            { "s8_24", "Обработка - Направлено извещение" },
            { "s8_25", "Обработка - Доставлено извещение" },
            { "s8_26", "Обработка - Зарегистрировано с новым номером" },
            { "s8_27", "Обработка - Истекает срок хранения (осталось 25 дней)" },
            { "s8_28", "Обработка - Истекает срок хранения (осталось 5 дней)" },
            { "s8_29", "Обработка - Находится на техническом исследовании" },
            { "s8_30", "Обработка - Поступило в ЦГП" },
            { "s8_31", "Обработка - Покинуло ЦГП" },
            { "s8_32", "Обработка - Направлено в УКД" },
            { "s8_33", "Обработка - Истекает срок хранения (осталось 23 дня)" },
            { "s8_34", "Обработка - До ввод данных" },
            { "s8_35", "Обработка - Истекает срок хранения (осталось 10 дней)" },
            { "s8_36", "Обработка - Зарегистрировано заявление на возврат" },
            { "s8_37", "Обработка - Зарегистрировано заявление отправителя на продление срока хранения" },
            { "s8_38", "Обработка - Зарегистрировано заявление адресата на продление срока хранения" },
            { "s8_39", "Обработка - Сдано в ПВЗ" },
            { "s8_40", "Обработка - Истек срок хранения" },
            { "s8_41", "Обработка - Возврат скомплектован" },
            { "s8_42", "Обработка - Истекает срок хранения (остался 1 день)" },
            { "s8_43", "Обработка - Истек срок хранения (осталось 2 дня)" },
            { "s8_44", "Обработка - Сдано в СЦ" },
            { "s8_45", "Обработка - Резерв" },
            { "s8_46", "Обработка - Резерв" },
            { "s8_47", "Обработка - Приостановлена обработка по 115-ФЗ" },
            { "s8_48", "Обработка - Присвоено место адресного хранения" },
            { "s8_49", "Обработка - Направлено в пункт выдачи" },
            { "s9", "Импорт международной почты" },
            { "s10", "Экспорт международной почты" },
            { "s11", "Прием на таможню" },
            { "s12", "Неудачная попытка вручения" },
            { "s12_1", "Неудачная попытка вручения - Временное отсутствие адресата" },
            { "s12_2", "Неудачная попытка вручения - Доставка отложена по просьбе адресата" },
            { "s12_3", "Неудачная попытка вручения - Неполный адрес" },
            { "s12_4", "Неудачная попытка вручения - Неправильный/нечитаемый/неполный адрес" },
            { "s12_5", "Неудачная попытка вручения - Адресат выбыл" },
            { "s12_6", "Неудачная попытка вручения - Адресат отказался от отправления" },
            { "s12_7", "Неудачная попытка вручения - Форс-мажор/непредвиденные обстоятельства" },
            { "s12_8", "Неудачная попытка вручения - Иная" },
            { "s12_9", "Неудачная попытка вручения - Адресат заберет отправление сам" },
            { "s12_10", "Неудачная попытка вручения - Адресат не доступен" },
            { "s12_11", "Неудачная попытка вручения - Неудачная доставка" },
            { "s12_12", "Неудачная попытка вручения - Истек срок хранения в почтомате" },
            { "s12_13", "Неудачная попытка вручения - По требованию отправителя" },
            { "s12_14", "Неудачная попытка вручения - Отправление повреждено и/или без вложения" },
            { "s12_15", "Неудачная попытка вручения - В ожидании оплаты сбора" },
            { "s12_16", "Неудачная попытка вручения - Адресат переехал" },
            { "s12_17", "Неудачная попытка вручения - У адресата есть абонентский ящик" },
            { "s12_18", "Неудачная попытка вручения - Нет доставки на дом" },
            { "s12_19", "Неудачная попытка вручения - Не отвечает таможенным требованиям" },
            { "s12_20", "Неудачная попытка вручения - Неполные/недостаточные/неверные документы" },
            { "s12_21", "Неудачная попытка вручения - Невозможно связаться с клиентом" },
            { "s12_22", "Неудачная попытка вручения - Адресат бастует" },
            { "s12_23", "Неудачная попытка вручения - Запрещенные вложения – отправление не доставлено" },
            { "s12_24", "Неудачная попытка вручения - Отказ в импорте – запрещенные вложения" },
            { "s12_25", "Неудачная попытка вручения - Засыл отправления" },
            { "s12_26", "Неудачная попытка вручения - За смертью получателя" },
            { "s12_27", "Неудачная попытка вручения - Национальный праздник" },
            { "s12_28", "Неудачная попытка вручения - Утрата" },
            { "s13", "Регистрация отправки" },
            { "s14", "Таможенное оформление" },
            { "s14_1", "Таможенное оформление - Выпущено таможней" },
            { "s14_2", "Таможенное оформление - Возвращено таможней" },
            { "s14_3", "Таможенное оформление - Осмотрено таможней" },
            { "s14_4", "Таможенное оформление - Отказ в выпуске" },
            { "s14_5", "Таможенное оформление - Направлено с таможенным уведомлением" },
            { "s14_6", "Таможенное оформление - Направлено с обязательной уплатой таможенных платежей" },
            { "s14_7", "Таможенное оформление - Требуется досмотр" },
            { "s14_8", "Таможенное оформление - Выпуск приостановлен" },
            { "s14_9", "Таможенное оформление - Отказ в приеме на таможню" },
            { "s14_10", "Таможенное оформление - Проведен осмотр с использованием ТСТК" },
            { "s14_11", "Таможенное оформление - ТК в рамках СУР*" },
            { "s14_12", "Таможенное оформление - Досмотр завершен" },
            { "s14_13", "Таможенное оформление - Выпуск разрешен. Таможенные платежи уплачены" },
            { "s14_14", "Таможенное оформление - Отказ в выпуске. Таможенные платежи не уплачены" },
            { "s14_15", "Таможенное оформление - Таможенная декларация не прошла ФЛК" },
            { "s14_16", "Таможенное оформление - Выпущено таможней. Средства зарезервированы" },
            { "s14_17", "Таможенное оформление - Отказ в выпуске по решению должностного лица" },
            { "s14_18", "Таможенное оформление - Требуется предъявление в ТО" },
            { "s14_19", "Таможенное оформление - Отказ в автоматическом выпуске" },
            { "s14_20", "Таможенное оформление - Отказ в выпуске. Товары не предъявлены" },
            { "s14_21", "Таможенное оформление - Требуются результаты осмотра" },
            { "s14_22", "Таможенное оформление - Возврат разрешен" },
            { "s14_23", "Таможенное оформление - Отказ в выпуске возвращаемых товаров" },
            { "s14_24", "Таможенное оформление - Требуется рентген-контроль" },
            { "s14_25", "Таможенное оформление - В убытии отказано" },
            { "s14_26", "Таможенное оформление - Убытие разрешено" },
            { "s15", "Передача на временное хранение" },
            { "s16", "Уничтожение" },
            { "s17", "Оформление в собственность" },
            { "s18", "Регистрация утраты" },
            { "s19", "Таможенные платежи поступили" },
            { "s20", "Регистрация" },
            { "s21", "Доставка" },
            { "s21_1", "Доставка - Доставлено в почтовый ящик" },
            { "s21_2", "Доставка - Доставлено в руки адресату под роспись" },
            { "s21_3", "Доставка - Доставлено на ресепшн или секретариат под роспись" },
            { "s22", "Недоставка" },
            { "s22_1", "Недоставка - Отсутствие п/я" },
            { "s22_2", "Недоставка - Отсутствие улицы, дома, квартиры" },
            { "s22_3", "Недоставка - Несоответствие индекса ОПС выдачи" },
            { "s22_4", "Недоставка - Неверный адрес/адрес не существует" },
            { "s22_5", "Недоставка - Отказ в получении" },
            { "s22_6", "Недоставка - Нет доступа по адресу" },
            { "s22_7", "Недоставка - Организация сменила адрес" },
            { "s22_8", "Недоставка - Адресат выбыл" },
            { "s22_9", "Недоставка - Отправление не поместилось в п/я" },
            { "s23", "Поступление на временное хранение" },
            { "s24", "Продление срока выпуска таможней" },
            { "s24_1", "Продление срока выпуска таможней - Предъявить на ветеринарный контроль" },
            { "s24_2", "Продление срока выпуска таможней - Предъявить на фитосанитарный контроль" },
            { "s24_3", "Продление срока выпуска таможней - Отбор проб и образцов" },
            { "s24_4", "Продление срока выпуска таможней - Прочее" },
            { "s24_1019", "Продление срока выпуска таможней - Запрещенные объекты" },
            { "s24_1020", "Продление срока выпуска таможней - Имеются ограничения на импортируемые вложения" },
            { "s24_1050", "Продление срока выпуска таможней - Счет отсутствует" },
            { "s24_1051", "Продление срока выпуска таможней - Счет некорректен" },
            { "s24_1052", "Продление срока выпуска таможней - Сертификат отсутствует или некорректен" },
            { "s24_1053", "Продление срока выпуска таможней - Сертификат некорректен" },
            { "s24_1054", "Продление срока выпуска таможней - Таможенная декларация отсутствует или некорректна" },
            { "s24_1055", "Продление срока выпуска таможней - CN 22/23 некорректна" },
            { "s24_1056", "Продление срока выпуска таможней - Вложения с высокой стоимостью - требуется ТД" },
            { "s24_1057", "Продление срока выпуска таможней - Контакт с клиентом невозможен" },
            { "s24_1058", "Продление срока выпуска таможней - Ожидается передача в таможенный орган" },
            { "s24_1059", "Продление срока выпуска таможней - Требуются данные НДС/Номер ввоза" },
            { "s24_1060", "Продление срока выпуска таможней - Требуется сертификат для возвращенных вложений" },
            { "s24_1061", "Продление срока выпуска таможней - Требуется форма перевода для банка" },
            { "s24_1062", "Продление срока выпуска таможней - Некомплектная поставка" },
            { "s24_1063", "Продление срока выпуска таможней - Передано в таможенный орган" },
            { "s24_1064", "Продление срока выпуска таможней - Задержано в таможенном органе без указания причины" },
            { "s24_1065", "Продление срока выпуска таможней - Ожидается подтверждение стоимости от получателя" },
            { "s24_1070", "Продление срока выпуска таможней - Имеются ограничения на экспортируемые вложения" },
            { "s24_1073", "Продление срока выпуска таможней - Неполная или некорректная документация" },
            { "s25", "Вскрытие" },
            { "s26", "Отмена" },
            { "s26_1", "Отмена - Требование отправителя" },
            { "s26_2", "Отмена - Ошибка оператора" },
            { "s27", "Получена электронная регистрация" },
            { "s28", "Присвоение идентификатора" },
            { "s29", "Регистрация прохождения в ММПО" },
            { "s30", "Отправка SRM" },
            { "s31", "Обработка перевозчиком" },
            { "s31_1", "Обработка перевозчиком - Транспорт прибыл" },
            { "s31_5", "Обработка перевозчиком - Бронирование подтверждено" },
            { "s31_6", "Обработка перевозчиком - Включено в план погрузки" },
            { "s31_7", "Обработка перевозчиком - Исключено из плана погрузки" },
            { "s31_14", "Обработка перевозчиком - Транспортный участок завершен" },
            { "s31_21", "Обработка перевозчиком - Доставлено" },
            { "s31_23", "Обработка перевозчиком - Почта в месте назначения" },
            { "s31_24", "Обработка перевозчиком - Погружено на борт" },
            { "s31_31", "Обработка перевозчиком - В пути" },
            { "s31_40", "Обработка перевозчиком - Почта поступила на склад перевозчика" },
            { "s31_41", "Обработка перевозчиком - Перегрузка" },
            { "s31_42", "Обработка перевозчиком - Передано другому перевозчику" },
            { "s31_43", "Обработка перевозчиком - Получено от другого перевозчика" },
            { "s31_48", "Обработка перевозчиком - Погружено" },
            { "s31_57", "Обработка перевозчиком - Не погружено" },
            { "s31_59", "Обработка перевозчиком - Выгружено" },
            { "s31_74", "Обработка перевозчиком - Принято к перевозке" },
            { "s31_82", "Обработка перевозчиком - Возвращено" },
            { "s32", "Поступление АПО" },
            { "s33", "Международная обработка" },
            { "s33_1", "Международная обработка - Передано перевозчику" },
            { "s33_2", "Международная обработка - Получено назначенным оператором" },
            { "s33_3", "Международная обработка - Обработка назначенным оператором" },
            { "s34", "Электронное уведомление загружено" },
            { "s35", "Отказ в курьерской доставке" },
            { "s35_1", "Отказ в курьерской доставке - Не подлежащий доставке вид почтового отправления" },
            { "s35_2", "Отказ в курьерской доставке - Превышение предельного веса, подлежащее доставке" },
            { "s35_3", "Отказ в курьерской доставке - Превышение габаритных размеров, подлежащее доставке" },
            { "s35_4", "Отказ в курьерской доставке - Дефектное почтовое отправление" },
            { "s35_5", "Отказ в курьерской доставке - Наличие Таможенного уведомления" },
            { "s35_6", "Отказ в курьерской доставке - Отсутствие Соглашения об обмене почтовыми отправлениями с наложенным платежом" },
            { "s35_7", "Отказ в курьерской доставке - Возвращенное почтовое отправление" },
            { "s35_8", "Отказ в курьерской доставке - Превышение суммы наложенного платежа, подлежащей взиманию на дому" },
            { "s35_9", "Отказ в курьерской доставке - Неверно оформленные бланки или их отсутствие" },
            { "s36", "Уточнение вида оплаты доставки" },
            { "s36_0", "Уточнение вида оплаты доставки - Включена в тариф" },
            { "s36_1", "Уточнение вида оплаты доставки - Платная" },
            { "s36_2", "Уточнение вида оплаты доставки - Предоплаченная" },
            { "s37", "Предварительное оформление" },
            { "s38", "Задержка для уточнений у отправителя" },
            { "s38_4", "Задержка для уточнений у отправителя - Неправильный/нечитаемый/неполный адрес" },
            { "s38_13", "Задержка для уточнений у отправителя - По требованию отправителя" },
            { "s38_25", "Задержка для уточнений у отправителя - Засыл отправления" },
            { "s39", "Таможенное декларирование" },
            { "s39_1", "Таможенное декларирование - Регистрация" },
            { "s39_2", "Таможенное декларирование - Предварительное решение  \\\"выпуск разрешен\\\"" },
            { "s39_3", "Таможенное декларирование - Отказ в выпуске товаров. Требуется предъявление таможенному органу без осмотра" },
            { "s39_4", "Таможенное декларирование - Отказ в выпуске товаров. Требуется предъявление таможенному органу с осмотром" },
            { "s39_5", "Таможенное декларирование - Отказ в регистрации" },
            { "s39_6", "Таможенное декларирование - Отказ в выпуске. Товары не предъявлены" },
            { "s39_7", "Таможенное декларирование - Данные от торговой площадки получены" },
            { "s39_8", "Таможенное декларирование - Выпуск разрешен" },
            { "s39_9", "Таможенное декларирование - Отказ в выпуске" },
            { "s40", "Таможенный контроль" },
            { "s40_1", "Таможенный контроль - Платежи уплачены" },
            { "s40_2", "Таможенный контроль - Уведомление о проведении контроля" },
            { "s40_3", "Таможенный контроль - Отказ в выпуске по решению должностного лица" },
            { "s41", "Обработка таможенных платежей" },
            { "s41_1", "Обработка таможенных платежей - Сумма платежа удержана УО" },
            { "s41_2", "Обработка таможенных платежей - Сумма платежа списана ФТС" },
            { "s41_3", "Обработка таможенных платежей - Сумма платежа для удержания УО" },
            { "s41_4", "Обработка таможенных платежей - Сумма платежа удержана УО полностью" },
            { "s41_5", "Обработка таможенных платежей - Сумма платежа рассчитана УО" },
            { "s41_6", "Обработка таможенных платежей - Сумма платежа удержана ФТС полностью" },
            { "s42", "Вторая неудачная попытка вручения" },
            { "s42_1", "Вторая неудачная попытка вручения - Временное отсутствие адресата" },
            { "s42_2", "Вторая неудачная попытка вручения - Доставка отложена по просьбе адресата" },
            { "s42_3", "Вторая неудачная попытка вручения - Неполный адрес" },
            { "s42_4", "Вторая неудачная попытка вручения - Неправильный/нечитаемый/неполный адрес" },
            { "s42_5", "Вторая неудачная попытка вручения - Адресат выбыл" },
            { "s42_6", "Вторая неудачная попытка вручения - Адресат отказался от отправления" },
            { "s42_7", "Вторая неудачная попытка вручения - Форс-мажор/непредвиденные обстоятельства" },
            { "s42_8", "Вторая неудачная попытка вручения - Иная" },
            { "s42_9", "Вторая неудачная попытка вручения - Адресат заберет отправление сам" },
            { "s42_10", "Вторая неудачная попытка вручения - Адресат не доступен" },
            { "s42_11", "Вторая неудачная попытка вручения - Неудачная доставка" },
            { "s42_12", "Вторая неудачная попытка вручения - Истек срок хранения в почтомате" },
            { "s42_13", "Вторая неудачная попытка вручения - По требованию отправителя" },
            { "s42_14", "Вторая неудачная попытка вручения - Отправление повреждено и/или без вложения" },
            { "s42_15", "Вторая неудачная попытка вручения - В ожидании оплаты сбора" },
            { "s42_16", "Вторая неудачная попытка вручения - Адресат переехал" },
            { "s42_17", "Вторая неудачная попытка вручения - У адресата есть абонентский ящик" },
            { "s42_18", "Вторая неудачная попытка вручения - Нет доставки на дом" },
            { "s42_19", "Вторая неудачная попытка вручения - Не отвечает таможенным требованиям" },
            { "s42_20", "Вторая неудачная попытка вручения - Неполные/недостаточные/неверные документы" },
            { "s42_21", "Вторая неудачная попытка вручения - Невозможно связаться с клиентом" },
            { "s42_22", "Вторая неудачная попытка вручения - Адресат бастует" },
            { "s42_23", "Вторая неудачная попытка вручения - Запрещенные вложения – отправление не доставлено" },
            { "s42_24", "Вторая неудачная попытка вручения - Отказ в импорте – запрещенные вложения" },
            { "s42_25", "Вторая неудачная попытка вручения - Засыл отправления" },
            { "s42_26", "Вторая неудачная попытка вручения - За смертью получателя" },
            { "s42_27", "Вторая неудачная попытка вручения - Национальный праздник" },
            { "s42_28", "Вторая неудачная попытка вручения - Утрата" },
            { "s43", "Вручение разрешено" },
            { "s44", "Отказ в приеме" },
            { "s45", "Отказ от отправки электронного уведомления получателем" },
            { "s46", "Отмена присвоения идентификатора" },
            { "s47", "Подтверждение возможности приема" },
            { "s48", "Частичное вручение" },
            { "s49", "Отказ в продлении срока хранения" },
            { "s49_1", "Отказ в продлении срока хранения - Отправление уже вручено" },
            { "s49_2", "Отказ в продлении срока хранения - Заявка на продление срока хранения получена повторно" },
            { "s49_3", "Отказ в продлении срока хранения - Заявка на продление срока хранения подана позже, чем за 24 часа окончания нормативного срока хранения" },
            { "s49_4", "Отказ в продлении срока хранения - Наличие дебиторской задолженности по корпоративному клиенту" },
            { "s49_5", "Отказ в продлении срока хранения - Наличие запрета со стороны Компании дистанционной торговли (интернет-магазина) на продление срока хранения" },
            { "s49_6", "Отказ в продлении срока хранения - ПВЗ закрыт" },
            { "s50", "Неудачная доставка в АПС" },
            { "s50_1", "Неудачная доставка в АПС - АПС отсутствует" },
            { "s50_2", "Неудачная доставка в АПС - АПС не работает" },
            { "s50_3", "Неудачная доставка в АПС - К АПС нет доступа" },
            { "s50_4", "Неудачная доставка в АПС - Курьер не успел" },
            { "s50_5", "Неудачная доставка в АПС - Нет свободных ячеек в АПС" },
            { "s50_6", "Неудачная доставка в АПС - Курьер не смог совершить закладку" },
            { "s50_7", "Неудачная доставка в АПС - Размер открывшейся ячейки меньше размера отправления" },
            { "s51", "Неудачная доставка в ПВЗ" },
            { "s51_1", "Неудачная доставка в ПВЗ - ПВЗ закрыт временно" },
            { "s51_2", "Неудачная доставка в ПВЗ - ПВЗ закрыт постоянно" },
            { "s51_3", "Неудачная доставка в ПВЗ - Отказ ПВЗ в приеме" },
            { "s51_4", "Неудачная доставка в ПВЗ - Курьер не успел" },
            { "s52", "Отказ в резервировании ячейки АПС" }
        };

        #endregion

        #region IShippingSupportingTheHistoryOfMovement

        public bool ActiveHistoryOfMovement
        {
            get { return true; }
        }
        public List<HistoryOfMovement> GetHistoryOfMovement(Order order)
        {
            if (!string.IsNullOrEmpty(order.TrackNumber) && !string.IsNullOrEmpty(_trackingLogin) && !string.IsNullOrEmpty(_trackingPassword))
            {
                var history = _russianPostTrackingApiService.GetBarcodeHistory(order.TrackNumber);
                if (history.Body != null && history.Body.Response != null &&
                    history.Body.Response.OperationHistoryData != null &&
                    history.Body.Response.OperationHistoryData.HistoryRecords != null &&
                    history.Body.Response.OperationHistoryData.HistoryRecords.Count > 0)
                {
                    return history.Body.Response.OperationHistoryData.HistoryRecords
                        .OrderByDescending(x => x.OperationParameters.OperDate).Select(statusInfo =>
                            new HistoryOfMovement()
                            {
                                Code = statusInfo.OperationParameters.OperType.Id.ToString(),
                                Name = statusInfo.OperationParameters.OperAttr.Name.IsNotEmpty()
                                    ? statusInfo.OperationParameters.OperAttr.Name
                                    : statusInfo.OperationParameters.OperType.Name,
                                Date = statusInfo.OperationParameters.OperDate,
                                Comment = statusInfo.AddressParameters != null &&
                                          statusInfo.AddressParameters.OperationAddress != null
                                    ? string.Join(", ", new[]
                                        {
                                            statusInfo.AddressParameters.OperationAddress.Index,
                                            statusInfo.AddressParameters.OperationAddress.Description
                                        }
                                        .Where(x => x.IsNotEmpty()))
                                    : null
                            }).ToList();
                }
            }

            return null;
        }

        #endregion IShippingSupportingTheHistoryOfMovement

        protected override IEnumerable<BaseShippingOption> CalcOptions(CalculationVariants calculationVariants)
        {
            //GlorySoft_001
            if (_customerType != CustomerType.All && !_calculationParameters.IsFromAdminArea && CustomerContext.CurrentCustomer?.CustomerType != _customerType)
                return null;
            if (_maxWeight > 0)
            {
                int weight = (int)GetTotalWeight(1000);
                if (weight > _maxWeight * 1000)
                    return null;
            }
            var totalPrice = _calculationParameters.ItemsTotalPriceWithDiscounts;
            if (totalPrice > 50000 && _customerType == CustomerType.LegalEntity)
                return null;

            var cancellationTokenSource = new CancellationTokenSource();
#if DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));
#endif
#if !DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(9));
#endif
            try
            {
                var shippingOptions = new List<BaseShippingOption>();

                string countryIso2 = null;
                if (_calculationParameters.Country.IsNotEmpty())
                    countryIso2 = CountryService.GetIso2(_calculationParameters.Country);

                var isInternational = !"ru".Equals(countryIso2, StringComparison.OrdinalIgnoreCase);
                var countryCode = isInternational
                    ? CountryService.Iso2ToIso3Number(countryIso2).TryParseInt(true)
                    : null;
                var zip = isInternational ? null : LoadZip(); // индекс при расчете нужен только для россии (иначе сервис перестает считать)

                var settigns = CacheManager.Get(string.Format("RussianPostAccountSettings_{0}", _token), 60,
                    () => _russianPostApiService.GetAccountSettings());

                if (settigns != null && settigns.ShippingPoints != null)
                {
                    var shippingPoint = settigns.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex == _pointIndex);
                    if (shippingPoint != null)
                    {
                        IList<AvailableProduct> availableProducts = isInternational
                            // международная доставка
                            ? shippingPoint.AvailableProducts.Where(x => IsInternational(x) && _internationalDeliveryTypes.Any(dt => dt.Item1 == x.MailType && dt.Item2 == x.MailCategory)).ToList()
                            // по России
                            : shippingPoint.AvailableProducts.Where(x => !IsInternational(x) && _localDeliveryTypes.Any(dt => dt.Item1 == x.MailType && dt.Item2 == x.MailCategory)).ToList(); 

                        if (isInternational && !countryCode.HasValue)
                            return shippingOptions;

                        if (availableProducts.Count > 0)
                        {
                            var orderCost = _totalPrice;
                            int weight = (int)GetTotalWeight(1000);
                            var dimensionsHelp = GetDimensions(rate: 10);
                            var dimensions = new Dimension()
                            {
                                Length = (int)Math.Ceiling(dimensionsHelp[0]),
                                Width = (int)Math.Ceiling(dimensionsHelp[1]),
                                Height = (int)Math.Ceiling(dimensionsHelp[2]),
                            };

                            string selectedPoint = null;
                            if (_calculationParameters.ShippingOption != null &&
                                _calculationParameters.ShippingOption.ShippingType == ((ShippingKeyAttribute)typeof(RussianPost).GetCustomAttributes(typeof(ShippingKeyAttribute), false).First()).Value)
                            {
                                if (_calculationParameters.ShippingOption.GetType() == typeof(RussianPostPointDeliveryMapOption))
                                    selectedPoint = ((RussianPostPointDeliveryMapOption)_calculationParameters.ShippingOption).PickpointId;
                            }

                            var calcMail = new HashSet<int>();

                            var tasks = new List<Task<BaseShippingOption>>();

                            foreach (var availableProduct in availableProducts/*.OrderBy(x => x.MailType.ToString())*/)
                            {
                                var isPickPointProduct = 
                                    availableProduct.MailType == EnMailType.ECOM 
                                    || IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory)
                                    || RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(availableProduct.MailType)
                                    || RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(availableProduct.MailType);
                                var isCourierProduct = IsCourierTariff(availableProduct.MailType);
                                
                                // если нет возможности отображать карту
                                // убираем продуктыы, для которых эти данные обязательны (отображение на карте)
                                if (_yaMapsApiKey.IsNullOrEmpty() 
                                    && isPickPointProduct
                                    && !isCourierProduct)
                                    continue;
                                    
                                if (!calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                    && isPickPointProduct
                                    && !isCourierProduct)
                                    continue;
                                
                                if (!calculationVariants.HasFlag(CalculationVariants.Courier)
                                    && isCourierProduct
                                    && !isPickPointProduct)
                                    continue;
                                
                                if (IsCodCategory(availableProduct.MailCategory))
                                    continue;// убираем данный тип из расчетов, т.к. это будет определяться выбраным типом оплаты (наложенный платеж)

                                var hash = 17 ^ availableProduct.MailType.ToString().GetHashCode() ^ availableProduct.MailCategory.ToString().GetHashCode();
                                
                                if (!calcMail.Add(hash))
                                    continue;

                                if (isInternational)
                                {
                                    if (RussianPostAvailableOption.InternationalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                                        foreach (var transportType in RussianPostAvailableOption.InternationalTransportTypeAvailable[availableProduct.MailType])
                                        {
                                            if (_internationalDeliveryTypes.Any(dt => dt.Item1 == availableProduct.MailType && dt.Item2 == availableProduct.MailCategory && dt.Item3 == transportType))
                                                tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, zip, countryCode, selectedPoint,
                                                      orderCost, weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: false, toPochtamat: false, cancellationTokenSource.Token));
                                        }
                                    else
                                        tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, zip, countryCode, selectedPoint, orderCost,
                                                  weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: false, toPochtamat: false, cancellationTokenSource.Token));
                                }
                                else
                                {
                                    if (RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                                        foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[availableProduct.MailType])
                                        {
                                            if (_localDeliveryTypes.Any(dt => dt.Item1 == availableProduct.MailType && dt.Item2 == availableProduct.MailCategory && dt.Item3 == transportType))
                                            {
                                                // через один тариф ЕКОМ Маркетплейс можно доставить 3мя способами
                                                // (тут только курьера пропускаем для ЕКОМ Маркетплейс)
                                                if (availableProduct.MailType != EnMailType.EcomMarketplace
                                                    || !_courier
                                                    || calculationVariants.HasFlag(CalculationVariants.Courier))
                                                    tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, zip, countryCode, selectedPoint,
                                                        orderCost, weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: false, toPochtamat: false, cancellationTokenSource.Token));
                                                
                                                // до востредования
                                                if (_deliveryToOps
                                                  && calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                                  && RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(availableProduct.MailType)
                                                  // в ЕКОМ и почтамат не надо считать как "до востредования"
                                                  && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                                    tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, zip, countryCode, selectedPoint,
                                                        orderCost, weight, dimensions, availableProducts, settigns, isInternational, toOps: true, toPvz: false, toPochtamat: false, cancellationTokenSource.Token));
                                                
                                                // в постомат
                                                if (_deliveryToPochtamat
                                                  && calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                                  && RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(availableProduct.MailType)
                                                  // в ЕКОМ и почтамат не надо считать
                                                  && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                                    tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, zip, countryCode, selectedPoint,
                                                        orderCost, weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: false, toPochtamat: true, cancellationTokenSource.Token));
                                                
                                                // в ПВЗ
                                                if (_deliveryToPvz
                                                  && calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                                  && RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(availableProduct.MailType)
                                                  // в ЕКОМ и почтамат не надо считать
                                                  && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                                    tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, zip, countryCode, selectedPoint,
                                                        orderCost, weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: true, toPochtamat: false, cancellationTokenSource.Token));
                                            }
                                        }
                                    else
                                    {
                                        // через один тариф ЕКОМ Маркетплейс можно доставить 3мя способами
                                        // (тут только курьера пропускаем для ЕКОМ Маркетплейс)
                                        if (availableProduct.MailType != EnMailType.EcomMarketplace 
                                            || !_courier
                                            || calculationVariants.HasFlag(CalculationVariants.Courier))
                                            tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, zip, countryCode, selectedPoint, orderCost,
                                                      weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: false, toPochtamat: false, cancellationTokenSource.Token));

                                        // до востредования
                                        if (_deliveryToOps 
                                          && calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                          && RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(availableProduct.MailType)
                                          // в ЕКОМ и почтамат не надо считать как "до востредования"
                                          && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                            tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, zip, countryCode, selectedPoint, orderCost,
                                                weight, dimensions, availableProducts, settigns, isInternational, toOps: true, toPvz: false, toPochtamat: false, cancellationTokenSource.Token));
                                                
                                        // в постомат
                                        if (_deliveryToPochtamat
                                            && calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                            && RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(availableProduct.MailType)
                                            // в ЕКОМ и почтамат не надо считать
                                            && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                            tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, zip, countryCode, selectedPoint, orderCost,
                                                weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: false, toPochtamat: true, cancellationTokenSource.Token));
                                                
                                        // в ПВЗ
                                        if (_deliveryToPvz
                                            && calculationVariants.HasFlag(CalculationVariants.PickPoint)
                                            && RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(availableProduct.MailType)
                                            // в ЕКОМ и почтамат не надо считать
                                            && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                            tasks.Add(CalcOptionsAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, zip, countryCode, selectedPoint, orderCost,
                                                weight, dimensions, availableProducts, settigns, isInternational, toOps: false, toPvz: true, toPochtamat: false, cancellationTokenSource.Token));
                                    }
                                }
                            }

                        Task.WaitAll(tasks.ToArray()/*, TimeSpan.FromSeconds(30)*/);
                        tasks
                           .Where(x => x.Exception != null)
                           .ForEach(Debug.Log.Warn);
                        tasks
                           .Where(x => x.IsCompleted && x.IsFaulted is false)
                           .Select(x => x.Result)
                           .Where(x => x != null)
                           .ForEach(shippingOptions.Add);
                        }
                    }
                }

                return shippingOptions.OrderBy(x => x.Rate);
            }
            catch (ThreadAbortException)
            {
                cancellationTokenSource.Cancel();
            }

            return null;
        }

        protected override IEnumerable<BaseShippingOption> CalcOptionsToPoint(string pointId)
        {
            var cancellationTokenSource = new CancellationTokenSource();
#if DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));
#endif
#if !DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
#endif
            try
            {
                var cancellationToken = cancellationTokenSource.Token;
                var settigns = CacheManager.Get(string.Format("RussianPostAccountSettings_{0}", _token), 60,
                    () => _russianPostApiService.GetAccountSettings());
                
                if (settigns is null || settigns.ShippingPoints is null)
                    return null;
                
                var shippingPoint = settigns.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex == _pointIndex);
                if (shippingPoint is null)
                    return null;
                
                // по России
                IList<AvailableProduct> availableProducts = 
                    shippingPoint.AvailableProducts
                                 .Where(x => !IsInternational(x) && _localDeliveryTypes.Any(dt => dt.Item1 == x.MailType && dt.Item2 == x.MailCategory))
                                 .ToList();
                
                if (availableProducts.Count <= 0)
                    return null;

                var isAvailableECOM = availableProducts.Any(product => product.MailType == EnMailType.ECOM);
                var isAvailablePochtamats = 
                    availableProducts.Any(product => 
                        IsPochtamatsTariff(product.MailType, product.MailCategory)
                        || (_deliveryToPochtamat && RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(product.MailType)));
                var isAvailableOps = _deliveryToOps && availableProducts.Any(product => RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(product.MailType));
                var isAvailablePvz = _deliveryToPvz && availableProducts.Any(product => RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(product.MailType));
                
                if (isAvailableECOM is false 
                    && isAvailablePochtamats is false 
                    && isAvailableOps is false
                    && isAvailablePvz is false)
                    return null;
                   
                var orderCost = _totalPrice;
                int weightInGrams = (int)GetTotalWeight(1000);
                var dimensionsInCentimeter = GetDimensions(rate: 10);
                var dimensions = new Dimension()
                {
                    Length = (int)Math.Ceiling(dimensionsInCentimeter[0]),
                    Width = (int)Math.Ceiling(dimensionsInCentimeter[1]),
                    Height = (int)Math.Ceiling(dimensionsInCentimeter[2]),
                };
                var dimensionType = EnDimensionType.GetDimensionType(dimensions);

                DeliveryPoint deliveryPointECOM = null;
                PickPointRussianPost pickPointRussianPost = null;

                if (isAvailablePochtamats
                    || isAvailableOps
                    || isAvailablePvz)
                {
                    var weightInKg = MeasureUnits.ConvertWeight(weightInGrams, MeasureUnits.WeightUnit.Grams, MeasureUnits.WeightUnit.Kilogramm);

                    pickPointRussianPost = pointId.IsInt()
                        ? PickPointsServices.Get(pointId.TryParseInt())
                        : null;
                    
                    if (pickPointRussianPost != null)
                    {
                        // не проходит по весу
                        if ((pickPointRussianPost?.WeightLimit).HasValue && weightInKg > pickPointRussianPost.WeightLimit.Value)
                            pickPointRussianPost = null;

                        // не проходит по габаритам
                        if ((pickPointRussianPost?.DimensionLimit) != null && dimensionType > pickPointRussianPost.DimensionLimit)
                            pickPointRussianPost = null;

                        // почтамат не может принимать негабарит
                        if (pickPointRussianPost?.TypePoint == EnTypePoint.Aps 
                            && (dimensionType == null || dimensionType == EnDimensionType.Oversized))
                            pickPointRussianPost = null;

                        if (pickPointRussianPost?.TypePoint == EnTypePoint.Aps
                            && isAvailablePochtamats is false)
                            pickPointRussianPost = null;

                        if (pickPointRussianPost?.TypePoint == EnTypePoint.Ops
                            && isAvailableOps is false)
                            pickPointRussianPost = null;

                        if (pickPointRussianPost?.TypePoint == EnTypePoint.Pvz
                            && isAvailablePvz is false)
                            pickPointRussianPost = null;
                    }
                }

                if (isAvailableECOM)
                {
                    if (dimensionType != null)
                    {
                        List<DeliveryPoint> points = (cancellationToken.CancelIfRequestedAsync<List<DeliveryPoint>>() ?? GetAllPointsAsync(cancellationToken)).ConfigureAwait(false).GetAwaiter().GetResult();
                        deliveryPointECOM = points
                                            // искомый пункт
                                          ?.Where(point => string.Equals(point.DeliveryPointIndex, pointId, StringComparison.Ordinal))
                                            // не закрыт
                                           .Where(point => !point.Closed && !point.TemporaryClosed)
                                            // проходит по весу
                                           .Where(point => point.WeightLimit.HasValue == false || weightInGrams <= point.WeightLimit.Value)
                                            // проходит по габаритам
                                           .Where(point => point.DimensionLimit == null || dimensionType <= point.DimensionLimit)
                                           .FirstOrDefault();
                    }
                }

                if (pickPointRussianPost is null
                    && deliveryPointECOM is null)
                    return null;
                
                var shippingOptions = new List<BaseShippingOption>();
                var calcMail = new HashSet<int>();
                var tasks = new List<Task<BaseShippingOption>>();
                
                foreach (var availableProduct in availableProducts /*.OrderBy(x => x.MailType.ToString())*/)
                {
                    if (!IsValidCargo(availableProduct.MailType, weightInGrams, dimensions, isInternational: false)) 
                        continue;

                    // считаем только тарифы до ПВЗ
                    if (availableProduct.MailType != EnMailType.ECOM
                        && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory)
                        && !RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(availableProduct.MailType)
                        && !RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(availableProduct.MailType)
                        && !RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(availableProduct.MailType))
                        continue;

                    if (IsCodCategory(availableProduct.MailCategory))
                        continue;// убираем данный тип из расчетов, т.к. это будет определяться выбраным типом оплаты (наложенный платеж)
                    
                    if (availableProduct.MailType == EnMailType.ECOM
                        && deliveryPointECOM is null)
                        continue;
                    
                    // if (IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory)
                    //     && pickPointRussianPost?.TypePoint != EnTypePoint.Aps)
                    //     continue;
                    //
                    // if (RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(availableProduct.MailType)
                    //     && pickPointRussianPost?.TypePoint != EnTypePoint.Ops)
                    //     continue;

                    var hash = 17 ^ availableProduct.MailType.ToString().GetHashCode() ^ availableProduct.MailCategory.ToString().GetHashCode();

                    if (calcMail.Contains(hash))
                        continue;
                    calcMail.Add(hash);
                    
                    if (RussianPostAvailableOption.LocalTransportTypeAvailable.ContainsKey(availableProduct.MailType))
                        foreach (var transportType in RussianPostAvailableOption.LocalTransportTypeAvailable[availableProduct.MailType])
                        {
                            if (_localDeliveryTypes.Any(dt => dt.Item1 == availableProduct.MailType && dt.Item2 == availableProduct.MailCategory && dt.Item3 == transportType))
                            {
                                if ((availableProduct.MailType == EnMailType.ECOM || IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                    && (availableProduct.MailType != EnMailType.EcomMarketplace || !_courier))
                                    tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, pickPointRussianPost, deliveryPointECOM,
                                        orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: false, toPvz: false, toPochtamat: false, cancellationToken));
                                
                                // до востредования
                                if (_deliveryToOps
                                    && RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(availableProduct.MailType)
                                    // в ЕКОМ и почтамат не надо считать как "до востредования"
                                    && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                    tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, pickPointRussianPost, deliveryPointECOM,
                                        orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: true, toPvz: false, toPochtamat: false, cancellationToken));
                                
                                // в постомат
                                if (_deliveryToPochtamat
                                  && RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(availableProduct.MailType)
                                  // в ЕКОМ и почтамат не надо считать
                                  && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                    tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, pickPointRussianPost, deliveryPointECOM,
                                        orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: false, toPvz: false, toPochtamat: true, cancellationToken));
                                
                                // в ПВЗ
                                if (_deliveryToPvz
                                  && RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(availableProduct.MailType)
                                  // в ЕКОМ и почтамат не надо считать
                                  && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                                    tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, transportType, pickPointRussianPost, deliveryPointECOM,
                                        orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: false, toPvz: true, toPochtamat: false, cancellationToken));
                            }
                        }
                    else
                    {
                        if ((availableProduct.MailType == EnMailType.ECOM || IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                            && (availableProduct.MailType != EnMailType.EcomMarketplace || !_courier))
                            tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, pickPointRussianPost, deliveryPointECOM,
                                orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: false, toPvz: false, toPochtamat: false, cancellationToken));

                        // до востредования
                        if (_deliveryToOps 
                            && RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(availableProduct.MailType)
                            // в ЕКОМ и почтамат не надо считать как "до востредования"
                            && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                            tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, pickPointRussianPost, deliveryPointECOM,
                                    orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: true, toPvz: false, toPochtamat: false, cancellationToken));
                                
                        // в постомат
                        if (_deliveryToPochtamat
                            && RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(availableProduct.MailType)
                            // в ЕКОМ и почтамат не надо считать
                            && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                            tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, pickPointRussianPost, deliveryPointECOM,
                                orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: false, toPvz: false, toPochtamat: true, cancellationToken));
                                
                        // в ПВЗ
                        if (_deliveryToPvz
                            && RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(availableProduct.MailType)
                            // в ЕКОМ и почтамат не надо считать
                            && availableProduct.MailType != EnMailType.ECOM && !IsPochtamatsTariff(availableProduct.MailType, availableProduct.MailCategory))
                            tasks.Add(CalcToPointAsync(shippingPoint, availableProduct.MailType, availableProduct.MailCategory, null, pickPointRussianPost, deliveryPointECOM,
                                orderCost, weightInGrams, dimensions, availableProducts, settigns, toOps: false, toPvz: true, toPochtamat: false, cancellationToken));
                    }
                }
       
                Task.WaitAll(tasks.ToArray()/*, TimeSpan.FromSeconds(30)*/);
                tasks
                   .Where(x => x.Exception != null)
                   .ForEach(Debug.Log.Warn);
                tasks
                   .Where(x => x.IsCompleted && x.IsFaulted is false)
                   .Select(x => x.Result)
                   .Where(x => x != null)
                   .ForEach(shippingOptions.Add);
             
                return shippingOptions.OrderBy(x => x.Rate);
            }
            catch (ThreadAbortException)
            {
                cancellationTokenSource.Cancel();
            }

            return null;
        }

        private async Task<BaseShippingOption> CalcOptionsAsync(ShippingPoint shippingPoint, EnMailType enMailType, EnMailCategory mailCategory, EnTransportType transportType, string zip, int? countryCode,
            string selectedPoint, float orderCost, int weight, Dimension dimensions, IList<AvailableProduct> availableProducts, AccountSettings settigns, bool isInternational, 
            bool toOps, bool toPvz, bool toPochtamat, CancellationToken cancellationToken)
        {
            if (!IsValidCargo(enMailType, weight, dimensions, isInternational)) 
                return null;

            var isDeclare = IsDeclareCategory(mailCategory);
            int declareValue = (int)(orderCost * 100);

            // ECOM
            int? goodValue = null;
            RussianPostPoint deliveryPoint = null;
            List<RussianPostPoint> deliveryPoints = null;
            if (enMailType == EnMailType.ECOM)
            {
                if (EnDimensionType.GetDimensionType(dimensions) == null)
                    return null;

                var points = 
                    _calculationParameters.Region.IsNotEmpty() && _calculationParameters.City.IsNotEmpty()
                        ? await (cancellationToken.CancelIfRequestedAsync<List<DeliveryPoint>>() ?? GetPointsCityAsync(_calculationParameters.Region, _calculationParameters.City, _calculationParameters.District, weight, dimensions, cancellationToken))
                           .ConfigureAwait(false)
                        : null;
                if (points == null || points.Count == 0)
                    return null;

                deliveryPoints = CastPoints(points);
                deliveryPoint = selectedPoint != null
                    ? deliveryPoints.FirstOrDefault(x => x.Id == selectedPoint) ?? deliveryPoints[0]
                    : deliveryPoints[0];

                goodValue = (int)(orderCost * 100);
            }

            if (IsPochtamatsTariff(enMailType, mailCategory) || toPochtamat)
            {
                var dimensionType = EnDimensionType.GetDimensionType(dimensions);
                if (dimensionType == null || dimensionType == EnDimensionType.Oversized)
                    return null;

                var points = 
                    _calculationParameters.Region.IsNotEmpty() && _calculationParameters.City.IsNotEmpty()
                        ? GetPochtamatsCity(_calculationParameters.Region, _calculationParameters.City, _calculationParameters.District, weight, dimensions)
                        : null;
                if (points == null || points.Count == 0)
                    return null;

                deliveryPoints = CastPoints(points);
                deliveryPoint = selectedPoint != null
                    ? deliveryPoints.FirstOrDefault(x => x.Id == selectedPoint) ?? deliveryPoints[0]
                    : deliveryPoints[0];
            }

            if (toOps)
            {
                var points = 
                    _calculationParameters.Region.IsNotEmpty() && _calculationParameters.City.IsNotEmpty()
                        ? GetOpsCity(_calculationParameters.Region, _calculationParameters.City, _calculationParameters.District, weight, dimensions)
                        : null;
                if (points == null || points.Count == 0)
                    return null;

                deliveryPoints = CastPoints(points);
                deliveryPoint = selectedPoint != null
                    ? deliveryPoints.FirstOrDefault(x => x.Id == selectedPoint) ?? deliveryPoints[0]
                    : deliveryPoints[0];
            }

            if (toPvz)
            {
                var points = 
                    _calculationParameters.Region.IsNotEmpty() && _calculationParameters.City.IsNotEmpty()
                        ? GetPvzCity(_calculationParameters.Region, _calculationParameters.City, _calculationParameters.District, weight, dimensions)
                        : null;
                if (points == null || points.Count == 0)
                    return null;

                deliveryPoints = CastPoints(points);
                deliveryPoint = selectedPoint != null
                    ? deliveryPoints.FirstOrDefault(x => x.Id == selectedPoint) ?? deliveryPoints[0]
                    : deliveryPoints[0];
            }

            return await CalcOptionsAsync(shippingPoint, enMailType, mailCategory, transportType, zip, countryCode, 
                weight, dimensions, availableProducts, settigns, isInternational, toOps, toPvz, toPochtamat, deliveryPoint, deliveryPoints, 
                isDeclare, declareValue, goodValue, cancellationToken).ConfigureAwait(false);
        }

        private async Task<BaseShippingOption> CalcToPointAsync(ShippingPoint shippingPoint, EnMailType enMailType, EnMailCategory mailCategory, EnTransportType transportType, 
            PickPointRussianPost pickPointRussianPost, DeliveryPoint deliveryPointEcom, float orderCost, int weightInGrams, Dimension dimensions, IList<AvailableProduct> availableProducts, 
            AccountSettings settigns, bool toOps, bool toPvz, bool toPochtamat, CancellationToken cancellationToken)
        {
            var isDeclare = IsDeclareCategory(mailCategory);
            int declareValue = (int)(orderCost * 100);
            
            // ECOM
            int? goodValue = null;
            RussianPostPoint deliveryPoint = null;
            List<RussianPostPoint> deliveryPoints = null;
            if (enMailType == EnMailType.ECOM
                && deliveryPointEcom != null)
            {
                deliveryPoints = CastPoints(new List<DeliveryPoint>(){deliveryPointEcom});
                deliveryPoint = deliveryPoints[0];
            
                goodValue = (int)(orderCost * 100);
            }

            if ((IsPochtamatsTariff(enMailType, mailCategory) || toPochtamat)
                && pickPointRussianPost?.TypePoint == EnTypePoint.Aps)
            {
                deliveryPoints = CastPoints(new List<PickPointRussianPost>(){pickPointRussianPost});
                deliveryPoint = deliveryPoints[0];
            }

            if (toOps && pickPointRussianPost?.TypePoint == EnTypePoint.Ops)
            {
                deliveryPoints = CastPoints(new List<PickPointRussianPost>(){pickPointRussianPost});
                deliveryPoint = deliveryPoints[0];
            }

            if (toPvz && pickPointRussianPost?.TypePoint == EnTypePoint.Pvz)
            {
                deliveryPoints = CastPoints(new List<PickPointRussianPost>(){pickPointRussianPost});
                deliveryPoint = deliveryPoints[0];
            }

            if (deliveryPoint is null
                || deliveryPoints?.Count > 0 is false)
                return null;

            var baseShippingOption = await CalcOptionsAsync(shippingPoint, enMailType, mailCategory, transportType, zip: null, countryCode: null, 
                    weightInGrams, dimensions, availableProducts, settigns, isInternational: false, toOps, toPvz, toPochtamat, deliveryPoint, deliveryPoints, 
                    isDeclare, declareValue, goodValue, cancellationToken).ConfigureAwait(false) 
                as RussianPostPointDeliveryMapOption;
            
            if (baseShippingOption != null)
            {
                baseShippingOption.SelectedPoint = baseShippingOption.CurrentPoints[0];
                baseShippingOption.PickpointId = baseShippingOption.SelectedPoint.Id;
            }
            return baseShippingOption;
        }

        private async Task<BaseShippingOption> CalcOptionsAsync(ShippingPoint shippingPoint, EnMailType enMailType, EnMailCategory mailCategory,
            EnTransportType transportType, string zip, int? countryCode, int weight, Dimension dimensions,
            IEnumerable<AvailableProduct> availableProducts, AccountSettings settigns, bool isInternational, bool toOps, bool toPvz, bool toPochtamat,
            RussianPostPoint deliveryPoint, List<RussianPostPoint> deliveryPoints, bool isDeclare, int declareValue, int? goodValue, CancellationToken cancellationToken)
        {
            // для расчета по России нужен корректный индекс
            // для доставки в ПВЗ и за границей индекс не важен
            if (deliveryPoint == null && !isInternational && (zip.IsNullOrEmpty() || !zip.IsInt() || zip.Length != 6))
                return null;
            
            // для вывода точек доставки нужна работающая карта
            if (_yaMapsApiKey.IsNullOrEmpty() && deliveryPoint != null && deliveryPoints != null && deliveryPoints.Count > 0)
                return null;

            //var calc = await (cancellationToken.CancelIfRequestedAsync<CalculateResponse>() ?? CalculateAsync(shippingPoint.OperatorIndex, enMailType, mailCategory, transportType, zip, countryCode, deliveryPoint,
            //    isDeclare ? declareValue : (int?)null, goodValue, weight, dimensions, cancellationToken)).ConfigureAwait(false);
            var calcTariff = await 
                (cancellationToken.CancelIfRequestedAsync<TariffApi.CalculateResponse>()
                 ?? CalculateTariffAsync(shippingPoint.OperatorIndex, enMailType, mailCategory, transportType, zip, countryCode, deliveryPoint,
                isDeclare ? declareValue : (int?)null, goodValue, weight, dimensions, settigns, cancellationToken))
               .ConfigureAwait(false);

            //if (calc != null && calc.TotalRateNoVat > 0)
            if (calcTariff != null && calcTariff.Errors == null)
            {
                var cashProduct = GetCashProduct(enMailType, mailCategory, availableProducts);

                var cashMailCategory = cashProduct != null ? cashProduct.MailCategory : null;

                if (deliveryPoint != null && !(deliveryPoint.AvailableCardOnDelivery ?? false) && !(deliveryPoint.AvailableCashOnDelivery ?? false))
                    cashMailCategory = null;// для выбранной точки не доступно оплата при получении

                //CalculateResponse calcCash = cashMailCategory != null
                //    ? await (cancellationToken.CancelIfRequestedAsync<CalculateResponse>()
                //             ?? CalculateAsync(shippingPoint.OperatorIndex, enMailType, cashMailCategory, transportType, zip, countryCode, deliveryPoint,
                //                  declareValue, goodValue, weight, dimensions, cancellationToken))
                //             .ConfigureAwait(false)
                //    : null;

                var calcCashTariff = cashMailCategory != null
                    ? await 
                        (cancellationToken.CancelIfRequestedAsync<TariffApi.CalculateResponse>()
                         ?? CalculateTariffAsync(shippingPoint.OperatorIndex, enMailType, cashMailCategory, transportType, zip, countryCode, deliveryPoint,
                             declareValue, goodValue, weight, dimensions, settigns, cancellationToken))
                       .ConfigureAwait(false)
                    : null;

                return
                    CreateOption(
                        //rate: GetDeliverySum(calc, withInsurance: isDeclare || goodValue.HasValue),
                        //rateCash: GetDeliverySum(calcCash != null && calcCash.TotalRateNoVat > 0 ? calcCash : calc, withInsurance: true),
                        rate: GetDeliverySum(calcTariff),
                        rateCash: GetDeliverySum(calcCashTariff != null && calcCashTariff.Errors == null ? calcCashTariff : calcTariff),
                        //deliveryTime: calc.DeliveryTime != null ? new DeliveryPeriod { MinDays = calc.DeliveryTime.MinDays, MaxDays = calc.DeliveryTime.MaxDays } : null,
                        deliveryTime: calcTariff.Delivery != null 
                            ? new DeliveryPeriod
                            {
                                MinDays = calcTariff.Delivery.Min, 
                                MaxDays = calcTariff.Delivery.Min != calcTariff.Delivery.Max 
                                    ? calcTariff.Delivery.Max 
                                    : (int?)null
                            } 
                            : null,
                        indexFrom: shippingPoint.OperatorIndex,
                        indexTo: deliveryPoint != null ? deliveryPoint.Id : zip,
                        countryCode: countryCode,
                        mailType: enMailType,
                        mailCategory: mailCategory,
                        cashMailCategory: cashMailCategory,
                        transportType: transportType,
                        //cashOnDeliveryAvailable: calcCash != null && calcCash.TotalRateNoVat > 0,
                        cashOnDeliveryAvailable: calcCashTariff != null && calcCashTariff.Errors == null,
                        deliveryPoint: deliveryPoint,
                        deliveryPoints: deliveryPoints,
                        weight: weight,
                        dimensions: dimensions,
                        toOps: toOps,
                        toPvz: toPvz,
                        toPochtamat: toPochtamat);
            }

            return null;
        }

        private AvailableProduct GetCashProduct(EnMailType enMailType, EnMailCategory mailCategory,
            IEnumerable<AvailableProduct> availableProducts)
        {
            var cashProduct = !mailCategory.Value.StartsWith("COMBINED_")
                ? availableProducts
                 .OrderByDescending(x =>
                      (_deliveryWithCod && (x.MailCategory == EnMailCategory.WithCompulsoryPayment
                                            || x.MailCategory == EnMailCategory.WithDeclaredValueAndCompulsoryPayment)) ||
                      (!_deliveryWithCod && x.MailCategory == EnMailCategory.WithDeclaredValueAndCashOnDelivery)
                          ? 1
                          : 0)
                 .FirstOrDefault(x =>
                      x.MailType == enMailType &&
                      (x.MailCategory == EnMailCategory.WithDeclaredValueAndCashOnDelivery
                       || x.MailCategory == EnMailCategory.WithCompulsoryPayment
                       || x.MailCategory == EnMailCategory.WithDeclaredValueAndCompulsoryPayment))
                : availableProducts
                   .FirstOrDefault(x =>
                        x.MailType == enMailType
                        && (x.MailCategory == EnMailCategory.CombinedWithDeclaredValueAndCashOnDelivery));
            return cashProduct;
        }

        private static bool IsValidCargo(EnMailType enMailType, int weightInGrams, Dimension dimensions, bool isInternational)
        {
            // Проверка на превышение веса для типа отправления
            if (RussianPostAvailableOption.MaxWeightMailType.ContainsKey(enMailType) 
                && weightInGrams > RussianPostAvailableOption.MaxWeightMailType[enMailType])
                return false;
            if (isInternational && RussianPostAvailableOption.MaxWeightForInternationalMailType.ContainsKey(enMailType) &&
                weightInGrams > RussianPostAvailableOption.MaxWeightForInternationalMailType[enMailType])
                return false;

            // Проверка на минимальный вес для типа отправления
            if (RussianPostAvailableOption.MinWeightMailType.ContainsKey(enMailType) 
                && weightInGrams < RussianPostAvailableOption.MinWeightMailType[enMailType])
                return false;

            // Проверка на превышение габаритов для типа отправления
            if (RussianPostAvailableOption.MaxSumDimensionsMailType.ContainsKey(enMailType))
            {
                var sumDimensions = (dimensions.Height + dimensions.Length + dimensions.Width) * 10;
                if (sumDimensions > RussianPostAvailableOption.MaxSumDimensionsMailType[enMailType])
                    return false;
            }

            // Проверка на превышение габарита для типа отправления
            if (RussianPostAvailableOption.MaxDimensionMailType.ContainsKey(enMailType))
            {
                var maxDimensions = new[] { dimensions.Height, dimensions.Length, dimensions.Width }.Max() * 10;
                if (maxDimensions > RussianPostAvailableOption.MaxDimensionMailType[enMailType])
                    return false;
            }

            // Проверка на превышение габаритов для типа отправления
            if (RussianPostAvailableOption.MaxDimensionsMailType.ContainsKey(enMailType))
            {
                if (dimensions.Length * 10 > RussianPostAvailableOption.MaxDimensionsMailType[enMailType][0] ||
                    dimensions.Width * 10 > RussianPostAvailableOption.MaxDimensionsMailType[enMailType][1] ||
                    dimensions.Height * 10 > RussianPostAvailableOption.MaxDimensionsMailType[enMailType][2])
                    return false;
            }

            // Проверка на превышение габаритов для типа отправления (сумма длины и периметра наибольшего поперечного сечения)
            if (RussianPostAvailableOption.MaxLength2Height2WidthDimensionsMailType.ContainsKey(enMailType))
            {
                var sumDimensions = (dimensions.Length + 2 * dimensions.Height + 2 * dimensions.Width) * 10;
                if (sumDimensions > RussianPostAvailableOption.MaxLength2Height2WidthDimensionsMailType[enMailType])
                    return false;
            }

            return true;
        }

        private async Task<CalculateResponse> CalculateAsync(string indexFrom, EnMailType enMailType, EnMailCategory mailCategory, EnTransportType transportType, string indexTo, int? countryCode, DeliveryPoint deliveryPoint,
            int? declaredValue, int? goodValue, int weight, Dimension dimensions, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return default;
            
            return await _russianPostApiService.CalculateAsync(new CalculateOptions()
            {
                DeclaredValue = declaredValue,
                GoodValue = goodValue,
                Dimension = RussianPostAvailableOption.DimensionsNotRequired.Contains(enMailType) ? null : dimensions,
                Weight = weight,
                IndexFrom = indexFrom,
                IndexTo = deliveryPoint != null ? deliveryPoint.Address.Index : indexTo,
                DeliveryPointIndex = deliveryPoint != null ? deliveryPoint.DeliveryPointIndex : null,
                CountryCode = countryCode,
                MailCategory = mailCategory,
                MailType = enMailType,
                TransportType = transportType,
                DimensionType = enMailType == EnMailType.ECOM ? EnDimensionType.GetDimensionType(dimensions) : null,
                Courier =
                    _courier && RussianPostAvailableOption.CourierOptionAvailable.Contains(enMailType)
                        ? true
                        : (bool?) null,
                Fragile =
                    _fragile && RussianPostAvailableOption.FragileOptionAvailable.Contains(enMailType)
                        ? true
                        : (bool?) null,
                //PaymentMethod = EnPaymentMethod.Cashless,

                SmsNoticeRecipient =
                    _smsNotification &&
                    RussianPostAvailableOption.SmsNoticeOptionAvailable.ContainsKey(enMailType) &&
                    RussianPostAvailableOption.SmsNoticeOptionAvailable[enMailType].Contains(mailCategory)
                        ? 1 : (int?)null,
                WithOrderOfNotice =
                    _typeNotification == EnTypeNotification.WithOrderOfNotice &&
                    RussianPostAvailableOption.OrderOfNoticeOptionAvailable.ContainsKey(enMailType) &&
                    RussianPostAvailableOption.OrderOfNoticeOptionAvailable[enMailType].Contains(mailCategory)
                        ? true
                        : (bool?)null,
                WithSimpleNotice = 
                    _typeNotification == EnTypeNotification.WithSimpleNotice &&
                    RussianPostAvailableOption.SimpleNoticeOptionAvailable.ContainsKey(enMailType) &&
                    RussianPostAvailableOption.SimpleNoticeOptionAvailable[enMailType].Contains(mailCategory) 
                        ? true
                        : (bool?)null,
                WithElectronicNotice = 
                    _typeNotification == EnTypeNotification.WithElectronicNotice &&
                    RussianPostAvailableOption.ElectronicNoticeOptionAvailable.ContainsKey(enMailType) &&
                    RussianPostAvailableOption.ElectronicNoticeOptionAvailable[enMailType].Contains(mailCategory)
                        ? true
                        : (bool?)null
            }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<TariffApi.CalculateResponse> CalculateTariffAsync(string indexFrom, EnMailType enMailType, 
            EnMailCategory mailCategory, EnTransportType transportType, string indexTo, int? countryCode, RussianPostPoint deliveryPoint,
            int? declaredValue, int? goodValue, int weight, Dimension dimensions, AccountSettings settigns, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return default;
            
            var calculateParams = new TariffApi.CalculateParams
            {
                ObjectId = GetTariffObjectId(enMailType, mailCategory, isInternational: countryCode.HasValue)
            };

            if (calculateParams.ObjectId <= 0)
                return null;

            // ToDo: стоит проверять какие параметры принимаются объектом и на основе этого 
            // заполнять параметры с учетом остальных усовий
            // Вердикт - не стоит. Замечено, что их api возвращает параметры, которые для рассчета
            // не нужны. А также не возвращает параметры, которые нужны для рассчета. Передача, же 
            // лишних параметров и услуг не приводит к проблемам.
            // Upd: по некоторым лишним услугам выдает ошибку
            // Проверка по услугам ниже

            calculateParams.IndexFrom = indexFrom;
            calculateParams.IndexTo = deliveryPoint != null ? deliveryPoint.Id : indexTo; // deliveryPoint.Address.Index

            if (transportType != null)
            {
                if (transportType == EnTransportType.Avia)
                    calculateParams.Transtype = TariffApi.EnTransType.Avia;
                else if (transportType == EnTransportType.Surface)
                    calculateParams.Transtype = TariffApi.EnTransType.Surface;
                else if (transportType == EnTransportType.Combined)
                    calculateParams.Transtype = TariffApi.EnTransType.Combined;
                else if (transportType == EnTransportType.Express)
                    calculateParams.Transtype = TariffApi.EnTransType.Express;
                else if (transportType == EnTransportType.Standard)
                    calculateParams.Transtype = null;
                else 
                    return default;
            }
            else if (countryCode.HasValue)
            {
                // замечено что сервис otpravka.pochta.ru международные отправления
                // отправляет в приоритете авиадаставкой
                calculateParams.IsAvia = TariffApi.EnIsAviaType.AviaOrSurface;
            }

            calculateParams.Country = countryCode;
            calculateParams.CountryTo = countryCode;

            calculateParams.Sumoc = declaredValue;

            if (mailCategory == EnMailCategory.WithDeclaredValueAndCashOnDelivery 
                || mailCategory == EnMailCategory.WithDeclaredValueAndCompulsoryPayment
                || mailCategory == EnMailCategory.CombinedWithDeclaredValueAndCashOnDelivery)
                calculateParams.Sumnp = declaredValue;

            calculateParams.Sumin = goodValue;

            calculateParams.Weight = weight;

            if (enMailType == EnMailType.ECOM)
            {
                var dimensionType = EnDimensionType.GetDimensionType(dimensions);
                if (dimensionType != null)
                {
                    if (dimensionType == EnDimensionType.S)
                        calculateParams.Pack = TariffApi.EnPackType.BoxS;
                    else if (dimensionType == EnDimensionType.M)
                        calculateParams.Pack = TariffApi.EnPackType.BoxM;
                    else if (dimensionType == EnDimensionType.L)
                        calculateParams.Pack = TariffApi.EnPackType.BoxL;
                    else if (dimensionType == EnDimensionType.XL)
                        calculateParams.Pack = TariffApi.EnPackType.BoxXL;
                    else if (dimensionType == EnDimensionType.Oversized)
                        calculateParams.Pack = TariffApi.EnPackType.Oversized;
                }
            }
            else
                calculateParams.Size = $"{dimensions.Height}x{dimensions.Length}x{dimensions.Width}";

            var deliveryParams = CacheManager.Get(
                "RussianPostDeliveryParams-" + calculateParams.ObjectId,
                60,
                () => 
                    (cancellationToken.CancelIfRequestedAsync<TariffApi.GetDeliveryParamsResponse>()
                       ?? TariffApi.RussianPostTariffApiService.GetDeliveryParamsAsync(calculateParams.ObjectId,
                           cancellationToken))
                   .ConfigureAwait(false).GetAwaiter().GetResult());
            var availableServices =
                deliveryParams?.Object
                    ?.FirstOrDefault(x => x.Id == calculateParams.ObjectId)
                    ?.Service
                    ?.Select(x => x.Id)
                    .ToList();
            calculateParams.Countinpack = settigns.PlannedMonthlyNumber;

            // Корпоративный клиент
            calculateParams.Services.Add(28);

            if (_courier 
                && deliveryPoint is null
                && RussianPostAvailableOption.CourierOptionAvailable.Contains(enMailType)
                && (availableServices is null || availableServices.Contains(26))
                )
                calculateParams.Services.Add(26);

            if (_fragile 
                && RussianPostAvailableOption.FragileOptionAvailable.Contains(enMailType)
                && (availableServices is null || availableServices.Contains(4))
                )
                calculateParams.Services.Add(4);

            if (_smsNotification
                && RussianPostAvailableOption.SmsNoticeOptionAvailable.ContainsKey(enMailType)
                && RussianPostAvailableOption.SmsNoticeOptionAvailable[enMailType].Contains(mailCategory)
                && (availableServices is null || availableServices.Contains(44))
                )
                calculateParams.Services.Add(44);

            if (_typeNotification == EnTypeNotification.WithOrderOfNotice
                && RussianPostAvailableOption.OrderOfNoticeOptionAvailable.ContainsKey(enMailType)
                && RussianPostAvailableOption.OrderOfNoticeOptionAvailable[enMailType].Contains(mailCategory)
                && (availableServices is null || availableServices.Contains(2))
                )
                calculateParams.Services.Add(2);

            if (_typeNotification == EnTypeNotification.WithSimpleNotice
                && RussianPostAvailableOption.SimpleNoticeOptionAvailable.ContainsKey(enMailType)
                && RussianPostAvailableOption.SimpleNoticeOptionAvailable[enMailType].Contains(mailCategory)
                && (availableServices is null || availableServices.Contains(1))
               )
                calculateParams.Services.Add(1);

            if (_typeNotification == EnTypeNotification.WithElectronicNotice
                && RussianPostAvailableOption.ElectronicNoticeOptionAvailable.ContainsKey(enMailType)
                && RussianPostAvailableOption.ElectronicNoticeOptionAvailable[enMailType].Contains(mailCategory)
                && (availableServices is null || availableServices.Contains(62))
               )
                calculateParams.Services.Add(62);

            // Посылка считается негабаритной, если сумма измерений трех сторон отправления превышает 120 см или одна из сторон отправления превышает 60 см.
            if (enMailType == EnMailType.PostalParcel && 
                (dimensions.Height + dimensions.Length + dimensions.Width) > 120 ||
                Math.Max(Math.Max(dimensions.Height.Value, dimensions.Length.Value), dimensions.Width.Value) > 60)
                if (availableServices is null || availableServices.Contains(12))
                    calculateParams.Services.Add(12);

            var termOfDelivery = calculateParams.IndexTo.IsNotEmpty();
            var result = await TariffApi.RussianPostTariffApiService.CalculateAsync(termOfDelivery, @params: calculateParams, cancellationToken).ConfigureAwait(false);

            if (termOfDelivery && result != null && ((result.Errors == null && !result.Paynds.HasValue) || (result.Errors != null && result.Errors.Any(er => er.Type == 2))))
            {
                // Баг со стороны ПР
                // На "Бандероль с объявленной ценностью" замечено, что расчет стоимости доставки со сроками
                // не возвращает стоимость доставки. Если же запросить только расчет стоимости, то возвращает.

                var resultOnlyCalc = await TariffApi.RussianPostTariffApiService.CalculateAsync(termOfDelivery: false, @params: calculateParams, cancellationToken).ConfigureAwait(false);
                if (resultOnlyCalc != null)
                    resultOnlyCalc.Delivery = result.Delivery;

                result = resultOnlyCalc;
            }

            return result;
        }

        private long GetTariffObjectId(EnMailType enMailType, EnMailCategory mailCategory, bool isInternational)
        {
            var mailTypeCode = string.Empty;
            var mailCategoryCode = string.Empty;

            // enMailType
            if (enMailType == EnMailType.PostalParcel)
                mailTypeCode = "4";

            else if (enMailType == EnMailType.OnlineParcel)
                mailTypeCode = "23";

            else if (enMailType == EnMailType.OnlineCourier)
                mailTypeCode = "24";

            else if (enMailType == EnMailType.Ems)
                mailTypeCode = "7";

            else if (enMailType == EnMailType.EmsOptimal)
                mailTypeCode = "34";

            else if (enMailType == EnMailType.EmsRT)
                mailTypeCode = "41";

            else if (enMailType == EnMailType.EmsTender)
                mailTypeCode = "52";

            else if (enMailType == EnMailType.Letter)
                mailTypeCode = "2";

            else if (enMailType == EnMailType.LetterClass1)
                mailTypeCode = "15";

            else if (enMailType == EnMailType.Banderol)
                mailTypeCode = "3";

            else if (enMailType == EnMailType.BusinessCourier)
                mailTypeCode = "30";

            else if (enMailType == EnMailType.BusinessCourierExpress)
                mailTypeCode = "31";

            else if (enMailType == EnMailType.ParcelClass1)
                mailTypeCode = "47";

            else if (enMailType == EnMailType.BanderolClass1)
                mailTypeCode = "16";

            else if (enMailType == EnMailType.VGPOClass1)
                mailTypeCode = "46";

            else if (enMailType == EnMailType.SmallPacket)
                mailTypeCode = "5";

            else if (enMailType == EnMailType.VSD)
                mailTypeCode = "";// нету такого объекта

            else if (enMailType == EnMailType.ECOM)
                mailTypeCode = "53";

            else if (enMailType == EnMailType.EcomMarketplace)
                mailTypeCode = "54";


            // mailCategory
            if (mailCategory == EnMailCategory.Simple)
                mailCategoryCode = "00";
            else if (mailCategory == EnMailCategory.Ordered)
                mailCategoryCode = "01";
            else if (mailCategory == EnMailCategory.Ordinary)
                mailCategoryCode = "03";
            else if (mailCategory == EnMailCategory.WithDeclaredValue)
                mailCategoryCode = "02";
            else if (mailCategory == EnMailCategory.WithDeclaredValueAndCashOnDelivery)
                mailCategoryCode = "04";
            else if (mailCategory == EnMailCategory.WithDeclaredValueAndCompulsoryPayment)
                mailCategoryCode = "06";
            else if (mailCategory == EnMailCategory.WithCompulsoryPayment)
                mailCategoryCode = "07";
            else if (mailCategory == EnMailCategory.CombinedOrdinary)
                mailCategoryCode = "08";
            else if (mailCategory == EnMailCategory.CombinedWithDeclaredValue)
                mailCategoryCode = "09";
            else if (mailCategory == EnMailCategory.CombinedWithDeclaredValueAndCashOnDelivery)
                mailCategoryCode = "10";

            return mailTypeCode.IsNotEmpty() && mailCategoryCode.IsNotEmpty()
                ? (mailTypeCode + mailCategoryCode + (isInternational ? "1" : "0")).TryParseLong()
                : 0;
        }

        private BaseShippingOption CreateOption(float rate, float rateCash, DeliveryPeriod deliveryTime, string indexFrom, string indexTo, int? countryCode, EnMailType mailType, EnMailCategory mailCategory, 
            EnMailCategory cashMailCategory, EnTransportType transportType, bool cashOnDeliveryAvailable, RussianPostPoint deliveryPoint, List<RussianPostPoint> deliveryPoints, int weight, Dimension dimensions,
            bool toOps, bool toPvz, bool toPochtamat)
        {
            var name = string.Format("{2} ({0} {1}{3}{4})",
                        mailType.Localize(),
                        mailCategory.Localize().ToLower(),
                        _method.Name,
                        transportType != null
                            ? " " + transportType.Localize().ToLower()
                            : string.Empty,
                        toOps
                            ? " до востребования"
                            : string.Empty);

            var deliveryTimeStr = deliveryTime != null && deliveryTime.MinDays.HasValue
                    ? string.Format("{0:. дн\\.}{1}{2:. дн\\.}",
                        deliveryTime.MinDays.HasValue ? deliveryTime.MinDays.Value + _increaseDeliveryTime : (int?)null,
                        deliveryTime.MinDays.HasValue && deliveryTime.MaxDays.HasValue ? " - " : "",
                        deliveryTime.MaxDays.HasValue ? deliveryTime.MaxDays.Value + _increaseDeliveryTime : (int?)null)
                    : null;

            if (deliveryPoint != null && deliveryPoints != null && deliveryPoints.Count > 0)
            {
                var option = new RussianPostPointDeliveryMapOption(_method, _totalPrice)
                {
                    DeliveryId = $"{indexFrom}\\{mailCategory}\\{mailType}\\{transportType}\\{toOps}\\{toPvz}\\{toPochtamat}".GetHashCode(),
                    Name = name,
                    Rate = rate,
                    BasePrice = rate,
                    PriceCash = rateCash,
                    CashMailCategory = cashMailCategory,
                    MailCategory = mailCategory,
                    DeliveryTime = deliveryTimeStr,
                    IsAvailablePaymentCashOnDelivery = cashOnDeliveryAvailable && (deliveryPoint.AvailableCashOnDelivery is true || deliveryPoint.AvailableCardOnDelivery is true),
                    CalculateOption = new RussianPostCalculateOption
                    {
                        MailType = mailType,
                        MailCategory = mailCategory,
                        IndexFrom = indexFrom,
                        IndexTo = deliveryPoint.Id,
                        CountryCode = countryCode,
                        TransportType = transportType,
                        ToOps = toOps,
                        ToPvz = toPvz,
                        ToPochtamat = toPochtamat,
                    },
                    CurrentPoints = deliveryPoints,
                };
                SetMapData(option, weight, dimensions, toOps, toPvz);

                return option;
            }
            
            return new RussianPostOption(_method, _totalPrice)
            {
                DeliveryId = $"{indexFrom}\\{mailCategory}\\{mailType}\\{transportType}\\{toOps}\\{toPvz}\\{toPochtamat}".GetHashCode(),
                Name = name,
                Rate = rate,
                BasePrice = rate,
                PriceCash = rateCash,
                CashMailCategory = cashMailCategory,
                MailCategory = mailCategory,
                DeliveryTime = deliveryTimeStr,
                IsAvailablePaymentCashOnDelivery = cashOnDeliveryAvailable,
                CalculateOption = new RussianPostCalculateOption
                {
                    MailType = mailType,
                    MailCategory = mailCategory,
                    IndexFrom = indexFrom,
                    IndexTo = indexTo,
                    CountryCode = countryCode,
                    TransportType = transportType,
                },
            };
        }

        private void SetMapData(RussianPostPointDeliveryMapOption option, int weight, Dimension dimensions, bool toOps, bool toPvz)
        {
            string lang = "en_US";
            switch (Localization.Culture.Language)
            {
                case Localization.Culture.SupportLanguage.Russian:
                    lang = "ru_RU";
                    break;
                case Localization.Culture.SupportLanguage.English:
                    lang = "en_US";
                    break;
                case Localization.Culture.SupportLanguage.Ukrainian:
                    lang = "uk_UA";
                    break;
            }
            option.MapParams = new PointDelivery.MapParams();
            option.MapParams.Lang = lang;
            option.MapParams.YandexMapsApikey = _yaMapsApiKey;
            option.MapParams.Destination = string.Join(", ", new[] { _calculationParameters.Country, _calculationParameters.Region, _calculationParameters.District, _calculationParameters.City }.Where(x => x.IsNotEmpty()));

            option.PointParams = new PointDelivery.PointParams();
            option.PointParams.IsLazyPoints = (option.CurrentPoints != null ? option.CurrentPoints.Count : 0) > 30;
            option.PointParams.PointsByDestination = true;

            if (option.PointParams.IsLazyPoints)
            {
                option.PointParams.LazyPointsParams = new Dictionary<string, object>
                {
                    { "region", _calculationParameters.Region },
                    { "city", _calculationParameters.City },
                    { "district", _calculationParameters.District },
                    { "weight", weight },
                    { "dimensionsH", dimensions.Height },
                    { "dimensionsW", dimensions.Width },
                    { "dimensionsL", dimensions.Length },
                    {
                        "typePoints", 
                        option.CalculateOption.MailType == EnMailType.ECOM
                            ? "ecom"
                            : toOps
                                ? "ops"
                                : toPvz
                                    ? "pvz"
                                    :"pochtamats"
                    },
                };
            }
            else
            {
                option.PointParams.Points = GetFeatureCollection(option.CurrentPoints);
            }
        }

        public object GetLazyData(Dictionary<string, object> data)
        {
            if (data == null || !data.ContainsKey("region") || !data.ContainsKey("city") || !data.ContainsKey("district")
                || !data.ContainsKey("weight") || !data.ContainsKey("dimensionsH") || 
                !data.ContainsKey("dimensionsW") || !data.ContainsKey("dimensionsL") || !data.ContainsKey("typePoints"))
                return null;

            var region = (string)data["region"];
            var city = (string)data["city"];
            var district = (string)data["district"];
            var weight = data["weight"].ToString().TryParseInt();
            var dimensions = new Dimension
            {
                Height = data["dimensionsH"].ToString().TryParseInt(true),
                Width = data["dimensionsW"].ToString().TryParseInt(true),
                Length = data["dimensionsL"].ToString().TryParseInt(true),
            };
            var typePoints = (string)data["typePoints"];

            List<RussianPostPoint> points = null;
            if (string.Equals(typePoints, "ecom", StringComparison.OrdinalIgnoreCase))
            {
                var deliveryPoints =
                    GetPointsCityAsync(region, city, district, weight, dimensions, CancellationToken.None)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
                points = CastPoints(deliveryPoints);
            }
            else if (string.Equals(typePoints, "ops", StringComparison.OrdinalIgnoreCase))
            {
                var deliveryPoints =
                    GetOpsCity(region, city, district, weight, dimensions);
                points = CastPoints(deliveryPoints);
            }
            else if (string.Equals(typePoints, "pochtamats", StringComparison.OrdinalIgnoreCase))
            {
                var deliveryPoints =
                    GetPochtamatsCity(region, city, district, weight, dimensions);
                points = CastPoints(deliveryPoints);
            }
            else if (string.Equals(typePoints, "pvz", StringComparison.OrdinalIgnoreCase))
            {
                var deliveryPoints =
                    GetPvzCity(region, city, district, weight, dimensions);
                points = CastPoints(deliveryPoints);
            }

            return points != null
                ? GetFeatureCollection(points)
                : null;
        }

        public PointDelivery.FeatureCollection GetFeatureCollection(List<RussianPostPoint> points)
        {
            return new PointDelivery.FeatureCollection
            {
                Features = points.Select(p =>
                {
                    var intId = p.Id.GetHashCode();
                    return new PointDelivery.Feature
                    {
                        Id = intId,
                        Geometry = new PointDelivery.PointGeometry {PointX = p.Latitude ?? 0f, PointY = p.Longitude ?? 0f},
                        Options = new PointDelivery.PointOptions {Preset = "islands#dotIcon"},
                        Properties = new PointDelivery.PointProperties
                        {
                            BalloonContentHeader = p.Address,
                            HintContent = p.Address,
                            BalloonContentBody =
                                string.Format(
                                    "{0}{1}<a class=\"btn btn-xsmall btn-submit\" href=\"javascript:void(0)\" onclick=\"window.PointDeliveryMap({2}, '{3}')\">Выбрать</a>",
                                    p.TimeWorkStr,
                                    p.TimeWorkStr.IsNotEmpty() ? "<br>" : "",
                                    intId,
                                    p.Id),
                            BalloonContentFooter = /*_showAddressComment
                                ?*/ p.Description
                            //: null
                        }
                    };
                }).ToList()
            };
        }

        private string LoadZip()
        {
            string zip = null;

            if (_calculationParameters.Zip.IsNotEmpty())
            {
                zip = _calculationParameters.Zip;
            }
            else if (_calculationParameters.City.IsNotEmpty() && _calculationParameters.Region.IsNotEmpty())
            {
                zip = CacheManager.Get(
                    "RussianPost_CityIndex_" +
                    (_calculationParameters.City + "_" + _calculationParameters.District + "_" + _calculationParameters.Region).GetHashCode(),
                    60 * 24,
                    () =>
                    {
                        var pickPoints = PickPointsServices.Find(
                            _calculationParameters.Region.RemoveTypeFromRegion(),
                            _calculationParameters.City,
                            EnTypePoint.Ops);
                        if (pickPoints != null && pickPoints.Count > 0)
                        {
                            PickPointRussianPost pickPointRussianPost = null;
                            if (_calculationParameters.District.IsNotEmpty())
                                pickPointRussianPost = pickPoints.FirstOrDefault(x =>
                                    x.Area.Contains(_calculationParameters.District, StringComparison.OrdinalIgnoreCase));
                            if (pickPointRussianPost == null)
                                pickPointRussianPost = pickPoints.FirstOrDefault();

                            if (pickPointRussianPost != null)
                                return pickPointRussianPost.Id.ToString();
                        }
                        
                        var postOfficesCodes = _russianPostApiService.GetCityPostOfficesCodes(
                            settlement: _calculationParameters.City,
                            region: _calculationParameters.Region.RemoveTypeFromRegion(),
                            district: _calculationParameters.District
                        );

                        if (postOfficesCodes != null && postOfficesCodes.Count > 0)
                        {
                            // С 9 начинаются офисы/постоматы партнеров, остальные являются индексами ОПС
                            return postOfficesCodes.FirstOrDefault(code => !code.StartsWith("9"));
                        }

                        return null;
                    });
            }
            return zip;
        }

        public static bool IsDeclareCategory(EnMailCategory mailCategory)
        {
            return mailCategory == EnMailCategory.WithDeclaredValue ||
                mailCategory == EnMailCategory.WithDeclaredValueAndCashOnDelivery ||
                mailCategory == EnMailCategory.WithDeclaredValueAndCompulsoryPayment ||
                mailCategory == EnMailCategory.CombinedWithDeclaredValue ||
                mailCategory == EnMailCategory.CombinedWithDeclaredValueAndCashOnDelivery;
        }
        
        public static bool IsCodCategory(EnMailCategory mailCategory)
        {
            return mailCategory == EnMailCategory.WithCompulsoryPayment ||
                   mailCategory == EnMailCategory.WithDeclaredValueAndCompulsoryPayment ||
                   mailCategory == EnMailCategory.WithDeclaredValueAndCashOnDelivery ||
                   mailCategory == EnMailCategory.CombinedWithDeclaredValueAndCashOnDelivery;
        }

        public static bool IsInternational(AvailableProduct availableProduct)
        {
            return availableProduct.ProductType.StartsWith("international_", StringComparison.OrdinalIgnoreCase) || availableProduct.MailType == EnMailType.SmallPacket;
        }

        public static bool IsPochtamatsTariff(EnMailType mailType, EnMailCategory mailCategory)
        {
            // тариф для постаматов
            return mailType == EnMailType.OnlineParcel &&
                   (mailCategory == EnMailCategory.CombinedOrdinary ||
                    mailCategory == EnMailCategory.CombinedWithDeclaredValue ||
                    mailCategory == EnMailCategory.CombinedWithDeclaredValueAndCashOnDelivery);
        }

        public bool IsCourierTariff(EnMailType mailType)
        {
            // курьерские тарифы
            return mailType == EnMailType.OnlineCourier
                   || mailType == EnMailType.BusinessCourier
                   || mailType == EnMailType.BusinessCourierExpress
                   || mailType == EnMailType.Ems
                   || (_courier && RussianPostAvailableOption.CourierOptionAvailable.Contains(mailType));
        }
        
        public async Task<List<DeliveryPoint>> GetAllPointsAsync(CancellationToken cancellationToken)
        {
            List<DeliveryPoint> points = null;
            var pointsCacheKey = string.Format("RussianPostApi-{0}-delivery-point", _token);
            if (!CacheManager.TryGetValue(pointsCacheKey, out points))
            {
                points = await _russianPostApiService.GetDeliveryPointsAsync(cancellationToken).ConfigureAwait(false);
                if (points != null)
                    CacheManager.Insert(pointsCacheKey, points, 60 * 24);
            }

            return points ?? new List<DeliveryPoint>();
        }

        private async Task<List<DeliveryPoint>> GetPointsCityAsync(string region, string city, string district,
            int weight, Dimension dimensions, CancellationToken cancellationToken)
        {
            if (city.IsNullOrEmpty())
                return null;

            List<DeliveryPoint> points = await GetAllPointsAsync(cancellationToken).ConfigureAwait(false);

            if (points != null && points.Count > 0)
            {
                var regionFind = (region ?? string.Empty).RemoveTypeFromRegion();

                var dimensionType = EnDimensionType.GetDimensionType(dimensions);

                points = points
                    // имеет адрес
                    .Where(x => x.Address != null && !x.Closed && !x.TemporaryClosed)
                    // проходит по весу
                    .Where(x => x.WeightLimit.HasValue == false || weight <= x.WeightLimit.Value)
                    // проходит по габаритам
                    .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                    // нужного региона
                    .Where(x => (regionFind.IsNullOrEmpty() || x.Address.Region.IsNotEmpty()) && x.Address.Region.IndexOf(regionFind, StringComparison.OrdinalIgnoreCase) != -1)
                    // нужного города
                    .Where(x => x.Address.Place.IsNotEmpty() && x.Address.Place.IndexOf(city, StringComparison.OrdinalIgnoreCase) != -1)
                    .ToList();

                if (district.IsNotEmpty() && points.Any(x => x.Address.Area.IsNotEmpty() && x.Address.Area.Contains(district, StringComparison.OrdinalIgnoreCase)))
                    points = points
                        // дофильтровываем по району
                        .Where(x => x.Address.Area.IsNotEmpty() && x.Address.Area.Contains(district, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            return points;
        }

        private List<RussianPostPoint> CastPoints(List<DeliveryPoint> points)
        {
            return points
                 ?.Select(CastPoint)
                  .ToList();
        }

        private List<PickPointRussianPost> GetPochtamatsCity(string region, string city, string district, int weight, Dimension dimensions)
        {
            if (city.IsNullOrEmpty())
                return null;

            var points = PickPointsServices.Find(region, city, EnTypePoint.Aps);

            if (points != null && points.Count > 0)
            {
                var dimensionType = EnDimensionType.GetDimensionType(dimensions);
                var weightInKg = weight / 1000f;

                points = points
                    // имеет адрес
                    .Where(x => x.Address.IsNotEmpty())
                    // проходит по весу
                    .Where(x => x.WeightLimit.HasValue == false || weightInKg <= x.WeightLimit.Value)
                    // проходит по габаритам
                    .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                    .ToList();

                if (district.IsNotEmpty() && points.Any(x => x.Area.IsNotEmpty() && x.Area.Contains(district, StringComparison.OrdinalIgnoreCase)))
                    points = points
                        // дофильтровываем по району
                        .Where(x => x.Area.IsNotEmpty() && x.Area.Contains(district, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            return points;
        }

        private List<PickPointRussianPost> GetOpsCity(string region, string city, string district, int weight, Dimension dimensions)
        {
            if (city.IsNullOrEmpty())
                return null;

            var points = PickPointsServices.Find(region, city, EnTypePoint.Ops);

            if (points != null && points.Count > 0)
            {
                var dimensionType = EnDimensionType.GetDimensionType(dimensions);
                var weightInKg = weight / 1000f;

                points = points
                    // имеет адрес
                    .Where(x => x.Address.IsNotEmpty())
                    // проходит по весу
                    .Where(x => x.WeightLimit.HasValue == false || weightInKg <= x.WeightLimit.Value)
                    // проходит по габаритам
                    .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                    .ToList();

                if (district.IsNotEmpty() && points.Any(x => x.Area.IsNotEmpty() && x.Area.Contains(district, StringComparison.OrdinalIgnoreCase)))
                    points = points
                        // дофильтровываем по району
                        .Where(x => x.Area.IsNotEmpty() && x.Area.Contains(district, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            return points;
        }

        private List<PickPointRussianPost> GetPvzCity(string region, string city, string district, int weight, Dimension dimensions)
        {
            if (city.IsNullOrEmpty())
                return null;

            var points = PickPointsServices.Find(region, city, EnTypePoint.Pvz);

            if (points != null && points.Count > 0)
            {
                var dimensionType = EnDimensionType.GetDimensionType(dimensions);
                var weightInKg = weight / 1000f;

                points = points
                    // имеет адрес
                    .Where(x => x.Address.IsNotEmpty())
                    // проходит по весу
                    .Where(x => x.WeightLimit.HasValue == false || weightInKg <= x.WeightLimit.Value)
                    // проходит по габаритам
                    .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                    .ToList();

                if (district.IsNotEmpty() && points.Any(x => x.Area.IsNotEmpty() && x.Area.Contains(district, StringComparison.OrdinalIgnoreCase)))
                    points = points
                        // дофильтровываем по району
                        .Where(x => x.Area.IsNotEmpty() && x.Area.Contains(district, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            return points;
        }

        private List<RussianPostPoint> CastPoints(List<PickPointRussianPost> points)
        {
            return points
                 ?.Select(CastPoint)
                  .ToList();
        }

        private float GetDeliverySum(CalculateResponse deliveryCost, bool withInsurance = true, bool withFragile = true, bool withNotice = true)
        {
            return ((deliveryCost.TotalRateNoVat + deliveryCost.TotalVat) -
                (withInsurance || deliveryCost.InsuranceRate == null ? 0 : (deliveryCost.InsuranceRate.Rate + deliveryCost.InsuranceRate.Vat)) -
                (withFragile || deliveryCost.FragileRate == null ? 0 : (deliveryCost.FragileRate.Rate + deliveryCost.FragileRate.Vat)) -
                (withNotice || deliveryCost.NoticeRate == null ? 0 : (deliveryCost.NoticeRate.Rate + deliveryCost.NoticeRate.Vat))) / 100F;
        }

        private float GetDeliverySum(TariffApi.CalculateResponse deliveryCost)
        {
            return (deliveryCost.Paynds ?? 0F) / 100F;
        }

        public override IEnumerable<BaseShippingPoint> CalcShippingPoints(float topLeftLatitude,
            float topLeftLongitude, float bottomRightLatitude,
            float bottomRightLongitude)
        {
            var cancellationTokenSource = new CancellationTokenSource();
#if DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));
#endif
#if !DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
#endif
            try
            {
                var cancellationToken = cancellationTokenSource.Token;
                var settigns = CacheManager.Get(string.Format("RussianPostAccountSettings_{0}", _token), 60,
                    () => _russianPostApiService.GetAccountSettings());
                
                if (settigns is null || settigns.ShippingPoints is null)
                    return null;
                
                var shippingPoint = settigns.ShippingPoints.FirstOrDefault(x => x.Enabled == true && x.OperatorIndex == _pointIndex);
                if (shippingPoint is null)
                    return null;
         
                // по России
                IList<AvailableProduct> availableProducts = 
                    shippingPoint.AvailableProducts
                                 .Where(x => !IsInternational(x) && _localDeliveryTypes.Any(dt => dt.Item1 == x.MailType && dt.Item2 == x.MailCategory))
                                 .ToList();

                var isAvailableECOM = availableProducts.Any(product => product.MailType == EnMailType.ECOM);
                var isAvailablePochtamats = 
                    availableProducts.Any(product => 
                        IsPochtamatsTariff(product.MailType, product.MailCategory)
                        || (_deliveryToPochtamat && RussianPostAvailableOption.DeliveryToPochtamatAvailable.Contains(product.MailType)));
                var isAvailableOps = _deliveryToOps && availableProducts.Any(product => RussianPostAvailableOption.DeliveryToOpsAvailable.Contains(product.MailType));
                var isAvailablePvz = _deliveryToPvz && availableProducts.Any(product => RussianPostAvailableOption.DeliveryToPvzAvailable.Contains(product.MailType));
        
                if (isAvailableECOM is false 
                    && isAvailablePochtamats is false 
                    && isAvailableOps is false
                    && isAvailablePvz is false)
                    return null;
            
                var shippingPoints = new List<RussianPostPoint>();
                int weightInGrams = (int)GetTotalWeight(1000);
                var dimensionsHelp = GetDimensions(rate: 10);
                var dimensions = new Dimension()
                {
                    Length = (int)Math.Ceiling(dimensionsHelp[0]),
                    Width = (int)Math.Ceiling(dimensionsHelp[1]),
                    Height = (int)Math.Ceiling(dimensionsHelp[2]),
                };

                var dimensionType = EnDimensionType.GetDimensionType(dimensions);
                if (isAvailableECOM)
                {
                    if (dimensionType != null)
                    {
                        List<DeliveryPoint> points = (cancellationToken.CancelIfRequestedAsync<List<DeliveryPoint>>() ?? GetAllPointsAsync(cancellationToken)).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (points != null && points.Count > 0)
                        {
                            shippingPoints.AddRange(points
                                                   .Where(point => topLeftLatitude > point.Latitude)
                                                   .Where(point => topLeftLongitude < point.Longitude)
                                                   .Where(point => bottomRightLatitude < point.Latitude)
                                                   .Where(point => bottomRightLongitude > point.Longitude)
                                                    // не закрыт
                                                   .Where(point => !point.Closed && !point.TemporaryClosed)
                                                    // проходит по весу
                                                   .Where(point => point.WeightLimit.HasValue == false || weightInGrams <= point.WeightLimit.Value)
                                                    // проходит по габаритам
                                                   .Where(point => point.DimensionLimit == null || dimensionType <= point.DimensionLimit)
                                                   .Select(CastPoint));
                        }
                    }
                }

                var weightInKg = MeasureUnits.ConvertWeight(weightInGrams, MeasureUnits.WeightUnit.Grams, MeasureUnits.WeightUnit.Kilogramm);
                if (isAvailablePochtamats)
                {
                    if (dimensionType != null && dimensionType != EnDimensionType.Oversized)
                    {
                        shippingPoints.AddRange(PickPointsServices.FindByBounds(topLeftLatitude, topLeftLongitude, bottomRightLatitude, bottomRightLongitude, EnTypePoint.Aps)
                                                                   // проходит по весу
                                                                  .Where(x => x.WeightLimit.HasValue == false || weightInKg <= x.WeightLimit.Value)
                                                                   // проходит по габаритам
                                                                  .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                                                                  .Select(CastPoint));
               
                    }
                }
                
                if (isAvailableOps)
                {
                    shippingPoints.AddRange(PickPointsServices.FindByBounds(topLeftLatitude, topLeftLongitude, bottomRightLatitude, bottomRightLongitude, EnTypePoint.Ops)
                                                               // проходит по весу
                                                              .Where(x => x.WeightLimit.HasValue == false || weightInKg <= x.WeightLimit.Value)
                                                               // проходит по габаритам
                                                              .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                                                              .Select(CastPoint));
               
                }
                
                if (isAvailablePvz)
                {
                    shippingPoints.AddRange(PickPointsServices.FindByBounds(topLeftLatitude, topLeftLongitude, bottomRightLatitude, bottomRightLongitude, EnTypePoint.Pvz)
                                                               // проходит по весу
                                                              .Where(x => x.WeightLimit.HasValue == false || weightInKg <= x.WeightLimit.Value)
                                                               // проходит по габаритам
                                                              .Where(x => x.DimensionLimit == null || dimensionType <= x.DimensionLimit)
                                                              .Select(CastPoint));
               
                }

                return shippingPoints;
            }
            catch (ThreadAbortException)
            {
                cancellationTokenSource.Cancel();
            }

            return null;
        }

        public override BaseShippingPoint LoadShippingPointInfo(string pointId)
        {
            var cancellationTokenSource = new CancellationTokenSource();
#if DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));
#endif
#if !DEBUG
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
#endif
            try
            {
                var cancellationToken = cancellationTokenSource.Token;
                var pickPointRussianPost =
                    pointId.IsInt()
                        ? PickPointsServices.Get(pointId.TryParseInt())
                        : null;

                if (pickPointRussianPost != null)
                    return CastPoint(pickPointRussianPost);
                
                List<DeliveryPoint> points = (cancellationToken.CancelIfRequestedAsync<List<DeliveryPoint>>() ?? GetAllPointsAsync(cancellationToken)).ConfigureAwait(false).GetAwaiter().GetResult();
                var deliveryPoint = points?.FirstOrDefault(point =>
                    string.Equals(point.DeliveryPointIndex, pointId, StringComparison.Ordinal));

                if (deliveryPoint != null)
                    return CastPoint(deliveryPoint);

                return null;
            }
            catch (ThreadAbortException)
            {
                cancellationTokenSource.Cancel();
            }

            return null;
        }
        
        private RussianPostPoint CastPoint(PickPointRussianPost point)
        {
            return new RussianPostPoint
            {
                Id = point.Id.ToString(),
                Code = point.Id.ToString(),
                Name = $"{point.Type} {point.Id}",
                Address = point.Address,
                Description = point.AddressDescription,
                Longitude = point.Longitude,
                Latitude = point.Latitude,
                AvailableCashOnDelivery = point.Cash,
                AvailableCardOnDelivery = point.Card,
                TimeWorkStr = point.WorkTime,
                MaxWeightInGrams = point.WeightLimit,
            }.SetDimensionLimit(point.DimensionLimit);
        }
        
        private RussianPostPoint CastPoint(DeliveryPoint point)
        {
            return new RussianPostPoint
            {
                Id = point.DeliveryPointIndex,
                Code = point.DeliveryPointIndex,
                Name = point.LegalShortName,
                Address = point.Address.AddressFromStreet(),
                Description = point.GettoDescription,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                AvailableCashOnDelivery = point.CashPayment,
                AvailableCardOnDelivery = point.CardPayment,
                TimeWorkStr = point.WorkTime,
                MaxWeightInGrams = point.WeightLimit,
            }.SetDimensionLimit(point.DimensionLimit);
        }

        #region IShippingWithBackgroundMaintenance

        public void ExecuteJob()
        {
            if ((_login.IsNotEmpty() && _password.IsNotEmpty()) || _token.IsNotEmpty())
            {
                SyncPickPoints(_russianPostApiService);
            }
        }

        public static void SyncPickPoints(RussianPostApiService russianPostApiService)
        {
            var lattDateSync = Configuration.SettingProvider.Items["RussianPostLastDatePickPointsSync"].TryParseDateTime(true);
            try
            {
               var currentDateTime = DateTime.UtcNow;

                if (!lattDateSync.HasValue || (currentDateTime - lattDateSync.Value.ToUniversalTime() > TimeSpan.FromHours(23)))
                {
                    // пишем в начале импорта, чтобы, если запустят в паралель еще
                    // то не прошло по условию времени последнего запуска
                    Configuration.SettingProvider.Items["RussianPostLastDatePickPointsSync"] = currentDateTime.ToString("O");

                    PickPointsServices.Sync(russianPostApiService);
                }
            }
            catch (Exception ex)
            {
                // возвращаем предыдущее заначение, чтобы при следующем запуске снова сработало
                Configuration.SettingProvider.Items["RussianPostLastDatePickPointsSync"] = lattDateSync.HasValue ? lattDateSync.Value.ToString("O") : null;
                Debug.Log.Warn(ex);
            }
        }

        #endregion

        #region Help

        public static EnMailType GetEnMailTypeByInt(int oldEnumIntValue)
        {
            switch (oldEnumIntValue)
            {
                case 0:
                    return EnMailType.PostalParcel;

                case 1:
                    return EnMailType.OnlineParcel;

                case 2:
                    return EnMailType.OnlineCourier;

                case 3:
                    return EnMailType.Ems;

                case 4:
                    return EnMailType.EmsOptimal;

                case 5:
                    return EnMailType.EmsRT;

                case 6:
                    return EnMailType.EmsTender;

                case 7:
                    return EnMailType.Letter;

                case 8:
                    return EnMailType.LetterClass1;

                case 9:
                    return EnMailType.Banderol;

                case 10:
                    return EnMailType.BusinessCourier;

                case 11:
                    return EnMailType.BusinessCourierExpress;

                case 12:
                    return EnMailType.ParcelClass1;

                case 13:
                    return EnMailType.BanderolClass1;

                case 14:
                    return EnMailType.VGPOClass1;

                case 15:
                    return EnMailType.SmallPacket;

                case 16:
                    return EnMailType.VSD;

                case 17:
                    return EnMailType.ECOM;

                default:
                    return null;
            }
        }

        public static EnMailCategory GetEnMailCategoryByInt(int oldEnumIntValue)
        {
            switch (oldEnumIntValue)
            {
                case 0:
                    return EnMailCategory.Simple;

                case 1:
                    return EnMailCategory.Ordinary;

                case 2:
                    return EnMailCategory.Ordered;

                case 3:
                    return EnMailCategory.WithDeclaredValue;

                case 4:
                    return EnMailCategory.WithDeclaredValueAndCashOnDelivery;

                case 5:
                    return EnMailCategory.WithCompulsoryPayment;

                case 6:
                    return EnMailCategory.WithDeclaredValueAndCompulsoryPayment;

                default:
                    return null;
            }
        }

        public static EnTransportType GetEnTransportTypeByInt(int oldEnumIntValue)
        {
            switch (oldEnumIntValue)
            {
                case 0:
                    return EnTransportType.Standard;

                case 1:
                    return EnTransportType.Surface;

                case 2:
                    return EnTransportType.Avia;

                case 3:
                    return EnTransportType.Combined;

                case 4:
                    return EnTransportType.Express;

                default:
                    return null;
            }
        }

        #endregion
    }

    public class RussianPostPoint : BaseShippingPoint
    {
    }

    public class DeliveryPeriod
    {
        public int? MaxDays { get; set; }
        public int? MinDays { get; set; }
    }

    public enum EnTypeNotification
    {
        /// <summary>
        /// Заказное
        /// </summary>
        [Localize("Заказное")]
        WithOrderOfNotice = 0,

        /// <summary>
        /// Простое
        /// </summary>
        [Localize("Простое")]
        WithSimpleNotice = 1,

        /// <summary>
        /// Электронное
        /// </summary>
        [Localize("Электронное")]
        WithElectronicNotice = 2,
    }

    [Obsolete]
    public enum EnTypeInsure
    {
        /// <summary>
        /// С объявленной ценностью и без
        /// </summary>
        [Localize("С объявленной ценностью и без")]
        Both = 2,

        /// <summary>
        /// С объявленной ценностью
        /// </summary>
        [Localize("С объявленной ценностью")]
        WithDeclaredValue = 0,

        /// <summary>
        /// Без объявленной ценности
        /// </summary>
        [Localize("Без объявленной ценности")]
        WithoutDeclaredValue = 1,
    }
}
