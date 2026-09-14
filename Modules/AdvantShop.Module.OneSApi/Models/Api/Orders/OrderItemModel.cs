using AdvantShop.Core.Services.Webhook.Models.Api;
using AdvantShop.Orders;

namespace AdvantShop.Module.OneSApi.Models.Api.Orders
{
    public class OrderItemV8Model : OrderItemModel
    {
        public static OrderItemV8Model FromOrderItemV8(OrderItem orderItem)
        {
            if (orderItem == null)
                return null;

            return new OrderItemV8Model
            {
                ArtNo = orderItem.ArtNo,
                Name = orderItem.Name,
                Color = orderItem.Color,
                Size = orderItem.Size,
                Price = orderItem.Price,
                Amount = orderItem.Amount,
                TaxRate = orderItem.TaxRate ?? 0,
                Unit = orderItem.Unit,
                ProductId = orderItem.ProductID ?? 0
            };
        }

        public float TaxRate { get; set; }
        public string Unit { get; set; }
        public int ProductId { get; set; }
    }
}
