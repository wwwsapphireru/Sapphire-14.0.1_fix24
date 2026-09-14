using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Controls;
using AdvantShop.Core.Modules;
using AdvantShop.Handlers.PreOrderProducts;
using AdvantShop.Helpers;
using AdvantShop.Orders;
using AdvantShop.SEO;
using AdvantShop.Models.PreOrder;
using AdvantShop.ViewModel.PreOrder;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using BotDetect.Web.Mvc;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AdvantShop.Controllers
{
    public partial class PreOrderController : BaseClientController
    {
        //public ActionResult Index(int offerId)GlorySoft_009
        //{
        //    var product = ProductService.GetProductByOfferId(offerId);
        //    if (product == null || product.UrlPath.IsNullOrEmpty())
        //        return Error404();
        //    return RedirectToRoutePermanent("Product", new { url = product.UrlPath } );
        //}

        public ActionResult Index(int offerId, float amount = 1, string optionsHash = null)//GlorySoft_009
        {
            if (offerId == 0)
                return Error404();

            var model = new PreOrderHandler().Get(new PreOrderViewModel()
            {
                OfferId = offerId,
                Amount = amount,
                OptionsHash = optionsHash
            });
            if (model == null)
                return Error404();

            SetNgController(NgControllers.NgControllersTypes.PreorderCtrl);

            SetMetaInformation(
                new MetaInfo(string.Format("{0} - {1} - {2}", model.Offer.Product.Name, T("PreOrder.Index.OrderByRequest"), SettingsMain.ShopName)),
                string.Empty);

            SetNoFollowNoIndex();
            SetMobileTitle(T("PreOrder.Index.MobileTitle"));

            return View(model);
        }

        public ActionResult Success(bool? isLanding)
        {
            SetMetaInformation(T("PreOrder.Success.Title"));
            SetNoFollowNoIndex();
            return isLanding is true ? View("Success", "_LayoutEmpty") : View("Success");
        }

        public ActionResult LinkByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return RedirectToRoute("Home");

            SetMetaInformation(T("PreOrder.LinkByCode.Header"));
            SetMobileTitle(T("PreOrder.Index.MobileTitle"));
            SetNoFollowNoIndex();

            var model = new LinkByCodeViewModel();

            // Если код правильный, и такого же товара нет в корзине - то всё ок.
            var orderByRequest = OrderByRequestService.GetOrderByRequest(code);
            if (orderByRequest == null)
            {
                model.Error = T("PreOrder.Index.OrderProductMessage");
                return View(model);
            }

            if (ShoppingCartService.CurrentShoppingCart.Any(p => p.Offer.OfferId == orderByRequest.OfferId))
            {
                var item = ShoppingCartService.CurrentShoppingCart.First(p => p.Offer.OfferId == orderByRequest.OfferId);
                if (item.Amount != orderByRequest.Quantity)
                {
                    item.Amount = orderByRequest.Quantity;
                    ShoppingCartService.UpdateShoppingCartItem(item);
                }
                return RedirectToRoute("Cart");
            }

            var offer = OfferService.GetOffer(orderByRequest.OfferId);
            if (orderByRequest.IsValidCode && ProductService.IsExists(orderByRequest.ProductId) && offer != null && offer.BasePrice > 0)
            {
                ShoppingCartService.AddShoppingCartItem(new ShoppingCartItem
                {
                    OfferId = orderByRequest.OfferId,
                    Amount = orderByRequest.Quantity,
                    ShoppingCartType = ShoppingCartType.ShoppingCart,
                    AttributesXml = orderByRequest.Options,
                    AddedByRequest = orderByRequest.Quantity > offer.Amount
                });

                return RedirectToRoute("Cart");
            }
            model.Error = T("PreOrder.Index.OrderProductMessage");

            return View(model);
        }


        [HttpPost, ValidateAntiForgeryToken]
        [CaptchaValidation("CaptchaCode", "CaptchaSource")]
        public ActionResult PreOrderIndex(PreOrderViewModel requestModel)//GlorySoft_009
        {
            var isValid = requestModel.FirstName.IsNotEmpty() && requestModel.Email.IsNotEmpty() && requestModel.Phone.IsNotEmpty();
            isValid &= requestModel.Agreement == SettingsCheckout.IsShowUserAgreementText;
            isValid &= requestModel.Amount > 0;
            isValid &= ValidationHelper.IsValidEmail(requestModel.Email);

            if (SettingsMain.EnableCaptchaInPreOrder)
            {
                if (!ModelState.IsValidField("CaptchaCode"))
                    isValid = false;

                MvcCaptcha.ResetCaptcha("CaptchaSource");
            }

            requestModel.Comment = HttpUtility.HtmlEncode(requestModel.Comment);
            requestModel.Email = HttpUtility.HtmlEncode(requestModel.Email);
            requestModel.FirstName = HttpUtility.HtmlEncode(requestModel.FirstName);
            requestModel.LastName = HttpUtility.HtmlEncode(requestModel.LastName);
            requestModel.Phone = HttpUtility.HtmlEncode(requestModel.Phone);

            var allow = ModulesExecuter.CheckInfo(System.Web.HttpContext.Current, Core.Modules.Interfaces.ECheckType.CheckoutUserInfo, requestModel.Email, requestModel.FirstName, message: requestModel.Comment, phone: requestModel.Phone);
            if (!allow)
                isValid = false;

            var handler = new PreOrderHandler();
            var model = handler.Get(requestModel);

            if (model == null)
                return Error404();

            SetMetaInformation(new MetaInfo(string.Format("{0} - {1}", SettingsMain.ShopName, T("PreOrder.Index.OrderByRequest"))), "");

            SetMobileTitle(T("PreOrder.Index.MobileTitle"));

            if (!isValid)
            {
                ShowMessage(NotifyType.Error,
                    requestModel.Agreement != SettingsCheckout.IsShowUserAgreementText
                        ? T("Js.Subscribe.ErrorAgreement")
                        : T("PreOrder.Index.WrongData"));

                return !model.IsLanding ? View(model) : View("Index", "_LayoutEmpty", model);
            }

            var isSendSuccess = handler.Send(requestModel, model.Offer);
            if (isSendSuccess)
            {
                model.IsSuccess = true;
                model.SuccessText = T("PreOrder.Index.MessageSent");
            }

            return !model.IsLanding ? View("PreorderSuccess") : View("Success", "_LayoutEmpty");
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public ActionResult PreOrderUnregistered(int offerId, bool addToWishlist)//GlorySoft_009
        {
            if (addToWishlist)
            {
                if (Configuration.SettingsDesign.WishListVisibility)
                {
                    var offer = OfferService.GetOffer(offerId);
                    if (offer != null)
                    {
                        var item = new ShoppingCartItem
                        {
                            OfferId = offer.OfferId,
                            ShoppingCartType = ShoppingCartType.Wishlist
                        };

                        var itemId = ShoppingCartService.AddShoppingCartItem(item);

                        //int wishCount = ShoppingCartService.CurrentWishlist.Count;
                        //string wishlistCount = string.Format("{0} {1}",
                        //    wishCount == 0 ? "" : wishCount.ToString(CultureInfo.InvariantCulture),
                        //    Strings.Numerals(wishCount,
                        //        T("Common.TopPanel.WishList0"),
                        //        T("Common.TopPanel.WishList1"),
                        //        T("Common.TopPanel.WishList2"),
                        //        T("Common.TopPanel.WishList5")));

                        //ModulesExecuter.AddToCompare(item, Url.AbsoluteRouteUrl("Product", new { url = offer.Product.UrlPath }));
                    }
                }
            }

            return RedirectToRoute("Login");
        }

    }
}