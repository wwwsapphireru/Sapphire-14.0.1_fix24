using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;
using AdvantShop.Models.Cart;
using AdvantShop.Orders;
using AdvantShop.Web.Infrastructure.Extensions;

namespace AdvantShop.Handlers.Cart
{
    public sealed class GetCartHandler
    {
        private readonly UrlHelper _urlHelper  = new UrlHelper(HttpContext.Current.Request.RequestContext);
        private readonly ShoppingCart _cart;

        public GetCartHandler() : this(ShoppingCartService.CurrentShoppingCart)
        {
        }
        
        public GetCartHandler(ShoppingCart cart)
        {
            _cart = cart;
        }
        
        public GetCartHandler(GetCartModel model)
        {
            if (model?.FromCheckout is true)
            {
                var data = OrderConfirmationService.Get(CustomerContext.CustomerId);
                if (data != null)
                    _cart =
                        ShoppingCartService.GetShoppingCart(
                            ShoppingCartType.ShoppingCart,
                            false,
                            data.SelectPayment?.Id,
                            data.SelectShipping?.MethodId);
            }
            
            if (_cart == null)
                _cart = ShoppingCartService.CurrentShoppingCart;
        }

        public CartModel Get()
        {
            var cartProducts =
                (from item in _cart
                 let product = item.Offer.Product
                 let tax = Taxes.TaxService.GetTax(product.TaxId ?? 0)//GlorySoft_006
                 select new CartItemModel()
                 {
                     OfferId = item.OfferId,
                     ProductId = product.ProductId,
                     Sku = item.Offer.ArtNo,
                     Name = product.Name,
                     Link = _urlHelper.AbsoluteRouteUrl("Product", new { url = product.UrlPath }),
                     Amount = item.Amount,
                     Price = item.Price.FormatPrice(),
                     PriceWithDiscount = item.PriceWithDiscount.FormatPrice(),
                     Discount = item.Discount,
                     DiscountText = item.Discount.GetText(),
                     Cost = PriceService.SimpleRoundPrice(item.PriceWithDiscount * item.Amount).FormatPrice(),
                     CostWithoutDiscount = PriceService.SimpleRoundPrice(item.Price * item.Amount).FormatPrice(),
                     PhotoPath = item.Offer.PhotoByColour.ImageSrcXSmall(),
                     PhotoSmallPath = item.Offer.PhotoByColour.ImageSrcSmall(),
                     PhotoMiddlePath = item.Offer.PhotoByColour.ImageSrcMiddle(),
                     PhotoAlt = product.Name,
                     ShoppingCartItemId = item.ShoppingCartItemId,
                     SelectedOptions =
                         CustomOptionsService.DeserializeFromXml(item.AttributesXml, product.Currency.Rate),
                     AttributesXml = item.AttributesXml,
                     ColorName = item.Offer.Color?.ColorName,
                     SizeName = item.Offer.SizeForCategory?.GetFullName(),
                     Avalible = ShoppingCartService.GetAvailableState(item, _cart),
                     AvailableAmount = item.Offer.Amount,
                     MinAmount = product.GetMinAmount(),
                     MaxAmount = item.Offer.GetMaxAvailableAmount(),
                     Multiplicity = product.Multiplicity > 0 ? product.Multiplicity : 1,
                     FrozenAmount = item.FrozenAmount,
                     IsGift = item.IsGift,
                     Unit = product.Unit?.DisplayName,
                     PriceRuleName = item.Offer.PriceRule?.Name,
                     BriefDescription = item.Offer.Product.GetProductBriefDescriptionFormatted(),
                     InWishlist = ShoppingCartService.CurrentWishlist.Any(x => x.OfferId == item.OfferId),

                     //GlorySoft_006
                     PriceValue = item.PriceWithDiscount,
                     Tax = tax != null ? new Taxes.OrderTax { TaxId = tax.TaxId, Name = tax.Name, Rate = tax.Rate, Sum = (float)Math.Round(item.PriceWithDiscount * item.Amount * tax.Rate / (100 + tax.Rate), 2) } : null
                 }).ToList();

            var totalPrice = _cart.Sum(x => x.Price * x.Amount);/*GlorySoft_006 _cart.TotalPrice;*/
            var totalDiscount = _cart.Sum(x => (x.Price - x.PriceWithDiscount) * x.Amount);/*GlorySoft_006 _cart.TotalDiscount;*/
            var priceWithDiscount = totalPrice - totalDiscount;
            var totalItems = _cart.TotalItems;
            var discountOnTotalPrice = _cart.DiscountPercentOnTotalPrice;
            var discountOnTotalPriceAmount = (discountOnTotalPrice * priceWithDiscount / 100).RoundPrice(null);//GlorySoft_006

            var count = string.Format("{0} {1}",
                totalItems == 0 ? "" : totalItems.ToString(CultureInfo.InvariantCulture),
                Strings.Numerals(totalItems,
                    LocalizationService.GetResource("Cart.Product0"),
                    LocalizationService.GetResource("Cart.Product1"),
                    LocalizationService.GetResource("Cart.Product2"),
                    LocalizationService.GetResource("Cart.Product5")));

            float bonusPlus = 0;

            if (totalPrice > 0)
            {
                bonusPlus = BonusSystem.GetAccrueBonuses(_cart, shippingCost: 0, paymentFeeOrDiscount: 0, usedBonuses: null);
            }

            var showConfirmButtons = true;
            foreach (var module in AttachedModules.GetModules<IShoppingCart>())
            {
                var moduleObject = (IShoppingCart)Activator.CreateInstance(module);
                showConfirmButtons &= moduleObject.ShowConfirmButtons;
                if (module.FullName.Contains("OrderConfirmationInShoppingCart"))
                {
                    showConfirmButtons = false;
                }
            }

            string isValidCart = ShoppingCartService.IsValidCart(_cart, totalItems, totalPrice);
            var isDefaultCustomerGroup = CustomerContext.CurrentCustomer.CustomerGroup.CustomerGroupId == CustomerGroupService.DefaultCustomerGroup;

            //GlorySoft_006
            var taxes = new List<Taxes.OrderTax>();
            foreach (var item in cartProducts.Where(x => x.Tax != null))
            {
                var tax = taxes.FirstOrDefault(x => x.TaxId == item.Tax.TaxId);
                if (tax == null)
                {
                    tax = new Taxes.OrderTax { TaxId = item.Tax.TaxId, Name = item.Tax.Name, Rate = item.Tax.Rate, Sum = 0 };
                    taxes.Add(tax);
                }
                tax.Sum += item.Tax.Sum;
            }

            var model = new CartModel
            {
                CartProducts = cartProducts,
                ColorHeader = SettingsCatalog.ColorsHeader,
                SizeHeader = SettingsCatalog.SizesHeader,
                Count = count,
                TotalItems = totalItems,
                BonusPlus = bonusPlus > 0 ? bonusPlus.FormatBonuses() : null,
                Valid = isValidCart,

                CouponInputVisible = _cart.HasItems && _cart.Coupon == null && _cart.Certificate == null && SettingsCheckout.DisplayPromoTextbox &&
                    (isDefaultCustomerGroup || SettingsCheckout.EnableGiftCertificateService), // не выводить поле, если покупатель не в группе по умолчанию и сертификаты запрещены
                IsDefaultCustomerGroup = isDefaultCustomerGroup,

                ShowConfirmButtons = showConfirmButtons,
                ShowBuyInOneClick = showConfirmButtons && SettingsCheckout.BuyInOneClick,
                BuyInOneClickText = SettingsCheckout.BuyInOneClickLinkText,
                EnablePhoneMask = SettingsMain.EnablePhoneMask,

                TotalProductPrice = totalPrice.FormatPrice(),
                TotalPrice = (priceWithDiscount - discountOnTotalPriceAmount) > 0 ? (priceWithDiscount - discountOnTotalPriceAmount).FormatPrice() : 0F.FormatPrice(),/*GlorySoft_006 priceWithDiscount > 0 ? priceWithDiscount.FormatPrice() : 0F.FormatPrice(),*/

                DiscountPrice =
                    totalDiscount/*GlorySoft_006 discountOnTotalPrice > 0 && totalPrice - _cart.TotalPriceIgnoreDiscount*/ > 0
                        ? PriceFormatService.FormatDiscountPercent(totalPrice, 0, totalDiscount, true)/*GlorySoft_006 PriceFormatService.FormatDiscountPercent(totalPrice - _cart.TotalPriceIgnoreDiscount, discountOnTotalPrice, 0, true)*/
                        : null,

                //GlorySoft_006
                DiscountOnTotalPrice = discountOnTotalPrice > 0 ? discountOnTotalPrice.ToString() : null,
                DiscountOnTotalPriceAmount = discountOnTotalPrice > 0
                    ? PriceFormatService.FormatDiscountPercent(priceWithDiscount, discountOnTotalPrice, 0, true)
                    : null,

                Certificate = _cart.Certificate?.Sum.FormatPrice(),
                CertificateCode = _cart.Certificate?.CertificateCode,
                MobileIsFullCheckout = SettingsMobile.IsFullCheckout && showConfirmButtons,
                IsShowUnits = SettingsCatalog.ShowUnitsInCatalog,
                IsWishListVisibility = SettingsDesign.WishListVisibility,

                //GlorySoft_006
                TotalWeight = string.Format("{0} {1}", _cart.Where(x => x.Offer != null).Sum(x => x.Offer.GetWeight() * x.Amount).ToString("F3"), LocalizationService.GetResource("Product.ProductInfo.Kg")),
                TaxesNames = taxes.Select(tax => tax.Name).AggregateString(','),
                TaxesPrice = taxes.Any(x => x.Sum.HasValue) ? taxes.Sum(x => x.Sum).Value.FormatPrice(SettingsCatalog.DefaultCurrency) : "-",
            };

            if (_cart.Coupon != null)
            {
                switch (_cart.Coupon.Type)
                {
                    case CouponType.Fixed:
                    case CouponType.Percent:
                        model.Coupon = totalDiscount != 0
                        ? new CartCoupon()
                        {
                            Code = _cart.Coupon.Code,
                            Price = (_cart.Coupon.Type == CouponType.Percent ? totalDiscount : _cart.Coupon.GetRate()).FormatPrice(),
                            Percent =
                                _cart.Coupon.Type == CouponType.Percent
                                    ? _cart.Coupon.Value.FormatPriceInvariant()
                                    : null
                        }
                        : new CartCoupon()
                        {
                            Code = _cart.Coupon.Code,
                            Price = 0f.FormatPrice(),
                            NotApplied = true,
                        };
                        break;
                    case CouponType.FixedOnGiftOffer:
                        if (_cart.Coupon.GiftOfferId.HasValue && _cart.Coupon.GiftOfferId != 0)
                            model.Coupon = new CartCoupon
                            {
                                Code = _cart.Coupon.Code,
                                Price = $"{ProductService.GetProductByOfferId(_cart.Coupon.GiftOfferId.Value)?.Name} ({_cart.Coupon.GetRate().FormatPrice()})"
                            };
                        break;
                }
            }

            return model;
        }
    }
}