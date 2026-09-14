using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Controls;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Bonuses.Internal;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.Core.Services.Crm;
using AdvantShop.Core.Services.Landing;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.Services.SEO;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Extensions;
using AdvantShop.Handlers.Checkout;
using AdvantShop.Helpers;
using AdvantShop.Models.Checkout;
using AdvantShop.Orders;
using AdvantShop.Payment;
using AdvantShop.Repository;
using AdvantShop.Repository.Currencies;
using AdvantShop.Shipping;
using AdvantShop.Track;
using AdvantShop.Trial;
using AdvantShop.ViewModel.Checkout;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Extensions;
using AdvantShop.Web.Infrastructure.Filters;
using BotDetect.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AdvantShop.Areas.Api.Attributes;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Diagnostics;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Security;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Handlers.PreOrderProducts;
using AdvantShop.Models.PreOrder;
using AdvantShop.CMS;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Core.Services.Configuration;
using AdvantShop.Core.Services.Shop;
using AdvantShop.Handlers.Common;
using AdvantShop.MobileApp;
using AdvantShop.Core.Services.Attachments;
using AdvantShop.Core.Services.Auth;
using AdvantShop.Core.Services.GeoModes;
using AdvantShop.Models.Attachments;
using AdvantShop.Core.Services.Helpers;
using AdvantShop.GeoModes;
using AdvantShop.Handlers.Checkout.GeoMode;
using AdvantShop.ViewModel.PreOrder;

namespace AdvantShop.Controllers
{
    [ExcludeFilter(typeof(CacheFilterAttribute))]
    public partial class CheckoutController : BaseClientController
    {
        #region Checkout

        // GET: /checkout
        [AccessByChannel(EProviderSetting.StoreActive)]
        public ActionResult Index(ECheckoutView? checkoutType)
        {
            var cart = new ShoppingCart();
            if (DebugMode.IsDebugMode(eDebugMode.CriticalCss))
            {
                var firstProduct = ProductService.GetFirstProduct();
                var firstOffer = OfferService.GetProductOffers(firstProduct.ProductId).FirstOrDefault();
                var cartItem = new ShoppingCartItem()
                {
                    Amount = firstOffer.Amount,
                    OfferId = firstOffer.OfferId
                };
                ShoppingCartService.AddShoppingCartItem(cartItem);
            }
            else
            {
                if (SettingsMain.CheckoutLoginDisplayMode == ECheckoutLoginDisplayMode.Page
                    && CustomerContext.CurrentCustomer.CustomerRole == Role.Guest)
                    return RedirectToAction(
                        "Login",
                        "User",
                        new
                        {
                            from = Url.RouteUrl("Checkout"),
                        }
                    );

                cart = ShoppingCartService.CurrentShoppingCart;
                if (!cart.CanOrder)
                    return RedirectToRoute("Cart");
                if (cart.Any(x => x.Offer.Amount <= 0))//GlorySoft_013
                    return RedirectToRoute("Cart");
                if (CustomerContext.CurrentCustomer == null || !CustomerContext.CurrentCustomer.RegistredUser)//GlorySoft_018
                    return RedirectToRoute("Registration");

                var showConfirmButtons =
                    AttachedModules.GetModules<IShoppingCart>()
                        .Select(t => (IShoppingCart)Activator.CreateInstance(t))
                        .Aggregate(true, (current, module) => current & module.ShowConfirmButtons);

                if (!showConfirmButtons)
                    return RedirectToRoute("Cart");

                if (MobileHelper.IsMobileEnabled() && !SettingsMobile.IsFullCheckout)
                    return Redirect("mobile/checkoutmobile/index");

                WriteLog("", Url.AbsoluteRouteUrl("Checkout"), ePageType.order);
            }

            var model = new GetCheckoutPage().Execute(cart);

            SetNgController(NgControllers.NgControllersTypes.CheckOutCtrl);
            SetMetaInformation(T("Checkout.Index.CheckoutTitle"));
            SetNoFollowNoIndex();
            HttpContext.HiddenBottomPanel();


            if (SettingsMain.CheckoutType == ECheckoutView.Modern || (checkoutType.HasValue && checkoutType.Value == ECheckoutView.Modern))
                return View("Types/CheckoutModern/Index", model);


            return View(model);
        }

        [AccessByChannel(EProviderSetting.ActiveLandingPage)]
        public ActionResult Lp(int? lpId, int? lpUpId, CheckoutLpMode? mode, string products)
        {
            if (!lpId.HasValue && LandingHelper.IsLandingDomain(HttpContext.Request.Url, out int lpIdByDomain))
                lpId = lpIdByDomain;
            if (MobileHelper.IsMobileEnabled())
                MobileHelper.SetMobileSessionCookie();

            if (!string.IsNullOrWhiteSpace(products))
                ShoppingCartService.AddShoppingCartItems(products);

            var cart = ShoppingCartService.CurrentShoppingCart;
            if (lpId.HasValue)
            {
                if (!ShopService.AllowCheckoutNow())
                    return Redirect(new LpService().GetLpLink(lpId.Value) + "?errors=" + SettingsCheckout.NotAllowCheckoutText);
                
                if (!cart.CanOrder)
                    return Redirect(new LpService().GetLpLink(lpId.Value));
            }

            var model = new GetCheckoutPage().Execute(cart, lpId, lpUpId, mode);

            model.IsLanding = true;
            model.ShowMode = CheckoutShowMode.Lp;
            
            SetNgController(NgControllers.NgControllersTypes.CheckOutCtrl);
            SetMetaInformation(T("Checkout.Index.CheckoutTitle"));
            SetNoFollowNoIndex();

            WriteLog("", Url.AbsoluteRouteUrl("Checkout"), ePageType.order);

            return View("Index", "_LayoutEmpty", model);
        }
        
        [ExcludeFilter(typeof(TechDomainGuardAttribute))]
        [AuthUserApi(UseCookie = true)]
        public ActionResult ApiAuth(ApiAuthModel model)
        {
            var cookie = CommonHelper.GetCookie(MobileAppConst.CookieName);
            if (cookie == null || string.IsNullOrEmpty(cookie.Value))
            {
                CommonHelper.SetCookie(MobileAppConst.CookieName, "true", new TimeSpan(90, 0, 0, 0), true);
            }
            
            var customer = CustomerContext.CurrentCustomer;
            if (customer.RegistredUser)
            {
                if (string.IsNullOrEmpty(customer.Password))
                {
                    var password = StringHelper.GeneratePassword(8);
                    customer.Password = SecurityHelper.GetPasswordHash(password);
                    
                    CustomerService.UpdateCustomerPassword(customer.Id, customer.Password);
                }
                
                var isAuth = false;
                
                if (customer.EMail.IsNotEmpty())
                {
                    isAuth = AuthorizeService.SignIn(customer.EMail, customer.Password, true, true);
                }
                else if (customer.StandardPhone != null)
                {
                    isAuth = AuthorizeService.SignInByPhone(customer.StandardPhone, customer.Password, true, true);
                }

                if (!isAuth)
                    return Content("Не удалось авторизоваться");
            }
            else
                CustomerContext.SetCustomerCookie(customer.Id);

            if (model != null && !string.IsNullOrEmpty(model.Type))
            {
                if (model.Type.Equals("billing", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(model.Code) &&
                    !string.IsNullOrEmpty(model.Hash))
                {
                    SettingsDesign.IsEmptyLayout = true;
                    
                    return RedirectToAction("Billing", new {code = model.Code, hash = model.Hash});
                }
            }
            
            return RedirectToAction("Api");
        }

        public ActionResult Api()
        {
            var cart = ShoppingCartService.CurrentShoppingCart;
            if (!cart.CanOrder)
                return Content("В корзине есть товары недоступные к оформлению");

            var model = new GetCheckoutPage().Execute(cart, isMobileApp: true);
            model.IsLanding = true;
            model.IsApi = true;
            model.ShowMode = CheckoutShowMode.EmptyLayout;
            model.OrderType = OrderType.MobileApp;

            SetNgController(NgControllers.NgControllersTypes.CheckOutCtrl);
            SetMetaInformation(T("Checkout.Index.CheckoutTitle"));
            SetNoFollowNoIndex();

            WriteLog("", Url.AbsoluteRouteUrl("Checkout"), ePageType.order);

            return View("Index", "_LayoutEmpty", model);
        }

        // POST: Confirm order
        [HttpPost, ValidateAntiForgeryToken]
        [CaptchaValidation("CaptchaCode", "CaptchaSource")]
        public ActionResult IndexPost(CheckoutPostModel model)
        {
            var currentCustomer = CustomerContext.CurrentCustomer;
            var current = MyCheckout.Factory(currentCustomer.Id);
            var cart = current.Cart;

            if (!model.IsLanding && !cart.CanOrder)
            {
                return RedirectToRoute("Cart");
            }
            
            if (model.IsLanding && !ShopService.AllowCheckoutNow())
            {
                ShowMessage(NotifyType.Error, SettingsCheckout.NotAllowCheckoutText);
                return RedirectToReferrerOnPost("Index");
            }

            if (SettingsMain.CheckoutLoginDisplayMode == ECheckoutLoginDisplayMode.Modal
                && currentCustomer.CustomerRole == Role.Guest)
            {
                ShowMessage(
                    NotifyType.Error,
                    SettingsMain.RegistrationIsProhibited
                        ? T("Checkout.Error.LoginRequired")
                        : T("Checkout.Error.LoginOrRegisterRequired")
                );
                return RedirectToReferrerOnPost("Index");
            }

            if (SettingsMain.EnableCaptchaInCheckout)
            {
                if (!ModelState.IsValidField("CaptchaCode"))
                {
                    ShowMessage(NotifyType.Error, T("Captcha.Wrong"));
                    return RedirectToReferrerOnPost("Index");
                }
                MvcCaptcha.ResetCaptcha("CaptchaSource");
            }

            if (ShoppingCartService.CurrentShoppingCart.GetHashCode() != current.Data.ShopCartHash)
                return RedirectToReferrerOnPost("Index");

            var checkEmail =
                (current.Data.User.WantRegist && SettingsCheckout.IsShowEmail)
                || (currentCustomer.RegistredUser 
                    && SettingsCheckout.IsShowEmail 
                    && SettingsCheckout.IsRequiredEmail 
                    && currentCustomer.EMail.IsNullOrEmpty());

            if (checkEmail)
            {
                if (!ValidationHelper.IsValidEmail(current.Data.User.Email))
                {
                    ShowMessage(NotifyType.Error, T("User.Registration.ErrorCustomerEmailIsWrong"));
                    return RedirectToReferrerOnPost("Index");
                }

                if (!currentCustomer.RegistredUser && CustomerService.GetCustomerByEmail(current.Data.User.Email) != null)
                {
                    ShowMessage(NotifyType.Error, T("User.Registration.ErrorCustomerExist", "forgotpassword"));
                    return RedirectToReferrerOnPost("Index");
                }

                if (currentCustomer.RegistredUser && CustomerService.GetCustomerByEmail(current.Data.User.Email) != null)
                {
                    ShowMessage(NotifyType.Error, T("User.Registration.ErrorCustomerEmailAlreadyExists"));
                    return RedirectToReferrerOnPost("Index");
                }
            }

            //GlorySoft_030
            if (!currentCustomer.RegistredUser && current.Data.User.WantRegist)
            {
                //if (SettingsCheckout.IsShowEmail)
                //{
                //    if (!ValidationHelper.IsValidEmail(current.Data.User.Email))
                //    {
                //        ShowMessage(NotifyType.Error, T("User.Registration.ErrorCustomerEmailIsWrong"));
                //        return RedirectToReferrerOnPost("Index");
                //    }
                //    if (CustomerService.GetCustomerByEmail(current.Data.User.Email) != null)
                //    {
                //        ShowMessage(NotifyType.Error, string.Format(LocalizationService.GetResource("User.Registration.ErrorCustomerExist"), "forgotpassword"));
                //        return RedirectToReferrerOnPost("Index");
                //    }
                //}
                var validUser = IsValidUser(current.Data.User);
                if (validUser.IsNotEmpty())
                {
                    ShowMessage(NotifyType.Error, validUser);
                    return RedirectToReferrerOnPost("Index");
                }
            }
            if (currentCustomer.RegistredUser)
            {
                var validFIO = true;
                if (String.IsNullOrWhiteSpace(current.Data.User.FirstName))
                    validFIO = false;
                if (SettingsCheckout.IsShowLastName && SettingsCheckout.IsRequiredLastName && String.IsNullOrWhiteSpace(current.Data.User.LastName))
                    validFIO = false;
                if (SettingsCheckout.IsShowPatronymic && SettingsCheckout.IsRequiredPatronymic && String.IsNullOrWhiteSpace(current.Data.User.Patronymic))
                    validFIO = false;
                if (!validFIO)
                {
                    ShowMessage(NotifyType.Error, "Заполните все поля ФИО на странице <a href='myaccount#tab=commoninf' target='_blank'>Личные данные</a>");
                    return RedirectToReferrerOnPost("Index");
                }
            }

            if (!currentCustomer.RegistredUser && SettingsMain.RegistrationIsProhibited)
            {
                ShowMessage(NotifyType.Error, LocalizationService.GetResource("Checkout.BuyInOneClick.ErrorRegistrationIsProhibited"));
                return RedirectToReferrerOnPost("Index");
            }

            var valid = current.Data.SelectShipping.Validate();
            if (!valid.IsValid)
            {
                ShowMessage(NotifyType.Error, valid.ErrorMessage);
                return RedirectToReferrerOnPost("Index");
            }

            if (currentCustomer != null)//GlorySoft_030
            {
                //var settings = ModuleService.GetImportExportSettings();
                //var method = ShippingMethodService.GetShippingMethod(current.Data.SelectShipping.MethodId);
                var method = ShippingMethodService.GetShippingParams(current.Data.SelectShipping.MethodId);
                var shippingForTK = method["ShippingForTK"] != null && method["ShippingForTK"].TryParseBool() == true;
                if (currentCustomer.CustomerType != CustomerType.LegalEntity && shippingForTK)
                {
                    if (CustomerFieldService.GetCustomerFieldsWithValue(currentCustomer.Id).Where(x => x.CustomerType == CustomerType.PhysicalEntity && x.Value.IsNullOrEmpty()).Any())
                    {
                        ShowMessage(NotifyType.Error, "Для выбранного способа доставки необходимо заполнить паспортные данные на странице личных данных!");
                        return RedirectToReferrerOnPost("Index");
                    }
                }
                if ((currentCustomer.FirstName != null && current.Data.User.FirstName.ToLower() != currentCustomer.FirstName.ToLower())
                 || (currentCustomer.LastName != null && current.Data.User.LastName.ToLower() != currentCustomer.LastName.ToLower())
                 || (currentCustomer.StandardPhone != null && StringHelper.ConvertToStandardPhone(current.Data.User.Phone) != currentCustomer.StandardPhone))
                {
                    if (shippingForTK)
                    {
                        ShowMessage(NotifyType.Error, "Для выбранного способа доставки недоступна смена получателя!");
                        return RedirectToReferrerOnPost("Index");
                    }
                }
            }

            if (!current.Data.IsValid(out var error))
            {
                ShowMessage(NotifyType.Error, error);
                return RedirectToReferrerOnPost("Index");
            }

            if (cart.Coupon != null)
            {
                if (!currentCustomer.RegistredUser && current.Data.User.Email.IsNotEmpty())
                {
                    var customerByEmail = CustomerService.GetCustomerByEmail(current.Data.User.Email);
                    
                    if (customerByEmail != null 
                        && !CouponService.CanApplyCustomerCoupon(cart.Coupon, customerByEmail.Id))
                    {
                        CouponService.DeleteCustomerCoupon(cart.Coupon.CouponID, currentCustomer.Id);
                        
                        ShowMessage(NotifyType.Error, T("Checkout.CheckoutCart.CouponNotApplied"));
                        
                        return RedirectToReferrerOnPost("Index");
                    }
                }
                
                if (!cart.CouponCanBeApplied || !cart.Any(x => x.IsCouponApplied))
                    cart.Coupon = null;
            }

            var minimumOrderPrice =
                CustomerGroupService.GetMinimumOrderPrice(
                    current.Cart,
                    currentCustomer.CustomerGroupId,
                    currentCustomer.RegistredUser ? currentCustomer.CustomerType : current.Data.User.CustomerType);
            
            if (minimumOrderPrice > 0 && current.Cart.TotalPrice < minimumOrderPrice)
            {
                ShowMessage(NotifyType.Error, string.Format(LocalizationService.GetResource("Cart.Error.MinimalOrderPrice"), minimumOrderPrice.FormatPrice(),
                    (minimumOrderPrice - current.Cart.TotalPrice).FormatPrice()));
                return RedirectToReferrerOnPost("Index");
            }
            var orderCode = "";

            try
            {
                // игнорируем базовый индекс населенного пункта из ipzone, если индекс не выводится в клиентке
                if (!SettingsCheckout.IsShowZip && current.Data.Contact.Zip.IsNotEmpty())//GlorySoft_030
                    current.Data.Contact.Zip = null;

                var allow = 
                    ModulesExecuter.CheckInfo(System.Web.HttpContext.Current, ECheckType.Order,
                    current.Data.User.Email, 
                    current.Data.User.FirstName, 
                    message: current.Data.CustomerComment,
                    phone: current.Data.User.Phone);
                
                if (!allow)
                {
                    ShowMessage(NotifyType.Error, T("Common.SpamCheckFailed"));
                    return RedirectToAction("Index");
                }

                var order = current.ProcessOrder(model.CustomData, model.OrderType, model.IsLanding);
                
                orderCode = order.Code.ToString();
                
                TempData["orderid"] = order.OrderID.ToString();
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                ShowMessage(NotifyType.Error, "Error");
                return RedirectToAction("Index");
            }

            this.ToTempData();

            if (!string.IsNullOrEmpty(LandingHelper.LandingRedirectUrl))
                return Redirect(LandingHelper.LandingRedirectUrl);

            if (current.Data.LpId != null)
                return RedirectToRoute("CheckoutSuccess", new {code = orderCode, showMode = (int) CheckoutShowMode.Lp});

            TrackService.TrackEvent(SettingsDesign.IsMobileTemplate 
                ? ETrackEvent.Core_Orders_OrderCreated_Mobile 
                : ETrackEvent.Core_Orders_OrderCreated_Desktop);

            return RedirectToRoute("CheckoutSuccess",
                new
                {
                    code = orderCode,
                    showMode = model.ShowMode != CheckoutShowMode.None ? (int) model.ShowMode : default(int?)
                });
        }

        // GET: /checkout/success
        public ActionResult Success(string code, string mode, string lid, CheckoutShowMode? showMode)
        {
            this.ToContext();

            if (!string.IsNullOrEmpty(lid))
                return RedirectToAction("BuyInOneClickSuccess", new { id = lid, area = "" });

            if (string.IsNullOrWhiteSpace(code))
                return Error404();

            SetNgController(NgControllers.NgControllersTypes.CheckOutSuccessCtrl);
            SetMetaInformation(T("Checkout.Index.CheckoutTitle"));
            SetNoFollowNoIndex();

            var isLanding = mode == "lp" || showMode == CheckoutShowMode.Lp;
            var emptyLayout = showMode == CheckoutShowMode.EmptyLayout;

            var tempOrderId = TempData["orderid"] != null ? TempData["orderid"].ToString() : string.Empty;

            var order = OrderService.GetOrderByCode(code);
            if (order == null || (order.OrderID.ToString() != tempOrderId && !isLanding && !emptyLayout))
                return View("OrderComplete", new CheckoutSuccess() { IsEmptyLayout = isLanding, IsLanding = isLanding });

            var model = new CheckoutSuccessHandler().Get(order, isLanding, showMode);

            TrialService.TrackEvent(TrialEvents.CheckoutOrder, order.OrderID.ToString());

            WriteLog("", Url.AbsoluteRouteUrl("CheckoutSuccess"), ePageType.purchase);
            HttpContext.HiddenBottomPanel();

            return View("Success", model.IsEmptyLayout ? "_LayoutEmpty" : "_Layout", model);
        }

        public JsonResult GetPaymentInfo(int orderId)
        {
            var order = OrderService.GetOrder(orderId);
            var confirmed = !SettingsCheckout.ManagerConfirmed || order.ManagerConfirmed;
            var processType = order?.PaymentMethod?.ProcessType ?? ProcessType.None;
            var availablePay = order != null && !order.Payed && !order.OrderStatus.IsCanceled;

            var willProceedToPayment = 
                SettingsCheckout.ProceedToPayment 
                && confirmed 
                && availablePay 
                && processType != ProcessType.None;
            
            // костыль для счетов
            if (willProceedToPayment
               && processType == ProcessType.Javascript
               && (order?.PaymentMethod?.PaymentKey == AttributeHelper.GetAttributeValue<PaymentKeyAttribute, string>(typeof(SberBank))
                   || order?.PaymentMethod?.PaymentKey == AttributeHelper.GetAttributeValue<PaymentKeyAttribute, string>(typeof(Bill))
                   || order?.PaymentMethod?.PaymentKey == AttributeHelper.GetAttributeValue<PaymentKeyAttribute, string>(typeof(BillUa))
                   || order?.PaymentMethod?.PaymentKey == AttributeHelper.GetAttributeValue<PaymentKeyAttribute, string>(typeof(BillBy))
                   || order?.PaymentMethod?.PaymentKey == AttributeHelper.GetAttributeValue<PaymentKeyAttribute, string>(typeof(BillKz))
                   || order?.PaymentMethod?.PaymentKey == AttributeHelper.GetAttributeValue<PaymentKeyAttribute, string>(typeof(Check))))
            {
                willProceedToPayment = false;
            }
            
            return Json(new
            {
                proceedToPayment = SettingsCheckout.ProceedToPayment,
                willProceedToPayment = willProceedToPayment
            });
        }

        [ChildActionOnly]
        public ActionResult CheckoutUser(bool? isLanding, bool? isApi)
        {
            return PartialView(new CheckoutUserHandler(isLanding, isApi).Execute());
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult GetCheckoutUser()
        {
            return JsonOk(new CheckoutUserHandler(false, false).Execute());
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutUserPost(CheckoutUser customer)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
           current.Data.User.Email =
                !string.IsNullOrEmpty(customer.Email)
                    ? StringHelper.HtmlEncode(customer.Email)
                    : (CustomerContext.CurrentCustomer.RegistredUser ? CustomerContext.CurrentCustomer.EMail : null);
            current.Data.User.FirstName = StringHelper.HtmlEncode(customer.FirstName);
            current.Data.User.LastName = StringHelper.HtmlEncode(customer.LastName);
            current.Data.User.Patronymic = StringHelper.HtmlEncode(customer.Patronymic);
            current.Data.User.Phone = StringHelper.HtmlEncode(customer.Phone);
            current.Data.User.WantRegist = customer.WantRegist;
            current.Data.User.Password = customer.Password;
            current.Data.User.CustomerFields = customer.CustomerFields;
            current.Data.User.BirthDay = customer.BirthDay;
            current.Data.User.CustomerType = customer.CustomerType;
            current.Data.User.IsAddRecipient = customer.IsAddRecipient;
            current.Data.User.RecipientFirstName = customer.RecipientFirstName;
            current.Data.User.RecipientLastName = customer.RecipientLastName;
            current.Data.User.RecipientPatronymic = customer.RecipientPatronymic;
            current.Data.User.RecipientPhone = customer.RecipientPhone;
            current.Data.User.Confirm = customer.Confirm;//GlorySoft_007
            current.Update();

            return Json(true);
        }
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutUserRequiredFieldsPost(CheckoutUser customer)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
           
            if (SettingsCheckout.IsShowLastName && SettingsCheckout.IsRequiredLastName && customer.LastName.IsNotEmpty())
                current.Data.User.LastName = StringHelper.HtmlEncode(customer.LastName);
            
            if (SettingsCheckout.IsShowPatronymic && SettingsCheckout.IsRequiredPatronymic && customer.Patronymic.IsNotEmpty())
                current.Data.User.Patronymic = StringHelper.HtmlEncode(customer.Patronymic);
            
            if (SettingsCheckout.IsShowPhone && SettingsCheckout.IsRequiredPhone && customer.Phone.IsNotEmpty())
                current.Data.User.Phone = StringHelper.HtmlEncode(customer.Phone);
            
            if (SettingsCheckout.IsShowBirthDay && SettingsCheckout.IsRequiredBirthDay && customer.BirthDay != null)
                current.Data.User.BirthDay = customer.BirthDay;
            
            if (SettingsCheckout.IsShowEmail && SettingsCheckout.IsRequiredEmail && customer.Email.IsNotEmpty())
                current.Data.User.Email = HttpUtility.HtmlEncode(customer.Email);
            
            current.Update();

            return Json(true);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutRecipientPost(CheckoutUser customer)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.User.IsAddRecipient = customer.IsAddRecipient;
            current.Data.User.RecipientFirstName = customer.RecipientFirstName;
            current.Data.User.RecipientLastName = customer.RecipientLastName;
            current.Data.User.RecipientPatronymic = customer.RecipientPatronymic;
            current.Data.User.RecipientPhone = customer.RecipientPhone;
            current.Update();

            return Json(true);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SaveWantBonusCard(bool wantBonusCard)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.User.WantBonusCard = wantBonusCard;
            current.Update();

            return Json(true);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutProcessContactPost(CheckoutAddressQueryModel address)
        {
            if (address == null || address.City.IsNullOrEmpty())
                return JsonError();

            IpZone zone;
            if ((address.ByCity || address.Region.IsNullOrEmpty()) && (zone = IpZoneService.GetZoneByCity(address.City, null)) != null)
            {
                address.District = zone.District;
                address.Region = zone.Region;
                address.Country = zone.CountryName;
                address.Zip = zone.Zip;
            }
            ModulesExecuter.ProcessCheckoutAddress(address);

            return JsonOk(address);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutContactPost(CheckoutAddress address)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            // игнорируем базовый индекс населенного пункта из ipzone, если индекс не выводится в клиентке
            if (!SettingsCheckout.IsShowZip && address.Zip.IsNotEmpty())
                address.Zip = null;

            if (current.Data.Contact == address)
                return Json(true);

            current.Data.Contact = address;
            current.Update();

            return Json(true);
        }
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SetCalculationVariants([ModelBinder(typeof(Models.CalculationVariantsModelBinder))] CalculationVariants? typeCalculationVariants)
        {
            return ProcessJsonResult(() =>
            {
                var current = MyCheckout.Factory(CustomerContext.CustomerId);
                if (current.Data.TypeCalculationVariants != typeCalculationVariants)
                {
                    current.Data.TypeCalculationVariants = typeCalculationVariants;
                    current.Update();
                }
            });
        }


        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutShippingJson( 
            List<PreOrderItem> preorderList = null,
            [ModelBinder(typeof(Models.CalculationVariantsModelBinder))] CalculationVariants? typeCalculationVariants = null,
            bool allowChangeSelectedShipping = true)
        {
            return Json(new GetCheckoutShippings(preorderList, typeCalculationVariants, allowChangeSelectedShipping).Execute());
        }
        

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutShippingPost(
            BaseShippingOption shipping, 
            List<PreOrderItem> preorderList = null,
            [ModelBinder(typeof(Models.CalculationVariantsModelBinder))] CalculationVariants? typeCalculationVariants = null)
        {
            var warehouseId = shipping.GetWarehouseId();
            var warehouseIds = warehouseId != null ? new List<int>() { warehouseId.Value } : null;

            WarehouseContext.CurrentWarehouseIds = warehouseIds;
            WarehouseService.TryUpdateWarehouseCookieByShipping(shipping);
            
            var current = MyCheckout.Factory(CustomerContext.CustomerId);

            if (!current.Data.HideShippig)
                current.UpdateSelectShipping(preorderList, shipping, typeCalculationVariants: typeCalculationVariants, warehouseId: warehouseId);

            return Json(new { selectShipping = current.Data.SelectShipping });
        }

        // Shipping lazy load
        public JsonResult GetShippingData(int methodId, Dictionary<string, object> data)
        {
            var shippingMethod = ShippingMethodService.GetShippingMethod(methodId);
            if (shippingMethod == null)
                return Json(null);

            var type = ReflectionExt.GetTypeByAttributeValue<Core.Common.Attributes.ShippingKeyAttribute>(typeof(BaseShipping), atr => atr.Value, shippingMethod.ShippingType);
            if (!type.GetInterfaces().Contains(typeof(IShippingLazyData)))
                return Json(null);

            var shipping = (BaseShipping)Activator.CreateInstance(type, shippingMethod, null);
            return Json(((IShippingLazyData)shipping).GetLazyData(data));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutPaymentJson(List<PreOrderItem> preorderList = null)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            var options = current.AvailablePaymentOptions(preorderList);

            var cookiePayment = CommonHelper.GetCookie("payment");
            if (cookiePayment != null && !string.IsNullOrEmpty(cookiePayment.Value))
            {
                var paymentId = cookiePayment.Value.TryParseInt();
                current.Data.SelectPayment = options.FirstOrDefault(x => x.Id == paymentId);
                CommonHelper.DeleteCookie("payment");
            }

            current.UpdateSelectPayment(preorderList, current.Data.SelectPayment, options);
            return Json(new { selectPayment = current.Data.SelectPayment, option = options });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutPaymentPost(BasePaymentOption payment, List<PreOrderItem> preorderList = null)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            var temp = current.UpdateSelectPayment(preorderList, payment);
            return Json(temp);
        }

        [ChildActionOnly]
        public ActionResult CheckoutShippingAddress()
        {
            var customer = CustomerContext.CurrentCustomer;

            var hasAddresses = customer.Contacts.Count > 0 && !string.IsNullOrEmpty(customer.Contacts[0].Street);
            var hasCustomShippingFields = SettingsCheckout.IsShowCustomShippingField1 ||
                                          SettingsCheckout.IsShowCustomShippingField2 ||
                                          SettingsCheckout.IsShowCustomShippingField3 ||/*GlorySoft_032*/ true;

            //if (hasAddresses && !hasCustomShippingFields)GlorySoft_032
            //    return new EmptyResult();

            if (!hasCustomShippingFields &&
                (!SettingsCheckout.IsShowAddress || hasAddresses) &&
                (!SettingsCheckout.IsShowZip || SettingsCheckout.ZipDisplayPlace))
                return new EmptyResult();

            var current = MyCheckout.Factory(CustomerContext.CustomerId);

            //GlorySoft_032
            var contact = current.Data.Contact;
            if (hasAddresses)
            {
                var c = customer.Contacts[0];
                if (c.City == contact.City)
                {
                    contact.Street = c.Street;
                    contact.House = c.House;
                    contact.Structure = c.Structure;
                    contact.Apartment = c.Apartment;
                    contact.Entrance = c.Entrance;
                    contact.Floor = c.Floor;
                }
            }

            var model = new CheckoutShippingAddressViewModel()
            {
                AddressContact = contact/*GlorySoft_032 current.Data.Contact*/,
                HasAddresses = hasAddresses,
                HasCustomShippingFields = hasCustomShippingFields
            };

            return PartialView(model);
        }

        [ChildActionOnly]
        public ActionResult CheckoutBonus()
        {
            var model = new CheckoutBonusHandler().Execute();
            if (model == null)
                return new EmptyResult();

            if (!model.HasCard && !CustomerContext.CurrentCustomer.RegistredUser)
                return new EmptyResult();

            return PartialView(model);
        }

        public JsonResult CheckoutBonusAutorizePost(string cardNumber)
        {
            if (!BonusSystem.IsActive)
                return Json(false);

            var card = BonusSystem.GetCardByNumber(cardNumber);
            if (card != null)
            {
                var current = MyCheckout.Factory(CustomerContext.CustomerId);
                current.Data.User.BonusCardNumber = card.Number;
                current.Update();
            }
            return Json(true);
        }

        public JsonResult CheckoutBonusApplyPost(float appliedBonuses)
        {
            if (!BonusSystem.IsActive)
                return Json(new { result = false });

            appliedBonuses = PriceService.GetFinalPrice(appliedBonuses);
            if (appliedBonuses > 0 
                && BonusSystem.IsInternal
                && InternalBonusSystem.ForbidOnCoupon 
                && ShoppingCartService.CurrentShoppingCart.Coupon != null)
            {
                return Json(new { result = false, msg = T("Checkout.Checkout.BonusNotAppliedWithCoupon"), appliedBonuses = 0 });// "Нельзя применить бонусы при использование купона" });
            }

            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.Bonus = current.Data.Bonus ?? new CheckoutBonus();

            var bonusApply = BonusSystem.GetApplyBonuses(current.Cart, current.Data.SelectShipping?.FinalRate ?? 0, current.Data.SelectPayment?.Rate ?? 0, appliedBonuses);

            if (appliedBonuses <= bonusApply)
                current.Data.Bonus.AppliedBonuses = appliedBonuses;
            else if (!BonusSystem.IsInternal
                    || !InternalBonusSystem.AllowSpecifyBonusAmount)
                current.Data.Bonus.AppliedBonuses = bonusApply;
            else
                return Json(new { result = false, msg = T("Checkout.Checkout.CannotChargeMoreOrderAmount"), appliedBonuses = current.Data.Bonus.AppliedBonuses });
            current.Update();
            return Json(new { result = true, appliedBonuses = current.Data.Bonus.AppliedBonuses });
        }

        [ChildActionOnly]
        public ActionResult CheckoutCoupon()
        {
            var show = SettingsCheckout.DisplayPromoTextbox &&
                (CustomerContext.CurrentCustomer.CustomerGroupId == CustomerGroupService.DefaultCustomerGroup || SettingsCheckout.EnableGiftCertificateService);

            if (!show)
                return new EmptyResult();

            return PartialView();
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public ActionResult CheckoutCouponApplied()
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);

            //if (current.Data.Bonus.UseIt && BonusSystem.ForbidOnCoupon)
            //{
            //    return Json("Нельзя применить купон при использование бонусов");
            //}

            current.Data.ShopCartHash = ShoppingCartService.CurrentShoppingCart.GetHashCode();
            current.Update();

            return Json(true);
        }

        public JsonResult CheckoutCartJson()
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);

            var shippingPrice = current.Data.SelectShipping != null ? current.Data.SelectShipping.FinalRate : 0;
            var paymentCost = current.Data.SelectPayment != null ? current.Data.SelectPayment.Rate : 0;
            var currency = CurrencyService.CurrentCurrency;

            var model = new CheckoutCartHandler().Get(current.Data, current.Cart, shippingPrice, paymentCost, currency);

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CommentPost(string message)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.CustomerComment = StringHelper.HtmlEncode(message);
            current.Update();
            return Json(true);
        }
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SaveAgreementForNewsletter(bool isAgreeForPromotionalNewsletter)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.User.IsAgreeForPromotionalNewsletter = isAgreeForPromotionalNewsletter;
            current.Update();

            return Json(true);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SaveDontCallBack(bool dontCallBack)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.DontCallBack = dontCallBack;
            current.Update();

            return Json(true);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SaveCountDevices(int countDevices)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.CountDevices = countDevices;
            current.Update();

            return Json(true);
        }

        #region Attachments

        [HttpGet]
        public ActionResult GetCheckoutFile(int attachmentId)
        {
            var attachment = AttachmentService.GetAttachment<CheckoutAttachment>(attachmentId);
            if (attachment == null)
                return Error404();

            if (attachment.ObjId != CustomerContext.CurrentCustomer.Id.GetHashCode())
                return Error404();

            if (!new FileExtensionContentTypeHelper().TryGetContentType(attachment.Path, out var contentType))
                contentType = "text/plain";

            if (System.IO.File.Exists(attachment.PathAbsolut))
                return File(attachment.PathAbsolut, contentType, attachment.OriginFileName.IsNullOrEmpty() ? attachment.FileName : attachment.OriginFileName);
            return Error404();
        }


        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult UploadAttachments()
        {
            if (!SettingsCheckout.AllowUploadFiles)
                return JsonError(T("Checkout.Attachments.ProhibitUploadFiles"));
            return ProcessJsonResult(new UploadAttachmentsWithResizePhotoHandler<CheckoutAttachment>(CustomerContext.CurrentCustomer.Id.GetHashCode(), SettingsCheckout.CheckoutImageWidth, SettingsCheckout.CheckoutImageHeight));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteAttachment(int id)
        {
            if (id == 0)
                return Json(false);

            var attachment = AttachmentService.GetAttachment<CheckoutAttachment>(id);
            if (attachment == null)
                return Json(true);

            if (attachment.ObjId != CustomerContext.CurrentCustomer.Id.GetHashCode())
                return Json(false);

            return Json(AttachmentService.DeleteAttachment<CheckoutAttachment>(id));
        }

        public JsonResult GetCheckoutAttachments()
        {
            return Json(new
            {
                Attachments = AttachmentService.GetAttachments<CheckoutAttachment>(CustomerContext.CurrentCustomer.Id.GetHashCode())
                .Select(x => new AttachmentModel
                {
                    Id = x.Id,
                    ObjId = x.ObjId,
                    FileName = x.OriginFileName.IsNullOrEmpty() ? x.FileName : x.OriginFileName
                }),
                FilesHelpText = FileHelpers.GetFilesHelpText(AttachmentService.FileTypes[AttachmentType.Checkout], "15MB"),
                AllowedFileExtensions = FileHelpers.GetAllowedFileExtensions(AttachmentService.FileTypes[AttachmentType.Checkout]).AggregateString(",")
            });
        }

        #endregion

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult ReceivingMethodSave(EnTypeOfReceivingMethod? receivingMethod)
        {
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.ReceivingMethod = receivingMethod;
            current.Update();

            return Json(true);
        }

        #region Cart

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult UpdateCart(Dictionary<int, float> items)
        {
            if (items == null)
                return Json(new { status = "fail" });

            var cart = ShoppingCartService.CurrentShoppingCart;

            foreach (var pair in items)
            {
                var cartItem = cart.Find(x => x.ShoppingCartItemId == pair.Key);

                if (cartItem == null || pair.Value <= 0)
                    return Json(new { status = "fail" });

                if (cartItem.FrozenAmount)
                    continue;

                if (ShoppingCartService.ShoppingCartHasProductsWithGifts())
                    ProductGiftService.UpdateGiftByOfferToCart(cart, cartItem.Offer, items);

                cartItem.Amount = pair.Value;
                ShoppingCartService.UpdateShoppingCartItem(cartItem);
            }
            var current = MyCheckout.Factory(CustomerContext.CustomerId); 
            current.Data.ShopCartHash = cart.GetHashCode();
            current.Update();

            return Json(new { ShoppingCartService.CurrentShoppingCart.TotalItems, status = "success" });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult RemoveFromCart(int itemId)
        {
            if (itemId == 0)
                return Json(new { status = "fail" });

            var cart = ShoppingCartService.CurrentShoppingCart;

            var cartItem = cart.Find(item => item.ShoppingCartItemId == itemId);
            if (cartItem != null)
            {
                if (cart.Any(x => x.IsGift))
                    ProductGiftService.RemoveGiftByOfferToCart(cart, itemId, cartItem.Offer);

                ShoppingCartService.DeleteShoppingCartItem(cartItem);
            }

            cart.Remove(cartItem);
            var current = MyCheckout.Factory(CustomerContext.CustomerId);
            current.Data.ShopCartHash = cart.GetHashCode();
            current.Update();

            return Json(new
            {
                cart.TotalItems,
                status = "success",
                offerId = cartItem != null ? cartItem.OfferId : 0
            });
        }

        #endregion

        #endregion Checkout

        #region GeoMode 

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult GetGeoModeDeliveries([ModelBinder(typeof(Models.CalculationVariantsModelBinder))] CalculationVariants? typeCalculationVariants)
        {
            var model = new GetGeoModeDeliveries(typeCalculationVariants).Execute();
            
            return Json(model);
        }
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SetCourierDeliveryGeoMode(Guid contactId)
        {
            return JsonOk(new SetCourierDeliveryGeoMode(contactId).Execute());
        }
        
        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult SetGeoModePoint(int shippingMethodId, string pointId)
        {
            return JsonOk(new SetPickPointGeoMode(shippingMethodId, pointId).Execute());
        }
  
        #endregion

        #region Buy in one click

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutBuyInOneClick(BuyOneInClickJsonModel model)
        {
            if (SettingsMain.EnableCaptchaInBuyInOneClick &&
                !MvcCaptcha.Validate("CaptchaSourceBuyInOneClick", model.CaptchaCode, model.CaptchaSource))
            {
                return Json(new BuyOneClickResult { error = T("Checkout.CheckoutBuyInOneClick.CapchaWrong") });
            }

            var allow = ModulesExecuter.CheckInfo(System.Web.HttpContext.Current, ECheckType.Order, model.Email, model.Name, message: model.Comment, phone: model.Phone);
            if (!allow)
                return Json(new BuyOneClickResult { error = T("Common.SpamCheckFailed") });

            var returnModel = new BuyInOneClickHandler(model).Create();
            if (returnModel != null)
                TempData["orderid"] = returnModel.orderId;

            MvcCaptcha.ResetCaptcha("CaptchaSourceBuyInOneClick");

            return Json(returnModel);
        }

        public JsonResult CheckoutBuyInOneClickFields()
        {
            var suggestionsModule = AttachedModules.GetModules<ISuggestions>().Select(x => (ISuggestions)Activator.CreateInstance(x)).FirstOrDefault();

            var obj = new
            {
                SettingsCheckout.IsShowBuyInOneClickName,
                SettingsCheckout.IsShowBuyInOneClickEmail,
                SettingsCheckout.IsShowBuyInOneClickPhone,
                SettingsCheckout.IsShowBuyInOneClickComment,
                SettingsCheckout.BuyInOneClickName,
                SettingsCheckout.BuyInOneClickEmail,
                SettingsCheckout.BuyInOneClickPhone,
                SettingsCheckout.BuyInOneClickComment,
                SettingsCheckout.IsRequiredBuyInOneClickName,
                SettingsCheckout.IsRequiredBuyInOneClickEmail,
                SettingsCheckout.IsRequiredBuyInOneClickPhone,
                SettingsCheckout.IsRequiredBuyInOneClickComment,
                BuyInOneClickFinalText = T("Checkout.BuyInOneClickFinalText"),
                SettingsCheckout.BuyInOneClickFirstText,
                SettingsCheckout.BuyInOneClickButtonText,
                SettingsCheckout.BuyInOneClickLinkText,
                SettingsCheckout.IsShowUserAgreementText,
                SettingsCheckout.UserAgreementText,
                SettingsCheckout.AgreementDefaultChecked,
                UseFullNameSuggestions = suggestionsModule != null && suggestionsModule.SuggestFullNameInClient,
                SuggestFullNameUrl = suggestionsModule != null ? suggestionsModule.SuggestFullNameUrl : null,
                SettingsMain.EnableCaptchaInBuyInOneClick,
                CaptchaCode = T("Captcha.Code"),
                ShowUserAgreementForPromotionalNewsletter = SettingsDesign.ShowUserAgreementForPromotionalNewsletter 
                                                            && !CustomerContext.CurrentCustomer.IsAgreeForPromotionalNewsletter,
                SettingsDesign.SetUserAgreementForPromotionalNewsletterChecked,
                SettingsDesign.UserAgreementForPromotionalNewsletter
            };

            return Json(obj);
        }

        public JsonResult CheckoutBuyInOneClickCustomer()
        {
            var customer = CustomerContext.CurrentCustomer;

            return Json(customer != null
                ? new
                {
                    name = string.Join(" ", new[] { customer.FirstName, customer.LastName }.Where(x => x.IsNotEmpty())),
                    email = customer.EMail,
                    phone = customer.Phone
                }
                : new
                {
                    name = "",
                    email = "",
                    phone = ""
                });
        }

        public ActionResult BuyInOneClickSuccess(int id)
        {
            var lead = LeadService.GetLead(id);
            if (lead == null)
                return Error404();

            var model = new BuyInOneClickSuccess() { Lead = lead };

            SetMetaInformation(T("Checkout.BuyInOneClickSuccess.CheckoutTitle"));
            SetNoFollowNoIndex();

            return View(model);
        }

        #endregion Buy in one click

        #region Print Order

        public ActionResult PrintOrder(PrintOrderModel printOrder)
        {
            if (string.IsNullOrWhiteSpace(printOrder.Code))
                return Error404();

            var order = OrderService.GetOrderByCode(printOrder.Code);
            if (order == null)
                return Error404();

            var model = new PrintOrderHandler(order, printOrder).Execute();

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        #endregion Print Order

        #region Billing page

        public ActionResult Billing(string code, string number, string hash, string payCode)
        {
            if (((String.IsNullOrWhiteSpace(code) && String.IsNullOrWhiteSpace(number)) || String.IsNullOrWhiteSpace(hash)) &&
                String.IsNullOrWhiteSpace(payCode))
                return Error404();

            Order order = null;
            if (payCode.IsNotEmpty())
                order = OrderService.GetOrderByPayCode(payCode);
            else
            {
                if (code.IsNotEmpty())
                    order = OrderService.GetOrderByCode(code);
                if (number.IsNotEmpty() && order == null) //для старых ссылок на оплату, которы по номеру открывались
                    order = OrderService.GetOrderByNumber(number);
                if (order != null && hash != OrderService.GetBillingLinkHash(order))
                    return Error404();
            }

            if (order == null || order.IsDraft)
                return Error404();

            //GlorySoft_026
            var paymentUrl = OrderService.GetOrderPay(order, order.PaymentMethodId);
            if (paymentUrl.IsNotEmpty())
            {
                return Redirect(paymentUrl);
            }

            var model = new BillingViewModel
            {
                Order = order,
                IsMobile = SettingsDesign.IsMobileTemplate,
                IsMobileApp = Request.IsMobileApp(),
                Header =
                    T("Checkout.Billing.BillingTitle") + " " + order.Number +
                    (order.Payed
                        ? " - " + T("Core.Orders.Order.OrderPaied")
                        : order.OrderStatus.IsCanceled ? " - " + T("Core.Crm.LeadStatus.NotClosedDeal") : "")
            };

            SetNgController(NgControllers.NgControllersTypes.BillingCtrl);
            SetMetaInformation(model.Header);
            SetNoFollowNoIndex();

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult BillingPaymentJson(int orderId)
        {
            var order = OrderService.GetOrder(orderId);
            if (order == null)
                return Json("error");

            return Json(new GetBillingPayment(order).Execute());
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult BillingPaymentPost(BasePaymentOption payment, int orderId)
        {
            var order = OrderService.GetOrder(orderId);

            if (order == null || order.Payed || order.OrderStatus.IsCanceled)
                return Json(null);

            var paymentMethod = PaymentService.GetPaymentMethod(payment.Id);
            if (paymentMethod == null)
                return Json(null);

            order.PaymentMethodId = paymentMethod.PaymentMethodId;
            order.ArchivedPaymentName = paymentMethod.Name;
            order.PaymentCost = paymentMethod.GetExtracharge(order);

            order.PaymentDetails = payment.GetDetails(order);

            OrderService.UpdatePaymentDetails(order.OrderID, order.PaymentDetails);
            OrderService.UpdateOrderMain(order);

            OrderService.RefreshTotal(order);
            order = OrderService.GetOrder(order.OrderID);

            return Json(new { proceedToPayment = SettingsCheckout.ProceedToPayment });
        }

        public JsonResult BillingCartJson(int orderId)
        {
            var order = OrderService.GetOrder(orderId);
            if (order == null)
                return Json("error");

            var model = new BillingCartHandler().Get(order);

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        #endregion Billing page

        #region Check order

        [ChildActionOnly]
        public ActionResult CheckOrderBlock()
        {
            if (!SettingsDesign.CheckOrderVisibility)
                return new EmptyResult();

            return PartialView();
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckOrder(string ordernumber)
        {
            if (string.IsNullOrEmpty(ordernumber))
                return Json(null);
            
            var statusInfo = OrderService.GetStatusInfo(ordernumber);
            if (statusInfo == null)
                return Json(new CheckOrderModel() {Error = T("Checkout.CheckOrder.StatusCommentNotFound")});

            var order = OrderService.GetOrderByNumber(ordernumber);
            if (order == null)
                return Json(new CheckOrderModel() {Error = T("Checkout.CheckOrder.StatusCommentNotFound")});

            var model = new CheckOrderModel
            {
                StatusName = statusInfo.Hidden ? statusInfo.PreviousStatus : statusInfo.Status,
                StatusComment = statusInfo.Comment,
                OrderNumber = order.Number,
                ShippingHistory = new GetShippingHistoryAndPointInfoHandler(order).Get()
            };

            return Json(model);
        }

        #endregion Check order

        #region Address

        public ActionResult AddressModal()
        {
            return PartialView(new CheckoutAddressViewModel());
        }

        #endregion Address

        #region ThankYouPage

        [ChildActionOnly]
        public ActionResult ThankYouPage(int? orderId, int? leadId)
        {
            var model = new ThankYouPageHandler(orderId, leadId).Execute();
            if (model == null)
                return new EmptyResult();

            return PartialView(model);
        }

        #endregion ThankYouPage

        #region DDeivery crutch

        //ToDo: переписать под IShippingLazyData
        public ActionResult DdeliveryRequest(int id, bool nppOption, string url, Dictionary<string, string> data)
        {
            var ddeliveryShipping = ShippingMethodService.GetShippingMethod(id);

            if (ddeliveryShipping == null)
                return new HttpUnauthorizedResult();

            var apiKey = ddeliveryShipping.Params.ElementOrDefault(Shipping.DDelivery.DDeliveryTemplate.ApiKey);
            var token = ddeliveryShipping.Params.ElementOrDefault(Shipping.DDelivery.DDeliveryTemplate.Token);
            var shopId = ddeliveryShipping.Params.ElementOrDefault(Shipping.DDelivery.DDeliveryTemplate.ShopId);
            url = url.Replace(":key", apiKey);

            var urlParams = string.Empty;
            if (url.Contains("calculator.json"))
            {
                var index = 0;
                foreach (var key in Request.QueryString.AllKeys)
                {
                    if (key == "data[apply_sdk_settings]")
                    {
                        urlParams += "apply_sdk_settings=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[city_to]")
                    {
                        urlParams += "city_to=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[item_count]")
                    {
                        urlParams += "item_count=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[side1]")
                    {
                        urlParams += "side1=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[side2]")
                    {
                        urlParams += "side2=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[side3]")
                    {
                        urlParams += "side3=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[weight]")
                    {
                        urlParams += "weight=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[price_declared]")
                    {
                        urlParams += "price_declared=" + Request.QueryString[key] + "&";
                        if (nppOption)
                        {
                            urlParams += "price_payment=" + Request.QueryString[key] + "&";
                            urlParams += "is_payment=1&";
                        }
                    }
                    /////////////////////////////////////////////////////
                    if (key == "data[products][" + index + "][price_declared]")
                    {
                        urlParams += "[products][" + index + "][price_declared]=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[products][" + index + "][count]")
                    {
                        urlParams += "[products][" + index + "][count]=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[products][" + index + "][discount]")
                    {
                        urlParams += "[products][" + index + "][discount]=" + Request.QueryString[key] + "&";
                    }
                    if (key == "data[products][" + index + "][nds]")
                    {
                        urlParams += "[products][" + index + "][nds]=" + Request.QueryString[key] + "&";
                        index++;
                    }
                }

                //foreach (KeyValuePair<string, string> pair in data)
                //{
                //    urlParams += pair.Key + "=" + pair.Value + "&";
                //    // do something with entry.Value or entry.Key
                //}
            }
            else if (url.Contains("index-by-address.json") || url.Contains("phone.json"))
            {
                foreach (var keyValue in data)
                {
                    urlParams += keyValue.Key + "=" + keyValue.Value + "&";
                }
            }

            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + token },
                { "shop-id", shopId }
            };
            var result = Core.Services.Helpers.RequestHelper.MakeRequest<string>(url + "?" + urlParams, headers: headers, method: Core.Services.Helpers.ERequestMethod.GET);
            //var result = RequestHelper.MakeRequest<string>(url, method: ERequestMethod.GET);
            return Content(result);

            //var dataString = Request.QueryString.ToString().Contains("data")
            //    ? //"data[apply_sdk_settings]=1&data[city_to]=151184&data[stopSubmit]=true&data[userFullName]=&data[userPhone]=&data[itemCount]=1&data[width]=10.0&data[height]=15.0&data[length]=5.0&data[weight]=0.3&data[nppOption]=false&data[products][0][name]=Суши Хамачи&data[products][0][vendorCode]=10500&data[products][0][price]=1000.0&data[products][0][count]=1"//Request.QueryString.ToString().Remove(0, Request.QueryString.ToString().IndexOf("data"))
            //    "data[apply_sdk_settings]=1&data[city_to]=151184&data[products][0][price_declared]=500&data[products][0][count]=1&data[products][0][discount]=0&data[products][0][nds]=0&data[price_declared]=0"
            //    : Request.QueryString.ToString();

            //var result = RequestHelper.MakeRequest<string>(url, System.Web.HttpUtility.UrlEncode(dataString), method: Request.HttpMethod.ToLower() == "get" ? ERequestMethod.GET : ERequestMethod.POST, contentType: ERequestContentType.FormUrlencoded);

            //return Content(result);
        }

        #endregion DDeivery crutch

        #region Pay

        public ActionResult PayRedirect(string code)
        {
            var order = OrderService.GetOrderByCode(code);

            var isEmptyLayout = order != null &&
                                order.OrderSource != null && 
                                order.OrderSource.Type == OrderType.MobileApp;
            
            var layout = isEmptyLayout ? "_LayoutEmpty" : "_Layout";
            
            if (order == null || order.Payed)
                return View("PayRedirectError", layout, T("Checkout.PayRedirectError.OrderNotFound"));

            if (order.PaymentMethod == null || order.PaymentMethod.ProcessType != ProcessType.ServerRequest)
                return View("PayRedirectError", layout, T("Checkout.PayRedirectError.PaymentMethodNotFound"));

            var shouldBeConfirmedByManager = SettingsCheckout.ManagerConfirmed && !order.ManagerConfirmed;
            if (shouldBeConfirmedByManager)
                return View("PayRedirectError", layout, T("Checkout.PayRedirectError.NotConfirmedByManager"));

            var url = order.PaymentMethod.ProcessServerRequest(order);
            if (string.IsNullOrEmpty(url))
                return View("PayRedirectError", layout, T("Checkout.PayRedirectError.LinkNotAvailable"));

            return Redirect(url);
        }
        
        public ActionResult PayRedirectForMokka(string code)
        {
            var order = OrderService.GetOrderByCode(code);
            
            var isEmptyLayout = order != null &&
                                order.OrderSource != null && 
                                order.OrderSource.Type == OrderType.MobileApp;
            
            var layout = isEmptyLayout ? "_LayoutEmpty" : "_Layout";
            
            if (order == null || order.Payed)
                return View("PayRedirectErrorMokka", layout, T("Checkout.PayRedirectError.OrderNotFound"));

            if (order.PaymentMethod == null || !(order.PaymentMethod is Mokka))
                return View("PayRedirectErrorMokka", layout, T("Checkout.PayRedirectError.PaymentMethodNotFound"));

            var shouldBeConfirmedByManager = SettingsCheckout.ManagerConfirmed && !order.ManagerConfirmed;
            if (shouldBeConfirmedByManager)
                return View("PayRedirectErrorMokka", layout, T("Checkout.PayRedirectError.NotConfirmedByManager"));

            var url = order.PaymentMethod.ProcessServerRequest(order);
            if (string.IsNullOrEmpty(url))
                return View("PayRedirectErrorMokka", layout, T("Checkout.PayRedirectError.LinkNotAvailable"));

            return Redirect(url);
        }

        public ActionResult GetOrderPay(Models.Checkout.OrderPayModel model)
        {
            var order = 
                model.OrderCode.IsNotEmpty() 
                    ? OrderService.GetOrderByCode(model.OrderCode) 
                    : null;
            // order = order ??
            //         (model.OrderId.HasValue
            //             ? OrderService.GetOrder(model.OrderId.Value)
            //             : null);

            var paymentMethod =
                model.PaymentMethodId.HasValue
                        ? PaymentService.GetPaymentMethod(model.PaymentMethodId.Value)
                        : order?.PaymentMethod;

            if (
                order == null
                || order.Payed
                || order.OrderStatus.IsCanceled
                || paymentMethod is null
                || (SettingsCheckout.ManagerConfirmed && !order.ManagerConfirmed)
               )
                return new EmptyResult();
            
            if (paymentMethod is ICreditPaymentMethod creditPaymentMethod 
                && creditPaymentMethod.ActiveCreditPayment 
                && (creditPaymentMethod.MinimumPrice > order.Sum.ConvertCurrency(order.OrderCurrency, paymentMethod.PaymentCurrency ?? order.OrderCurrency)
                    || creditPaymentMethod.MaximumPrice < order.Sum.ConvertCurrency(order.OrderCurrency, paymentMethod.PaymentCurrency ?? order.OrderCurrency)))
                return new EmptyResult();

            
            var viewModel =
                new OrderPayHandler(
                        order,
                        paymentMethod,
                        model.PageWithPaymentButton,
                        model.ValidationDisabled)
                    .Execute();
            
            if (viewModel == null)
                return new EmptyResult();

            if (
                viewModel.ViewPath != null &&
                ViewEngineCollection.FindPartialView(ControllerContext, viewModel.ViewPath)?.View != null
                )
                return PartialView(viewModel.ViewPath, viewModel);
            return PartialView("OrderPay/_Common", viewModel);
        }

        #endregion Pay

        #region Pre Order

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutPreOrder(PreOrderViewModel/*GlorySoft_029 PreOrderModel*/ requestModel)
        {
            var isValid = requestModel.FirstName.IsNotEmpty() && requestModel.Email.IsNotEmpty() && requestModel.Phone.IsNotEmpty();
            isValid &= requestModel.Agreement == SettingsCheckout.IsShowUserAgreementText;
            isValid &= requestModel.Amount > 0;
            isValid &= ValidationHelper.IsValidEmail(requestModel.Email);

            if (SettingsMain.EnableCaptchaInPreOrder && !MvcCaptcha.Validate("CaptchaSourcePreOrder", requestModel.CaptchaCode, requestModel.CaptchaSource))
                return JsonError("Неверный код");

            requestModel.Comment = HttpUtility.HtmlDecode(requestModel.Comment);
            requestModel.Email = HttpUtility.HtmlDecode(requestModel.Email);
            requestModel.FirstName = HttpUtility.HtmlDecode(requestModel.FirstName);
            requestModel.LastName = HttpUtility.HtmlDecode(requestModel.LastName);
            requestModel.Phone = HttpUtility.HtmlDecode(requestModel.Phone);
            requestModel.OptionsHash = HttpUtility.HtmlDecode(requestModel.OptionsHash);

            var allow = ModulesExecuter.CheckInfo(System.Web.HttpContext.Current, ECheckType.CheckoutUserInfo, requestModel.Email, requestModel.FirstName, message: requestModel.Comment, phone: requestModel.Phone);
            if (!allow)
                isValid = false;

            var handler = new PreOrderHandler();
            var model = handler.Get(requestModel);
            if (model == null)
                return JsonError();

            if (!model.CanOrderByRequest)
                return JsonError(T("PreOrder.Index.CantBeOrdered"));

            if (!isValid)
                return JsonError(requestModel.Agreement != SettingsCheckout.IsShowUserAgreementText
                        ? T("Js.Subscribe.ErrorAgreement")
                        : T("PreOrder.Index.WrongData"));

            if (handler.Send(requestModel, model.Offer))
                return JsonOk(UrlService.GetUrl($"preorder{(model.IsLanding ? "/lp" : string.Empty)}/success"));
            return JsonError();
        }

        [HttpGet]
        public JsonResult GetPreOrderFormData()
        {
            var customer = CustomerContext.CurrentCustomer;
            return Json(new
            {
                data = new PreOrderModel
                {
                    FirstName = customer.RegistredUser ? customer.FirstName : string.Empty,
                    LastName = customer.RegistredUser ? customer.LastName : string.Empty,
                    Email = customer.RegistredUser ? customer.EMail : string.Empty,
                    Phone = customer.RegistredUser ? customer.Phone : string.Empty
                },
                field = new
                {
                    SettingsMain.EnablePhoneMask,
                    SettingsMain.EnableCaptchaInPreOrder,
                    SettingsCheckout.IsShowUserAgreementText,
                    SettingsCheckout.UserAgreementText,
                    SettingsCheckout.AgreementDefaultChecked,
                    PreOrderTextStaticBlock = StaticBlockService.GetPagePartByKeyWithCache("requestOnProduct")
                }
            });
        }

        #endregion

        public ActionResult PrintCart()//GlorySoft_028
        {
            var model = new PrintCartHandler(/*printOrder*/).Execute();

            SettingsDesign.IsMobileTemplate = false;

            return View(model);
        }

        private string IsValidUser(CheckoutUser model)//GlorySoft_030
        {
            var isValid = ValidationHelper.IsValidEmail(model.Email);

            if (!string.IsNullOrWhiteSpace(model.Email) && CustomerService.IsEmailExist(model.Email))
            {
                return string.Format(LocalizationService.GetResource("User.Registration.ErrorCustomerExist"), "forgotpassword");
            }

            isValid &= !String.IsNullOrWhiteSpace(model.Confirm) && !String.IsNullOrWhiteSpace(model.Password) &&
                       model.Password == model.Confirm;

            if (!isValid)
                return LocalizationService.GetResource("User.Registration.ErrorPasswordNotMatch");

            isValid &= model.Password.Length >= 6;
            if (!isValid)
                return LocalizationService.GetResource("User.Registration.PasswordLenght");

            if (SettingsCheckout.IsShowPhone && SettingsCheckout.IsRequiredPhone && String.IsNullOrWhiteSpace(model.Phone))
                isValid = false;

            if (SettingsCheckout.IsShowPhone && SettingsCheckout.IsRequiredPhone && !String.IsNullOrWhiteSpace(model.Phone))
            {
                var standardPhone = StringHelper.ConvertToStandardPhone(HttpUtility.HtmlEncode(model.Phone));

                if (model.CustomerType != CustomerType.LegalEntity && CustomerService.IsPhoneExist(model.Phone, standardPhone, CustomerType.PhysicalEntity))
                    return LocalizationService.GetResource("User.Registration.ErrorCustomerPhoneExist");
            }

            if (SettingsCheckout.IsShowLastName && SettingsCheckout.IsRequiredLastName && String.IsNullOrWhiteSpace(model.LastName))
                isValid = false;

            if (SettingsCheckout.IsShowPatronymic && SettingsCheckout.IsRequiredPatronymic && String.IsNullOrWhiteSpace(model.Patronymic))
                isValid = false;

            if (SettingsCheckout.IsShowBirthDay && SettingsCheckout.IsRequiredBirthDay && model.BirthDay == null)
                isValid = false;

            isValid &= !String.IsNullOrWhiteSpace(model.FirstName);

            //if (SettingsCheckout.IsShowUserAgreementText && !model.Agree)
            //    return LocalizationService.GetResource("User.Registration.ErrorAgreement");

            if (BonusSystem.IsActive && model.WantBonusCard)
            {
                var bonusCard = InternalBonusSystemService.GetCard(CustomerContext.CurrentCustomer.Id);
                if (bonusCard != null)
                    return "Бонусная карта уже используется";
            }

            if (!isValid)
                return LocalizationService.GetResource("User.Registration.Error");

            return null;
        }

    }
}
