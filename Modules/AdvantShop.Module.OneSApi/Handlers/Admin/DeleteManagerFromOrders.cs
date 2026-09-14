using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AdvantShop.Core.Modules;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class DeleteManagerFromOrders
    {
        //private bool _withStatisctic;

        public DeleteManagerFromOrders(/*bool withStatisctic*/)
        {
            //_withStatisctic = withStatisctic;
        }

        public string Execute(int managerId)
        {
            try
            {
                var manager = ManagerService.GetManager(managerId);
                if (manager != null)
                {
                    var ids = ModulesRepository.ModuleExecuteReadColumn<int>(
                        "Select OrderId From [Order].[Order] Where ManagerId = @managerId ",
                        CommandType.Text, "OrderId",
                        new SqlParameter("@managerId", managerId));
                    foreach (var id in ids)
                        OrderServiceV8.UpdateManagerName(id, manager);
                }

                ModulesRepository.ModuleExecuteNonQuery(
                    "Update [Order].[Order] Set ManagerId = NULL Where ManagerId = @managerId ",
                    CommandType.Text,
                    new SqlParameter("@managerId", managerId));

                return null;
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                return ex.Message;
            }
        }

    }
}
