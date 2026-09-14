using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using AdvantShop.Localization;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Orders;
using AdvantShop.Repository;
using AdvantShop.Taxes;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class PrintCartHandler
    {
        //private readonly PrintOrderModel _printOrder;

        public PrintCartHandler(/*PrintOrderModel printOrder*/)
        {
            //_printOrder = printOrder;
        }

        public PrintCartViewModel Execute()
        {
            var currency = SettingsCatalog.DefaultCurrency;
            var currentCustomer = CustomerContext.CurrentCustomer;
            var current = MyCheckout.Factory(currentCustomer.Id);
            var cart = current.Cart;

            var model = new PrintCartViewModel()
            {
                OrderItems = cart,
                Currency = currency,
                //ShowStatusInfo = SettingsCheckout.PrintOrder_ShowStatusInfo,
                ShowMap = SettingsCheckout.PrintOrder_ShowMap,// && _printOrder.ShowMap,
                MapType = SettingsCheckout.PrintOrder_MapType,
                MapAdress =
                    StringHelper.AggregateStrings(", ", current.Data.Contact.Country, current.Data.Contact.Region,
                        current.Data.Contact.District, current.Data.Contact.City, current.Data.Contact.Street + " " + current.Data.Contact.House),
                ShowContacts = cart.Certificate == null,
                Customer = currentCustomer,
                Contact = currentCustomer.Contacts.Count > 0 ? currentCustomer.Contacts[0] : new CustomerContact()
            };

            var productPrice = cart.Certificate != null
                                    ? cart.Certificate.Sum
                                    : cart.Sum(item => PriceService.SimpleRoundPrice(item.Amount * item.Price, currency));
            var productsIgnoreDiscountPrice = 0;// _order.OrderItems.Where(item => item.IgnoreOrderDiscount).Sum(item => item.Price * item.Amount);

            model.ProductsPrice = productPrice.FormatPrice(currency);

            if ((cart.TotalDiscount != 0 || cart.DiscountPercentOnTotalPrice != 0) && productPrice - productsIgnoreDiscountPrice > 0)
            {
                model.OrderDiscount = PriceFormatService.FormatDiscountPercent(productPrice - productsIgnoreDiscountPrice, cart.TotalDiscount,
                    cart.DiscountPercentOnTotalPrice, currency.Symbol, currency.IsCodeBefore, false);
            }

            //if (_order.BonusCost != 0)
            //{
            //    model.OrderBonus = _order.BonusCost.FormatPrice(currency);
            //}

            if (cart.Certificate != null)
            {
                model.Certificate = cart.Certificate.Sum.FormatPrice(currency);
            }

            if (cart.Coupon != null)
            {
                switch (cart.Coupon.Type)
                {
                    case CouponType.Fixed:
                        model.Coupon = String.Format("-{0} ({1})",
                                        cart.Coupon.Value.FormatPrice(currency),
                                        cart.Coupon.Code);
                        break;

                    case CouponType.Percent:
                        var productsWithCoupon = cart.Where(item => item.IsCouponApplied).Sum(item => item.Price * item.Amount);

                        model.Coupon = String.Format("-{0} ({1}%) ({2})",
                                        PriceService.SimpleRoundPrice(productsWithCoupon * cart.Coupon.Value / 100, currency).FormatPrice(currency),
                                        cart.Coupon.Value.FormatPriceInvariant(),
                                        cart.Coupon.Code);
                        break;
                }
            }

            //model.ShippingPrice = _order.ShippingCost.FormatPrice(currency);
            //model.ShippingMethodName = _order.ArchivedShippingName +
            //                           (_order.OrderPickPoint != null && !string.IsNullOrEmpty(_order.OrderPickPoint.PickPointAddress)
            //                               ? " (" + _order.OrderPickPoint.PickPointAddress + ")"
            //                               : "");

            //if (_order.DeliveryDate != null || !string.IsNullOrEmpty(_order.DeliveryTime))
            //{
            //    model.ShippingDeliveryTime =
            //        (_order.DeliveryDate != null ? Culture.ConvertShortDate(_order.DeliveryDate.Value) : "") + " " +
            //        _order.DeliveryTime;
            //}

            //model.PaymentPriceTitle =
            //    _order.PaymentCost == 0
            //        ? LocalizationService.GetResource("Checkout.Payment.Title")
            //        : (_order.PaymentCost > 0
            //            ? LocalizationService.GetResource("Checkout.PaymentCost")
            //            : LocalizationService.GetResource("Checkout.PaymentDiscount"));
            //model.PaymentPrice = _order.PaymentCost.FormatPrice(currency);
            //model.PaymentMethodName = _order.ArchivedPaymentName;

            model.Taxes = new List<OrderTax>();
            foreach (var taxCartItem in cart.Where(x => x.Offer != null).Where(x => x.Offer.Product.TaxId.HasValue))
            {
                var tax = TaxService.GetTax(taxCartItem.Offer.Product.TaxId.Value);
                var taxItem = model.Taxes.Find(x => x.TaxId == taxCartItem.Offer.Product.TaxId);
                if (taxItem == null)
                {
                    taxItem = new OrderTax()
                    {
                        TaxId = taxCartItem.Offer.Product.TaxId.Value,
                        Name = tax.Name,
                        ShowInPrice = tax.ShowInPrice,
                        Sum = 0
                    };
                    model.Taxes.Add(taxItem);
                }
                taxItem.Sum += (float)Math.Round(taxCartItem.PriceWithDiscount * tax.Rate / (100 + tax.Rate), 2);
            }
            //model.Taxes = cart.Where(x => x.Offer != null).Where(x => x.Offer.Product.TaxId.HasValue)
            //    //.Select(x => TaxService.GetTax(x.Off))
            //    .Select(x => new OrderTax 
            //    { 
            //        TaxId = x.Offer.Product.TaxId.Value,
            //        Name = TaxService.GetTax(x.Offer.Product.TaxId.Value).Name,
            //        ShowInPrice = TaxService.GetTax(x.Offer.Product.TaxId.Value).ShowInPrice,
            //        Sum = (float)Math.Round(x.PriceWithDiscount * TaxService.GetTax(x.Offer.Product.TaxId.Value).Rate / (100 + TaxService.GetTax(x.Offer.Product.TaxId.Value).Rate), 2) 
            //    }).ToList();
            
            model.TotalPrice = cart.TotalPrice.FormatPrice(currency);

            //if (!string.IsNullOrEmpty(_printOrder.Sorting) && !string.IsNullOrEmpty(_printOrder.SortingType))
            //{
            //    var sorting = _printOrder.Sorting.ToLower();
            //    var sortingType = _printOrder.SortingType.ToLower();

            //    switch (sorting)
            //    {
            //        case "name":
            //            if (sortingType == "asc")
            //                model.OrderItems = model.OrderItems.OrderBy(x => x.Name).ToList();
            //            else if (sortingType == "desc")
            //                model.OrderItems = model.OrderItems.OrderByDescending(x => x.Name).ToList();
            //            break;

            //        case "pricestring":
            //            if (sortingType == "asc")
            //                model.OrderItems = model.OrderItems.OrderBy(x => x.Price).ToList();
            //            else if (sortingType == "desc")
            //                model.OrderItems = model.OrderItems.OrderByDescending(x => x.Price).ToList();
            //            break;

            //        case "cost":
            //            if (sortingType == "asc")
            //                model.OrderItems = model.OrderItems.OrderBy(x => x.Amount * x.Price).ToList();
            //            else if (sortingType == "desc")
            //                model.OrderItems = model.OrderItems.OrderByDescending(x => x.Amount * x.Price).ToList();
            //            break;
            //        default:
            //            break;
            //    }
            //}

            //model.TotalDimensions = MeasureHelper.GetDimensions(_order);
            model.TotalWeight = cart.TotalShippingWeight;//MeasureHelper.GetTotalWeight(_order, _order.OrderItems);

            return model;
        }
    }
}