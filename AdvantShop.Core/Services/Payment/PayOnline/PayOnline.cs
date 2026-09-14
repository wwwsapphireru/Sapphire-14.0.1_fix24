//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Orders;
using AdvantShop.Shipping;
using Newtonsoft.Json;

namespace AdvantShop.Payment
{
    public enum PayOnlineType
    {
        Select,
        WebMoney,
        QIWI,
        YandexMoney,
        CreditCard_EN,
        CreditCard_RU

    }

    [PaymentKey("PayOnline")]
    public class PayOnline : PaymentMethod
    {
        private string Url
        {
            get
            {
                switch (PayType)
                {
                    case PayOnlineType.QIWI:
                        //Форма оплаты через QIWI:
                        return LinkService.PaymentMethod.PayOnline.Qiwi + "/";

                    case PayOnlineType.WebMoney:
                        //Форма оплаты через WebMoney:
                        return LinkService.PaymentMethod.PayOnline.WebMoney + "/";

                    case PayOnlineType.YandexMoney:
                        return LinkService.PaymentMethod.PayOnline.YandexMoney + "/";

                    case PayOnlineType.CreditCard_EN:
                        //Форма оплаты с банковской карты – английский интерфейс
                        return LinkService.PaymentMethod.PayOnline.CreditCardEN + "/";

                    case PayOnlineType.CreditCard_RU:
                        //Форма оплаты с банковской карты  – русский интерфейс
                        return LinkService.PaymentMethod.PayOnline.CreditCardRU + "/";
                    default:
                        //Форма выбора платежного инструмента:
                        return LinkService.PaymentMethod.PayOnline.Select + "/";
                }
            }
        }

        public string MerchantId { get; set; }
        public string SecretKey { get; set; }
        public PayOnlineType PayType { get; set; }
        public string ShowForNewUsers { get; set; }//GlorySoft_001


        public override ProcessType ProcessType
        {
            get { return ProcessType.FormPost; }
        }
        public override NotificationType NotificationType
        {
            get { return NotificationType.Handler; }
        }
        public override UrlStatus ShowUrls
        {
            get { return UrlStatus.NotificationUrl; }
        }

        public override bool CurrencyAllAvailable => false;

        public override string[] CurrencyIso3Available => new[] {"USD", "RUB", "EUR"};

        public override Dictionary<string, string> Parameters
        {
            get
            {
                return new Dictionary<string, string>
                           {
                               {PayOnlineTemplate.MerchantId, MerchantId},
                               {PayOnlineTemplate.SecretKey, SecretKey},
                               {PayOnlineTemplate.PayType, ((int)PayType).ToString()},
                               {PayOnlineTemplate.ShowForNewUsers, ShowForNewUsers}//GlorySoft_001
                           };
            }
            set
            {
                MerchantId = value.ElementOrDefault(PayOnlineTemplate.MerchantId);
                SecretKey = value.ElementOrDefault(PayOnlineTemplate.SecretKey);
                int intval;
                PayType = int.TryParse(value.ElementOrDefault(PayOnlineTemplate.PayType), out intval) ? (PayOnlineType)intval : PayOnlineType.Select;
                ShowForNewUsers = value.ElementOrDefault(PayOnlineTemplate.ShowForNewUsers);//GlorySoft_001
            }
        }

        public override PaymentForm GetPaymentForm(Order order)
        {
            var paymentNo = order.OrderID.ToString();
            var paymentCurrency = PaymentCurrency ?? order.OrderCurrency;
            //var orderSumStr = order.Sum
            //    .ConvertCurrency(order.OrderCurrency, paymentCurrency)
            //    .ToString("F2", CultureInfo.InvariantCulture);GlorySoft_001
            var orderSumStr = PayOnlineService.GetOrderSum(order.OrderID).ToString("F2", CultureInfo.InvariantCulture);//GlorySoft_001

            var currency = paymentCurrency.Iso3;
            return new PaymentForm
            {
                Url = Url,
                InputValues = new NameValueCollection
                {
                    {"MerchantId", MerchantId},
                    {"OrderId", paymentNo},
                    {"Amount",orderSumStr},
                    {"Currency",currency}, // Можно использовать следующие валюты: RUB, USD и EUR
                    {"SecurityKey",GetMd5("MerchantId="+MerchantId+"&OrderId="+paymentNo+"&Amount="+orderSumStr+"&Currency="+currency+"&PrivateSecurityKey="+SecretKey) },
                    {"ReturnUrl", HttpUtility.UrlDecode(SuccessUrl)},
                    {"FailUrl", HttpUtility.UrlDecode(FailUrl)},
                    {"Email", order.OrderCustomer.Email}//GlorySoft_001
                }
            };
        }

        public override string ProcessResponse(HttpContext context)
        {
            var req = context.Request;
            if (!CheckData(req))
                return NotificationMessahges.InvalidRequestData;
            var paymentNumber = req["OrderId"];
            if (int.TryParse(paymentNumber, out var orderId) /*GlorySoft_001 && OrderService.GetOrder(orderId) != null*/)
            {
                //OrderService.PayOrder(orderId, true);GlorySoft_001
                //return NotificationMessahges.SuccessfullPayment(paymentNumber);GlorySoft_001

                //GlorySoft_001
                var order = OrderService.GetOrder(orderId);
                if (order != null)
                {
                    OrderService.PayOrder(orderId, true);

                    var isQr = req["QrCodeId"].IsNotEmpty();
                    if (!isQr)
                        OrderService.AddUpdateOrderAdditionalData(orderId, "CardHolder", req["CardHolder"]);

                    //фискализация
                    try
                    {
                        var url = "https://secure.payonlinesystem.com/Services/Fiscal/Request.ashx";
                        var message = new PayOnlineFiscalRequestInput
                        {
                            operation = "Benefit",
                            transactionId = isQr ? req["QrCodeId"] : req["TransactionID"],
                            paymentSystemType = req["Provider"],
                            totalAmount = PayOnlineService.GetOrderSum(orderId),//(decimal/*float*/)Math.Round(order.Sum, 2),
                            email = order.OrderCustomer.Email,
                            //clientPhone = order.OrderCustomer.StandardPhone.HasValue ? "+" + order.OrderCustomer.StandardPhone.Value.ToString() : null
                            goods = order.OrderItems/*GetOrderItemsForFiscal(false)*/.Select(x => new PayOnlineFiscalRequestInput_good
                            {
                                description = x.Name,
                                quantity = (decimal)Math.Round(x.Amount, 3),
                                amount = (decimal/*float*/)Math.Round(x.Price, 2),
                                tax = "Vat22"/*(x.TaxType ?? Taxes.TaxType.Vat20).ToString()*/.ToLower(),
                                paymentMethodType = 4,
                                paymentSubjectType = 1
                            }).ToList()
                        };
                        var shipCost = (float)Math.Round(order.Sum, 2) - (float)Math.Round(message.goods.Sum(x => x.quantity * x.amount), 2);
                        if (shipCost > 0)
                        {
                            var tax = Taxes.TaxService.GetTax((order.ShippingMethod ?? new Shipping.ShippingMethod()).TaxId ?? 0) ?? new Taxes.TaxElement() { TaxType = Taxes.TaxType.None };
                            message.goods.Add(new PayOnlineFiscalRequestInput_good
                            {
                                description = "Доставка",
                                quantity = 1,
                                amount = (decimal/*float*/)Math.Round(shipCost, 2),
                                tax = tax.TaxType.ToString().ToLower(),
                                paymentMethodType = 4,
                                paymentSubjectType = 4
                            });
                        }
                        message.totalAmount = message.goods.Sum(x => x.quantity * x.amount);
                        var jsonSettins = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                        var json = JsonConvert.SerializeObject(message, jsonSettins);
                        var parameters = new Dictionary<string, string>
                        {
                            { "MerchantId", MerchantId },
                            { "SecurityKey", GetMd5(string.Format("RequestBody={0}&MerchantId={1}&PrivateSecurityKey={2}", json, MerchantId, SecretKey)) },
                        };
                        string error = "";
                        var request = ApiRequest(url, "POST", json, parameters, ref error);
                        string response = "";
                        GetResponse(request, ref response);
                        Debug.Log.Info(string.Format("PayOnlineWithFiscal Request Response: {0}", response));

                        var output = JsonConvert.DeserializeObject<PayOnlineFiscalRequestOutput>(response);
                        if (output?.payload != null && output.payload.inn.IsNotEmpty() && output.payload.name_document.IsNotEmpty())
                        {
                            OrderService.AddUpdateOrderAdditionalData(orderId, "ChequeUrl",
                                $"https://cheques-lk.orangedata.ru/{output.payload.inn}/{output.payload.name_document}");
                        }
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                    }

                    return NotificationMessahges.SuccessfullPayment(paymentNumber);
                }
            }
            return NotificationMessahges.Fail;
        }

        private static string GetMd5(string str)
        {
            return str.Md5(false, Encoding.UTF8);
        }

        private bool CheckData(HttpRequest req)
        {
            return !new[]
                        {
                            "DateTime",
                            "TransactionID",
                            "OrderId",
                            "Amount",
                            "Currency",
                        }.Any(field => string.IsNullOrEmpty(req[field]))
                   &&
                   GetMd5("DateTime=" + req["DateTime"] +
                          "&TransactionID=" + req["TransactionID"] +
                          "&OrderId=" + req["OrderId"] +
                          "&Amount=" + req["Amount"] +
                          "&Currency=" + req["Currency"] +
                          (req["QrCodeId"].IsNotEmpty() 
                              ? "&QrCodeId=" + req["QrCodeId"] 
                              : string.Empty) +
                          "&PrivateSecurityKey=" + SecretKey
                   ) == req["SecurityKey"];
        }

        public override BasePaymentOption GetOption(BaseShippingOption shippingOption, float preCoast, CustomerType? customerType)//GlorySoft_001
        {
            if (ShowForNewUsers == "reg")
            {
                if (CustomerContext.CurrentCustomer == null || !CustomerContext.CurrentCustomer.RegistredUser || CustomerContext.CurrentCustomer.ClientStatus == CustomerClientStatus.Bad)
                    return null;
                if (!PayOnlineService.HasCompletedOrders(CustomerContext.CustomerId))
                    return null;
            }
            else if (ShowForNewUsers == "new")
            {
                if (CustomerContext.CurrentCustomer != null && PayOnlineService.HasCompletedOrders(CustomerContext.CustomerId))
                    return null;
            }
            var option = new BasePaymentOption(this, preCoast);
            return option;
        }


        private HttpWebRequest ApiRequest(string url, string method, string json, Dictionary<string, string> parameters, ref string error)//GlorySoft_001
        {
            var urlWithPrams = url + (parameters.Count > 0 ? "?" : "");
            foreach (var p in parameters)
            {
                urlWithPrams += p.Key + "=" + HttpUtility.UrlEncode(p.Value) + "&";
            }
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;// | SecurityProtocolType.Tls13;
            var request = (HttpWebRequest)WebRequest.Create(urlWithPrams.TrimEnd('&'));
            Debug.Log.Info(string.Format("PayOnlineWithFiscal Request : {0} {1}", urlWithPrams, json));

            try
            {
                //if (message != null)
                //{
                //    var jsonSettins = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                //    json = JsonConvert.SerializeObject(message, jsonSettins);
                //}

                var body = Encoding.UTF8.GetBytes(json);

                request.Method = method;

                if (json.IsNotEmpty())
                {
                    request.ContentType = "application/json";
                    request.ContentLength = body.Length;

                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(body, 0, body.Length);
                        stream.Close();
                    }
                }
            }
            catch (Exception Ex)
            {
                Debug.Log.Error(Ex);
                Debug.Log.Error(urlWithPrams + " " + json);
                error = Ex.Message;

                if (Ex.GetType() == typeof(WebException) && ((WebException)Ex).Response != null)
                {
                    try
                    {
                        using (var reader = new StreamReader(((WebException)Ex).Response.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                        {
                            error = reader.ReadToEnd();
                            Debug.Log.Error(error);
                        }
                    }
                    catch { }
                }
            }

            return request;
        }

        private void GetResponse(HttpWebRequest request, ref string responseFromServer)//GlorySoft_001
        {
            try
            {
                var response = request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                {
                    responseFromServer = reader.ReadToEnd();
                }
            }
            catch (Exception Ex)
            {
                if (Ex.GetType() == typeof(WebException) && ((WebException)Ex).Response != null)
                {
                    try
                    {
                        using (var reader = new StreamReader(((WebException)Ex).Response.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                        {
                            responseFromServer = reader.ReadToEnd();
                            //Debug.Log.Error(responseFromServer);
                        }
                    }
                    catch
                    {
                        Debug.Log.Error(Ex);
                        Debug.Log.Error(request.RequestUri.AbsoluteUri);
                        //exception = Ex;
                    }
                }
                else
                {
                    Debug.Log.Error(Ex);
                    Debug.Log.Error(request.RequestUri.AbsoluteUri);
                    //exception = Ex;
                }
            }
        }

    }
}