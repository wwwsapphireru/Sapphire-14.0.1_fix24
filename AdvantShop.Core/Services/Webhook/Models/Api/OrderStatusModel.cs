using AdvantShop.Orders;

namespace AdvantShop.Core.Services.Webhook.Models.Api
{
    public /*GlorySoft_002 sealed*/ class OrderStatusModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public bool IsCanceled { get; set; }
        public bool IsCompleted { get; set; }
        public bool Hidden { get; set; }
        public bool IsCancellationForbidden { get; set; }
        
        public static OrderStatusModel FromOrderStatus(OrderStatus orderStatus)
        {
            if (orderStatus == null)
                return null;

            return new OrderStatusModel
            {
                Id = orderStatus.StatusID,
                Name = orderStatus.StatusName,
                Color = orderStatus.Color,
                IsCanceled = orderStatus.IsCanceled,
                IsCompleted = orderStatus.IsCompleted,
                Hidden = orderStatus.Hidden,
                IsCancellationForbidden = orderStatus.CancelForbidden
            };
        }
        
        public static OrderStatusModel FromOrderStatus(string statusName)
        {
            return new OrderStatusModel
            {
                Name = statusName,
                Color = "3f3f3f"
            };
        }

        public static explicit operator OrderStatus(OrderStatusModel model)
        {
            return new OrderStatus
            {
                StatusID = model.Id,
                StatusName = model.Name,
                Color = model.Color,
                IsCanceled = model.IsCanceled,
                IsCompleted = model.IsCompleted,
                Hidden = model.Hidden,
                CancelForbidden = model.IsCancellationForbidden
            };
        }
    }
}
