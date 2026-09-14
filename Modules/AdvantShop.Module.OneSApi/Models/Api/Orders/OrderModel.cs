using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Core.Services.Webhook.Models.Api;
using AdvantShop.Module.OneSApi.Models.Api.Orders;
using AdvantShop.Orders;

namespace AdvantShop.Module.OneSApi.Models.Api.Orders
{
    public class OrderV8Model : OrderModel
    {
        public static OrderV8Model FromOrderV8(Order order)
        {
            if (order == null || order.IsDraft)
                return null;

            return new OrderV8Model
            {
                Id = order.OrderID,
                Number = order.Number,
                Currency = order.OrderCurrency != null ? order.OrderCurrency.CurrencyCode : null,
                Sum = order.Sum,
                Date = order.OrderDate,

                CustomerComment = order.CustomerComment,
                AdminComment = order.AdminOrderComment,

                PaymentName = order.ArchivedPaymentName,
                PaymentCost = order.PaymentCost,

                ShippingId = order.ShippingMethodId,
                ShippingName = order.ArchivedShippingName,
                ShippingCost = order.ShippingCost,
                ShippingTaxName = order.ShippingTaxType.Localize(),
                TrackNumber = order.TrackNumber,
                DeliveryDate = order.DeliveryDate,
                DeliveryTime = order.DeliveryTime,

                OrderDiscount = order.OrderDiscount,
                OrderDiscountValue = order.OrderDiscountValue,

                BonusCardNumber = order.BonusCardNumber,
                BonusCost = order.BonusCost,

                LpId = order.LpId,

                IsPaid = order.Payed,
                PaymentDate = order.PaymentDate,

                Customer = OrderCustomerModel.FromOrderCustomer(order.OrderCustomer),
                CustomerAddress = order.OrderCustomer.GetCustomerAddress(),

                Status = OrderStatusModel.FromOrderStatus(order.OrderStatus),

                Source = OrderSourceModel.FromOrderSource(order.OrderSource),

                Items = order.OrderItems != null
                    ? order.OrderItems.Select(OrderItemV8Model.FromOrderItemV8).ToList()
                    : new List<OrderItemV8Model>(), 

                INN = order.PaymentDetails.INN,
                CompanyName = order.PaymentDetails.CompanyName,

                ManagerId = order.ManagerId,
                ManagerFirstName = order.Manager != null ? order.Manager.FirstName : "",
                ManagerLastName = order.Manager != null ? order.Manager.LastName : "",
                //ManagerPatronymic = order.Manager != null ? order.Manager.Patronymic : "",
                ManagerEmail = order.Manager != null ? order.Manager.Email : "",
                ManagerPhone = order.Manager != null ? order.Manager.StandardPhone : null,

                PickPointId = order.OrderPickPoint != null ? order.OrderPickPoint.PickPointId : "",
                PickPointAddress = order.OrderPickPoint != null ? order.OrderPickPoint.PickPointAddress : ""
            };
        }

        public string INN { get; set; }
        public string CompanyName { get; set; }
        public int? ManagerId { get; set; }
        public string ManagerFirstName { get; set; }
        public string ManagerLastName { get; set; }
        //public string ManagerPatronymic { get; set; }
        public string ManagerEmail { get; set; }
        public long? ManagerPhone { get; set; }
        public string PickPointId { get; set; }
        public string PickPointAddress { get; set; }
        public int ShippingId { get; set; }
        public string CustomerAddress { get; set; }
        public DateTime? PaymentDate { get; set; }

        public new List<OrderItemV8Model> Items { get; set; }

    }
}
