using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using AdvantShop.Orders;
using AdvantShop.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class OrderServiceV8
    {
        public static string GetOrderCode(int orderId)
        {
            return ModulesRepository.ModuleExecuteReadOne(
                "SELECT [Code] FROM [Order].[Order] WHERE [OrderID] = @OrderId",
                CommandType.Text,
                reader =>
                    SQLDataHelper.GetString(reader, "Code"),
                new SqlParameter("@OrderId", orderId));
        }

        public static string GetOrderPayCode(int orderId)
        {
            return ModulesRepository.ModuleExecuteReadOne(
                "SELECT [PayCode] FROM [Order].[Order] WHERE [OrderID] = @OrderId",
                CommandType.Text,
                reader =>
                    SQLDataHelper.GetString(reader, "PayCode"),
                new SqlParameter("@OrderId", orderId));
        }

        public static string GetBillingLinkHash(int orderId)
        {
            return OrderService.GetBillingLinkHash(OrderService.GetOrder(orderId));
        }

        public static void UpdateAdminOrderComment(int id, string adminOrderComment, OrderChangedBy changedBy = null, bool trackChanges = true)
        {
            if (trackChanges)
                OrderHistoryService.ChangingAdminComment(id, adminOrderComment, changedBy);

            ModulesRepository.ModuleExecuteNonQuery("[Order].[sp_UpdateOrderAdminOrderComment]", CommandType.StoredProcedure, new SqlParameter("@OrderID", id),
                                        new SqlParameter("@AdminOrderComment", adminOrderComment ?? string.Empty));

            //ModulesExecuter.UpdateComments(id);
        }

        public static void UpdateManagerName(int orderId, Manager manager = null)
        {
            if (manager == null)
            {
                var ord = OrderService.GetOrder(orderId);
                manager = ord.Manager;
            }
            var key = "ManagerName";
            if (manager != null)
                OrderService.AddUpdateOrderAdditionalData(orderId, key, manager.FullName);
            //else
            //    OrderService.DeleteOrderAdditionalData(order.OrderID, key);
        }

        public static void CheckNullDimensions(Order order, bool updateInDb)
        {
            if (order.OrderItems.Where(x => x.Length == 0 && x.Width == 0 && x.Height == 0).Count() > 0)
            {
                var totalDimensions = MeasureHelperV8.GetDimensions(order);
                if (updateInDb)
                    UpdateDimensions(order.OrderID, totalDimensions);
            }
            else if (order.TotalLength.HasValue || order.TotalWidth.HasValue || order.TotalHeight.HasValue)
            {
                if (updateInDb)
                    UpdateDimensions(order.OrderID, null);
            }
        }

        public static float[] CheckNullDimensions(Order order)
        {
            if (order.OrderItems.Where(x => x.Length == 0 && x.Width == 0 && x.Height == 0).Count() > 0)
                return MeasureHelperV8.GetDimensions(order);
            else
                return null;
        }

        public static void UpdateDimensions(int orderId, float[] dimensions)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Order].[Order] " +
                "Set TotalLength=@TotalLength, TotalWidth=@TotalWidth, TotalHeight=@TotalHeight " +
                "Where OrderID=@OrderID", 
                CommandType.Text, new SqlParameter("@OrderID", orderId),
                new SqlParameter("@TotalLength", dimensions != null ? dimensions[0] : (object)DBNull.Value), 
                new SqlParameter("@TotalWidth", dimensions != null ? dimensions[1] : (object)DBNull.Value), 
                new SqlParameter("@TotalHeight", dimensions != null ? dimensions[2] : (object)DBNull.Value));
        }

    }
}
