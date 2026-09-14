using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;
using AdvantShop.Payment;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Repository.Currencies;
using AdvantShop.Orders;
using System.Web.WebPages;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class GetProductHandler
    {
        private readonly Product _product;
        private readonly int? _color;
        private readonly int? _size;
        private readonly string _view;
        private readonly Discount _customDiscount;
        private readonly bool _hidePriceDiscount;

        public GetProductHandler(Product product, int? color, int? size, string view)
        {
            _product = product;
            _color = color;
            _size = size;
            _view = view;
        }

        public GetProductHandler(Product product, int? color, int? size, string view, Discount customDiscount) : this(product, color, size, view)
        {
            _customDiscount = customDiscount;
        }

        public GetProductHandler(Product product, int? color, int? size, string view, Discount customDiscount, bool hidePriceDiscount) : this(product, color, size, view, customDiscount)
        {
            _hidePriceDiscount = hidePriceDiscount;
        }

        public ProductDetailsViewModel Get()
        {
            var offer = OfferService.GetMainOffer(_product.Offers, _product.AllowPreOrder, _color, _size);
            var currentWarehouseId = WarehouseContext.CurrentWarehouseId;
            if (offer != null
                && currentWarehouseId.HasValue)
                offer.SetAmountByWarehouse(currentWarehouseId.Value);

            var amountMinToBuy = _product.GetMinAmount();
            var customer = CustomerContext.CurrentCustomer;

            var model = new ProductDetailsViewModel
            {
                IsAdmin = customer.IsAdmin,
                Product = _product,
                Offer = offer,
                ColorId = _color,
                SizeId = _size,
                MinAmount = amountMinToBuy
            };

            model.AllowBuyOutOfStockProducts = _product.AllowBuyOutOfStockProducts();
            
            var amountByMultiplicity = offer.GetAmountByMultiplicity(_product.Multiplicity);
                    
            var amount = amountByMultiplicity > 0 ? offer.Amount : 0;
            
            var isAvailable = offer != null && amountByMultiplicity > 0;

            model.IsAvailable = isAvailable || model.AllowBuyOutOfStockProducts;
            model.ShowAvailableLable = isAvailable ? SettingsCatalog.ShowAvaliableLableInProduct : SettingsCatalog.ShowNotAvaliableLableInProduct;
            model.ShowAvaliableLabelInProduct = SettingsCatalog.ShowAvaliableLableInProduct;
            model.ShowNotAvaliableLabelInProduct = SettingsCatalog.ShowNotAvaliableLableInProduct;

            
            model.Availble = string.Format("{0}{2}{1}",
                isAvailable || model.AllowBuyOutOfStockProducts
                    ? LocalizationService.GetResource("Product.Available") 
                    : LocalizationService.GetResource("Product.NotAvailable"),
                isAvailable && SettingsCatalog.ShowStockAvailability
                    ? string.Format(
                        " (<div class=\"details-avalable-text inplace-offset inplace-rich-simple inplace-obj\" {1}>{0}</div><div class=\"details-avalable-unit\">{2}</div>)",
                        StockLabelService.GetLabel(amountByMultiplicity),
                        InplaceExtensions.InplaceOfferAmount(offer.OfferId),
                        (_product.Unit?.DisplayName).IsNotEmpty() ? " " + _product.Unit?.DisplayName : "")
                    : string.Empty,
                isAvailable && SettingsCatalog.ShowAvailableInWarehouseInProduct && currentWarehouseId is null
                    ? string.Format(
                        LocalizationService.GetResource("Product.AvailableInWarehouse"),
                        WarehouseStocksService.GetOfferStocks(offer.OfferId).Count(x => x.Quantity > 0f))
                    : string.Empty);
            
            if (offer != null)
            {
                model.ShowAddButton = true;
                model.ShowPreOrderButton = _product.AllowPreOrder;
                model.ShowBuyOneClick = SettingsCheckout.BuyInOneClick;
                model.ShowMarketplaceButton = SettingsDesign.ShowMarketplaceButton && ModulesExecuter.ShowMarketplaceProductButton(_product.ProductId);

                var optionsHandler = new GetProductCustomOptionsHandler(_product.ProductId, string.Empty);
                model.HasCustomOptions = optionsHandler.HasOptions;
                var customOptionsXml = optionsHandler.GetXml();

                var (oldPrice, finalPrice, finalDiscount, preparedPrice) =
                    offer.GetOfferPricesWithPriceRule(amountMinToBuy, customOptionsXml, CustomerContext.CurrentCustomer, _customDiscount);
                 
                model.FinalDiscount = finalDiscount;
                model.FinalPrice = finalPrice;
                model.PreparedPrice =
                    _hidePriceDiscount
                        ? PriceFormatService.FormatPrice(model.FinalPrice, true, true)
                        : preparedPrice;
                
                model.BonusPrice = GetBonusCardPrice(_product, model.FinalPrice);

                if (_product.AllowPreOrder && amount > 0/*GlorySoft_063 (model.FinalPrice <= 0 || amount <= 0)*/)
                {
                    model.IsAvailable = true;
                    model.Availble = LocalizationService.GetResource("Product.AvailablePreorder");
                }

                var currencyIso3 = CurrencyService.CurrentCurrency.Iso3;
                model.MicrodataOffers = new List<MicrodataOffer>();
                float? highPrice = null;
                float? lowPrice = null;

                foreach (var itemOffer in _product.Offers.OrderByDescending(x => x.OfferId == offer.OfferId))
                {
                    var customOptionsPrice =
                        !string.IsNullOrEmpty(customOptionsXml)
                            ? CustomOptionsService.GetCustomOptionPrice(itemOffer.RoundedPrice, customOptionsXml, offer.Product.Currency.Rate)
                            : 0;
                    
                    var itemOfferPrice = PriceService.GetFinalPrice(itemOffer.RoundedPrice + customOptionsPrice, model.FinalDiscount);

                    if(highPrice == null || itemOfferPrice > highPrice)
                    {
                        highPrice = itemOfferPrice;
                    }
                    if (lowPrice == null || itemOfferPrice < lowPrice)
                    {
                        lowPrice = itemOfferPrice;
                    }

                    model.MicrodataOffers.Add(new MicrodataOffer()
                    {
                        Name = itemOffer.ArtNo,
                        Price = itemOfferPrice.ToInvariantString(),
                        Available = (itemOfferPrice > 0 || (itemOfferPrice == 0 && model.FinalDiscount.HasValue)) && itemOffer.Amount > 0,
                        ColorId = itemOffer.ColorID,
                        SizeId = itemOffer.SizeID,
                        Currency = currencyIso3
                    });
                }

                model.MicrodataAggregateOffer = new MicrodataAggregateOffer
                {
                    HighPrice = highPrice.ToInvariantString(),
                    LowPrice = lowPrice.ToInvariantString(),
                    Currency = currencyIso3
                };

                if (SettingsDesign.ShowShippingsMethodsInDetails != SettingsDesign.eShowShippingsInDetails.Never)
                {
                    model.RenderShippings = true;
                    model.ShowShippingsMethods =
                        Helpers.BrowsersHelper.IsBot()
                            ? SettingsDesign.eShowShippingsInDetails.ByClick
                            : SettingsDesign.ShowShippingsMethodsInDetails;
                }

                model.AllowAddReviews = true;

                if (SettingsDesign.WhoAllowReviews != SettingsDesign.eWhoAllowReviews.All && !customer.IsAdmin)
                    model.AllowAddReviews = SettingsDesign.WhoAllowReviews == SettingsDesign.eWhoAllowReviews.BoughtUser 
                        ? OrderService.GetProductIdsByCustomer(customer.Id).Contains(_product.ProductId) 
                        : SettingsDesign.WhoAllowReviews == SettingsDesign.eWhoAllowReviews.Registered && customer.RegistredUser;


                foreach (var creditPayment in PaymentService.GetCreditPaymentMethods())
                {
                    if (!creditPayment.ShowCreditButtonInProductCard)
                        continue;
                    
                    var paymentMethod = creditPayment as PaymentMethod;
                    var finalPriceInPaymentCurrency =
                        model.FinalPrice
                            .ConvertCurrency(CurrencyService.CurrentCurrency,
                                paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency);
                    
                    if (finalPriceInPaymentCurrency < creditPayment.MinimumPrice || 
                        finalPriceInPaymentCurrency > creditPayment.MaximumPrice)
                        continue;

                    float? firstPayment = null;
                    (float AmountPyament, int NumberOfPayments) amountAndNumberOfPayments;
                    if (creditPayment.TypePresentationOfCreditInformation
                        == EnTypePresentationOfCreditInformation.FirstPayment)
                    {
                        firstPayment = creditPayment.GetFirstPayment(finalPriceInPaymentCurrency);
                        if (firstPayment is null) 
                            continue;
                        
                        var firstPaymentInCurrentCurrency =
                            firstPayment.Value
                                        .ConvertCurrency(
                                             paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                                             CurrencyService.CurrentCurrency)
                                        .RoundPrice();

                        model.FirstPaymentPrice = firstPaymentInCurrentCurrency > 0
                            ? firstPaymentInCurrentCurrency.FormatPrice(true, false) + "*"
                            : LocalizationService.GetResource("Product.WithoutFirstPayment");
                    }

                    if (creditPayment.TypePresentationOfCreditInformation
                        == EnTypePresentationOfCreditInformation.AmountAndNumberOfPayments)
                    {
                        amountAndNumberOfPayments = creditPayment.GetAmountAndNumberOfPayments(finalPriceInPaymentCurrency);
                        if (amountAndNumberOfPayments.IsDefault())
                            continue;
                        
                        var amountPaymentInCurrentCurrency =
                            amountAndNumberOfPayments.AmountPyament
                                                     .ConvertCurrency(
                                                          paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                                                          CurrencyService.CurrentCurrency)
                                                     .RoundPrice();

                        model.FirstPaymentPrice = 
                            string.Format("{0} x {1}*", 
                                amountPaymentInCurrentCurrency.FormatPrice(true, false), 
                                amountAndNumberOfPayments.NumberOfPayments);
                    }
                        
                    model.ShowCreditButton = true;
                    model.CreditButtonText = creditPayment.CreditButtonTextInProductCard ?? LocalizationService.GetResource("Product.ProductInfo.BuyOnCredit");
                    model.FirstPaymentId = paymentMethod.PaymentMethodId;
                    model.FirstPaymentMinPrice =
                        creditPayment.MinimumPrice.ConvertCurrency(
                            paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                            CurrencyService.CurrentCurrency);
                    model.FirstPaymentMaxPrice = creditPayment.MaximumPrice.HasValue
                        ? creditPayment.MaximumPrice.Value.ConvertCurrency(
                            paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                            CurrencyService.CurrentCurrency)
                        : (float?) null;

                    break;
                }
            }

            model.ProductProperties = PropertyService.GetPropertyValuesByProductId(_product.ProductId);
            model.BriefProperties =
                model.ProductProperties
                    .Where(prop => prop.Property.UseInBrief)
                    .GroupBy(x => new {x.PropertyId})
                    .Select(x => new PropertyValue
                    {
                        PropertyId = x.Key.PropertyId,
                        Property = x.First(y => y.PropertyId == x.Key.PropertyId).Property,
                        PropertyValueId = x.First(y => y.PropertyId == x.Key.PropertyId).PropertyValueId,
                        SortOrder = x.First(y => y.PropertyId == x.Key.PropertyId).SortOrder,
                        Value = String.Join(", ", x.Where(y => y.PropertyId == x.Key.PropertyId).Select(v => v.Value))
                    })
                    .ToList();

            model.Gifts = ProductGiftService.GetGiftsByProductIdWithPrice(_product.ProductId);

            model.MinimumOrderPrice = CustomerGroupService.GetMinimumOrderPrice();

			model.RatingReadOnly = true;

            var reviewsCountString = string.Empty;
            var modules = AttachedModules.GetModules<IModuleReviews>();
            if (modules.Any())
            {
                foreach (var module in modules)
                {
                    var instance = (IModuleReviews)Activator.CreateInstance(module);
                    reviewsCountString += instance.GetReviewsCount(HttpContext.Current.Request.Url.AbsoluteUri);
                }
            }
            else
            {
                model.UseStandartReviews = true;
                model.ReviewsCountInt = ReviewService.GetReviewsCount(_product.ProductId, EntityType.Product, SettingsCatalog.ModerateReviews, true);
                reviewsCountString = string.Format("{0} {1}", model.ReviewsCountInt,
                    Strings.Numerals(model.ReviewsCountInt,
                        LocalizationService.GetResource("Product.Reviews0"),
                        LocalizationService.GetResource("Product.Reviews1"),
                        LocalizationService.GetResource("Product.Reviews2"),
                        LocalizationService.GetResource("Product.Reviews5")));
                
                if (model.ReviewsCountInt > 0)
                    model.AllReviewsCountInt = ReviewService.GetReviewsCount(_product.ProductId, EntityType.Product, SettingsCatalog.ModerateReviews, false);
            }
            
            model.ReviewsCount = reviewsCountString;
            model.AllowReviews = SettingsCatalog.AllowReviews;
            model.ShowBriefDescription = false;

            model.CustomViewPath = _view;

            model.PreOrderButtonText = SettingsCatalog.PreOrderButtonText;

            model.HidePrice = SettingsCatalog.HidePrice;
            model.TextInsteadOfPrice = SettingsCatalog.TextInsteadOfPrice;

            model.IsAvailableForPurchase = 
                offer.IsAvailableForPurchase(amount, amountMinToBuy, model.FinalPrice, model.FinalDiscount, model.AllowBuyOutOfStockProducts);

            model.IsAvailableForPurchaseOnCredit = 
                offer.IsAvailableForPurchaseOnCredit(model.IsAvailableForPurchase, model.FinalPrice, 
                                                     model.FirstPaymentMinPrice, model.FirstPaymentMaxPrice);
            
            model.IsAvailableForPurchaseOnBuyOneClick =
                offer.IsAvailableForPurchaseOnBuyOneClick(model.IsAvailableForPurchase, model.FinalPrice, model.MinimumOrderPrice);

            var productTabsModules = AttachedModules.GetModules<IProductTabs>();
            if (productTabsModules.Any())
            {
                var productTabsLinks = new List<ProductTabsLink>();
                foreach (var module in productTabsModules)
                {
                    var instance = (IProductTabs)Activator.CreateInstance(module);
                    var productDetailsTabList = instance.GetProductDetailsTabsLinksCollection(_product.ProductId);

                    if (productDetailsTabList == null)
                        continue;

                    foreach (var productDetailsTab in productDetailsTabList)
                    {
                        if (productDetailsTab.TabId.IsEmpty() || productDetailsTab.TitleOfLink.IsEmpty())
                            continue;

                        productTabsLinks.Add(new ProductTabsLink
                        {
                            TabId = productDetailsTab.TabId,
                            Title = productDetailsTab.TitleOfLink
                        });
                    }
                }

                model.ProductTabsLinks = productTabsLinks;
                model.AllowProductTabs = productTabsLinks.Count > 0;
            }
            else 
                model.AllowProductTabs = false;

            return model;
        }

        private string GetBonusCardPrice(Product product, float productPrice)
        {
            if (!BonusSystem.IsActive || productPrice <= 0 || !product.AccrueBonuses)
                return null;

            var bonusCard = BonusSystemService.GetCard(CustomerContext.CurrentCustomer.Id);
            if (bonusCard != null && bonusCard.Blocked)
                return null;

            var bonusPercent = bonusCard != null
                ? bonusCard.Grade.BonusPercent
                : BonusSystem.BonusFirstPercent;

            return BonusSystemService.GetBonusPlus(productPrice, productPrice, bonusPercent).FormatBonuses();
        }
    }
}