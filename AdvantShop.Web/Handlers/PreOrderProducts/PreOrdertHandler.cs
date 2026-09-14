using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Customers;
using AdvantShop.Mails;
using AdvantShop.Orders;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Handlers.Checkout;
using AdvantShop.Models.Checkout;
using AdvantShop.Models.PreOrder;
using AdvantShop.ViewModel.PreOrder;
using AdvantShop.CMS;
using AdvantShop.Core.Services.Localization;
using AdvantShop.FilePath;
using System.Web.Mvc;
using System.Web;
using AdvantShop.Web.Infrastructure.Extensions;

namespace AdvantShop.Handlers.PreOrderProducts
{
    public class PreOrderHandler
    {
        private readonly UrlHelper _urlHelper;//GlorySoft_029

        public PreOrderHandler()
        {
            _urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);//GlorySoft_029
        }

        public PreOrderViewModel/*GlorySoft_029 PreOrderModel*/ Get(PreOrderViewModel/*GlorySoft_029 PreOrderModel*/ model)
        {
            Offer offer = null;

            if (model.OfferId != 0)
            {
                offer = OfferService.GetOffer(model.OfferId);
            }

            if (offer == null || !offer.Product.Enabled || !offer.Product.CategoryEnabled)
                return null;

            var customer = CustomerContext.CurrentCustomer;

            model.Offer = offer;
            model.ProductId = offer.ProductId;
            model.FirstName = model.FirstName ?? (customer.RegistredUser ? customer.FirstName : string.Empty);
            model.LastName = model.LastName ?? (customer.RegistredUser ? customer.LastName : string.Empty);
            model.Email = model.Email ?? (customer.RegistredUser ? customer.EMail : string.Empty);
            model.Phone = model.Phone ?? (customer.RegistredUser ? customer.Phone : string.Empty);

            if (float.IsNaN(model.Amount))
                model.Amount = 1;

            model.ProdMinAmount = offer.Product.GetMinAmount();

            model.Amount = model.Amount == 0 || model.Amount < model.ProdMinAmount || Math.Abs(model.Amount% model.ProdMinAmount) > 0.1
                ? model.ProdMinAmount
                : model.Amount;
            
            model.CanOrderByRequest = offer.IsAvailableForPreOrder(model.Amount);

            //GlorySoft_029
            model.ManufacturerName = offer.Product.Brand != null ? offer.Product.Brand.Name : string.Empty;
            model.ManufacturerUrl = offer.Product.Brand != null ? offer.Product.Brand.UrlPath : string.Empty;
            model.Ratio = offer.Product.Ratio;
            model.ManualRatio = offer.Product.ManualRatio;
            model.EnabledReviewsCount = SettingsCatalog.AllowReviews;
            if (model.EnabledReviewsCount)
            {
                var reviewsCount = ReviewService.GetReviewsCount(offer.Product.ProductId, EntityType.Product, SettingsCatalog.ModerateReviews, true);
                model.ReviewsCount = string.Format("{0} {1}",
                    reviewsCount.ToString(CultureInfo.InvariantCulture),
                    Strings.Numerals(reviewsCount,
                        LocalizationService.GetResource("Product.Reviews0"),
                            LocalizationService.GetResource("Product.Reviews1"),
                            LocalizationService.GetResource("Product.Reviews2"),
                            LocalizationService.GetResource("Product.Reviews5")));
            }
            var productPhotoName = string.Empty;
            if (offer.ColorID != null)
            {
                var photo =
                    PhotoService.GetPhotos(offer.Product.ProductId, PhotoType.Product)
                        .FirstOrDefault(item => item.ColorID == offer.ColorID);

                if (photo != null)
                    productPhotoName = photo.PhotoName;
            }
            model.ImageSrc =
                FoldersHelper.GetImageProductPath(ProductImageType.Middle,
                    string.IsNullOrEmpty(productPhotoName) ? offer.Product.Photo : productPhotoName,
                    false);

            float optionsPrice = 0;//GlorySoft_029
            if (model.OptionsHash.IsNotEmpty())
            {
                var listOptions = CustomOptionsService.GetFromJsonHash(model.OptionsHash, offer.Product.Currency.Rate);
                model.OptionsRendered = OrderService.RenderSelectedOptions(listOptions, offer.Product.Currency);
                optionsPrice = CustomOptionsService.GetCustomOptionPrice(offer.RoundedPrice, listOptions);//GlorySoft_029
            }

            //GlorySoft_029
            var priceWithDiscount = PriceService.GetFinalPrice(offer, customer.CustomerGroup, optionsPrice);
            model.PreparedPrice = PriceFormatService.FormatPrice(offer.RoundedPrice + optionsPrice, priceWithDiscount, offer.Product.Discount, true);
            model.BreadCrumbs = new List<BreadCrumbs>()
            {
                new BreadCrumbs(LocalizationService.GetResource("MainPage"), _urlHelper.AbsoluteRouteUrl("Home")),
                new BreadCrumbs(LocalizationService.GetResource("PreOrder.Index.Header"), _urlHelper.AbsoluteRouteUrl("PreOrder"))
            };

            return model;
        }

        public bool Send(PreOrderViewModel/*GlorySoft_029 PreOrderModel*/ model, Offer offer)
        {
            var listOptions = new List<EvaluatedCustomOptions>();
            if (model.OptionsHash.IsNotEmpty())
            {
                listOptions = CustomOptionsService.GetFromJsonHash(model.OptionsHash, offer.Product.Currency.Rate);
                model.OptionsRendered = OrderService.RenderSelectedOptions(listOptions, offer.Product.Currency);
            }

            if (SettingsCheckout.OutOfStockAction == eOutOfStockAction.Preorder)
            {
                var orderByRequest = new OrderByRequest
                {
                    OfferId = offer.OfferId,
                    ProductId = offer.Product.ProductId,
                    ProductName = offer.Product.Name,
                    ArtNo = offer.ArtNo,
                    Quantity = model.Amount,
                    UserName = model.FirstName + " " + model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Comment = model.Comment,
                    IsComplete = false,
                    RequestDate = DateTime.Now,
                    Options = CustomOptionsService.SerializeToXml(listOptions)
                };

                OrderByRequestService.AddOrderByRequest(orderByRequest);

                var mail =
                    new OrderByRequestMailTemplate(
                            orderByRequest.OrderByRequestId.ToString(CultureInfo.InvariantCulture),
                            offer.ArtNo,
                            offer.Product.Name,
                            model.Amount.ToString(CultureInfo.InvariantCulture),
                            model.FirstName + " " + model.LastName,
                            model.Email,
                            model.Phone,
                            model.Comment,
                            offer.Color != null ? offer.Color.ColorName : string.Empty,
                            offer.Size != null ? offer.Size.SizeName : string.Empty,
                            model.OptionsRendered.IsNotEmpty() ? model.OptionsRendered : string.Empty);

                MailService.SendMailNow(CustomerContext.CustomerId, model.Email, mail);
                MailService.SendMailNow(SettingsMail.EmailForOrders, mail, replyTo: model.Email);

                return true;
            }

            if (SettingsCheckout.OutOfStockAction == eOutOfStockAction.Order || SettingsCheckout.OutOfStockAction == eOutOfStockAction.Lead)
            {
                var result = new BuyInOneClickHandler(new BuyOneInClickJsonModel()
                {
                    Amount = model.Amount,
                    Email = model.Email,
                    Comment = model.Comment,
                    Name = model.FirstName,
                    LastName = model.LastName,
                    OfferId = model.OfferId,
                    OrderType = Core.Services.Orders.OrderType.PreOrder,
                    Page = BuyInOneclickPage.PreOrder,
                    Phone = model.Phone,
                    ProductId = model.ProductId,
                    AttributesXml = CustomOptionsService.SerializeToXml(listOptions)
                }).Create();

                return true;
            }

            return false;
        }
    }
}