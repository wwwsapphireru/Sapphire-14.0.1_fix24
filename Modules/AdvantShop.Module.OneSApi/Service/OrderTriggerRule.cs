using System.Collections.Generic;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Core.Services.Triggers.Orders;
using AdvantShop.Letters;
using AdvantShop.Mails;
using AdvantShop.Orders;

namespace AdvantShop.Module.OneSApi.Service
{
    //public abstract class OrderTriggerRule : TriggerRule
    //{
    //    public override ETriggerObjectType ObjectType => ETriggerObjectType.Order;
    //}

    public class OrderCreatedTriggerRuleV8 : OrderCreatedTriggerRule
    {
        public override ETriggerEventType EventType => ETriggerEventType.OrderCreated;

        public override List<LetterFormatKey> AvailableVariables =>
            LetterBuilderHelper.GetLetterFormatKeys(
                    new List<OrderLetterTemplateKey>()
                    {
                        OrderLetterTemplateKey.Number,
                        OrderLetterTemplateKey.FirstName,
                        OrderLetterTemplateKey.LastName,
                        OrderLetterTemplateKey.Email,
                        OrderLetterTemplateKey.CustomerContacts,
                        OrderLetterTemplateKey.City,
                        OrderLetterTemplateKey.Address,
                        OrderLetterTemplateKey.AdditionalCustomerFields,
                        OrderLetterTemplateKey.Inn,
                        OrderLetterTemplateKey.CompanyName,
                        OrderLetterTemplateKey.CustomerComment,
                        OrderLetterTemplateKey.BillingLink,
                        OrderLetterTemplateKey.BillingShortLink,
                        OrderLetterTemplateKey.OrderItemsHtml,
                        OrderLetterTemplateKey.Sum,
                        OrderLetterTemplateKey.SumWithoutCurrency,
                        OrderLetterTemplateKey.TrackNumber,
                        OrderLetterTemplateKey.PaymentName,
                        OrderLetterTemplateKey.ShippingNameWithPickpointAddressHtml,
                        OrderLetterTemplateKey.ShippingName,
                        OrderLetterTemplateKey.PickpointAddress,
                        OrderLetterTemplateKey.DeliveryDate,
                        OrderLetterTemplateKey.DeliveryDateWithPrefix,
                        OrderLetterTemplateKey.ManagerName,
                        OrderLetterTemplateKey.CurrencyCode,
                        OrderLetterTemplateKey.PaymentStatus,
                        OrderLetterTemplateKey.OrderPaidOrNotPaid,
                        OrderLetterTemplateKey.PostalCode,
                        OrderLetterTemplateKey.GeneratedCouponCode//GlorySoft_003
                    })
                .Concat(LetterBuilderHelper.GetLetterFormatKeys<CommonLetterTemplateKey>())
                .Concat(LetterBuilderHelper.GetLetterFormatKeys<TriggerLetterTemplateKey>())
                .ToList();

        public override string ReplaceVariables(string value, ITriggerObject triggerObject, Coupon coupon,
            string triggerCouponCode)
        {
            var order = (Order) triggerObject;

            _mail = NewOrderMailTemplate.Create(order);

            var text = _mail.FormatValue(value, coupon, triggerCouponCode);/*GlorySoft_043*/

            //GlorySoft_043
            text = text.Replace("#MANAGER_SIGN#", order.Manager != null ? order.Manager.Sign : "");
            return text;
        }

        public override TriggerMailFormat GetDefaultMailTemplate()
        {
            var mail = MailFormatService.GetByType(MailType.OnNewOrder.ToString());
            if (mail != null)
                return new TriggerMailFormat(mail);

            return null;
        }
    }

    public class OrderStatusChangedTriggerRuleV8 : OrderStatusChangedTriggerRule
    {
        public override ETriggerEventType EventType => ETriggerEventType.OrderStatusChanged;

        public override string[] AvailableVariables =>
            LetterBuilderHelper.GetLetterFormatKeys(
                    new List<OrderLetterTemplateKey>()
                    {
                        OrderLetterTemplateKey.Number,
                        OrderLetterTemplateKey.Status,
                        OrderLetterTemplateKey.StatusCommentHtml,
                        OrderLetterTemplateKey.FirstName,
                        OrderLetterTemplateKey.LastName,
                        OrderLetterTemplateKey.Email,
                        OrderLetterTemplateKey.CustomerContacts,
                        OrderLetterTemplateKey.City,
                        OrderLetterTemplateKey.Address,
                        OrderLetterTemplateKey.AdditionalCustomerFields,
                        OrderLetterTemplateKey.Inn,
                        OrderLetterTemplateKey.CompanyName,
                        OrderLetterTemplateKey.CustomerComment,
                        OrderLetterTemplateKey.BillingLink,
                        OrderLetterTemplateKey.BillingShortLink,
                        OrderLetterTemplateKey.OrderItemsHtml,
                        OrderLetterTemplateKey.Sum,
                        OrderLetterTemplateKey.SumWithoutCurrency,
                        OrderLetterTemplateKey.TrackNumber,
                        OrderLetterTemplateKey.PaymentName,
                        OrderLetterTemplateKey.ShippingNameWithPickpointAddressHtml,
                        OrderLetterTemplateKey.ShippingName,
                        OrderLetterTemplateKey.PickpointAddress,
                        OrderLetterTemplateKey.DeliveryDate,
                        OrderLetterTemplateKey.DeliveryDateWithPrefix,
                        OrderLetterTemplateKey.ManagerName,
                        OrderLetterTemplateKey.CurrencyCode,
                        OrderLetterTemplateKey.PaymentStatus,
                        OrderLetterTemplateKey.OrderPaidOrNotPaid,
                        OrderLetterTemplateKey.PostalCode
                    })
                .Concat(LetterBuilderHelper.GetAllLetterFormatKeys<CommonLetterTemplateKey>())
                .Append("#GENERATED_COUPON_CODE#")
                .ToArray();


        public override string ReplaceVariables(string value, ITriggerObject triggerObject, Coupon coupon,
            string triggerCouponCode)
        {
            var order = (Order) triggerObject;

            _mail = new OrderStatusMailTemplate(order);

            var text = _mail.FormatValue(value, coupon, triggerCouponCode);/*GlorySoft_043*/

            //GlorySoft_043
            //Diagnostics.Debug.Log.Info(text);
            //Diagnostics.Debug.Log.Info($"{Core.UrlRewriter.UrlService.GenerateBaseUrl().TrimEnd('/')} ==> {Configuration.SettingsMain.SiteUrl.TrimEnd('/')}");
            text = text.Replace(Core.UrlRewriter.UrlService.GenerateBaseUrl().TrimEnd('/'), Configuration.SettingsMain.SiteUrl.TrimEnd('/'));
            text = text.Replace("#MANAGER_SIGN#", order.Manager != null ? order.Manager.Sign : "");
            return text;
        }


        public override TriggerMailFormat GetDefaultMailTemplate()
        {
            var mail = MailFormatService.GetByType(MailType.OnChangeOrderStatus.ToString());
            if (mail != null)
                return new TriggerMailFormat(mail);

            return null;
        }
    }

    public class OrderPayTriggerRuleV8 : OrderPayTriggerRule
    {
        public override ETriggerEventType EventType => ETriggerEventType.OrderPaied;

        public override string[] AvailableVariables =>
            LetterBuilderHelper.GetLetterFormatKeys(
                    new List<OrderLetterTemplateKey>()
                    {
                        OrderLetterTemplateKey.Number,
                        OrderLetterTemplateKey.FirstName,
                        OrderLetterTemplateKey.LastName,
                        OrderLetterTemplateKey.Email,
                        OrderLetterTemplateKey.CustomerContacts,
                        OrderLetterTemplateKey.City,
                        OrderLetterTemplateKey.Address,
                        OrderLetterTemplateKey.AdditionalCustomerFields,
                        OrderLetterTemplateKey.Inn,
                        OrderLetterTemplateKey.CompanyName,
                        OrderLetterTemplateKey.CustomerComment,
                        OrderLetterTemplateKey.BillingLink,
                        OrderLetterTemplateKey.BillingShortLink,
                        OrderLetterTemplateKey.OrderItemsHtml,
                        OrderLetterTemplateKey.Sum,
                        OrderLetterTemplateKey.SumWithoutCurrency,
                        OrderLetterTemplateKey.TrackNumber,
                        OrderLetterTemplateKey.PaymentName,
                        OrderLetterTemplateKey.ShippingNameWithPickpointAddressHtml,
                        OrderLetterTemplateKey.ShippingName,
                        OrderLetterTemplateKey.PickpointAddress,
                        OrderLetterTemplateKey.DeliveryDate,
                        OrderLetterTemplateKey.DeliveryDateWithPrefix,
                        OrderLetterTemplateKey.ManagerName,
                        OrderLetterTemplateKey.CurrencyCode,
                        OrderLetterTemplateKey.PaymentStatus,
                        OrderLetterTemplateKey.OrderPaidOrNotPaid,
                        OrderLetterTemplateKey.PostalCode
                    })
                .Concat(LetterBuilderHelper.GetAllLetterFormatKeys<CommonLetterTemplateKey>())
                .Append("#GENERATED_COUPON_CODE#")
                .ToArray();


        public override string ReplaceVariables(string value, ITriggerObject triggerObject, Coupon coupon,
            string triggerCouponCode)
        {
            var order = (Order) triggerObject;

            _mail = new PayOrderTemplate(order);

            var text = _mail.FormatValue(value, coupon, triggerCouponCode);/*GlorySoft_043*/

            //GlorySoft_043
            text = text.Replace("#MANAGER_SIGN#", order.Manager != null ? order.Manager.Sign : "");
            return text;
        }


        public override TriggerMailFormat GetDefaultMailTemplate()
        {
            var mail = MailFormatService.GetByType(MailType.OnPayOrder.ToString());
            if (mail != null)
                return new TriggerMailFormat(mail);

            return null;
        }
    }
}
