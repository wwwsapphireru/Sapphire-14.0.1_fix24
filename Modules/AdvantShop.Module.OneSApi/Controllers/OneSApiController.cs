using AdvantShop.Catalog;
using AdvantShop.Core;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Attributes;
using AdvantShop.Module.OneSApi.Handlers.Api.Customers;
using AdvantShop.Module.OneSApi.Handlers.Api.Orders;
using AdvantShop.Module.OneSApi.Handlers.Api.Product;
using AdvantShop.Module.OneSApi.Handlers.Api.Products;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Shipping;
using AdvantShop.Web.Infrastructure.Controllers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace AdvantShop.Module.OneSApi.Controllers
{
    public class OneSApiController : ModuleController
    {
        [/*LogRequest,*/ AuthApi/*, HttpPost*/]
        public JsonResult Check()
        {
            return Json(new { status = "ok" });
        }

        [/*LogRequest,*/ AuthApi/*, HttpPost*/]
        public JsonResult Error()
        {
            return Json(new { status = ((int?)null).Value });
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult ProductImport()
        {
            if (!ModelState.IsValid)
                return JsonError();

            string json = "";
            try
            {
                json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<CatalogImportModel>(json);

                //Debug.Log.Info(JsonConvert.SerializeObject(new { model }));
                return ProcessJsonResult(new ImportProducts(false/*, "ImportProductsFrom1C", "Загрузка товаров из 1C"*/), model ?? new CatalogImportModel());
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                Debug.Log.Error(json);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, */AuthApi/*, HttpPost*/]
        public JsonResult OrderGetList(FilterOrdersModel model)
        {
            var settings = ModuleService.GetImportExportSettings();
            model.ItemsPerPage = settings.ExportOrder.ItemsPerPage;

            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var handlerCustomers = new GetCustomers(new FilterCustomersModel());
                var customers = handlerCustomers.Execute();
                ExportService.SetExportedCustomers(customers.DataItems.Select(x => Guid.Parse(x.Id)).ToList());

                var handlerOrders = new GetOrders(model, settings);
                var orders = handlerOrders.Execute();
                ExportService.SetExportedOrders(orders.DataItems.Select(x => x.Id).ToList());

                var filename = ModuleService.WriteLog("GetOrders", JsonConvert.SerializeObject(orders, Formatting.Indented), null, finish: false);

                return JsonOk(new { Data = new { Customers = customers, Orders = orders }, LogFile = filename });
            }
            catch (BlException e)
            {
                ModelState.AddModelError(e.Property, e.Message);
                return JsonError();
            }
        }

        [/*LogRequest, */AuthApi, HttpGet] 
        public JsonResult OrderGet(string externalId, string orderId)
        {
            if (!ModelState.IsValid)
                return JsonError();

            return ProcessJsonResult(new GetOrder(), new KeyValuePair<string, string> (externalId, orderId));
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult OrderConfirm()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<ConfirmOrderModel>(json);

                return ProcessJsonResult(new ConfirmOrders(), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult OrderUnconfirm()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<ConfirmOrderModel>(json);

                return ProcessJsonResult(new UnconfirmOrders(), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult OrderImport()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<OrderImportModel>(json);

                //Debug.Log.Info(JsonConvert.SerializeObject(new { model }));
                return ProcessJsonResult(new ImportOrders(false), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult CustomersImport()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<CustomersImportModel>(json);

                //Debug.Log.Info(JsonConvert.SerializeObject(new { model }));
                return ProcessJsonResult(new ImportCustomers(false), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, */AuthApi, HttpGet]
        public JsonResult ProductExport(string externalId)
        {
            if (!ModelState.IsValid)
                return JsonError();

            return ProcessJsonResult(new GetProduct(), externalId);
        }

        [/*LogRequest, */AuthApi/*, HttpPost*/]
        public JsonResult ProductGetList(FilterProductsModel model)
        {
            var settings = ModuleService.GetImportExportSettings();
            model.ItemsPerPage = 100;// settings.ExportOrder.ItemsPerPage;

            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var handler = new GetProducts(model, settings);
                var products = handler.Execute();
                ExportService.SetExportedProducts(products.DataItems.Select(x => x.ProductId).ToList());

                var filename = ModuleService.WriteLog("ExportProducts", JsonConvert.SerializeObject(products, Formatting.Indented), null, finish: false);

                return JsonOk(new { Data = new { Products = products }, LogFile = filename });
            }
            catch (BlException e)
            {
                ModelState.AddModelError(e.Property, e.Message);
                return JsonError();
            }
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult ProductConfirm()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<ConfirmProductModel>(json);

                return ProcessJsonResult(new ConfirmProducts(), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult ProductUnconfirm()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<ConfirmProductModel>(json);

                return ProcessJsonResult(new UnconfirmProducts(), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        //[/*LogRequest, */AuthApi/*, HttpPost*/]
        //public JsonResult CustomerGetList(FilterCustomersModel model)
        //{
        //    //var settings = ModuleService.GetImportExportSettings();
        //    //model.ItemsPerPage = settings.ExportOrder.ItemsPerPage;

        //    if (!ModelState.IsValid)
        //        return JsonError();

        //    try
        //    {
        //        var handler = new GetCustomers(model);
        //        var items = handler.Execute();
        //        ExportService.SetExported(items.DataItems.Select(x => x.Id).ToList());

        //        var filename = ModuleService.WriteLog("GetCustomers", JsonConvert.SerializeObject(items, Formatting.Indented), null, finish: false);

        //        return JsonOk(new { Data = items, LogFile = filename });
        //    }
        //    catch (BlException e)
        //    {
        //        ModelState.AddModelError(e.Property, e.Message);
        //        return JsonError();
        //    }
        //}

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult CustomerConfirm()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var model = JsonConvert.DeserializeObject<ConfirmCustomerModel>(json);

                return ProcessJsonResult(new ConfirmCustomers(), model);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        //[/*LogRequest, */AuthApi, HttpPost]
        //public JsonResult CustomerUnconfirm()
        //{
        //    if (!ModelState.IsValid)
        //        return JsonError();

        //    try
        //    {
        //        var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
        //        var model = JsonConvert.DeserializeObject<ConfirmCustomerModel>(json);

        //        return ProcessJsonResult(new UnconfirmCustomers(), model);
        //    }
        //    catch (Exception E)
        //    {
        //        Debug.Log.Error(E);
        //        return JsonError(E.Message);
        //    }
        //}

        [/*LogRequest, */AuthApi, HttpPost]
        public JsonResult CalculateShippings()
        {
            if (!ModelState.IsValid)
                return JsonError();

            try
            {
                var json = new StreamReader(System.Web.HttpContext.Current.Request.InputStream).ReadToEnd();
                var request = JsonConvert.DeserializeObject<GetCheckoutShippingsRequest>(json);

                //var preorderList = new List<PreOrderItem>();
                //foreach (var item in request.PreOrderList)
                //{
                //    var productId = ImportService.GetIdByExternalId(item.ExternalId, "Product");
                //    var product = ProductService.GetProduct(productId);
                //    if (product == null)
                //        product = ProductService.GetProduct(item.ArtNo);
                //    if (product == null)
                //        continue;
                //    var offer = OfferService.GetMainOfferForExport(productId);
                //    preorderList.Add(new PreOrderItem
                //    {
                //        Id = preorderList.Count,
                //        ArtNo = offer.ArtNo,
                //        ProductId = product.ProductId,
                //        Name = product.Name,
                //        Price = item.Price,
                //        Amount = item.Amount,
                //        ShippingPrice = 0,
                //        Weight = offer.GetWeight(),
                //        Width = offer.GetWidth(),
                //        Height = offer.GetHeight(),
                //        Length = offer.GetLength()
                //    });
                //}

                var response = new GetCheckoutShippings(request, false).Execute();

                return JsonOk(new { Data = new { Options = response.option } });
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return JsonError(E.Message);
            }
        }

        [/*LogRequest, AuthApi, */HttpPost]
        public JsonResult CheckRefusing(int orderId)
        {
            //if (!ModelState.IsValid)
            //    return JsonError();

            Debug.Log.Info("OneSApi CheckRefusing orderId = " + orderId);

            var settings = ModuleService.GetImportExportSettings();
            if (settings.ExportOrder.StatusRefusing > 0)
            {
                Debug.Log.Info("OneSApi CheckRefusing StatusRefusing = " + settings.ExportOrder.StatusRefusing);
                var order = OrderService.GetOrder(orderId);
                if (order.OrderStatusId == settings.ExportOrder.StatusRefusing)
                {
                    Debug.Log.Info("OneSApi CheckRefusing OrderStatusId = " + order.OrderStatusId);
                    if (order.ShippingMethod?.ShippingType.Contains("Sdek") == true)
                    {
                        Debug.Log.Info("OneSApi CheckRefusing ShippingType = Sdek");
                        var type = ReflectionExt.GetTypeByAttributeValue<ShippingKeyAttribute>(typeof(BaseShipping), atr => atr.Value, order.ShippingMethod.ShippingType);
                        var shippingMethod = (Shipping.Sdek.Sdek)Activator.CreateInstance(type, order.ShippingMethod, null);
                        shippingMethod.SyncStatusOfOrder(order);
                    }
                    else if (order.ShippingMethod?.ShippingType.Contains("RussianPost") == true)
                    {
                        Debug.Log.Info("OneSApi CheckRefusing ShippingType = RussianPost");
                        var type = ReflectionExt.GetTypeByAttributeValue<ShippingKeyAttribute>(typeof(BaseShipping), atr => atr.Value, order.ShippingMethod.ShippingType);
                        var shippingMethod = (Shipping.RussianPost.RussianPost)Activator.CreateInstance(type, order.ShippingMethod, null);
                        shippingMethod.SyncStatusOfOrder(order);
                    }
                }
            }

            return JsonOk();
        }

    }
}
