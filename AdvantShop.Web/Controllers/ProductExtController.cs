using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Bonuses.Internal;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Customers;
using AdvantShop.Handlers.ProductDetails;
using AdvantShop.Payment;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using AdvantShop.App.Landing.Extensions;
using AdvantShop.CMS;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Landing.Blocks;
using AdvantShop.Core.Services.Landing.Forms;
using AdvantShop.Models.ProductDetails;
using AdvantShop.Repository.Currencies;
using AdvantShop.ViewModel.ProductDetails;
using AdvantShop.Web.Infrastructure.Filters;
using System.Collections.Generic;
using AdvantShop.Core;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.SEO;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Orders;
using AdvantShop.ViewCommon;
using AdvantShop.Core.Services.InplaceEditor;
using AdvantShop.Handlers.Inplace;

namespace AdvantShop.Controllers
{
    [SessionState(SessionStateBehavior.Disabled)]
    public partial class ProductExtController : BaseClientProductController
    {
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult GetShippings(int offerId, float amount, string customOptions, string zip)
        {
            var model = new GetShippingsHandler(offerId, amount, customOptions, zip).Get();
            return Json(model);
        }

        public JsonResult GetOffers(int productId, int? colorId, int? sizeId)
        {
            var product = ProductService.GetProduct(productId);
            if (product == null)
                return Json(null);

            var offers = product.Offers;
            if (offers == null || offers.Count == 0)
                return Json(null);

            var warehouseIds = WarehouseContext.GetAvailableWarehouseIds();
            var warehouseId = warehouseIds?.FirstOrDefault();
            if (warehouseIds != null)
                offers.SetAmountByStocksAndWarehouses(warehouseIds);
            
            var offerSelected = OfferService.GetMainOffer(offers, product.AllowPreOrder, colorId, sizeId, warehouseId);
            
            var result = new GetOffersModel(product, offers, offerSelected.OfferId, CustomerContext.CurrentCustomer, warehouseIds);
            
            return Json(result);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult GetOfferPrice(int offerId, string attributesXml, int? lpBlockId, float amount)
        {
            var offer = OfferService.GetOffer(offerId);
            if (offer == null)
                return Json(new {PriceString = "", PriceNumber = 0F, Bonuses = ""});

            var warehouseIds = WarehouseContext.GetAvailableWarehouseIds();
            if (warehouseIds != null)
                offer.SetAmountByStocksAndWarehouses(warehouseIds);

            Discount customDiscount = null;

            if (lpBlockId != null)
            {
                var block = new LpBlockService().Get(lpBlockId.Value);
                var button = block?.TryGetSetting<LpButton>("button");

                if (button != null && button.Discount != null)
                    customDiscount = button.Discount;
            }
            
            var customer = CustomerContext.CurrentCustomer;
            
            var (oldPrice, finalPrice, finalDiscount, preparedPrice) =
                offer.GetOfferPricesWithPriceRule(amount, attributesXml, customer, customDiscount);

            var bonusPlus = string.Empty;

            if (BonusSystem.IsActive && offer.RoundedPrice > 0 && offer.Product.AccrueBonuses)
            {
                var purchase = new Purchase()
                {
                    Currency = CurrencyService.CurrentCurrency,
                    Items = new List<ItemOfPurchase>
                    {
                        new ItemOfPurchase()
                        {
                            Code = offer.ArtNo,
                            Price = finalPrice,
                            BasePrice = oldPrice,
                            Amount = 1,
                            ApplyDiscounts = !offer.Product.DoNotApplyOtherDiscounts,
                            AccrueBonuses = offer.Product.AccrueBonuses,
                        }
                    }
                };

                var accrueBonuses = BonusSystem.GetAccrueBonuses(purchase, customer);
                bonusPlus = accrueBonuses > 0 
                    ? PriceFormatService.RenderBonusPrice(accrueBonuses) 
                    : null;

                if (accrueBonuses == 0
                    && BonusSystem.IsInternal)
                {
                    var bonusCard = InternalBonusSystemService.GetCard(customer.Id);
                    if (bonusCard != null && bonusCard.Blocked)
                    {
                        bonusPlus = null;
                    }
                    else if (bonusCard is null
                        && InternalBonusSystem.BonusFirstPercent != 0)
                    {
                        bonusPlus = PriceFormatService.RenderBonusPrice((float)InternalBonusSystem.BonusFirstPercent, finalPrice, finalDiscount);
                    }
                }
            }

            var amountByMultiplicity = offer.GetAmountByMultiplicity(offer.Product.Multiplicity);
            var minimumOrderPrice = CustomerGroupService.GetMinimumOrderPrice(ShoppingCartService.CurrentShoppingCart);

            var allowBuyOutOfStockProducts = offer.Product.AllowBuyOutOfStockProducts();
            
            var isAvailableForPurchase = 
                offer.IsAvailableForPurchase(amountByMultiplicity, amount, finalPrice, finalDiscount, allowBuyOutOfStockProducts);
            
            var isAvailableForPurchaseOnBuyOneClick =
                offer.IsAvailableForPurchaseOnBuyOneClick(isAvailableForPurchase, finalPrice, minimumOrderPrice);

            var amountPrice = finalPrice * amount;
            
            return Json(new
            {
                PriceString = preparedPrice,
                PriceNumber = finalPrice,
                PriceOldNumber = oldPrice,
                Bonuses = bonusPlus,
                
                AllowBuyOutOfStockProducts = allowBuyOutOfStockProducts,
                IsAvailableForPurchase = isAvailableForPurchase,
                IsAvailableForPurchaseOnBuyOneClick = isAvailableForPurchaseOnBuyOneClick,
                AmountPrice = PriceService.SimpleRoundPrice(amountPrice),
                AmountPriceString = PriceService.SimpleRoundPrice(amountPrice, CurrencyService.CurrentCurrency).FormatPrice()
            });
        }

        public JsonResult GetCreditPayment(float price, float discount, float discountAmount)
        {
            var finalPrice = PriceService.GetFinalPrice(price, new Discount(discount, discountAmount));
            foreach (var creditPayment in PaymentService.GetCreditPaymentMethods())
            {
                if (!creditPayment.ShowCreditButtonInProductCard)
                    continue;
                    
                var paymentMethod = creditPayment as PaymentMethod;
                var finalPriceInPaymentCurrency = 
                    finalPrice.ConvertCurrency(CurrencyService.CurrentCurrency,
                                        paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency);
                    
                if (finalPriceInPaymentCurrency < creditPayment.MinimumPrice || 
                    finalPriceInPaymentCurrency > creditPayment.MaximumPrice)
                    continue;

                string firstPaymentPrice = null;

                if (creditPayment.TypePresentationOfCreditInformation
                    == EnTypePresentationOfCreditInformation.FirstPayment)
                {
                    var firstPayment = creditPayment.GetFirstPayment(finalPriceInPaymentCurrency);
                    if (firstPayment is null) 
                        continue;
                    
                    var firstPaymentInCurrentCurrency =
                        firstPayment.Value
                                    .ConvertCurrency(paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                                         CurrencyService.CurrentCurrency)
                                    .RoundPrice();
                    var result = firstPaymentInCurrentCurrency > 0
                        ? firstPaymentInCurrentCurrency.FormatPrice(true, false) + "*"
                        : T("Product.WithoutFirstPayment");

                    firstPaymentPrice = result;
                }
                
                if (creditPayment.TypePresentationOfCreditInformation
                    == EnTypePresentationOfCreditInformation.AmountAndNumberOfPayments)
                {
                    var amountAndNumberOfPayments = creditPayment.GetAmountAndNumberOfPayments(finalPriceInPaymentCurrency);
                    if (amountAndNumberOfPayments.IsDefault())
                        continue;
                        
                    var amountPaymentInCurrentCurrency =
                        amountAndNumberOfPayments.AmountPyament
                                                 .ConvertCurrency(
                                                      paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                                                      CurrencyService.CurrentCurrency)
                                                 .RoundPrice();

                    var result = 
                        string.Format("{0} x {1}*", 
                            amountPaymentInCurrentCurrency.FormatPrice(true, false), 
                            amountAndNumberOfPayments.NumberOfPayments);

                    firstPaymentPrice = result;
                }

                var creditButtonText = creditPayment.CreditButtonTextInProductCard ?? LocalizationService.GetResource("Product.ProductInfo.BuyOnCredit");
                var firstPaymentId = paymentMethod.PaymentMethodId;
                var firstPaymentMinPrice =
                    creditPayment.MinimumPrice.ConvertCurrency(
                        paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                        CurrencyService.CurrentCurrency);
                var firstPaymentMaxPrice = creditPayment.MaximumPrice.HasValue
                    ? creditPayment.MaximumPrice.Value.ConvertCurrency(
                        paymentMethod.PaymentCurrency ?? CurrencyService.CurrentCurrency,
                        CurrencyService.CurrentCurrency)
                    : (float?)null;
                return Json(new
                {
                    showCreditButton = true,
                    firstPaymentPrice,
                    creditButtonText,
                    firstPaymentId,
                    firstPaymentMinPrice,
                    firstPaymentMaxPrice
                });
            }

            return Json(new
            {
                showCreditButton = false
            });
        }

        public JsonResult GetVideos(int productId)
        {
            return Json(ProductVideoService.GetProductVideos(productId));
        }
        public JsonResult GetVideoById(int videoId)
        {
            return Json(ProductVideoService.GetProductVideo(videoId));
        }
        public JsonResult GetPhotos(int productId)
        {
            var product = ProductService.GetProduct(productId);
            var photos = PhotoService.GetPhotos<ProductPhoto>(productId, PhotoType.Product).Select(photo => new
            {
                PathXSmall = photo.ImageSrcXSmall(),
                PathSmall = photo.ImageSrcSmall(),
                PathMiddle = photo.ImageSrcMiddle(),
                PathBig = photo.ImageSrcBig(),
                photo.ColorID,
                photo.PhotoId,
                photo.Description,
                photo.Main,
                SettingsPictureSize.XSmallProductImageHeight,
                SettingsPictureSize.XSmallProductImageWidth,
                SettingsPictureSize.SmallProductImageHeight,
                SettingsPictureSize.SmallProductImageWidth,
                SettingsPictureSize.MiddleProductImageWidth,
                SettingsPictureSize.MiddleProductImageHeight,
                SettingsPictureSize.BigProductImageWidth,
                SettingsPictureSize.BigProductImageHeight,
                Alt = (string.IsNullOrEmpty(photo.Description)
                            ? GlobalStringVariableService.TranslateExpression(SettingsSEO.ProductPhotoAlt, MetaType.ProductPhoto, product.Name, productArtNo: product.ArtNo)
                            : photo.Description),
                Title = (string.IsNullOrEmpty(photo.Description)
                            ? GlobalStringVariableService.TranslateExpression(SettingsSEO.ProductPhotoTitle, MetaType.ProductPhoto, product.Name, productArtNo: product.ArtNo)
                            : photo.Description)
            }).ToList();

            return Json(photos, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustomOptions(int productId)
        {
            var customOptions = 
                CustomOptionsService.GetCustomOptionsByProductId(productId)
                    .Select(x => new CustomOptionModel(x))
                    .ToList();
            
            return Json(customOptions);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CustomOptions(int productId, List<OptionItem> selectedOptions)
        {
            var handler = new GetProductCustomOptionsHandler(productId, selectedOptions, preSelect: false);
            return Json(new
            {
                xml = HttpUtility.UrlEncode(handler.GetXml()),
                jsonHash = HttpUtility.UrlEncode(handler.GetJsonHash()),
                error = handler.GetError()
            });
        }

        public JsonResult AddRating(int objid, int rating)
        {
            float newRating = 0;

            if (objid != 0 && rating != 0)
                newRating = RatingService.Vote(objid, rating);

            return Json(newRating);
        }

        [HttpGet]
        public JsonResult GetPropertiesNames(string q)
        {
            return Json(PropertyService.GetPropertiesByName(q).ToList());
        }

        [HttpGet]
        public JsonResult GetPropertiesValues(string q, int productId, int propertyId = 0)
        {
            return Json(PropertyService.GetPropertyValuesByNameAndProductId(q, productId, propertyId).ToList());
        }

        [HttpGet]
        public JsonResult GetReviewsCount(int productId)
        {
            var reviewsCount = ReviewService.GetReviewsCount(productId, EntityType.Product, SettingsCatalog.ModerateReviews, true);
            return Json(new { reviewsCount });
        }
        
        [HttpGet]
        public JsonResult GetPriceAmountList(GetPriceAmountListDto dto)
        {
            return ProcessJsonResult(new GetPriceAmountList(dto));
        }

        public JsonResult GetOfferStocks(int offerId, int? cityId = null)
        {
            return ProcessJsonResult(new GetOfferStocks(offerId, cityId) {ShowOnlyAvalible = SettingsCatalog.ShowOnlyAvailableWarehousesInProduct});
        }

        [ChildActionOnly]
        public ActionResult ProductViewButtons(ProductViewButtonsViewModel model) =>
            PartialView("_ProductViewButtons", model);
        
        [HttpGet]
        public ActionResult GetProductViewButtons(int productId, int offerId) =>
            PartialView("_ProductViewButtons", new GetProductViewButtonsHandler(productId, offerId).Execute());

        [HttpPost, ValidateJsonAntiForgeryToken]
        [InPlace(RoleKey = RoleAction.Catalog)]
        public JsonResult InplaceEditorProduct(int id, string content, ProductInplaceField field)//GlorySoft_034
        {
            Diagnostics.Debug.Log.Info("InplaceEditorProduct");
            var model = new InplaceProductHandler().Execute(id, content, field);
            return Json(model);
        }

    }
}