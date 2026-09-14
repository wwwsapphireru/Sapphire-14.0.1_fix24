using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using AdvantShop.Areas.Mobile.Models.ProductDetails;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Customers;
using AdvantShop.Handlers.ProductDetails;
using AdvantShop.SEO;
using AdvantShop.ViewModel.ProductDetails;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.SEO;
using AdvantShop.Repository.Currencies;
using AdvantShop.Core.UrlRewriter;
using System.Collections.Generic;
using AdvantShop.App.Landing.Extensions;
using AdvantShop.Areas.Mobile.Models.Product;
using AdvantShop.Core.Services.Landing.Blocks;
using AdvantShop.Core.Services.Landing.Forms;
using AdvantShop.FilePath;
using AdvantShop.Handlers.Catalog;
using AdvantShop.ViewModel.ProductDetailsLanding;
using AdvantShop.Web.Infrastructure.Filters;

namespace AdvantShop.Areas.Mobile.Controllers
{
    public class ProductController : BaseMobileController
    {
        public ActionResult Index(string url, int? color, int? size)
        {
            if (string.IsNullOrWhiteSpace(url))
                return Error404();

            var isManager = (CustomerContext.CurrentCustomer?.IsAdmin == true || CustomerContext.CurrentCustomer?.IsManager == true || CustomerContext.CurrentCustomer?.IsModerator == true);//GlorySoft_002

            var product = ProductService.GetProductByUrl(url);
            if (product == null || (!product.Enabled &&/*GlorySoft_002*/ !isManager) || !product.CategoryEnabled)
                return Error404();

            var model = new GetProductHandler(product, color, size, null).Get();

            if (isManager)//GlorySoft_002
            {
                var offer = OfferService.GetMainOffer(product.Offers, product.AllowPreOrder, color, size);
                var amountByMultiplicity = offer.GetAmountByMultiplicity(product.Multiplicity);
                var amount = amountByMultiplicity > 0 ? offer.Amount : 0;
                var isAvailable = offer != null && amountByMultiplicity > 0;
                model.IsAvailable = isAvailable || product.AllowBuyOutOfStockProducts();
            }

            model.BreadCrumbs =
                CategoryService.GetParentCategories(product.CategoryId)
                    .Reverse()
                    .Select(x => new BreadCrumbs(x.Name, x.CategoryId == 0 ? Url.AbsoluteRouteUrl("CatalogRoot") : Url.AbsoluteRouteUrl("Category", new { url = x.UrlPath })))
                    .ToList();

            model.BreadCrumbs.Insert(0, new BreadCrumbs(T("MainPage"), Url.AbsoluteRouteUrl("Home")));
            model.BreadCrumbs.Add(new BreadCrumbs(product.Name, Url.AbsoluteRouteUrl("Product", new { url = product.UrlPath })));

            RecentlyViewService.SetRecentlyView(CustomerContext.CustomerId, product.ProductId);

            SetMobileTitle(CategoryService.GetCategory(0).Name);
            SetNgController(NgControllers.NgControllersTypes.ProductCtrl);

            var category = CategoryService.GetCategory(product.CategoryId);

            var offerArtNo = product.Offers.Select(x => x.ArtNo).ToList();
            var productArtNo = product.ArtNo;

            SetMetaInformation(
                product.Meta, product.Name, category != null ? category.Name : string.Empty,
                product.Brand != null ? product.Brand.Name : string.Empty,
                tags: product.Tags.Select(x => x.Name).ToList(),
                price: PriceFormatService.FormatPricePlain(model.FinalPrice, CurrencyService.CurrentCurrency),
                offerArtNo: offerArtNo.Count > 0 ? string.Join(", ", offerArtNo) : string.Empty,
                productArtNo: productArtNo);

            var tagManager = GoogleTagManagerContext.Current;
            if (tagManager.Enabled)
            {
                tagManager.PageType = ePageType.product;
                tagManager.ProdId = model.Offer != null ? model.Offer.OfferId.ToString() : model.Product.ProductId.ToString();
                tagManager.ProdArtno = model.Offer != null ? model.Offer.ArtNo : model.Product.ArtNo;
                tagManager.ProdName = model.Product.Name;
                tagManager.ProdValue = model.Offer?.RoundedPrice ?? 0;
                tagManager.CatCurrentId = model.Product.MainCategory?.ID ?? 0;
                tagManager.CatCurrentName = model.Product.MainCategory?.Name ?? "";
            }

            var referrer = Request.GetUrlReferrer();

            if (referrer != null
                && referrer.AbsolutePath != "/"
                && referrer.AbsoluteUri.StartsWith(UrlService.GetUrl())
                && referrer.AbsolutePath != Request.Url.AbsolutePath
                && referrer.AbsolutePath.ToLower().Contains("/categories/"))
            {
                model.ReturnUrl = referrer.AbsoluteUri;
                model.UseHistoryApiForBack = true;
            }
            else if (product.MainCategory != null)
            {
                model.ReturnUrl = Url.AbsoluteRouteUrl("Category", new { url = product.MainCategory.UrlPath });
                model.UseHistoryApiForBack = false;
            }

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult ProductTabs(ProductDetailsViewModel productModel)
        {
            var model = new ProductTabsViewModel()
            {
                ProductModel = productModel,
                UseStandartReviews = productModel.UseStandartReviews,
                ReviewsCount = productModel.ReviewsCountInt
            };

            foreach (var tabsModule in AttachedModules.GetModules<IProductTabs>())
            {
                var classInstance = (IProductTabs)Activator.CreateInstance(tabsModule, null);
                model.Tabs.AddRange(classInstance.GetProductDetailsTabsCollection(productModel.Product.ProductId));
            }

            if (SettingsSEO.ProductAdditionalDescription.IsNotEmpty())
            {
                model.AdditionalDescription =
                    GlobalStringVariableService.TranslateExpression(
                        SettingsSEO.ProductAdditionalDescription, MetaType.Product, productModel.Product.Name,
                        CategoryService.GetCategory(productModel.Product.CategoryId).Name,
                        productModel.Product.Brand != null ? productModel.Product.Brand.Name : string.Empty,
                        price: PriceFormatService.FormatPricePlain(productModel.FinalPrice, CurrencyService.CurrentCurrency),
                        tags: productModel.Product.Tags.Select(x => x.Name).ToList().AggregateString(" "),
                        productArtNo: productModel.Product.ArtNo);
            }

            return PartialView(model);
        }

        [ChildActionOnly]
        public ActionResult ProductPhotos(ProductDetailsViewModel productModel)
        {
            var product = productModel.Product;

            var model = new ProductPhotosMobileViewModel()
            {
                Product = product,
                Discount = productModel.FinalDiscount, // todo: Check it
                ProductModel = productModel,
                Photos = product.ProductPhotos
                    .OrderByDescending(item => item.Main)
                    .ThenByDescending(item => productModel.ColorId.HasValue ? item.ColorID == productModel.ColorId.Value : item.Main)
                    .ThenBy(item => item.PhotoSortOrder)
                    .ToList(),
                ActiveThreeSixtyView = productModel.Product.ActiveView360 && product.ProductPhotos360.Any(),
                Photos360 = product.ProductPhotos360,
                Photos360Ext = product.ProductPhotos360.Any() ? Path.GetExtension(product.ProductPhotos360.First().PhotoName) : string.Empty,
                ColorId = productModel.ColorId.HasValue ? productModel.ColorId : (productModel.Offer != null ? productModel.Offer.ColorID : null),
                Offer = productModel.Offer,
            };

            model.CarouselPhotoHeight = SettingsPictureSize.SmallProductImageHeight;
            model.CarouselPhotoWidth = SettingsPictureSize.SmallProductImageWidth;
            model.PreviewPhotoHeight = SettingsPictureSize.MiddleProductImageHeight;
            model.PreviewPhotoWidth = SettingsPictureSize.MiddleProductImageWidth;

            foreach (var photo in model.Photos)
            {
                photo.Title = string.IsNullOrWhiteSpace(photo.Description)
                    ? GlobalStringVariableService.TranslateExpression(SettingsSEO.ProductPhotoTitle, MetaType.ProductPhoto, product.Name, productArtNo: product.ArtNo)
                    : photo.Description;
                photo.Alt = string.IsNullOrWhiteSpace(photo.Description)
                    ? GlobalStringVariableService.TranslateExpression(SettingsSEO.ProductPhotoAlt, MetaType.ProductPhoto, product.Name, productArtNo: product.ArtNo)
                    : photo.Description;
            }


            // if (productModel.Offer != null && productModel.Offer.Photo != null) //&& productModel.Offer.Photo.PhotoName.IsNotEmpty())
            // {
            //     model.Photos = model.Photos.OrderBy(x => x.PhotoId == productModel.Offer.Photo.PhotoId).ToList();
            // }
            // else
            // {
            //     model.Photos = 
            //         model.Photos.OrderByDescending(item => item.Main)
            //             .ThenBy(item => item.PhotoSortOrder)
            //             .ToList();
            // }
            model.VideosList = product.ProductVideos;
            model.Video = product.ProductVideos.FirstOrDefault();

            var customLabels = new List<ProductLabel>();

            foreach (var labelModule in AttachedModules.GetModules<ILabel>())
            {
                var classInstance = (ILabel)Activator.CreateInstance(labelModule);
                var label = classInstance.GetLabel();
                if (label != null)
                {
                    customLabels.Add(label);
                }

                var labels = classInstance.GetLabels();
                if (labels != null)
                {
                    customLabels.AddRange(labels);
                }
            }

            model.Labels = customLabels.Where(l => l.ProductIds.Contains(product.ProductId)).Select(l => l.LabelCode).ToList();

            return PartialView(model);
        }

        public ActionResult ProductViewPhoto(ProductPhotoMobileViewModel model)
        {
            return PartialView(model);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public ActionResult ProductViewPhoto(ProductPhotoMobileSetting setting)
        {
            var productPhotos = PhotoService.GetPhotos<ProductPhoto>(setting.ProductId, PhotoType.Product);
            var photos = productPhotos
                .Where(photo => setting.RenderedPhotoId != photo.PhotoId && (photo.ColorID == setting.ColorId || photo.ColorID.HasValue == false))
                .OrderByDescending(photo => photo.ColorID.HasValue)
                .Take(setting.LimitPhotoCount).ToList();
            var model = new ProductPhotoMobileViewModel(setting)
            {
                Photos = photos,
                ProductImageType = setting.ProductViewMode == ProductViewMode.Single ? ProductImageType.Middle : ProductImageType.Small,

            };
            return PartialView(model);
        }
        public ActionResult ProductQuickView(int productId, int? color, int? size, string from, int? landingId,
                                              bool? hideShipping, bool? showLeadButton, int? blockId, bool? showVideo = true, string descriptionMode = "",
                                              SettingsDesign.eCartAddTypeButton cartAddType = SettingsDesign.eCartAddTypeButton.Classic, int? offerId = null)
        {
            try
            {
                var (model, modelProduct, product, category, offerArtNos) = new ProductQuickViewHandler(
                    productId,
                    color,
                    size,
                    from,
                    landingId,
                    hideShipping,
                    showLeadButton,
                    blockId,
                    showVideo,
                    descriptionMode,
                    cartAddType,
                    offerId
                ).Execute();

                SetMetaInformation(
                    product.Meta, product.Name, category != null ? category.Name : string.Empty,
                    product.Brand != null ? product.Brand.Name : string.Empty,
                    tags: product.Tags.Select(x => x.Name).ToList(),
                    price: PriceFormatService.FormatPricePlain(model.FinalPrice, CurrencyService.CurrentCurrency),
                    offerArtNo: offerArtNos.Count > 0 ? string.Join(", ", offerArtNos) : string.Empty,
                    productArtNo: product.ArtNo);

                if (modelProduct == null) return PartialView(model);

                return PartialView("ProductQuickViewLanding", modelProduct);
            }
            catch (BlException)
            {
                return new EmptyResult();
            }
        }
    }
}