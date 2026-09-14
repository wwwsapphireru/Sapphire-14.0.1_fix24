using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.SessionState;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using AdvantShop.Orders;
using AdvantShop.Payment;
using AdvantShop.Repository.Currencies;
using AdvantShop.ViewModel.PaymentReceipt;

namespace AdvantShop.Controllers
{
    [SessionState(SessionStateBehavior.Disabled)]
    public class PaymentReceiptController : BaseClientController
    {
        #region Sberbank

        public ActionResult Sberbank(string ordernumber)
        {
            if (string.IsNullOrWhiteSpace(ordernumber))
                return Error404();

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Error404();

            var sberbank = order.PaymentMethod as SberBank;
            
            if (sberbank == null)
                return Error404();

            if (!order.CanAccessToPaymentReceipt(Request["ch"], CustomerContext.CurrentCustomer))
                return CustomerContext.CurrentCustomer.RegistredUser
                    ? Error404()
                    : RedirectToRoute("Login", new {redirectTo = sberbank.GetBillingLink(order)});

            var model = new SberbankViewModel
            {
                PaymentDescription = T("PaymentReceipt.PayOrder") + " #" + order.Number,
                CompanyName = sberbank.CompanyName,
                TransactAccount = sberbank.TransAccount,
                BankName = sberbank.BankName,
                Inn = sberbank.INN,
                Kpp = sberbank.KPP,
                Bik = sberbank.BIK,
                CorrespondentAccount = sberbank.CorAccount,
                Payer = order.PaymentDetails != null && !string.IsNullOrEmpty(order.PaymentDetails.CompanyName)
                            ? Request["bill_companyname"]
                            : StringHelper.AggregateStrings(" ", order.OrderCustomer.LastName, order.OrderCustomer.FirstName, order.OrderCustomer.Patronymic),
                PayerInn = order.PaymentDetails != null && !string.IsNullOrEmpty(order.PaymentDetails.INN)
                            ? order.PaymentDetails.INN
                            : Request["bill_inn"] ?? @"___________________",
                OrderCurrency = order.OrderCurrency
            };

            model.PayerAddress += StringHelper.AggregateStrings(", ", order.OrderCustomer.Country, order.OrderCustomer.Region, order.OrderCustomer.City);

            if (string.IsNullOrEmpty(order.OrderCustomer.Zip))
                model.PayerAddress += @", " + order.OrderCustomer.Zip;

            model.PayerAddress += @", " + order.OrderCustomer.GetCustomerShortAddress();

            if (model.OrderCurrency != null && model.OrderCurrency.Iso3 == "RUB")
            {
                float priceInBaseCurrency = order.Sum.ConvertCurrency(order.OrderCurrency, sberbank.PaymentCurrency ?? order.OrderCurrency);
                model.WholeSumPrice = Math.Floor(priceInBaseCurrency).ToString();
                model.FractSumPrice = ((int)Math.Round(priceInBaseCurrency - Math.Floor(priceInBaseCurrency), 2)*100).ToString();
            }
            else
            {
                var currency = order.OrderCurrency;
                model.OrderCurrency = currency;
                model.RenderCurrency = currency.CurrencyCode == sberbank.PaymentCurrency.Iso3 ? (Currency)currency : sberbank.PaymentCurrency;

                model.SumPrice = order.Sum.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            }

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion

        #region Bill

        public ActionResult Bill(string ordernumber, bool withoutStamp = false)
        {
            if (string.IsNullOrWhiteSpace(ordernumber))
                return Error404();

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Error404();

            var bill = order.PaymentMethod as Bill;
            
            if (bill == null)
                return Error404();

            if (!CustomerContext.CurrentCustomer.IsAdmin &&
                (CustomerContext.CurrentCustomer.CustomerRole != Role.Moderator ||
                !CustomerContext.CurrentCustomer.HasRoleAction(RoleAction.Orders)))//GlorySoft_028
            {
                if (order.OrderDate.AddDays(3) < DateTime.Now &&
                    (order.OrderCustomer == null || order.OrderCustomer.CustomerID != CustomerContext.CustomerId))
                    return Error404();
            }

            return Redirect(string.Format("~/content/bill/{0}.pdf", order.OrderID));//GlorySoft_028

            if (!order.CanAccessToPaymentReceipt(Request["ch"], CustomerContext.CurrentCustomer))
                return CustomerContext.CurrentCustomer.RegistredUser
                    ? Error404()
                    : RedirectToRoute("Login", new {redirectTo = bill.GetBillingLink(order)});

            var model = new BillViewModel()
            {
                PayeesBank = bill.BankName,
                TransactAccount = bill.TransAccount,
                Bik = bill.BIK,
                Inn = bill.INN,
                Kpp = bill.KPP,
                CompanyName = bill.CompanyName,
                CorrespondentAccount = bill.CorAccount,
                StampImageName = withoutStamp ? string.Empty : bill.StampImageName,
                Vendor =
                    string.Format("{0}, ИНН {1}, {2}{3}{4}",
                        bill.CompanyName,
                        bill.INN,
                        string.IsNullOrWhiteSpace(bill.KPP) ? string.Empty : "КПП " + bill.KPP + ", ",
                        bill.Address,
                        (string.IsNullOrEmpty(bill.Telephone) ? string.Empty : ", тел. " + bill.Telephone)),
                Director = (string.IsNullOrEmpty(bill.Director)) ? "______________________" : bill.Director,
                PosDirector = (string.IsNullOrEmpty(bill.PosDirector)) ? "______________________" : bill.PosDirector,
                Accountant = string.Empty,
                PosAccountant = string.Empty,
                OrderNumber = order.Number,
                OrderDate = order.OrderDate.ToLongDateString()
            };

            if (!string.IsNullOrEmpty(bill.Accountant) && !string.IsNullOrEmpty(bill.PosAccountant))
            {
                model.Accountant = string.IsNullOrEmpty(bill.Accountant)
                    ? "______________________"
                    : bill.Accountant;
                model.PosAccountant = string.IsNullOrEmpty(bill.PosAccountant)
                    ? "______________________"
                    : bill.PosAccountant;
            }

            var orderCustomer = order.OrderCustomer;
            string billInn = "", billCompanyName = "", kpp = "";

            if (!string.IsNullOrEmpty(Request["bill_inn"]) || !string.IsNullOrEmpty(Request["bill_CompanyName"]))
            {
                billCompanyName = !string.IsNullOrEmpty(Request["bill_CompanyName"]) ? Request["bill_CompanyName"] : null;
                billInn = !string.IsNullOrEmpty(Request["bill_inn"]) ? Request["bill_inn"] : null;
                kpp = !string.IsNullOrEmpty(Request["bill_kpp"]) ? Request["bill_kpp"] : null;
            }
            else if (order.PaymentDetails != null)
            {
                billCompanyName = order.PaymentDetails.CompanyName;
                billInn = order.PaymentDetails.INN;
                kpp = order.PaymentDetails.Kpp;
            }
            else if (bill.GetCustomerDataMethod == EGetCustomerDataMethod.FromAdditionalFields)
            {
                var customerCompanyNameField = CustomerFieldService.GetCustomerFieldsWithValue(orderCustomer.CustomerID, bill.CustomerCompanyNameField.TryParseInt());
                var customerInnField = CustomerFieldService.GetCustomerFieldsWithValue(orderCustomer.CustomerID, bill.CustomerINNField.TryParseInt());
                var customerKppField = CustomerFieldService.GetCustomerFieldsWithValue(orderCustomer.CustomerID, bill.CustomerKppField.TryParseInt());
                
                billCompanyName = customerCompanyNameField?.Value;
                billInn = customerInnField?.Value;
                kpp = customerKppField?.Value;
            }

            var legalAddress = string.Empty;

            if (orderCustomer.CustomerType == CustomerType.LegalEntity)
                legalAddress = CustomerFieldService.GetCustomerFieldWithValueByFieldAssignment(
                    orderCustomer.CustomerID,
                    CustomerFieldAssignment.LegalAddress
                )?.Value;

            model.Buyer =
                StringHelper.AggregateStrings(", ",
                    !string.IsNullOrEmpty(billCompanyName) ? billCompanyName : "",
                    !string.IsNullOrEmpty(billInn) ? "ИНН " + billInn : "",
                    !string.IsNullOrEmpty(kpp) ? "КПП " + kpp : "",
                    !string.IsNullOrWhiteSpace(legalAddress) ? legalAddress : orderCustomer.GetCustomerFullAddress(),
                    !string.IsNullOrEmpty(orderCustomer.Phone) ? "тел. " + orderCustomer.Phone : "");

            model.Consignee =
                StringHelper.AggregateStrings(", ",
                !string.IsNullOrEmpty(billCompanyName) ? billCompanyName : "",
                !string.IsNullOrEmpty(billInn) ? "ИНН " + billInn : "",
                !string.IsNullOrEmpty(kpp) ? "КПП " + kpp : "",
                orderCustomer.GetCustomerFullAddress(),
                !string.IsNullOrEmpty(orderCustomer.Phone) ? "тел. " + orderCustomer.Phone : "");

            var currency = order.OrderCurrency;
            model.OrderCurrency = currency;
            if (bill.PaymentCurrency != null)
                model.RenderCurrency = currency.CurrencyCode == bill.PaymentCurrency.Iso3
                    ? (Currency)currency
                    : bill.PaymentCurrency;
            else
                model.RenderCurrency = currency;

            var orderTotalDiscount = order.OrderItems.Sum(x => x.Amount * x.Price) + order.ShippingCost + order.PaymentCost - order.Sum;
            if (orderTotalDiscount != 0)
                model.DiscountCost = orderTotalDiscount.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            if (order.ShippingCost != 0)
                model.ShippingCost = order.ShippingCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            if (order.PaymentCost != 0)
                model.PaymentCost = order.PaymentCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            var productsCost = order.OrderItems.Sum(oi => oi.Price * oi.Amount) + order.ShippingCost +
                               order.PaymentCost + order.OrderCertificates.Sum(x => x.Sum);
            model.ProductsCost = productsCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.TotalCost = order.Sum.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.IntPartPrice = model.TotalCost.Trim(model.RenderCurrency.Symbol.ToArray()).Replace(" ", "");

            model.OrderItems = order.OrderItems;
            model.OrderCertificates = order.OrderCertificates;
            model.Taxes = order.Taxes;

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion

        #region Bill Ukraine

        public ActionResult BillUa(string ordernumber)
        {
            string GetPrice(float price, OrderCurrency currencyRender)
            {
                return (currencyRender.CurrencyValue == 1 ? price : (float)Math.Round(price * currencyRender.CurrencyValue, 4)).ToString("### ### ##0.00##", System.Globalization.CultureInfo.InvariantCulture).Trim();
            }
            
            if (string.IsNullOrWhiteSpace(ordernumber))
                return Error404();

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Error404();

            var billUa = order.PaymentMethod as BillUa;
            
            if (billUa == null)
                return Error404();

            if (!order.CanAccessToPaymentReceipt(Request["ch"], CustomerContext.CurrentCustomer))
                return CustomerContext.CurrentCustomer.RegistredUser
                    ? Error404()
                    : RedirectToRoute("Login", new {redirectTo = billUa.GetBillingLink(order)});

            var months = new List<string> { "січня", "лютого", "березня", "квітня", "травня", "червня", "липня", "серпня", "вересня", "жовтня", "листопада", "грудня" };

            var model = new BillUaViewModel()
            {
                CompanyName = billUa.CompanyName,
                CompanyCode = billUa.CompanyCode,
                Credit = billUa.Credit,
                BankCode = billUa.BankCode,
                BankName = billUa.BankName,
                CompanyEssencials = billUa.CompanyEssentials,

                OrderNumber = string.Format("Рахунок на оплату № {0} від {1} {2} {3} р.",
                    order.Number, order.OrderDate.Day.ToString("0#"),
                    months[order.OrderDate.Month - 1], order.OrderDate.Year),

                BuyerInfo = order.OrderCustomer.LastName + " " + order.OrderCustomer.FirstName
            };

            var currency = order.OrderCurrency;
            currency.CurrencyValue = currency.CurrencyValue / billUa.PaymentCurrency.Rate;
            model.OrderCurrency = currency;

            model.Total = GetPrice(order.Sum, currency);
            model.TotalCount = string.Format("Всього найменувань {0}, на суму {1} грн.", order.OrderItems.Count + order.OrderCertificates.Count + (order.ShippingCost != 0 ? 1 : 0) + (order.PaymentCost != 0 ? 1 : 0),
                model.Total);
            model.TotalPartPrice = model.Total.Replace(" ", "");

            var taxes = order.Taxes;
            if (taxes.Count > 0)
            {
                var taxSum = taxes.Any(x => x.Sum.HasValue) ? (float?)Math.Round(taxes.Sum(t => t.Sum).Value, 2) : null;

                model.TaxSum = taxSum.HasValue ? GetPrice(taxSum.Value, currency) : "-";
                //model.Tax = taxSum.HasValue ? GetPrice(taxSum.Value, currency) : "-";
                model.TaxSumPartPrice = model.TaxSum.Replace(" ", "");
            }

            var orderTotalDiscount = order.OrderItems.Sum(x => x.Amount * x.Price) + order.ShippingCost + order.PaymentCost - order.Sum;
            if (orderTotalDiscount != 0)
                model.Discount = GetPrice(orderTotalDiscount, currency);

            if (order.ShippingCost != 0)
                model.ShippingCost = GetPrice(order.ShippingCost, currency);

            if (order.PaymentCost != 0)
                model.PaymentCost = GetPrice(order.PaymentCost, currency);

            model.OrderItems = order.OrderItems;
            model.OrderCertificates = order.OrderCertificates;

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion

        #region Bill Belarus

        public ActionResult BillBy(string ordernumber, bool withoutStamp = false)
        {
            if (string.IsNullOrWhiteSpace(ordernumber))
                return Error404();

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Error404();

            var bill = order.PaymentMethod as BillBy;
            
            if (bill == null)
                return Error404();
            
            if (!order.CanAccessToPaymentReceipt(Request["ch"], CustomerContext.CurrentCustomer))
                return CustomerContext.CurrentCustomer.RegistredUser
                    ? Error404()
                    : RedirectToRoute("Login", new {redirectTo = bill.GetBillingLink(order)});

            var model = new BillByViewModel()
            {
                PayeesBank = bill.BankName,
                TransactAccount = bill.TransAccount,
                Bik = bill.BIK,
                Unp = bill.UNP,
                Okpo = bill.OKPO,
                CompanyName = bill.CompanyName,
                CorrespondentAccount = bill.CorAccount,
                StampImageName = withoutStamp? string.Empty : bill.StampImageName,
                Vendor =
                    string.Format("{0}, УНП {1}, {2}{3}{4}",
                        bill.CompanyName,
                        bill.UNP,
                        string.IsNullOrWhiteSpace(bill.OKPO) ? string.Empty : "ОКПО " + bill.OKPO + ", ",
                        bill.Address,
                        (string.IsNullOrEmpty(bill.Telephone) ? string.Empty : ", тел. " + bill.Telephone)),
                Director = (string.IsNullOrEmpty(bill.Director)) ? "______________________" : bill.Director,
                PosDirector = (string.IsNullOrEmpty(bill.PosDirector)) ? "______________________" : bill.PosDirector,
                Accountant = (string.IsNullOrEmpty(bill.Accountant)) ? "______________________" : bill.Accountant,
                PosAccountant = (string.IsNullOrEmpty(bill.PosAccountant)) ? "______________________" : bill.PosAccountant,
                OrderNumber = order.Number,
                OrderDate = order.OrderDate.ToLongDateString()
            };

            var userAddress = StringHelper.AggregateStrings(", ", order.OrderCustomer.Country,
                order.OrderCustomer.Region, order.OrderCustomer.City, order.OrderCustomer.GetCustomerShortAddress());

            var billInn = !string.IsNullOrEmpty(Request["bill_inn"]) ? Request["bill_inn"] : order.PaymentDetails != null ? order.PaymentDetails.INN : string.Empty;
            var billCompanyName = !string.IsNullOrEmpty(Request["bill_CompanyName"])
                ? Request["bill_CompanyName"]
                : order.PaymentDetails != null
                    ? order.PaymentDetails.CompanyName
                    : string.Empty;

            model.Buyer = order.PaymentDetails != null
                ? string.Format("{0}{1}{2}",
                    !string.IsNullOrEmpty(billCompanyName) ? billCompanyName + ", " : "",
                    !string.IsNullOrEmpty(billInn) ? "УНП " + billInn + ", " : "",
                    userAddress)
                : userAddress;

            var currency = order.OrderCurrency;
            //currency.CurrencyValue = bill.GetCurrencyRate(order.OrderCurrency);
            model.OrderCurrency = currency;
            model.RenderCurrency = bill.PaymentCurrency;

            var orderTotalDiscount = order.OrderItems.Sum(x => x.Amount * x.Price) + order.ShippingCost + order.PaymentCost - order.Sum;
            if (orderTotalDiscount != 0)
                model.DiscountCost = orderTotalDiscount.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            if (order.ShippingCost != 0)
                model.ShippingCost = order.ShippingCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            if (order.PaymentCost != 0)
                model.PaymentCost = order.PaymentCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            var productsCost = order.OrderItems.Sum(oi => oi.Price * oi.Amount) + order.ShippingCost +
                               order.PaymentCost + order.OrderCertificates.Sum(x => x.Sum);
            model.ProductsCost = productsCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.TotalCost = order.Sum.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.IntPartPrice = model.TotalCost.Trim(model.RenderCurrency.Symbol.ToArray()).Replace(" ", "");

            model.OrderItems = order.OrderItems;
            model.OrderCertificates = order.OrderCertificates;
            model.Taxes = order.Taxes;

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion

        #region Bill Kazakhstan

        public ActionResult BillKz(string ordernumber, bool withoutStamp = false)
        {
            if (string.IsNullOrWhiteSpace(ordernumber))
                return Error404();

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Error404();

            var bill = order.PaymentMethod as BillKz;
            
            if (bill == null)
                return Error404();

            if (!order.CanAccessToPaymentReceipt(Request["ch"], CustomerContext.CurrentCustomer))
                return CustomerContext.CurrentCustomer.RegistredUser
                    ? Error404()
                    : RedirectToRoute("Login", new {redirectTo = bill.GetBillingLink(order)});
            
            var model = new BillKzViewModel()
            {
                PayeesBank = bill.BankName,
                Iik = bill.IIK,
                Bik = bill.BIK,
                BinIin = bill.BINIIN,
                Kbe = bill.KBE,
                CompanyName = bill.CompanyName,
                Knp = bill.KNP,
                StampImageName = withoutStamp ? string.Empty : bill.StampImageName,
                Vendor =
                    string.Format("БИН/ИИН {1}, {0} ,{2}{3}",
                        bill.CompanyName,
                        bill.BINIIN,
                        bill.Address,
                        (string.IsNullOrEmpty(bill.Telephone) ? string.Empty : ", тел. " + bill.Telephone)),
                Contractor = (string.IsNullOrEmpty(bill.Contractor)) ? "______________________" : bill.Contractor,
                PosContractor = (string.IsNullOrEmpty(bill.Contractor)) ? "______________________" : bill.Contractor,
                OrderNumber = order.Number,
                OrderDate = order.OrderDate.ToLongDateString()
            };

            var userAddress = StringHelper.AggregateStrings(", ", order.OrderCustomer.Country,
                order.OrderCustomer.Region, order.OrderCustomer.City, order.OrderCustomer.GetCustomerShortAddress());

            var billInn = !string.IsNullOrEmpty(Request["bill_inn"]) ? Request["bill_inn"] : order.PaymentDetails != null ? order.PaymentDetails.INN : string.Empty;
            var billCompanyName = !string.IsNullOrEmpty(Request["bill_CompanyName"])
                ? Request["bill_CompanyName"]
                : order.PaymentDetails != null 
                    ? order.PaymentDetails.CompanyName
                    : string.Empty;

            var contract = !string.IsNullOrEmpty(Request["bill_Contract"]) ? Request["bill_Contract"] : order.PaymentDetails != null ? order.PaymentDetails.Contract : string.Empty;
            model.Contract = string.IsNullOrEmpty(contract) ? "Без договора" : contract;

            model.Buyer = order.PaymentDetails != null
                ? string.Format("{0}{1}{2}",
                    !string.IsNullOrEmpty(billInn) ? "БИН/ИИН " + billInn + ", " : "",
                    !string.IsNullOrEmpty(billCompanyName) ? billCompanyName + ", " : "",
                    userAddress)
                : userAddress;

            var currency = order.OrderCurrency;
            //currency.CurrencyValue = bill.GetCurrencyRate(order.OrderCurrency);
            model.OrderCurrency = currency;
            model.RenderCurrency = bill.PaymentCurrency;

            var orderTotalDiscount = order.OrderItems.Sum(x => x.Amount * x.Price) + order.ShippingCost + order.PaymentCost - order.Sum;
            if (orderTotalDiscount != 0)
                model.DiscountCost = orderTotalDiscount.RoundPrice(currency.CurrencyValue, model.RenderCurrency).ToString("### ### ##0.00##");

            if (order.ShippingCost != 0)
                model.ShippingCost = order.ShippingCost.RoundPrice(currency.CurrencyValue, model.RenderCurrency).ToString("### ### ##0.00##");

            if (order.PaymentCost != 0)
                model.PaymentCost = order.PaymentCost.RoundPrice(currency.CurrencyValue, model.RenderCurrency).ToString("### ### ##0.00##");

            var productsCost = order.OrderItems.Sum(oi => oi.Price * oi.Amount) + order.ShippingCost +
                               order.PaymentCost + order.OrderCertificates.Sum(x => x.Sum);
            model.ProductsCost = productsCost.RoundPrice(currency.CurrencyValue, model.RenderCurrency).ToString("### ### ##0.00##");
            model.TotalCost = order.Sum.RoundPrice(currency.CurrencyValue, model.RenderCurrency).ToString("### ### ##0.00##");
            model.TotalCostCurrency = order.Sum.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.IntPartPrice = model.TotalCost.Replace(" ", "");

            model.OrderItems = order.OrderItems;
            model.OrderCertificates = order.OrderCertificates;
            model.Taxes = order.Taxes;

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion

        #region Check 

        public ActionResult Check(string ordernumber)
        {
            if (string.IsNullOrWhiteSpace(ordernumber))
                return Error404();

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Error404();

            var check = order.PaymentMethod as Check;
            
            if (check == null)
                return Error404();

            if (!order.CanAccessToPaymentReceipt(Request["ch"], CustomerContext.CurrentCustomer))
                return CustomerContext.CurrentCustomer.RegistredUser
                    ? Error404()
                    : RedirectToRoute("Login", new {redirectTo = check.GetBillingLink(order)});

            var currency = order.OrderCurrency;

            var model = new CheckViewModel()
            {
                CompanyName = check.CompanyName,
                Address = check.Adress,
                Country = check.Country,
                State = check.State,
                City = check.City,

                CompanyPhone = check.Phone,
                InterPhone = check.IntPhone,
                CompanyFax = check.Fax,

                OrderDate = Localization.Culture.ConvertDate(order.OrderDate),
                OrderId = @"#" + order.Number,
                ShippingMethod = order.ShippingMethodName,

                Name = StringHelper.AggregateStrings(" ", order.OrderCustomer.LastName, order.OrderCustomer.FirstName, order.OrderCustomer.Patronymic),
                Phone = order.OrderCustomer.Phone,
                Email = order.OrderCustomer.Email,

                BillingAddress = order.OrderCustomer.GetCustomerShortAddress(),
                BillingCity = order.OrderCustomer.City,
                BillingState = order.OrderCustomer.Region,
                BillingCountry = order.OrderCustomer.Country,
                BillingZip = order.OrderCustomer.Zip,

                ShippingAddress = order.OrderCustomer.GetCustomerShortAddress(),
                ShippingCity = order.OrderCustomer.City,
                ShippingState = order.OrderCustomer.Region,
                ShippingCountry = order.OrderCustomer.Country,
                ShippingZip = order.OrderCustomer.Zip,

                OrderItems = order.OrderItems,
                OrderCurrency = currency,
                RenderCurrency = check.PaymentCurrency
            };

            model.SubTotal =
                (order.OrderItems.Sum(x => x.Price * x.Amount) + order.PaymentCost).RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.ShippingCost = order.ShippingCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            if (order.PaymentCost != 0)
                model.PaymentCost = order.PaymentCost.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.Discount = (order.OrderItems.Sum(x => x.Amount * x.Price) + order.ShippingCost + order.PaymentCost - order.Sum).RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            //(order.OrderDiscount*((order.Sum - order.ShippingCost - order.OrderDiscountValue) /(100 - order.OrderDiscount))).RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);
            model.Total = order.Sum.RoundAndFormatPrice(model.RenderCurrency, currency.CurrencyValue);

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion
    }
}