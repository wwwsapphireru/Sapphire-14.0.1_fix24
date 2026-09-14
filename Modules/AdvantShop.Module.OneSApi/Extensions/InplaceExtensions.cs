//using System;
//using System.Web;
//using System.Web.Mvc;
//using AdvantShop.Core.Services.InplaceEditor;
//using AdvantShop.Customers;
//using AdvantShop.Diagnostics;

//namespace AdvantShop.Extensions
//{
//    public static class InplaceExtensions
//    {
//        const string richSimple = "{editorSimple: true}";
//        const string richTpl = "data-inplace-rich=\"{4}\" data-inplace-url=\"{3}\" data-inplace-params=\"{{id: {0}, type: '{1}', field: '{2}'}}\" {6} placeholder=\"{5}\" data-inplace-on-save=\"{7}\"";

//        public static string InplaceStringFormat<T>(string id, InplaceType type, T field, string url, string rich, string placeholder, bool isBindable = false, string ngSaveCallback = "") where T : IComparable, IFormattable, IConvertible
//        {
//            return String.Format(richTpl, id, type, field, url, rich, placeholder, !isBindable ? "ng-non-bindable" : string.Empty, ngSaveCallback);
//        }

//        public static HtmlString InplaceProductDescription(this HtmlHelper helper, int productId, ProductInplaceField field, bool editorSimple = false, bool bindable = false)
//        {
//            Debug.Log.Info("InplaceProductDescription");
//            if (!InplaceEditorService.CanUseInplace(RoleAction.Catalog))
//                return new HtmlString("");

//            return new HtmlString(InplaceStringFormat(productId.ToString(), InplaceType.Product, field, "ones/inplaceeditorproduct", editorSimple ? richSimple : string.Empty, "Нажмите сюда, чтобы добавить описание", bindable));
//        }

//        public static HtmlString InplaceOfferAmount(int offerId)
//        {
//            if (!InplaceEditorService.CanUseInplace(RoleAction.Catalog))
//                return new HtmlString("");

//            return
//                new HtmlString(InplaceStringFormat(offerId.ToString(), InplaceType.Offer, OfferInplaceField.Amount, "inplaceeditor/offer", richSimple, "Нажмите сюда, чтобы добавить описание"));
//        }

//    }
//}