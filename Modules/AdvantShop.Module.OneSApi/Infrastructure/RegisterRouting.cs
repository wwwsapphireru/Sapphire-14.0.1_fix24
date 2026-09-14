using System.Web.Mvc;
using System.Web.Routing;
using AdvantShop.Web.Infrastructure.Routing;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Infrastructure
{
    public class RegisterRouting : IRegisterRouting
    {
        public void RegisterRoutes(RouteCollection routes)
        {

            #region OneSApi

            routes.MapRoute(
               name: "OneSApiCheck",
               url: "1cv8/check",
               defaults: new { controller = "OneSApi", action = "Check" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiError",
               url: "1cv8/error",
               defaults: new { controller = "OneSApi", action = "Error" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiProductImport",
               url: "1cv8/product/import",
               defaults: new { controller = "OneSApi", action = "ProductImport" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiOrderGetList",
               url: "1cv8/order/getlist",
               defaults: new { controller = "OneSApi", action = "OrderGetList" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiOrderGet",
               url: "1cv8/order/get",
               defaults: new { controller = "OneSApi", action = "OrderGet" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiOrderConfirm",
               url: "1cv8/order/confirm",
               defaults: new { controller = "OneSApi", action = "OrderConfirm" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiOrderUnconfirm",
               url: "1cv8/order/unconfirm",
               defaults: new { controller = "OneSApi", action = "OrderUnconfirm" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiOrderImport",
               url: "1cv8/order/import",
               defaults: new { controller = "OneSApi", action = "OrderImport" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiCustomersImport",
               url: "1cv8/customers/import",
               defaults: new { controller = "OneSApi", action = "CustomersImport" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiProductExport",
               url: "1cv8/product/export",
               defaults: new { controller = "OneSApi", action = "ProductExport" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiProductGetList",
               url: "1cv8/product/getlist",
               defaults: new { controller = "OneSApi", action = "ProductGetList" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiProductConfirm",
               url: "1cv8/product/confirm",
               defaults: new { controller = "OneSApi", action = "ProductConfirm" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiProductUnconfirm",
               url: "1cv8/product/unconfirm",
               defaults: new { controller = "OneSApi", action = "ProductUnconfirm" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            //routes.MapRoute(
            //   name: "OneSApiCustomerGetList",
            //   url: "1cv8/customer/getlist",
            //   defaults: new { controller = "OneSApi", action = "CustomerGetList" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            routes.MapRoute(
               name: "OneSApiCustomerConfirm",
               url: "1cv8/customer/confirm",
               defaults: new { controller = "OneSApi", action = "CustomerConfirm" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            //routes.MapRoute(
            //   name: "OneSApiCustomerUnconfirm",
            //   url: "1cv8/customer/unconfirm",
            //   defaults: new { controller = "OneSApi", action = "CustomerUnconfirm" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            routes.MapRoute(
               name: "OneSApiCalculateShippings",
               url: "1cv8/calculateshippings",
               defaults: new { controller = "OneSApi", action = "CalculateShippings" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            routes.MapRoute(
               name: "OneSApiCheckRefusing",
               url: "1cv8/checkrefusing/{orderId}",
               defaults: new { controller = "OneSApi", action = "CheckRefusing", orderId = UrlParameter.Optional },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            #endregion

            #region OneS

            routes.MapRoute(
               name: "ProductByExternalId",
               url: "1cv8/product/view/{externalId}",
               defaults: new { controller = "OneS", action = "ProductByExternalId" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );

            //routes.MapRoute(
            //   name: "ProductListRecomended",
            //   url: "productlist/recomended",
            //   defaults: new { controller = "OneS", action = "ProductListRecomended" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            routes.MapRoute(
               name: "CartGetCart1cv8",
               url: "1cv8/cart/getCart",
               defaults: new { controller = "OneS", action = "CartGetCart1cv8" },
               namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
               );
            //routes.MapRoute(
            //   name: "CartGetCart",
            //   url: "cart/getCart",
            //   defaults: new { controller = "OneS", action = "CartGetCart" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            //routes.MapRoute(
            //   name: "CheckoutIndex",
            //   url: "checkout",
            //   defaults: new { controller = "OneS", action = "CheckoutIndex" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );
            //routes.MapRoute(
            //   name: "CheckoutCartJson",
            //   url: "checkout/CheckoutCartJson",
            //   defaults: new { controller = "OneS", action = "CheckoutCartJson" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );
            //routes.MapRoute(
            //   name: "CheckoutBilling",
            //   url: "checkout/billing",
            //   defaults: new { controller = "OneS", action = "CheckoutBilling" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );
            //routes.MapRoute(
            //   name: "CheckoutBillingPayOrder",
            //   url: "pay/{paycode}",
            //   defaults: new { controller = "OneS", action = "CheckoutBilling" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //   name: "CheckoutPaymentJson",
            //   url: "checkout/CheckoutPaymentJson",
            //   defaults: new { controller = "OneS", action = "CheckoutPaymentJson" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );
            //routes.MapRoute(
            //   name: "CheckoutGetOrderPay",
            //   url: "checkout/getorderpay",
            //   defaults: new { controller = "OneS", action = "GetOrderPay" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            //routes.MapRoute(
            //   name: "MyAccountGetOrderDetails",
            //   url: "myaccount/GetOrderDetails",
            //   defaults: new { controller = "OneS", action = "GetOrderDetails" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );
            //routes.MapRoute(
            //   name: "MyAccountGetCustomerOrderHistory",
            //   url: "myaccount/GetCustomerOrderHistory",
            //   defaults: new { controller = "OneS", action = "GetCustomerOrderHistory" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );
            //routes.MapRoute(
            //   name: "MyAccountAddUpdateCustomerContact",
            //   url: "myaccount/AddUpdateCustomerContact",
            //   defaults: new { controller = "OneS", action = "AddUpdateCustomerContact" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            //routes.MapRoute(
            //   name: "PaymentReceiptBill",
            //   url: "paymentreceipt/bill",
            //   defaults: new { controller = "OneS", action = "PaymentReceiptBill" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            //routes.MapRoute(
            //    name: "UserRegistration",
            //    url: "registration",
            //    defaults: new { controller = "OneS", action = "UserRegistration" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //    name: "UserRegistrationLegal",
            //    url: "registrationlegal",
            //    defaults: new { controller = "OneS", action = "UserRegistrationLegal" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //    name: "RegCodePhysicalEntity",
            //    url: "registrationwaiting/{customerId}",
            //    defaults: new { controller = "OneS", action = "RegCodePhysicalEntity", customerId = UrlParameter.Optional },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //    name: "ConfirmRegistration",
            //    url: "confirmregistration/{hash}",
            //    defaults: new { controller = "OneS", action = "ConfirmRegistration", hash = UrlParameter.Optional },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //    name: "AccountForAdmin",
            //    url: "accountforadmin/{hash}",
            //    defaults: new { controller = "OneS", action = "AccountForAdmin", hash = UrlParameter.Optional },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //   name: "CheckoutCheckoutShippingAddress",
            //   url: "checkout/CheckoutShippingAddress",
            //   defaults: new { controller = "OneS", action = "CheckoutShippingAddress" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //   );

            //routes.MapRoute(
            //    name: "PrintCart",
            //    url: "printcart",
            //    defaults: new { controller = "OneS", action = "PrintCart" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //    name: "PreorderIndex",
            //    url: "preorder/{offerid}",
            //    defaults: new { controller = "OneS", action = "PreorderIndex", offerid = UrlParameter.Optional },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //    name: "FeedbackIndex",
            //    url: "feedback",
            //    defaults: new { controller = "OneS", action = "FeedbackIndex" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //    name: "FeedbackSuccess",
            //    url: "feedback/success",
            //    defaults: new { controller = "OneS", action = "FeedbackSuccess" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //    name: "CatalogProductList",
            //    url: "productlist/{type}/{list}",
            //    defaults: new { controller = "OneS", action = "CatalogProductList", type = UrlParameter.Optional, list = UrlParameter.Optional },
            //    constraints: new { type = "[a-z]*", list = "[0-9]*" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //   name: "CatalogProductListTag",
            //   url: "productlist/{type}/tag/{tagUrl}/{list}",
            //   defaults: new { controller = "OneS", action = "CatalogProductList", type = UrlParameter.Optional, list = UrlParameter.Optional },
            //   constraints: new { type = "[a-z]*", list = "[0-9]*" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //    name: "CatalogCategory",
            //    url: "categories/{url}",
            //    defaults: new { controller = "OneS", action = "CategoryIndex" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //    name: "CatalogCategoryTag",
            //    url: "categories/{url}/tag/{tagUrl}",
            //    defaults: new { controller = "OneS", action = "CategoryIndex" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);
            //routes.MapRoute(
            //   name: "MobileProductList",
            //   url: "mobileproductlist/{type}/{list}",
            //   defaults: new { controller = "OneS", action = "MobileProductList", type = UrlParameter.Optional, list = UrlParameter.Optional },
            //   constraints: new { type = "[a-z]*", list = "[0-9]*" },
            //   namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //    name: "ProductIndex",
            //    url: "products/{url}",
            //    defaults: new { controller = "OneS", action = "ProductIndex" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            //routes.MapRoute(
            //    name: "SearchIndex",
            //    url: "search",
            //    defaults: new { controller = "OneS", action = "SearchIndex" },
            //    namespaces: new[] { "AdvantShop.Module.OneSApi.Controllers" }
            //);

            #endregion

        }
    }
}
