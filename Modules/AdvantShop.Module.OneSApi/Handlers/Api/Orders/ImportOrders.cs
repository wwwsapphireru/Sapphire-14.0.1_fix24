using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Repository.Currencies;
using AdvantShop.Shipping;
using AdvantShop.Taxes;
using AdvantShop.Web.Infrastructure.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Orders
{
    public class ImportOrders : ICommandHandler<OrderImportModel, OrderImportResultModel>
    {
        //private const string _columnSeparator = "&&";
        //private const string _propertySeparator = "~";
        private const string _modifiedBy = OneSApi.ModuleName;
        private ImportExportSettingsModel _settings;
        private bool _withStatisctic;

        public ImportOrders(bool withStatisctic)
        {
            _withStatisctic = withStatisctic;
        }

        public OrderImportResultModel Execute(OrderImportModel catalog)
        {
            var start = DateTime.Now;
            _settings = ModuleService.GetImportExportSettings();

            var result = new OrderImportResultModel();
            if (catalog.Orders != null)
                result.Orders = new List<ImportResultModel> { };

            //var info = OneSApi.ModuleStringId + " ImportOrders " + (catalog.Orders != null ? catalog.Orders.Select(x => x.Number).AggregateString(',') : "");
            //Debug.Log.Info(info);

            try
            {
                ExecuteOrders(catalog.Orders, ref result);
            }
            catch (Exception e)
            {
                Debug.Log.Error(e);
                //Debug.Log.Info(info + " ERROR " + e.Message + " " + e.StackTrace);
            }

            ModuleService.WriteLog("ImportOrders", JsonConvert.SerializeObject(catalog, Formatting.Indented), JsonConvert.SerializeObject(result, Formatting.Indented), start);

            return result;
        }
    
        private void ExecuteOrders(List<OrderV8Model> orders, ref OrderImportResultModel result)
        {
            if (orders == null)
                return;

            foreach (var item in orders)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true };

                var orderId = ImportService.GetIdByExternalId(item.ExternalId, "Order");
                var order = OrderService.GetOrder(orderId);
                if (order == null && orderId > 0)
                {
                    orderId = 0;
                    ImportService.DeleteExternalId(item.ExternalId, "Order");
                }
                if (order == null)
                    order = OrderService.GetOrderByNumber(item.Number);
                if (order == null)
                    continue;

                //var trackChanges = false;
                var changedBy = new OrderChangedBy(_modifiedBy);

                //if (_settings.ImportOrder.UpdateCurrency && item.Currency.IsNotEmpty() && (order.OrderCurrency ?? new OrderCurrency()).CurrencyCode != item.Currency)
                //{
                //    var currency = CurrencyService.GetCurrencyByIso3(item.Currency);
                //    if (currency == null)
                //    {
                //        currency = new Currency
                //        {
                //        };
                //        CurrencyService.InsertCurrency(currency);
                //    }
                //    order.OrderCurrency = currency;
                //    trackChanges = true;
                //}
                if (_settings.ImportOrder.UpdateAdminComment && item.AdminComment.IsNotEmpty() && (order.AdminOrderComment != item.AdminComment))
                {
                    order.AdminOrderComment = item.AdminComment;
                    OrderServiceV8.UpdateAdminOrderComment(order.OrderID, item.AdminComment, changedBy);
                }
                
                if (_settings.ImportOrder.UpdatePayed && order.Payed != item.IsPaid)
                {
                    OrderService.PayOrder(order.OrderID, item.IsPaid, changedBy: changedBy, updateModules: false);
                }

                var orderShippingMethod = order.ShippingMethod;
                //if (item.ShippingId.HasValue && (order.ShippingMethodId != item.ShippingId || order.ShippingCost != item.ShippingCost))
                //{
                //    var s = ShippingMethodService.GetShippingMethod(item.ShippingId.Value);
                //    ImportService.UpdateOrderShipping(order.OrderID, s != null ? item.ShippingId.Value : _settings.ImportOrder.ShippingUnknown, item.ShippingCost, productsCost, s);
                //}
                //try
                //{
                    if (order.ShippingMethodId != item.ShippingId || order.ShippingCost != (float)item.ShippingCost)
                    {
                        var su = ShippingMethodService.GetShippingMethod(_settings.ImportOrder.ShippingUnknown);
                        var newProductsCost = order.OrderCertificates != null && order.OrderCertificates.Count > 0
                            ? (decimal)order.OrderCertificates.Sum(x => x.Sum)
                            : item.Items.Sum(x => x.Price * x.Amount);
                        if (item.ShippingId.HasValue)
                        {
                            var sn = ShippingMethodService.GetShippingMethod(item.ShippingId.Value);
                            ImportService.UpdateOrderShipping(order.OrderID, sn != null ? item.ShippingId.Value : _settings.ImportOrder.ShippingUnknown, item.ShippingCost, newProductsCost, sn ?? su, sn == null ? item.ShippingName : null);
                            orderShippingMethod = ShippingMethodService.GetShippingMethod(sn != null ? item.ShippingId.Value : _settings.ImportOrder.ShippingUnknown);
                        }
                        else
                        {
                            ImportService.UpdateOrderShipping(order.OrderID, _settings.ImportOrder.ShippingUnknown, item.ShippingCost, newProductsCost, su, item.ShippingName);
                            orderShippingMethod = ShippingMethodService.GetShippingMethod(_settings.ImportOrder.ShippingUnknown);
                        }
                    }
                    if (order.ShippingMethodId == _settings.ImportOrder.ShippingForTK && order.ShippingMethodName != item.ShippingName)
                    {
                        ImportService.UpdateOrderShippingName(order.OrderID, item.ShippingName);
                    }
                //}
                //catch (Exception e)
                //{
                //    Debug.Log.Error(e);
                //}

                if (item.PaymentType.IsNotEmpty() && order.PaymentMethod == null && orderShippingMethod != null)
                {
                    var paymentId = ImportService.GetPaymentIdByTypeAndShipping(item.PaymentType, orderShippingMethod.ShippingMethodId);
                    var payment = Payment.PaymentService.GetPaymentMethod(paymentId);
                    if (payment != null)
                    {
                        order.PaymentMethodId = paymentId;
                        ImportService.UpdateOrderPayment(order.OrderID, payment);
                    }
                }

                if (_settings.ImportOrder.UpdateTracking && item.TrackNumber.IsNotEmpty())
                {
                    order.AdminOrderComment = item.AdminComment;
                    if (orderShippingMethod != null && orderShippingMethod.ShippingType.Contains("RussianPost"))
                        ImportService.UpdateTrackNumber(order.OrderID, item.TrackNumber.ToUpper());
                    else
                        ImportService.UpdateTrackNumber(order.OrderID, item.TrackNumber);

                    if (orderShippingMethod != null && orderShippingMethod.ShippingType.Contains("Sdek"))
                    {
                        //Debug.Log.Info(string.Format("SdekAll Track {0} {1}", order.OrderID, item.TrackNumber));
                        var sdekOrderUuid = OrderService.GetOrderAdditionalData(order.OrderID, Shipping.Sdek.Sdek.KeyNameSdekOrderUuidInOrderAdditionalData);
                        var sdekOrderNumber = OrderService.GetOrderAdditionalData(order.OrderID, Shipping.Sdek.Sdek.KeyNameDispatchNumberInOrderAdditionalData);
                        if ((sdekOrderUuid.IsNullOrEmpty() || sdekOrderNumber.IsNullOrEmpty()) && item.TrackNumber.IsNotEmpty())
                        {
                            //Debug.Log.Info(string.Format("SdekAll Track GetUuid"));
                            var sdekApiServices20 = new List<Shipping.Sdek.Api.SdekApiService20>
                            {
                                new Shipping.Sdek.Api.SdekApiService20(orderShippingMethod.Params.ElementOrDefault(Shipping.Sdek.SdekTemplate.AuthLogin), orderShippingMethod.Params.ElementOrDefault(Shipping.Sdek.SdekTemplate.AuthPassword))
                            };
                            var additionalAccounts = (orderShippingMethod.Params.ElementOrDefault("AdditionalAccounts") ?? "").Split("\n");
                            foreach (var additional in additionalAccounts)
                            {
                                var add = additional.Trim().Split("|");
                                if (add.Length == 2)
                                    sdekApiServices20.Add(new Shipping.Sdek.Api.SdekApiService20(add[0], add[1]));
                                //Debug.Log.Info(string.Format("SdekAll Track GetUuid Auth {0}", add.AggregateString(',')));
                            }
                            foreach (var sdekApiService20 in sdekApiServices20)
                            {
                                var sdekOrder = sdekApiService20.GetOrder(null, item.TrackNumber, null);
                                //if (sdekOrder?.Entity == null)
                                //    sdekOrder = sdekApiService20.GetOrder(null, null, order.Number);
                                if (sdekOrder?.Entity != null)
                                {
                                    //Debug.Log.Info(string.Format("SdekAll Track GetUuid Entity {0}", JsonConvert.SerializeObject(sdekOrder?.Entity)));
                                    if (sdekOrderUuid.IsNullOrEmpty())
                                    {
                                        OrderService.AddUpdateOrderAdditionalData(
                                            order.OrderID,
                                            Shipping.Sdek.Sdek.KeyNameSdekOrderUuidInOrderAdditionalData,
                                            sdekOrder.Entity.Uuid.ToString());
                                    }
                                    if (sdekOrderNumber.IsNullOrEmpty() && sdekOrder.Entity.CdekNumber.IsNotEmpty())
                                    {
                                        OrderService.AddUpdateOrderAdditionalData(
                                            order.OrderID,
                                            Shipping.Sdek.Sdek.KeyNameDispatchNumberInOrderAdditionalData,
                                            sdekOrder.Entity.CdekNumber);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                //if (_settings.ImportOrder.UpdateOrderCustomer)
                //{
                //    if (order.OrderCustomer.FirstName != item.Customer.)
                //    OrderService.UpdateOrderCustomer(orderCustomer);
                //}
                if (OrderService.GetOrderAdditionalData(order.OrderID, OneSApi.ModuleStringId + "BillOrg") != item.BillOrg)
                {
                    OrderService.AddUpdateOrderAdditionalData(order.OrderID, OneSApi.ModuleStringId + "BillOrg", item.BillOrg);
                }

                if (item.ManagerEmail.IsNotEmpty())
                {
                    var mc = CustomerService.GetCustomerByEmail(item.ManagerEmail);
                    if (mc != null)
                    {
                        var man = ManagerService.GetManager(mc.Id);
                        if (man != null)
                            OrderService.UpdateOrderManager(order.OrderID, man.ManagerId);
                    }
                }

                var orderCurrency = order.OrderCurrency != null ? (Currency)order.OrderCurrency : CurrencyService.Currency(SettingsCatalog.DefaultCurrencyIso3);
                var productsCost = order.OrderCertificates != null && order.OrderCertificates.Count > 0
                    ? order.OrderCertificates.Sum(x => x.Sum)
                    : order.OrderItems.Sum(x => PriceService.SimpleRoundPrice((float)Math.Round(x.Price, 2) * x.Amount, orderCurrency));

                if (_settings.ImportOrder.UpdateItems)
                {
                    var oldItems = order.OrderItems;
                    var items = new List<OrderItem> { };

                    var changed = false;

                    foreach (var it in item.Items)
                    {
                        OrderItem newItem;

                        var productId = ImportService.GetIdByExternalId(it.ExternalId, "Product");
                        var product = ProductService.GetProduct(productId);
                        string artNo = "";
                        if (product == null)
                        {
                            if (_settings.ImportArtnoType == ImportArtnoType.ArtNo)
                                artNo = it.ArtNo;
                            else if (_settings.ImportArtnoType == ImportArtnoType.Code)
                                artNo = it.Code;
                            product = ProductService.GetProduct(artNo);
                        }
                        else
                            artNo = product.ArtNo;

                        OrderItem oldItem;
                        if (product != null)
                            oldItem = oldItems.FirstOrDefault(x => x.ProductID == product.ProductId);
                        else
                            oldItem = oldItems.FirstOrDefault(x => x.ArtNo == artNo);
                        if (oldItem != null)
                            newItem = oldItem.DeepCloneJson();
                        else
                        {
                            changed = true;
                            newItem = new OrderItem { ArtNo = artNo };
                            if (product != null)
                            {
                                newItem.ProductID = product.ProductId;
                                if (product.Unit != null)
                                {
                                    newItem.Unit = product.Unit.Name;
                                    newItem.MeasureType = product.Unit.MeasureType;
                                }
                                var o = OfferService.GetMainOfferForExport(product.ProductId);
                                if (o != null)
                                {
                                    newItem.Length = o.GetLength();
                                    newItem.Width = o.GetWidth();
                                    newItem.Height = o.GetHeight();
                                    newItem.Weight = o.GetWeight();
                                    newItem.BarCode = o.BarCode;
                                    newItem.BasePrice = o.BasePrice;
                                }
                                var mp = PhotoService.GetMainProductPhoto(product.ProductId);
                                if (mp != null)
                                    newItem.PhotoID = mp.PhotoId;
                                
                            }
                        }

                        if (it.Amount != (decimal)newItem.Amount)
                        {
                            changed = true;
                            newItem.Amount = (float)it.Amount;
                        }
                        if (it.Color != newItem.Color)
                        {
                            changed = true;
                            newItem.Color = it.Color;
                        }
                        if (it.Name != newItem.Name)
                        {
                            changed = true;
                            newItem.Name = it.Name;
                        }
                        if (it.Price != (decimal)newItem.Price)
                        {
                            changed = true;
                            newItem.Price = (float)it.Price;
                        }
                        if (it.Size != newItem.Size)
                        {
                            changed = true;
                            newItem.Size = it.Size;
                        }
                        if (it.TaxRate != newItem.TaxRate)
                        {
                            changed = true;
                            newItem.TaxRate = it.TaxRate;
                            if (newItem.TaxRate.HasValue)
                            {
                                var t = ImportService.FindTax(newItem.TaxRate.Value);
                                if (t.HasValue)
                                {
                                    newItem.TaxId = t.Value;
                                    var ti = TaxService.GetTax(t.Value);
                                    if (ti != null)
                                    {
                                        newItem.TaxName = ti.Name;
                                        newItem.TaxType = ti.TaxType;
                                        newItem.TaxShowInPrice = ti.ShowInPrice;
                                    }
                                }
                            }
                        }

                        items.Add(newItem);
                        if (oldItem != null)
                            oldItems.Remove(oldItem);
                    }

                    if (changed || oldItems.Count > 0)
                    {
                        OrderService.AddUpdateOrderItems(items, oldItems, order, changedBy, updateModules: false);
                        var newProductsCost = order.OrderCertificates != null && order.OrderCertificates.Count > 0
                            ? (decimal)order.OrderCertificates.Sum(x => x.Sum)
                            : item.Items.Sum(x => x.Price * x.Amount);
                            //: items.Sum(x => PriceService.SimpleRoundPrice((float)Math.Round(x.Price, 2) * x.Amount, orderCurrency));
                        var orderFromDb = OrderService.GetOrder(order.OrderID);
                        var newSum = newProductsCost + item.ShippingCost;//(decimal)((int)Math.Round(productsCost * 100) / 100f) + (decimal)((int)Math.Round(item.ShippingCost * 100) / 100f);
                        if (Math.Round(orderFromDb.Sum, 2) != (float)newSum)
                            ImportService.UpdateOrderSum(order.OrderID, newSum);

                        //Debug.Log.Info("CheckNullDimensions: " + JsonConvert.SerializeObject(orderFromDb.OrderItems));
                        OrderServiceV8.CheckNullDimensions(orderFromDb, true);
                    }
                }

                if (_settings.ImportOrder.UpdateStatus)// && item.Status.Name.IsNotEmpty() && (order.OrderStatus ?? new OrderStatus()).StatusName != item.Status.Name)
                {
                    var statusId = order.OrderStatusId;//OrderStatusService.GetOrderStatusByName(item.Status.Name);
                    if (_settings.ImportOrder.StatusConfirmed > 0 && item.OrderStatus == OrdersStatuses.Confirmed)
                        statusId = _settings.ImportOrder.StatusConfirmed;
                    else if (_settings.ImportOrder.StatusBuilding > 0 && item.OrderStatus == OrdersStatuses.Building)
                        statusId = _settings.ImportOrder.StatusBuilding;
                    else if (_settings.ImportOrder.StatusShipped > 0 && item.OrderStatus == OrdersStatuses.Shipped)
                        statusId = _settings.ImportOrder.StatusShipped;
                    else if (_settings.ImportOrder.StatusReady > 0 && item.OrderStatus == OrdersStatuses.Ready)
                        statusId = _settings.ImportOrder.StatusReady;
                    else if (_settings.ImportOrder.StatusReadyAtStore > 0 && item.OrderStatus == OrdersStatuses.ReadyAtStore)
                        statusId = _settings.ImportOrder.StatusReadyAtStore;
                    else if (_settings.ImportOrder.StatusDone > 0 && item.OrderStatus == OrdersStatuses.Done)
                        statusId = _settings.ImportOrder.StatusDone;
                    else if (_settings.ImportOrder.StatusWaiting > 0 && item.OrderStatus == OrdersStatuses.Waiting)
                        statusId = _settings.ImportOrder.StatusWaiting;
                    else if (_settings.ImportOrder.StatusCancelled > 0 && item.OrderStatus == OrdersStatuses.Cancelled)
                        statusId = _settings.ImportOrder.StatusCancelled;
                    if (!order.ManagerConfirmed && statusId != order.OrderStatusId && statusId != _settings.ImportOrder.StatusCancelled)
                        OrderService.ManagerConfirmOrder(order.OrderID, true);
                    var status = OrderStatusService.GetOrderStatus(statusId);
                    if (status != null && statusId != order.OrderStatusId)
                    {
                        //int oldStatusId = 0;
                        //if (order.OrderStatusId == _settings.ImportOrder.StatusConfirmed)
                        //    oldStatusId = (int)OrdersStatuses.Confirmed;
                        //else if (order.OrderStatusId == _settings.ImportOrder.StatusShipped)
                        //    oldStatusId = (int)OrdersStatuses.Shipped;
                        //else if (order.OrderStatusId == _settings.ImportOrder.StatusReady)
                        //    oldStatusId = (int)OrdersStatuses.Ready;
                        //else if (order.OrderStatusId == _settings.ImportOrder.StatusDone)
                        //    oldStatusId = (int)OrdersStatuses.Done;
                        //if (oldStatusId < statusId)
                        if (_settings.ImportOrder.StatusCancelled > 0 && statusId == _settings.ImportOrder.StatusCancelled)
                        {
                            if (!order.OrderStatus.IsCanceled)
                                OrderStatusService.ChangeOrderStatus(order.OrderID, status.StatusID, _modifiedBy/*, false*/);
                        }
                        else if (order.TrackNumber.IsNullOrEmpty())
                            OrderStatusService.ChangeOrderStatus(order.OrderID, status.StatusID, _modifiedBy/*, false*/);
                        else if (order.OrderStatus?.IsCompleted != true)
                        {
                            var dictionaryShipping = GetDictionarySupportedShipping();
                            if (!dictionaryShipping.Contains(order.ShippingMethodId))
                                OrderStatusService.ChangeOrderStatus(order.OrderID, status.StatusID, _modifiedBy/*, false*/);
                        }
                    }

                    if (item.KeepFreeUntil.HasValue)
                    {
                        var keepFreeUntilOld = OrderService.GetOrderAdditionalData(order.OrderID, "KeepFreeUntil");
                        var keepFreeUntilNew = item.KeepFreeUntil.ToString();
                        if (keepFreeUntilNew != keepFreeUntilOld)
                        {
                            OrderService.AddUpdateOrderAdditionalData(order.OrderID, "KeepFreeUntil", keepFreeUntilNew);
                            var comment = "Срок резерва заказа: " + keepFreeUntilNew;
                            OrderService.UpdateAdminOrderComment(order.OrderID, order.AdminOrderComment + "\n" + comment, changedBy);
                            OrderService.UpdateStatusComment(order.OrderID, comment, changedBy);
                        }
                    }
                }

                if (orderId == 0)
                {
                    orderId = order.OrderID;
                    ExportService.ConfirmOrders(new List<ConfirmOrderModel_item>
                    {
                        new ConfirmOrderModel_item
                        {
                            OrderId = order.OrderID,
                            ExternalId = item.ExternalId,
                            ExportType = ExportType.Empty,
                        }
                    });
                }

                if (item.BillStorage.IsNotEmpty())
                {
                    ImportService.SaveOrderBill(orderId, item.BillStorage);
                }

                if (resultItem.Success)
                    resultItem.Status = OrderService.GetStatusInfo(order.Number).Status;

                result.Orders.Add(resultItem);
            }
        }

        private static List<int>/*Dictionary<ShippingMethod, BaseShipping>*/ GetDictionarySupportedShipping()
        {
            var supportedTypes = ReflectionExt.GetTypesWith<ShippingKeyAttribute>(false)
                .Where(x => x.GetInterfaces().Contains(typeof(IShippingSupportingSyncOfOrderStatus)) && x.IsSubclassOf(typeof(BaseShipping)))
                .Select(AttributeHelper.GetAttributeValue<ShippingKeyAttribute, string>)
                .ToList();

            return ShippingMethodService.GetAllShippingMethods()
                .Where(method => method.Enabled && supportedTypes.Contains(method.ShippingType))
                //.ToDictionary(method => method, method =>
                //{
                //    var type = ReflectionExt.GetTypeByAttributeValue<ShippingKeyAttribute>(typeof(BaseShipping), atr => atr.Value, method.ShippingType);
                //    return (BaseShipping)Activator.CreateInstance(type, method, null);
                //})
                .Where(x => x.Params.ElementOrDefault(Shipping.Sdek.SdekTemplate.StatusesSync) == bool.TrueString)
                .Select(x => x.ShippingMethodId).ToList();
        }

    }
}
