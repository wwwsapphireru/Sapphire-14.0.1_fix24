using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Web.Infrastructure.Handlers;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Orders
{
    public class GetOrder : ICommandHandler<KeyValuePair<string, string>, OrderV8Model>
    {
        public OrderV8Model Execute(KeyValuePair<string, string> ids)
        {
            var orderId = ExportService.GetOrderIdFromExternalId(ids.Key);
            var order = OrderService.GetOrder(orderId);
            if (order == null)
                order = OrderService.GetOrder(ids.Value.TryParseInt());

            if (order == null || order.IsDraft)
                throw new BlException("Заказ не найден");

            return OrderV8Model.FromOrderV8(order);
        }
    }
}