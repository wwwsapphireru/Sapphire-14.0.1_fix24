using System;
using System.Collections.Generic;
using AdvantShop.Catalog;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Bonuses.Model;
using AdvantShop.Customers;
using AdvantShop.Payment;
using AdvantShop.Shipping;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class OrderDetailsItemModel
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public string BasePrice { get; set; }
        public string Discount { get; set; }
        public float Amount { get; set; }
        public string ArtNo { get; set; }
        public int? Id { get; set; }
        public string TotalPrice { get; set; }
        public string Photo { get; set; }
        public string Url { get; set; }
        public string ColorHeader { get; set; }
        public string SizeHeader { get; set; }
        public string ColorName { get; set; }
        public string SizeName { get; set; }
        public string UnitName { get; set; }
        public List<EvaluatedCustomOptions> CustomOptions { get; set; }
        public float Width { get; set; }
        public float Length { get; set; }
        public float Height { get; set; }
    }

    public class OrderDetailsCertificateModel
    {
        public string Code { get; set; }
        public string Price { get; set; }
    }

    public class OrderDetailsPaymentModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OrderDetailsModel
    {
        public string Email { get; set; }
        public int OrderID { get; set; }
        public string Number { get; set; }
        public Guid Code { get; set; }
        public string StatusName { get; set; }
        public List<OrderDetailsItemModel> OrderItems { get; set; }
        public List<OrderDetailsCertificateModel> OrderCertificates { get; set; }
        public CustomerContact BillingAddress { get; set; }
        public string ShippingName { get; set; }
        public ShippingInfo ShippingInfo { get; set; }
        public string ArchivedShippingName { get; set; }
        public string ShippingAddress { get; set; }
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public bool PaymentMethodIsOnline { get; set; }
        public float TotalDiscountPrice { get; set; }
        public string TotalDiscountPriceFormatted { get; set; }
        public string ProductsPrice { get; set; }
        public float TotalDiscount { get; set; }
        public string CertificatePrice { get; set; }
        public string TotalPrice { get; set; }
        public string Coupon{ get; set; }
        public string ShippingPrice { get; set; }
        public string PaymentPrice { get; set; }
        public string PaymentPriceText { get; set; }
        public string CustomerComment { get; set; }
        public List<OrderDetailsPaymentModel> Payments { get; set; }
        public PaymentDetails PaymentDetails { get; set; }
        public bool Canceled { get; set; }
        public bool Payed { get; set; }
        public string Bonus { get; set; }
        public string TaxesNames { get; set; }
        public string TaxesPrice { get; set; }
        public string TrackNumber { get; set; }
        public string TrackingUrl { get; set; }
        public bool StatusCancelForbidden { get; set; }
        public ShippingHistoryOfMovementInfo ShippingHistory { get; set; }
        public string OrderDate { get; set; }
        public Purchase BonusCardPurchase { get; set; }
        public Manager Manager { get; set; }
        public string ManagerPhone { get; set; }
        public string EmailForFeedback { get; set; }
        public string KeepFreeUntil { get; set; }
        public string ChequeUrl { get; set; }
        public string StatusColor { get; set; }
        public string AdminComment { get; set; }
        public string ShippingCompany { get; set; }
    }

    public class ShippingHistoryOfMovementInfo
    {
        public List<HistoryOfMovementModel> HistoryOfMovement { get; set; }
        public PointInfoModel PointInfo { get; set; }
    }

    public class HistoryOfMovementModel
    {
        public static HistoryOfMovementModel CreateBy(HistoryOfMovement historyOfMovement)
        {
            return new HistoryOfMovementModel
            {
                Code = historyOfMovement.Code,
                Name = historyOfMovement.Name,
                Comment = historyOfMovement.Comment,
                Date = historyOfMovement.Date,
                DateString = historyOfMovement.Date.ToString(Configuration.SettingsMain.ShortDateFormat) + " " + historyOfMovement.Date.ToString("HH:mm"),
            };
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string DateString { get; set; }
    }

    public class PointInfoModel
    {
        public static PointInfoModel CreateBy(BaseShippingPoint pointInfo)
        {
            return new PointInfoModel
            {
                Id = pointInfo.Id,
                Code = pointInfo.Code,
                Name = pointInfo.Name,
                Address = pointInfo.Address,
                Description = pointInfo.Description,
                Latitude = pointInfo.Latitude,
                Longitude = pointInfo.Longitude,
                AvailableCashOnDelivery = pointInfo.AvailableCashOnDelivery,
                AvailableCardOnDelivery = pointInfo.AvailableCardOnDelivery,
                Phone = pointInfo.Phones?.AggregateString(", "),
                TimeWork = pointInfo.TimeWorkStr,
            };
        }

        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public bool? AvailableCashOnDelivery { get; set; }
        public bool? AvailableCardOnDelivery { get; set; }
        public string Phone { get; set; }
        public string TimeWork { get; set; }
    }

    public class ShippingInfo : CustomerContact
    {
        public string AddressType { get; set; }
    }
}