using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Orders;
using AdvantShop.Repository.Currencies;
using AdvantShop.Taxes;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class CheckoutCartHandler
    {
        #region Constructor

        private readonly UrlHelper _urlHelper;

        public CheckoutCartHandler()
        {
            _urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
        }

        #endregion

        public CheckoutCartModel Get(CheckoutData checkoutData, ShoppingCart cart, float shippingPrice, float paymentCost, Currency currency)
        {
            var productsPrice = cart.Sum(x => x.Price * x.Amount);//cart.TotalPrice;
            var discountOnTotalPrice = cart.DiscountPercentOnTotalPrice;
            var totalDiscount = cart.Sum(x => (x.Price - x.PriceWithDiscount) * x.Amount);//cart.TotalDiscount;

            var priceWithDiscount = productsPrice - totalDiscount;
            var discountOnTotalPriceAmount = (discountOnTotalPrice * priceWithDiscount / 100).RoundPrice(currency);

            var bonusPrice = 0f;
            var bonusPlus = 0f;
            var couponPrice = totalDiscount;

            if (BonusSystem.IsActive)
            {
                var bonusCost = BonusSystemService.GetBonusCost(cart, shippingPrice, checkoutData.Bonus.AppliedBonuses,
                                                            (checkoutData.User.WantRegist || checkoutData.User.WantBonusCard));

                bonusPrice = bonusCost.BonusPrice;
                bonusPlus = bonusCost.BonusPlus + (checkoutData.User.WantRegist ? BonusSystem.BonusesForNewCard : 0);

                totalDiscount += bonusPrice;
            }

            TaxElement shippingTax = null;
            var shippingTaxType = checkoutData.SelectShipping != null 
                ? (checkoutData.SelectShipping.TaxId.HasValue && (shippingTax = TaxService.GetTax(checkoutData.SelectShipping.TaxId.Value)) != null ? shippingTax.TaxType : TaxType.None) 
                : TaxType.VatWithout;

            var taxesItems = TaxService.CalculateTaxes(cart, productsPrice - totalDiscount, shippingPrice, shippingTaxType);
            var taxesTotal = taxesItems.Where(tax => !tax.Key.ShowInPrice).Sum(item => item.Value);

            var totalTemp = (productsPrice + shippingPrice + taxesTotal - totalDiscount + paymentCost - discountOnTotalPriceAmount).RoundPrice(CurrencyService.CurrentCurrency.Rate);
            totalTemp = totalTemp > 0 ? totalTemp : 0;
            var totalPrice = totalTemp.FormatPrice();

            var model = new CheckoutCartModel
            {
                Items = cart.Select(item => new CheckoutCartItem()
                {
                    Name = item.Offer.Product.Name,
                    Amount = item.Amount,
                    Price = item.PriceWithDiscount.FormatPrice(),
                    Link = _urlHelper.RouteUrl("Product", new { url = item.Offer.Product.UrlPath }),
                    Cost = (item.PriceWithDiscount * item.Amount).FormatPrice(),
                    SelectedOptions = CustomOptionsService.DeserializeFromXml(item.AttributesXml, item.Offer.Product.Currency.Rate),
                    ColorName = item.Offer.Color != null ? item.Offer.Color.ColorName : null,
                    SizeName = item.Offer.Size != null ? item.Offer.Size.SizeName : null,
                }).ToList(),

                ColorHeader = SettingsCatalog.ColorsHeader,
                SizeHeader = SettingsCatalog.SizesHeader,

                Cost = productsPrice.FormatPrice(),
                Result = totalPrice,
                BuyOneClickEnabled = SettingsCheckout.BuyInOneClick && !SettingsCheckout.BuyInOneClickDisableInCheckout,
                ShowInCart = true,
                Delivery =
                    shippingPrice > 0
                        ? shippingPrice.FormatPrice()
                        : checkoutData.SelectShipping != null && !string.IsNullOrEmpty(checkoutData.SelectShipping.ZeroPriceMessage) ? checkoutData.SelectShipping.ZeroPriceMessage : null
            };

            if (paymentCost != 0)
                model.Payment = new CheckoutCartParam()
                {
                    Key =
                        paymentCost > 0
                            ? LocalizationService.GetResource("Checkout.PaymentCost")
                            : LocalizationService.GetResource("Checkout.PaymentDiscount"),
                    Value = paymentCost.FormatPrice()
                };

            if (totalDiscount/*discountOnTotalPrice*/ > 0)
                model.Discount = new CheckoutCartParam()
                {
                    //Key = discountOnTotalPrice.ToString(),
                    //Value = ((cart.TotalPrice - cart.TotalPriceIgnoreDiscount) * discountOnTotalPrice / 100).RoundPrice(CurrencyService.CurrentCurrency.Rate).FormatPrice()
                    Value = totalDiscount.FormatPrice()
                };

            if (discountOnTotalPrice > 0)
                model.DiscountOnTotalPrice = new CheckoutCartParam()
                {
                    Key = discountOnTotalPrice.ToString(),
                    Value = discountOnTotalPriceAmount.RoundPrice(CurrencyService.CurrentCurrency.Rate).FormatPrice()
                };

            if (cart.Certificate != null)
                model.Certificate = cart.Certificate.Sum.FormatPrice();

            if (cart.Coupon != null)
            {
                model.Coupon = couponPrice != 0
                    ? new CartCoupon()
                    {
                        Code = cart.Coupon.Code,
                        Price = couponPrice.FormatPrice(),
                        Percent =
                            cart.Coupon.Type == CouponType.Percent
                                ? cart.Coupon.Value.FormatPriceInvariant()
                                : null
                    }
                    : new CartCoupon()
                    {
                        Code = cart.Coupon.Code,
                        Price = 0f.FormatPrice(),
                        NotApplied = true,
                    };
            }

            model.Taxes =
                taxesItems.Select(
                    tax =>
                        new CheckoutCartParam()
                        {
                            Key =
                                string.Format("{0} {1}",
                                    tax.Key.ShowInPrice ? LocalizationService.GetResource("Core.Tax.IncludeTax") : "",
                                    tax.Key.Name),
                            Value = tax.Value.FormatPrice()
                        }).ToList();

            string bonus0 = LocalizationService.GetResource("Bonuses.Bonuses0");
            string bonus1 = LocalizationService.GetResource("Bonuses.Bonuses1");
            string bonus2 = LocalizationService.GetResource("Bonuses.Bonuses2");
            string bonus5 = LocalizationService.GetResource("Bonuses.Bonuses5");

            if (bonusPrice != 0)
            {
                bonusPrice = bonusPrice.FormatPriceInvariant().TryParseFloat();
                model.Bonuses = $"{bonusPrice} {Strings.Numerals(bonusPrice, bonus0, bonus1, bonus2, bonus5)}";
            }
                
            if (bonusPlus != 0)
            {
                bonusPlus = bonusPlus.FormatPriceInvariant().TryParseFloat();
                model.BonusPlus = $"{bonusPlus} {Strings.Numerals(bonusPlus, bonus0, bonus1, bonus2, bonus5)}";
            }

            model.TotalWeight = string.Format("{0} {1}", cart.Where(x => x.Offer != null).Sum(x => x.Offer.GetWeight() * x.Amount).ToString("F3"), LocalizationService.GetResource("Product.ProductInfo.Kg"));

            return model;
        }
    }
}