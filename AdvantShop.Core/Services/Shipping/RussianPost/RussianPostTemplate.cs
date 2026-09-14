using System;

namespace AdvantShop.Shipping.RussianPost
{
    public class RussianPostTemplate : DefaultCargoParams
    {
        public const string Login = "login";
        public const string Password = "password";
        public const string Token = "token";
        [Obsolete]
        public const string DeliveryTypes = "DeliveryTypes";
        [Obsolete]
        public const string OldLocalDeliveryTypes = "LocalDeliveryTypes";
        public const string LocalDeliveryTypes = "NewLocalDeliveryTypes";
        [Obsolete]
        public const string OldInternationalDeliveryTypes = "InternationalDeliveryTypes";
        public const string InternationalDeliveryTypes = "NewInternationalDeliveryTypes";

        public const string PointIndex = "PointIndex";
        public const string TypeInsure = "TypeInsure";
        public const string Courier = "Courier";
        public const string Fragile = "Fragile";
        public const string SmsNotification = "SmsNotification";
        public const string MinimalApiRequests = "MinimalApiRequests";
        public const string TypeNotification = "TypeNotification";
        public const string YaMapsApiKey = "YaMapsApiKey";
        public const string DeliveryWithCod = "DeliveryWithCod";
        public const string DeliveryToOps = "DeliveryToOps";
        public const string DeliveryToPochtamat = "DeliveryToPochtamat";
        public const string DeliveryToPvz = "DeliveryToPvz";

        public const string StatusesSync = "StatusesSync";
        public const string StatusesReference = "StatusesReference";
        public const string TrackingLogin = "TrackingLogin";
        public const string TrackingPassword = "TrackingPassword";

        //GlorySoft_001
        public const string StatusForReady = "StatusForReady";
        public const string CustomerType = "CustomerType";
        public const string MaxWeight = "MaxWeight";
        public const string KeyNameWrongTrackFormTaskAdditionalData = "WrongTrackFormTask";
    }
}
