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
using AdvantShop.Core.Services.Bonuses.Internal;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;
using AdvantShop.Extensions;
using AdvantShop.Payment;
using AdvantShop.ViewModel.ProductDetails;
using AdvantShop.Repository.Currencies;
using AdvantShop.Orders;
using System.Web.WebPages;

namespace AdvantShop.Handlers.ProductDetails
{
    public class GetProductHandler
    {
        private readonly Product _product;
        private readonly int? _color;
        private readonly int? _size;
        private readonly string _view;
        private readonly int? _offerId;
        private readonly Discount _customDiscount;
        private readonly bool _hidePriceDiscount;

        public GetProductHandler(Product product, int? color, int? size, string view, int? offerId = null)
        {
            _product = product;
            _color = color;
            _size = size;
            _view = view;
            _offerId = offerId;
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
            var warehouseIds = WarehouseContext.GetAvailableWarehouseIds();
            var warehouseId = warehouseIds?.FirstOrDefault();

            if (warehouseIds != null)
                _product.Offers.SetAmountByStocksAndWarehouses(warehouseIds);
            
            var offer = _offerId.HasValue ? OfferService.GetOffer(_offerId.Value): OfferService.GetMainOffer(_product.Offers, _product.AllowPreOrder, _color, _size, warehouseId);
            
            var amountMinToBuy = _product.GetMinAmount();
            var customer = CustomerContext.CurrentCustomer;

            var model = new ProductDetailsViewModel
            {
                IsAdmin = customer.IsAdmin,
                Product = _product,
                Offer = offer,
                ColorId = _color ?? (_offerId.HasValue ? offer.ColorID : null),
                SizeId = _size ?? (_offerId.HasValue ? offer.SizeID : null),
                MinAmount = amountMinToBuy,
                AllowBuyOutOfStockProducts = _product.AllowBuyOutOfStockProducts()
            };

            var amountByMultiplicity = offer.GetAmountByMultiplicity(_product.Multiplicity);
                    
            var amount = amountByMultiplicity > 0 ? offer.Amount : 0;
            
            var isAvailable = offer != null && amountByMultiplicity > 0;

            model.IsAvailable = isAvailable || model.AllowBuyOutOfStockProducts;
            model.ShowAvailableLable = isAvailable ? SettingsCatalog.ShowAvaliableLableInProduct : SettingsCatalog.ShowNotAvaliableLableInProduct;
            model.ShowAvaliableLabelInProduct = SettingsCatalog.ShowAvaliableLableInProduct;
            model.ShowNotAvaliableLabelInProduct = SettingsCatalog.ShowNotAvaliableLableInProduct;


            var showCountWarehousesWithAvailable = 
                isAvailable 
                && SettingsCatalog.ShowAvailableInWarehouseInProduct 
                && (warehouseIds is null || warehouseIds.Count > 1);
            var warehouseAmount =
                showCountWarehousesWithAvailable
                    ? WarehouseStocksService.GetOfferStocks(offer.OfferId)
                                            .Where(x => warehouseIds is null || warehouseIds.Contains(x.WarehouseId))
                                            .Count(x => x.Quantity > 0f)
                    : 0;

            var availableInWarehouseText = warehouseAmount == 1 
                ? LocalizationService.GetResource("Product.OneAvailableInWarehouse") 
                : LocalizationService.GetResource("Product.AvailableInWarehouse");

            model.Availble = string.Format("{0}{2}{1}",
                isAvailable || model.AllowBuyOutOfStockProducts
                    ? LocalizationService.GetResource("Product.Available") 
                    : LocalizationService.GetResource("Product.NotAvailable"),
                isAvailable && SettingsCatalog.ShowStockAvailability
                    ? string.Format(
                        " (<div class=\"details-avalable-text inplace-offset inplace-rich-simple inplace-obj\" {1}>{0}</div><div class=\"details-avalable-unit\">{2}</div>)",
                        StockLabelService.GetLabel(amountByMultiplicity),
                        Core.Common.Extensions.InplaceExtensions.InplaceOfferAmount(offer.OfferId),
                        (_product.Unit?.DisplayName).IsNotEmpty() ? "&nbsp" + _product.Unit?.DisplayName : "")
                    : string.Empty,
                showCountWarehousesWithAvailable
                    ? string.Format(availableInWarehouseText, warehouseAmount)
                    : string.Empty);
            
            if (offer != null)
            {
                model.ShowAddButton = true;
                model.ShowPreOrderButton = _product.AllowPreOrder;
                model.ShowBuyOneClick = SettingsCheckout.BuyInOneClick;
                model.ShowMarketplaceButton = SettingsDesign.ShowMarketplaceButton && ModulesExecuter.ShowMarketplaceProductButton(_product.ProductId);

                var optionsHandler = new GetProductCustomOptionsHandler(_product.ProductId, null);
                model.HasCustomOptions = optionsHandler.HasOptions;
                var customOptionsXml = optionsHandler.GetXml();

                var (oldPrice, finalPrice, finalDiscount, preparedPrice) =
                    offer.GetOfferPricesWithPriceRule(amountMinToBuy, customOptionsXml, CustomerContext.CurrentCustomer, _customDiscount);
                 
                model.FinalDiscount = finalDiscount;
                model.FinalPrice = finalPrice;
                model.PreparedPrice =
                    _hidePriceDiscount
                        ? PriceFormatService.FormatPrice(model.FinalPrice, true, true, unit: SettingsCatalog.ShowUnitsInCatalog ? _product.Unit?.DisplayName : null)
                        : preparedPrice;
                
                model.BonusPrice = GetBonusCardPrice(_product, model.FinalPrice, oldPrice);

                if (_product.AllowPreOrder && (model.FinalPrice <= 0 || amount <= 0))
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
                        ? OrderService.IsCustomerHasPaidOrderWithProduct(customer.Id, _product.ProductId, false, true) 
                        : SettingsDesign.WhoAllowReviews == SettingsDesign.eWhoAllowReviews.Registered && customer.RegistredUser;


                foreach (var creditPayment in PaymentService.GetCreditPaymentMethods())
                {
                    if (!creditPayment.ShowCreditButtonInProductCard)
                        continue;

                    model.ShowCreditButton = true;
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
                        : (float?)null;

                    break;
                }

                model.AmountPriceString = PriceService
                    .SimpleRoundPrice(model.FinalPrice * model.Offer.Product.GetMinAmount(),
                        CurrencyService.CurrentCurrency).FormatPrice();
            }

            model.ProductProperties = _product.ProductPropertyValues;
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

            model.MinimumOrderPrice = CustomerGroupService.GetMinimumOrderPrice(ShoppingCartService.CurrentShoppingCart);

			model.RatingReadOnly = true;

            var reviewsCountString = string.Empty;
            
            var modules = AttachedModules.GetModuleInstances<IModuleReviews>();
            if (modules != null && modules.Count != 0)
            {
                foreach (var module in modules)
                    reviewsCountString += module.GetReviewsCount(HttpContext.Current.Request.Url.AbsoluteUri);
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
                model.FirstPaymentPrice.IsNotEmpty() 
                && offer.IsAvailableForPurchaseOnCredit(model.IsAvailableForPurchase, model.FinalPrice,
                                                     model.FirstPaymentMinPrice, model.FirstPaymentMaxPrice);

            model.IsAvailableForPurchaseOnBuyOneClick =
                offer.IsAvailableForPurchaseOnBuyOneClick(model.IsAvailableForPurchase, model.FinalPrice, model.MinimumOrderPrice);
            var propertyValueIds = _product.ProductPropertyValues.Select(x => x.PropertyValueId).ToList();
            var sizeChart = SizeChartService.Get(_product.ProductId, ESizeChartEntityType.Product, true).FirstOrDefault();
            if (sizeChart == null)
                sizeChart = SizeChartService.GetFilteredSizeChart(_product.CategoryId, ESizeChartEntityType.Category, _product.BrandId, propertyValueIds, true).FirstOrDefault();
            if (sizeChart != null)
                model.SizeChart = new SizeChartViewModel
                {
                    Id = sizeChart.Id,
                    ModalHeader = sizeChart.ModalHeader,
                    LinkText = sizeChart.LinkText,
                    SourceType = sizeChart.SourceType,
                    Text = sizeChart.Text
                };
            var productTabsModules = AttachedModules.GetModuleInstances<IProductTabs>();
            if (productTabsModules != null && productTabsModules.Count != 0)
            {
                var productTabsLinks = new List<ProductTabsLink>();
                foreach (var module in productTabsModules)
                {
                    var productDetailsTabList = module.GetProductDetailsTabsLinksCollection(_product.ProductId);
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

        private string GetBonusCardPrice(Product product, float productPrice, float oldPrice)
        {
            if (!BonusSystem.IsActive || productPrice <= 0 || !product.AccrueBonuses)
                return null;
            
            var purchase = new Purchase()
            {
                Currency = CurrencyService.CurrentCurrency,
                Items = new List<ItemOfPurchase>
                {
                    new ItemOfPurchase()
                    {
                        Code = product.ArtNo,
                        Price = productPrice,
                        BasePrice = oldPrice,
                        Amount = 1,
                        ApplyDiscounts = !product.DoNotApplyOtherDiscounts,
                        AccrueBonuses = product.AccrueBonuses,
                    }
                }
            };
            var accrueBonuses = BonusSystem.GetAccrueBonuses(purchase, CustomerContext.CurrentCustomer);
            var bonusPlus = accrueBonuses > 0 
                ? accrueBonuses.FormatBonuses() 
                : null;

            if (accrueBonuses == 0
                && BonusSystem.IsInternal)
            {
                var bonusCard = InternalBonusSystemService.GetCard(CustomerContext.CurrentCustomer.Id);
                if (bonusCard != null
                    && bonusCard.Blocked)
                    return null;

                if (bonusCard is null
                    && InternalBonusSystem.BonusFirstPercent != 0)
                    return (productPrice * (float) InternalBonusSystem.BonusFirstPercent / 100).SimpleRoundPrice().FormatBonuses();
            }

            return bonusPlus;
        }
    }
}