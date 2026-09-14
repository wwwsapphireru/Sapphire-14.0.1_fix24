using AdvantShop.Catalog;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Orders;
using AdvantShop.Web.Infrastructure.Admin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class ExportService
    {
        public static void DoOrderAdded(IOrder order, ExportType exportType)
        {
            if (!order.IsDraft)
            {
                DoOrderAdded(order.OrderID, exportType);

                var settings = ModuleService.GetImportExportSettings();
                if (settings.ExportOrder.StatusLead > 0 && order.OrderItems.Count == 1)
                {
                    var s = OrderStatusService.GetOrderStatus(settings.ExportOrder.StatusLead);
                    if (s != null)
                    {
                        var i = order.OrderItems[0];
                        var p = OfferService.GetOffer(i.ArtNo);
                        if (p.Amount <= 0)
                            OrderStatusService.ChangeOrderStatus(order.OrderID, s.StatusID, OneSApi.ModuleName, false);
                    }
                }

                if (order.OrderItems.Where(x => x.Length == 0 && x.Width == 0 && x.Height == 0).Count() > 0)
                {
                    var ord = OrderService.GetOrder(order.OrderID);
                    OrderServiceV8.CheckNullDimensions(ord, true);
                }
            }
        }
        public static void DoOrderAdded(int orderId, ExportType exportType)
        {
            var externalId = ModulesRepository.ModuleExecuteScalar<string>("Select TOP 1 ExternalId From Module.OneSApi_Order WHERE OrderId = @OrderId AND ExternalId Is Not NULL",
                CommandType.Text, new SqlParameter("@OrderId", orderId));
            if (externalId.IsNullOrEmpty()) externalId = null;
            ModulesRepository.ModuleExecuteNonQuery(
                "Insert Into Module.OneSApi_Order (OrderId, ForExport, ExportType, Changed, ExternalId) Values (@OrderId, 1, @exportType, GetDate(), @externalId)",
                CommandType.Text,
                new SqlParameter("@OrderId", orderId), new SqlParameter("@exportType", exportType), new SqlParameter("@externalId", externalId ?? (object)DBNull.Value));
        }

        public static void SendOrder(int orderId, ExportType exportType)
        {
            var c = ModulesRepository.ModuleExecuteScalar<int>("Select Count(OrderId) From Module.OneSApi_Order WHERE OrderId = @OrderId AND ExportType = @exportType",
                CommandType.Text, new SqlParameter("@OrderId", orderId), new SqlParameter("@exportType", exportType));
            if (c > 0)
                ModulesRepository.ModuleExecuteNonQuery("Update Module.OneSApi_Order Set ForExport = 1, Changed = GetDate() Where OrderId = @OrderId AND ExportType = @exportType",
                CommandType.Text,
                new SqlParameter("@OrderId", orderId), new SqlParameter("@exportType", exportType));
            else
                DoOrderAdded(orderId, exportType);
        }

        public static void DoOrderChangeStatus(IOrder order)
        {
            var settings = ModuleService.GetImportExportSettings();
            if (settings.ExportOrder.ExportChangeStatus)
                SendOrder(order.OrderID, ExportType.Status);
        }

        public static void DoOrderUpdated(IOrder order)
        {
            var settings = ModuleService.GetImportExportSettings();
            if (settings.ExportOrder.ExportUpdated && !order.IsDraft)
                SendOrder(order.OrderID, ExportType.Order);

            OrderServiceV8.UpdateManagerName(order.OrderID);
        }

        public static void PayOrder(int orderId, bool payed)
        {
            if (payed)
            {
                var settings = ModuleService.GetImportExportSettings();
                if (settings.ExportOrder.ExportPayed)
                    SendOrder(orderId, ExportType.Payed);
            }
        }

        public static void SetExportedOrders(List<int> ids)
        {
            if (ids.Count == 0)
                return;
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Update Module.OneSApi_Order Set Exported = GetDate() Where OrderId IN ({0})", ids.AggregateString(',')),
                CommandType.Text);
        }

        public static void ConfirmOrders(List<ConfirmOrderModel_item> list)
        {
            foreach (var item in list)
            {
                //var order = OrderService.GetOrderByNumber(item.OrderId.ToString());
                //if (order != null)
                //{
                    ModulesRepository.ModuleExecuteNonQuery(
                        "Update Module.OneSApi_Order Set ExternalId = @ExternalId Where OrderId = @OrderId And ExportType = @ExportType",
                        CommandType.Text,
                        new SqlParameter("@OrderId", item.OrderId), new SqlParameter("@ExportType", item.ExportType), new SqlParameter("@ExternalId", item.ExternalId));
                    ModulesRepository.ModuleExecuteNonQuery(
                        "Update Module.OneSApi_Order Set ForExport = 0 Where OrderId = @OrderId And ExportType = @ExportType And [Changed] < [Exported]",
                        CommandType.Text,
                        new SqlParameter("@OrderId", item.OrderId), new SqlParameter("@ExportType", item.ExportType));
                //}
            }
        }

        public static List<OrderItemV8Model> GetOrderItems(int orderId)
        {
            var result =
                ModulesRepository.ModuleExecuteReadList("[Order].[sp_GetOrderItems]", CommandType.StoredProcedure,
                reader => new OrderItemV8Model
                {
                    Name = SQLDataHelper.GetString(reader, "Name"),
                    Price = Math.Round(SQLDataHelper.GetDecimal(reader, "Price"), 2),
                    Amount = Math.Round(SQLDataHelper.GetDecimal(reader, "Amount"), 3),
                    ArtNo = SQLDataHelper.GetString(reader, "ArtNo"),
                    Color = SQLDataHelper.GetString(reader, "Color", null),
                    Size = SQLDataHelper.GetString(reader, "Size", null),
                    TaxRate = SQLDataHelper.GetNullableFloat(reader, "TaxRate") ?? 0,
                    Unit = SQLDataHelper.GetString(reader, "Unit"),
                    ProductId = SQLDataHelper.GetNullableInt(reader, "ProductID") ?? 0,
                    ExternalId = ImportService.GetExternalIdById(SQLDataHelper.GetNullableInt(reader, "ProductID") ?? 0, "Product"),
                    Weight = SQLDataHelper.GetFloat(reader, "Weight")
                },
                new SqlParameter("@OrderID", orderId));

            return result;
        }

        public static int GetOrderIdFromExternalId(string externalId)
        {
            if (externalId.IsNullOrEmpty())
                return 0;
            return
                ModulesRepository.ModuleExecuteScalar<int>("Select OrderId From Module.OneSApi_Order Where ExternalId = @externalId",
                CommandType.Text,
                new SqlParameter("@externalId", externalId));
        }

        public static string GetExternalIdFromOrderId(int orderId)
        {
            return
                ModulesRepository.ModuleExecuteScalar<string>("Select ExternalId From Module.OneSApi_Order Where OrderId = @orderId",
                CommandType.Text,
                new SqlParameter("@orderId", orderId));
        }

        public static ExternalOrderModel GetExternalFromOrderId(int orderId)
        {
            return
                ModulesRepository.ModuleExecuteReadOne<ExternalOrderModel>("Select * From Module.OneSApi_Order Where OrderId = @orderId",
                CommandType.Text,
                reader => new ExternalOrderModel
                {
                    ExternalId = SQLDataHelper.GetString("ExternalId"),
                    ExportType = SQLDataHelper.GetInt(reader, "ExportType"),
                    Changed = SQLDataHelper.GetDateTime(reader, "Changed"),
                    Exported = SQLDataHelper.GetNullableDateTime(reader, "Exported"),
                });
        }

        public static List<int> GetAllOrderIds(DateTime from, DateTime to)
        {
            var settings = ModuleService.GetImportExportSettings();
            return ModulesRepository.ModuleExecuteReadColumn<int>(
                "SELECT OrderID FROM [Order].[Order] Where IsDraft <> 1 and OrderDate >= @from and OrderDate <= @to And IsDraft = 0 " +
                (settings.ExportOrder.UseIn1C ? "And UseIn1C = 1 " : ""),
                CommandType.Text, "OrderID",
                new SqlParameter("@from", from),
                new SqlParameter("@to", to));
        }

        public static void SendOrders(List<int> ids)
        {
            if (ids.Count == 0)
                return;

            var already = ModulesRepository.ModuleExecuteReadColumn<int>(
                string.Format("SELECT OrderId FROM Module.OneSApi_Order Where OrderId In ({0}) ", ids.AggregateString(',')),
                CommandType.Text, "OrderID");
            var need = ids.Where(x => !already.Contains(x)).ToList();

            foreach (var id in need)
                DoOrderAdded(id, ExportType.Order);
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Update Module.OneSApi_Order Set ForExport = 1, Changed = GetDate() Where OrderId In ({0})", already.AggregateString(',')),
                CommandType.Text);
        }

        public static void DeleteOrderFromExport(int orderId, int exportType)
        {
            ModulesRepository.ModuleExecuteNonQuery("Update Module.OneSApi_Order Set ForExport = 0, Exported = GetDate() Where OrderId = @orderId AND ExportType = @exportType",
            CommandType.Text,
            new SqlParameter("@orderId", orderId), new SqlParameter("@exportType", exportType));
        }

        public static List<OrderListModel> GetSelected(OrderListFilterModel filter)
        {
            return ModulesRepository.ModuleExecuteReadList(
                "Select OrderId, ExportType From Module.OneSApi_Order " +
                "Where OrderId <> 0 " + //where +
                GetWhereFromOrderFilter(filter, "[OrderId]"),
                CommandType.Text,
                reader => new OrderListModel
                {
                    OrderId = SQLDataHelper.GetInt(reader, "OrderId"),
                    ExportType = SQLDataHelper.GetInt(reader, "ExportType")
                });
        }

        public static string GetWhereFromOrderFilter(OrderListFilterModel filter, string fieldId)
        {
            if (filter == null || (filter.SelectMode == SelectModeCommand.None && filter.Ids.Count == 0))
                return "";

            string where = "";
            if (filter.SelectMode == SelectModeCommand.None)
                where = string.Format(" And {0} In ({1}) ", fieldId, filter.Ids.AggregateString(','));
            else
            {
                if (filter.Ids.Count > 0)
                    where = string.Format(" And {0} Not In ({1}) ", fieldId, filter.Ids.AggregateString(','));
            }

            return where;
        }

        public static List<OrderListModel> GetOrderList(OrderListFilterModel filter)
        {
            Type LocalizeAttributeType = typeof(LocalizeAttribute);
            return ModulesRepository.ModuleExecuteReadList(
                "Select OneSApi_Order.OrderId, ExportType, Number, OrderDate, Changed " +
                //string.Format("Case " +
                //    "When ExportType = 1 Then '{0}' " +
                //    "When ExportType = 2 Then '{1}' " +
                //    "When ExportType = 3 Then '{2}' " +
                //    "End As ChangeType ",
                //    ((IAttribute<string>)typeof(ExportType).GetField(ExportType.Order.ToString()).GetCustomAttributes(LocalizeAttributeType, false)[0]).Value,
                //    ((IAttribute<string>)typeof(ExportType).GetField(ExportType.Status.ToString()).GetCustomAttributes(LocalizeAttributeType, false)[0]).Value,
                //    ((IAttribute<string>)typeof(ExportType).GetField(ExportType.Payed.ToString()).GetCustomAttributes(LocalizeAttributeType, false)[0]).Value) +
                "From [Module].[OneSApi_Order] " +
                "Left Join [Order].[Order] ON [Order].[OrderId] = [OneSApi_Order].[OrderId] " +
                "Where ForExport = 1 " +
                GetWhereFromOrderFilter(filter, "OneSApi_Order.[OrderId]") +
                "Order By Changed ",
                CommandType.Text,
                reader => new OrderListModel
                {
                    OrderId = SQLDataHelper.GetInt(reader, "OrderId"),
                    ExportType = SQLDataHelper.GetInt(reader, "ExportType"),
                    Number = SQLDataHelper.GetString(reader, "Number"),
                    OrderDate = SQLDataHelper.GetDateTime(reader, "OrderDate"),
                    Changed = SQLDataHelper.GetDateTime(reader, "Changed"),
                    //ChangeType = SQLDataHelper.GetString(reader, "ChangeType"),
                });
        }

        public static void UpdateCustomerIds(List<ConfirmOrderModel_item> list)
        {
            foreach (var item in list.Where(x => x.CustomerId.IsNotEmpty()))
            {
                var order = OrderService.GetOrder(item.OrderId);
                var c = CustomerService.GetCustomer(order.OrderCustomer.CustomerID);
                if (order != null && c != null && item.CustomerId.ToLower() != order.OrderCustomer.CustomerID.ToString().ToLower())
                {
                    Debug.Log.Info(JsonConvert.SerializeObject(item));
                    Debug.Log.Info(JsonConvert.SerializeObject(order.OrderCustomer));
                    var nid = Guid.Parse(item.CustomerId);
                    if (CustomerService.GetCustomer(nid) == null)
                    {
                        c.Id = Guid.Parse(item.CustomerId);
                        c.EMail = null;
                        c.Phone = null;
                        c.StandardPhone = null;
                        Debug.Log.Info(JsonConvert.SerializeObject(c));
                        nid = CustomerService.InsertNewCustomer(c, trackChanges: false);
                    }
                    Debug.Log.Info(nid);
                    ModulesRepository.ModuleExecuteNonQuery(
                        "Update [Order].OrderCustomer Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].AdminNotifications Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Partners].BindedCustomer  Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Bonus].Card  Set CardId = @NewId Where CardId = @OldId; " +
                        "Update [Customers].Contact  Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Catalog].CouponCustomers   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].CustomerFieldValuesMap   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].CustomerRoleAction   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].CustomerSegment_Customer   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].FacebookUser   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].InstagramUser   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].ManagerRolesMap   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].Managers   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].ManagerTask   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].OkUser   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].OpenIdLinkCustomer   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].SmsNotifications   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].TagMap   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].Task   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].TelegramUser   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Partners].[Transaction]   Set CustomerId = @NewId Where CustomerId = @OldId; " +
                        "Update [Customers].VkUser   Set CustomerId = @NewId Where CustomerId = @OldId; ",
                        CommandType.Text,
                        new SqlParameter("@OldId", order.OrderCustomer.CustomerID.ToString()), new SqlParameter("@NewId", item.CustomerId));
                    ModulesRepository.ModuleExecuteNonQuery(
                        "Delete From [Customers].Customer Where CustomerId = @NewId ",
                        CommandType.Text,
                        new SqlParameter("@NewId", item.CustomerId));
                    ModulesRepository.ModuleExecuteNonQuery(
                        "Update [Customers].Customer Set CustomerId = @NewId Where CustomerId = @OldId; ",
                        CommandType.Text,
                        new SqlParameter("@OldId", order.OrderCustomer.CustomerID.ToString()), new SqlParameter("@NewId", item.CustomerId));
                }
            }
        }

        public static void CustomerUpdate(Customer customer)
        {
            if (OrderService.GetOrdersCountByCustomer(customer.Id) > 0)
                SendCustomer(customer.Id);
        }

        public static void SendCustomer(Guid customerId)
        {
            var c = ModulesRepository.ModuleExecuteScalar<int>("Select Count(CustomerId) From Module.OneSApi_Customer WHERE CustomerId = @CustomerId",
                CommandType.Text, new SqlParameter("@CustomerId", customerId));
            if (c > 0)
                ModulesRepository.ModuleExecuteNonQuery("Update Module.OneSApi_Customer Set ForExport = 1, Changed = GetDate() Where CustomerId = @CustomerId",
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId));
            else
                ModulesRepository.ModuleExecuteNonQuery("Insert Into Module.OneSApi_Customer " +
                    "(CustomerId, ForExport, Changed) Values (@CustomerId, 1, GetDate())",
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId));
        }

        public static void SetExportedCustomers(List<Guid> ids)
        {
            if (ids.Count == 0)
                return;
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Update Module.OneSApi_Customer Set Exported = GetDate() Where CustomerId IN ('{0}')", ids.AggregateString("','")),
                CommandType.Text);
        }

        public static void ConfirmCustomers(List<ConfirmCustomerModel_item> list)
        {
            foreach (var item in list)
            {
                ModulesRepository.ModuleExecuteNonQuery(
                    "Update Module.OneSApi_Customer Set ForExport = 0 Where CustomerId = @CustomerId And [Changed] < [Exported]",
                    CommandType.Text,
                    new SqlParameter("@CustomerId", item.CustomerId));
            }
        }

        public static void ConfirmCustomer(string customerId)
        {
            ConfirmCustomers(new List<ConfirmCustomerModel_item> { new ConfirmCustomerModel_item { CustomerId = customerId } });
        }

        public static void DoProductUpdated(int productId, ExportType exportType)
        {
            Debug.Log.Info("DoProductUpdated");
            var c = ModulesRepository.ModuleExecuteScalar<int>("Select Count(ProductId) From Module.OneSApi_ProductExport WHERE ProductId = @ProductId AND ExportType = @exportType",
                CommandType.Text, new SqlParameter("@ProductId", productId), new SqlParameter("@exportType", exportType));
            if (c > 0)
                ModulesRepository.ModuleExecuteNonQuery("Update Module.OneSApi_ProductExport Set ForExport = 1, Changed = GetDate() Where ProductId = @ProductId AND ExportType = @exportType",
                CommandType.Text,
                new SqlParameter("@ProductId", productId), new SqlParameter("@exportType", exportType));
            else
            {
                //var externalId = ModulesRepository.ModuleExecuteScalar<string>("Select TOP 1 ExternalId From Module.OneSApi_ProductExport WHERE ProductId = @ProductId AND ExternalId Is Not NULL",
                //    CommandType.Text, new SqlParameter("@ProductId", productId));
                //if (externalId.IsNullOrEmpty()) externalId = null;
                ModulesRepository.ModuleExecuteNonQuery(
                    "Insert Into Module.OneSApi_ProductExport (ProductId, ForExport, ExportType, Changed) Values (@ProductId, 1, @exportType, GetDate())",
                    CommandType.Text,
                    new SqlParameter("@ProductId", productId), new SqlParameter("@exportType", exportType)/*, new SqlParameter("@externalId", externalId ?? (object)DBNull.Value)*/);
            }
        }

        public static void DeleteProductFromExport(int productId, int exportType)
        {
            ModulesRepository.ModuleExecuteNonQuery("Update Module.OneSApi_ProductExport Set ForExport = 0, Exported = GetDate() Where ProductId = @productId AND ExportType = @exportType",
            CommandType.Text,
            new SqlParameter("@productId", productId), new SqlParameter("@exportType", exportType));
        }

        public static List<ProductListModel> GetProductList(ProductListFilterModel filter)
        {
            Type LocalizeAttributeType = typeof(LocalizeAttribute);
            return ModulesRepository.ModuleExecuteReadList(
                "Select OneSApi_Product.ProductId, Module.OneSApi_ProductExport.*, ArtNo, Name " +
                "From [Module].[OneSApi_Product] " +
                "Left Join Module.OneSApi_ProductExport On OneSApi_ProductExport.ProductId = OneSApi_Product.ProductId " +
                "Left Join [Catalog].[Product] ON [Product].[ProductId] = [OneSApi_Product].[ProductId] " +
                //"Where ForExport = 1 " +
                GetWhereFromProductFilter(filter, "OneSApi_Product.[ProductId]") +
                "Order By Changed ",
                CommandType.Text,
                reader => new ProductListModel
                {
                    ProductId = SQLDataHelper.GetInt(reader, "ProductId"),
                    ArtNo = SQLDataHelper.GetString(reader, "ArtNo"),
                    Name = SQLDataHelper.GetString(reader, "Name"),
                    ExportType = SQLDataHelper.GetNullableInt(reader, "ExportType") ?? 4,
                    Changed = SQLDataHelper.GetNullableDateTime(reader, "Changed") ?? DateTime.MinValue,
                    Exported = SQLDataHelper.GetNullableDateTime(reader, "Exported"),
                });
        }

        public static string GetWhereFromProductFilter(ProductListFilterModel filter, string fieldId)
        {
            if (filter == null || (filter.SelectMode == SelectModeCommand.None && filter.Ids.Count == 0))
                return "";

            string where = "";
            if (filter.SelectMode == SelectModeCommand.None)
                where = string.Format(" And {0} In ({1}) ", fieldId, filter.Ids.AggregateString(','));
            else
            {
                if (filter.Ids.Count > 0)
                    where = string.Format(" And {0} Not In ({1}) ", fieldId, filter.Ids.AggregateString(','));
            }

            return where;
        }

        public static ExternalProductModel GetExternalFromProductId(int productId)
        {
            return
                ModulesRepository.ModuleExecuteReadOne<ExternalProductModel>(
                    "Select ExternalId, Module.OneSApi_ProductExport.* From Module.OneSApi_Product " +
                    "Left Join Module.OneSApi_ProductExport On OneSApi_ProductExport.ProductId = OneSApi_Product.ProductId " +
                    "Where OneSApi_Product.ProductId = @productId",
                    CommandType.Text,
                    reader => new ExternalProductModel
                    {
                        ExternalId = SQLDataHelper.GetString("ExternalId"),
                        ExportType = SQLDataHelper.GetNullableInt(reader, "ExportType") ?? 4,
                        Changed = SQLDataHelper.GetNullableDateTime(reader, "Changed") ?? DateTime.MinValue,
                        Exported = SQLDataHelper.GetNullableDateTime(reader, "Exported"),
                    }, new SqlParameter("@productId", productId));
        }

        public static void SetExportedProducts(List<int> ids)
        {
            if (ids.Count == 0)
                return;
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Update Module.OneSApi_ProductExport Set Exported = GetDate() Where ProductId IN ({0})", ids.AggregateString(',')),
                CommandType.Text);
        }

        public static void ConfirmProducts(List<ConfirmProductModel_item> list)
        {
            foreach (var item in list)
            {
                //ModulesRepository.ModuleExecuteNonQuery(
                //    "Update Module.OneSApi_ProductExport Set ExternalId = @ExternalId Where ProductId = @ProductId And ExportType = @ExportType",
                //    CommandType.Text,
                //    new SqlParameter("@ProductId", item.ProductId), new SqlParameter("@ExportType", item.ExportType), new SqlParameter("@ExternalId", item.ExternalId));
                ModulesRepository.ModuleExecuteNonQuery(
                    "Update Module.OneSApi_ProductExport Set ForExport = 0 Where ProductId = @ProductId And ExportType = @ExportType And [Changed] < [Exported]",
                    CommandType.Text,
                    new SqlParameter("@ProductId", item.ProductId), new SqlParameter("@ExportType", item.ExportType));
            }
        }

    }
}
