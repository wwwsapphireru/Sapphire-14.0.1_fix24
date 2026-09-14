using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Diagnostics;
using AdvantShop.Web.Infrastructure.Routing;

namespace AdvantShop
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.LowercaseUrls = true;
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*botdetect}", new { botdetect = @"(.*)BotDetectCaptcha\.ashx" });

            RegisterAllRoutes(routes);

            routes.MapRoute(
                name: "Product",
                url: "products/{url}",
                defaults: new { controller = "Product", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Category",
                url: "categories/{url}",
                defaults: new { controller = "Catalog", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "CategoryTag",
                url: "categories/{url}/tag/{tagUrl}",
                defaults: new { controller = "Catalog", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            
            routes.MapRoute(
                name: "CatalogRoot",
                url: "catalog",
                defaults: new { controller = "Catalog", action = "Index", CategoryId = 0 },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "StaticPage",
                url: "pages/{url}",
                defaults: new { controller = "StaticPage", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "NewsHome",
                url: "news",
                defaults: new { controller = "News", action = "NewsCategory" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            
            routes.MapRoute(
                name: "Login",
                url: "login",
                defaults: new { controller = "User", action = "Login" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "NewsRss",
                url: "news/rss",
                defaults: new { controller = "News", action = "Rss" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "News",
                url: "news/{url}",
                defaults: new { controller = "News", action = "NewsItem" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "NewsSubscribe",
                url: "newssubscribe",
                defaults: new { controller = "News", action = "Subscribe" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Managers",
                url: "managers",
                defaults: new { controller = "Error", action = "NotFound" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Search",
                url: "search",
                defaults: new { controller = "Search", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "NewsCategory",
                url: "newscategory/{url}",
                defaults: new { controller = "News", action = "NewsCategory" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "LoginOpenId",
                url: "loginOpenId",
                defaults: new { controller = "User", action = "LoginOpenId" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Logout",
                url: "logout",
                defaults: new { controller = "User", action = "Logout" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Cart",
                url: "cart",
                defaults: new { controller = "Cart", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "BuyProduct",
                url: "buy/{offerArtNo}",
                defaults: new { controller = "Cart", action = "Buy" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "ShareCart",
                url: "cart/share/{key}",
                defaults: new { controller = "Cart", action = "Share" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "CheckoutPayRedirect",
                url: "checkout/payredirect/{code}",
                defaults: new { controller = "Checkout", action = "PayRedirect", code = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Checkout",
                url: "checkout",
                defaults: new { controller = "Checkout", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            
            routes.MapRoute(
                name: "CheckoutSuccess",
                url: "checkout/success/{code}",
                defaults: new { controller = "Checkout", action = "Success" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "BrandRoot",
                url: "manufacturers",
                defaults: new { controller = "Brand", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Brand",
                url: "manufacturers/{url}",
                defaults: new { controller = "Brand", action = "BrandItem" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "WarehouseHome",
                url: UrlService.NamesAndWords[ParamType.Warehouse],
                defaults: new { controller = "Warehouse", action = "Warehouses" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            
            routes.MapRoute(
                name: "WarehousesPage",
                url: UrlService.NamesAndWords[ParamType.Warehouse] + "/{warehouseUrl}",
                defaults: new { controller = "Warehouse", action = "ViewWarehouse" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "IgnoreHome",
                url: "Home",
                defaults: new { controller = "Error", action = "NotFound" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new { controller = "Home", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Compare",
                url: "compare",
                defaults: new { controller = "Compare", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "ProductList",
                url: "productlist/{type}/{list}",
                defaults: new { controller = "Catalog", action = "ProductList", type = UrlParameter.Optional, list = UrlParameter.Optional },
                constraints: new { type = "[a-z]*" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
               name: "ProductListTag",
               url: "productlist/{type}/tag/{tagUrl}/{list}",
               defaults: new { controller = "Catalog", action = "ProductList", type = UrlParameter.Optional, list = UrlParameter.Optional },
               constraints: new { type = "[a-z]*", list = "[0-9]*" },
               namespaces: new[] { "AdvantShop.Controllers" }
           );

            routes.MapRoute(
                name: "PreOrderByCode",
                url: "preorder/linkbycode",
                defaults: new { controller = "PreOrder", action = "LinkByCode" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "PreOrderSuccess",
                url: "preorder/success",
                defaults: new { controller = "PreOrder", action = "Success", isLanding = false },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
               name: "PreOrderLpSuccess",
               url: "preorder/lp/success",
               defaults: new { controller = "PreOrder", action = "Success", isLanding = true },
               namespaces: new[] { "AdvantShop.Controllers" }
           );

            routes.MapRoute(
               name: "PreOrderLp",
               url: "preorder/lp/{offerid}",
               defaults: new { controller = "PreOrder", action = "Index", offerid = UrlParameter.Optional },
               namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "PreOrder",
                url: "preorder/{offerid}",
                defaults: new { controller = "PreOrder", action = "Index", offerid = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "GiftCertificate",
                url: "giftcertificate",
                defaults: new { controller = "GiftCertificate", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Wishlist",
                url: "wishlist",
                defaults: new { controller = "Wishlist", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Feedback",
                url: "feedback",
                defaults: new { controller = "Feedback", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "PrintOrder",
                url: "printorder/{code}",
                defaults: new { controller = "Checkout", action = "PrintOrder" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "PayOrder",
                url: "pay/{paycode}",
                defaults: new { controller = "Checkout", action = "Billing" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "RecoveryPassword",
                url: "recoveryPassword",
                defaults: new { controller = "User", action = "RecoveryPassword" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "PaymentReturnUrl",
                url: "paymentreturnurl/{advPaymentId}",
                defaults: new { controller = "PaymentStatus", action = "Success", advPaymentId = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "PaymentNotification",
                url: "paymentnotification/{advPaymentId}",
                defaults: new { controller = "PaymentStatus", action = "Notification", advPaymentId = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
               name: "Cancel",
               url: "cancel",
               defaults: new { controller = "PaymentStatus", action = "Cancel", id = UrlParameter.Optional },
               namespaces: new[] { "AdvantShop.Controllers" }
           );

            routes.MapRoute(
               name: "Fail",
               url: "fail",
               defaults: new { controller = "PaymentStatus", action = "Fail", id = UrlParameter.Optional },
               namespaces: new[] { "AdvantShop.Controllers" }
           );

            routes.MapRoute(
                name: "MyAccount",
                url: "myaccount",
                defaults: new { controller = "MyAccount", action = "Index" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
                name: "Closed",
                url: "closed",
                defaults: new { controller = "Common", action = "ClosedStore" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            routes.MapRoute(
               name: "GetBonusCardRoute",
               url: "getbonuscard",
               defaults: new { controller = "Bonuses", action = "GetBonusCard" },
               namespaces: new[] { "AdvantShop.Controllers" }
               );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );

            //GlorySoft_028
            routes.MapRoute(
                name: "RegCodePhysicalEntity",
                url: "registrationwaiting/{customerId}",
                defaults: new { controller = "User", action = "RegCodePhysicalEntity", customerId = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            routes.MapRoute(
                name: "ConfirmRegistration",
                url: "confirmregistration/{hash}",
                defaults: new { controller = "User", action = "ConfirmRegistration", hash = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            routes.MapRoute(
                name: "AccountForAdmin",
                url: "accountforadmin/{hash}",
                defaults: new { controller = "User", action = "AccountForAdmin", hash = UrlParameter.Optional },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            routes.MapRoute(
                name: "PrintCart",
                url: "printcart",
                defaults: new { controller = "Checkout", action = "PrintCart" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
            routes.MapRoute(
                name: "FeedbackSuccess",
                url: "feedback/success",
                defaults: new { controller = "Feedback", action = "Success" },
                namespaces: new[] { "AdvantShop.Controllers" }
            );
        }

        private static void RegisterAllRoutes(RouteCollection routes)
        {
            var iRegisterRouting = typeof(IRegisterRouting);

            try
            {
                var assemblies = 
                    AppDomain.CurrentDomain.GetAssemblies()
                        .Where(x => x.FullName.Contains("AdvantShop.Module") && !x.FullName.Contains(".CRUSHED"));
                
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var type = assembly.GetTypes().FirstOrDefault(x => x.GetInterfaces().Contains(iRegisterRouting));
                        if (type != null)
                            ((IRegisterRouting)Activator.CreateInstance(type)).RegisterRoutes(routes);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }
    }
}