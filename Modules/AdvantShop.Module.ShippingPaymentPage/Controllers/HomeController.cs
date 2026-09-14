using System.Collections.Generic;
using System.Web.Mvc;
using AdvantShop.Core.Modules;
using AdvantShop.Module.ShippingPaymentPage.Models;
using AdvantShop.Shipping;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using AdvantShop.Module.ShippingPaymentPage.Services;
using AdvantShop.Repository.Currencies;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.SEO;

namespace AdvantShop.Module.ShippingPaymentPage.Controllers
{
    [Module(Type = "ShippingPaymentPage")]
    public partial class HomeController : ModuleController
    {
        public ActionResult ShippingPayment()
        {
            var model = new ShippingPaymentModel()
            {
                Zone = Repository.IpZoneContext.CurrentZone,
                TextBlock = ShippingPaymentPageSettings.ShippingTextBlock,
                TextBlockBottom = ShippingPaymentPageSettings.ShippingTextBlockBottom
            };

            var meta = new MetaInfo(0, 0, MetaType.Module, 
                ShippingPaymentPageSettings.Title, 
                ShippingPaymentPageSettings.MetaKeywords,
                ShippingPaymentPageSettings.MetaDescription, 
                ShippingPaymentPageSettings.Title);

            SetMetaInformation(meta);

            return View("~/Modules/ShippingPaymentPage/Views/Home/ShippingPayment.cshtml", model);
        }

        public JsonResult GetListProduct()
        {
            var currency = CurrencyService.GetCurrencyByIso3(ShippingPaymentPageSettings.DefaultPriceCurrencyIso3 ?? CurrencyService.BaseCurrency?.Iso3);
            var roundedPrice = PriceService.RoundPrice(ShippingPaymentPageSettings.DefaultPrice, null, currency.Rate);
            var price = PriceService.RoundPrice(roundedPrice, null, CurrencyService.CurrentCurrency.Rate);

            var listProduct = new List<PreOrderItem>()
            {
                new PreOrderItem()
                {
                    Name = "TEST_PRODUCT",
                    Amount = 1,
                    Price = price,
                    ShippingPrice = ShippingPaymentPageSettings.DefaultShippingPrice,

                    Weight = ShippingPaymentPageSettings.DefaultWeight,
                    Width = ShippingPaymentPageSettings.DefaultWidth,
                    Height = ShippingPaymentPageSettings.DefaultHeight,
                    Length = ShippingPaymentPageSettings.DefaultLength
                }
            };

            return Json(listProduct, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult CheckoutShippingJson(Orders.CheckoutAddress contact, List<PreOrderItem> preorderList = null)//GlorySoft_008
        {
            //var current = MyCheckout.Factory(CustomerContext.CustomerId);

            var options = new List<BaseShippingOption>();

            //if (!current.Data.HideShippig)
            //{
            options = ShippingPaymentPageService/*current*/.AvailableShippingOptions(contact, preorderList);

            //    if (current.Data.SelectShipping == null || !options.Any(x => x.Id == current.Data.SelectShipping.Id))
            //        current.Data.SelectShipping = null;

            //    current.UpdateSelectShipping(preorderList, current.Data.SelectShipping, options);
            //}
            //else
            //{
            //    options.Add(current.Data.SelectShipping);
            //}

            return Json(new { selectShipping = options.Count > 0 ? options[0] : null/*current.Data.SelectShipping*/, option = options });
        }

    }
}