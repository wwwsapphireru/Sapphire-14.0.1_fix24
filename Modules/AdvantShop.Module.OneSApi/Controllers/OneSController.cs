using AdvantShop.Catalog;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Module.OneSApi.Handlers.Client;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using System.Linq;
using System.Web.Mvc;

namespace AdvantShop.Module.OneSApi.Controllers
{
    public class OneSController : ModuleController
    {
        private ActionResult RedirectToReferrerOnPost(string action)
        {
            if (Request.GetUrlReferrer() != null)
                return Redirect(Request.GetUrlReferrer().ToString());

            return RedirectToAction(action);
        }

        public ActionResult ProductDepotsAmounts(Product product, Offer offer)
        {
            return ProductDepotsAmounts(product, "_DepotsAmounts.cshtml");
        }

        public ActionResult ProductDepotsAmountsMobile(Product product, Offer offer)
        {
            return ProductDepotsAmounts(product, "_DepotsAmountsMobile.cshtml");
        }

        public ContentResult ProductViewStatus(int productId)
        {
            string cls = "", text = "";

            if (OfferService.GetProductOffers(productId).Sum(x => x.Amount) > 0)
            {
                cls = "depot-available";
                text = LocalizationService.GetResource("Product.Available");
            }
            else
            {
                var product = ImportService.GetProduct(productId);
                if (product.ExpectedDate.HasValue)
                {
                    cls = "depot-not-available";
                    text = "Ожидается " + product.ExpectedDate.Value.ToString("dd.MM.yyyy");
                }
                else
                {
                    cls = "depot-not-available";
                    text = LocalizationService.GetResource("Product.NotAvailable");
                }
            }

            return Content(string.Format("<div><span class='{0}'>{1}</span></div>", cls, text));
        }

        private ActionResult ProductDepotsAmounts(Product product, string view)
        {
            var model = new ClientDepotsProductModel() { ProductId = product.ProductId };
            model.Amounts = DepotAmountsService.GetClientAmounts(product.ProductId);

            //var offersAmount = product.Offers.Sum(x => x.Amount);
            //if (offersAmount > 0)
            //    model.Amounts.Insert(0, new ClientDepotProductModel
            //    {
            //        DepotTitle = "Магазин Текстильщики",
            //        Amount = offersAmount,
            //        Unit = product.Unit?.DisplayName
            //    });

            //if (model.Amounts.Sum(x => x.Amount) <= 0)
            //{
            var p1c = ImportService.GetProduct(product.ProductId);
            if (p1c != null)
                model.ExpectedDate = p1c.ExpectedDate;
            //}

            return PartialView("~/Modules/" + OneSApi.ModuleStringId + "/Views/Client/Product/" + view, model);
        }

        public ActionResult ProductByExternalId(string externalId)
        {
            var productId = ImportService.GetIdByExternalId(externalId, "Product");
            var product = ProductService.GetProduct(productId);

            Response.Redirect(Url.RouteUrl("Product", new { url = product != null ? product.UrlPath : "" }), false);
            return new EmptyResult();
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CartGetCart1cv8()
        {
            return Json(new GetCartHandler().Get());
        }

        [ChildActionOnly]
        public ActionResult AdminOrderInfo(int orderId)
        {
            if (orderId == 0)
                return new EmptyResult();
            var order = OrderService.GetOrder(orderId);
            if (order == null)
                return new EmptyResult();

            var model = new OrderViewModel();
            if (order.PaymentDetails != null)
            {
                model.INN = order.PaymentDetails.INN;
                model.CompanyName = order.PaymentDetails.CompanyName;
            }
            model.KeepFreeUntil = OrderService.GetOrderAdditionalData(orderId, "KeepFreeUntil");
            if (order.Manager == null)
                model.ManagerName = OrderService.GetOrderAdditionalData(orderId, "ManagerName");
            model.ChequeUrl = OrderService.GetOrderAdditionalData(orderId, "ChequeUrl");
            model.CardHolder = OrderService.GetOrderAdditionalData(orderId, "CardHolder");
            //if (model.ChequeUrl.IsNullOrEmpty())
            //    model.ChequeUrl = OrderService.GetOrderAdditionalData(orderId, "ChequesKey");

            return PartialView("~/modules/" + OneSApi.ModuleStringId + "/Views/Admin/Order/_OrderInfo.cshtml", model);
        }

    }
}
