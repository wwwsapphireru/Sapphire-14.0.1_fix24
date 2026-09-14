using AdvantShop.Core.SQL;
using AdvantShop.Orders;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
//GlorySoft_001
namespace AdvantShop.Payment
{
    public class PayOnlineService
    {
        public static decimal GetOrderSum(int orderId)
        {
            return SQLDataAccess.ExecuteScalar<decimal>("SELECT [Sum] FROM [Order].[Order] WHERE [OrderId] = @orderId",
                CommandType.Text, new SqlParameter("@orderId", orderId));
        }

        public static bool HasCompletedOrders(Guid customerId)
        {
            var stIds = OrderStatusService.GetOrderStatuses().Where(x => x.IsCompleted).Select(x => x.StatusID).ToList();
            return OrderService.GetCustomerOrderHistory(customerId).Any(x => x.Payed && stIds.Contains(x.StatusID));
        }
    }
}
