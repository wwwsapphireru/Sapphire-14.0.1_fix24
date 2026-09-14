using AdvantShop.Core.Modules;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class ShippingMethodsService
    {

        public static List<ShippingMethodModel> GetShippingMethods()
        {
            return ModulesRepository.ModuleExecuteReadList<ShippingMethodModel>(
                @"SELECT * FROM [Module].[OneSApi_ShippingMethod] ",
                CommandType.Text,
                reader => GetShippingMethodFromReader(reader));
        }

        private static ShippingMethodModel GetShippingMethodFromReader(SqlDataReader reader)
        {
            return new ShippingMethodModel
            {
                ShippingMethodKey = SQLDataHelper.GetString(reader, "ShippingMethodKey"),
                TrackingUrl = SQLDataHelper.GetString(reader, "TrackingUrl"),
            };
        }

        public static ShippingMethodModel GetShippingMethod(string key)
        {
            var result = ModulesRepository.ModuleExecuteReadOne(
                @"SELECT * FROM [Module].[OneSApi_ShippingMethod] Where [ShippingMethodKey]=@key",
                CommandType.Text,
                reader => GetShippingMethodFromReader(reader),
                new SqlParameter("@key", key));

            return result;
        }

        public static void AddUpdateShippingMethod(ShippingMethodModel model)
        {
            var m = GetShippingMethod(model.ShippingMethodKey);
            string sql;
            if (m == null)
                sql = @"INSERT INTO [Module].[OneSApi_ShippingMethod] (ShippingMethodKey, TrackingUrl) Values (@ShippingMethodKey, @TrackingUrl) ";
            else
                sql = @"UPDATE [Module].[OneSApi_ShippingMethod] Set ShippingMethodKey=@ShippingMethodKey, TrackingUrl=@TrackingUrl Where [ShippingMethodKey]=@ShippingMethodKey ";
            ModulesRepository.ModuleExecuteNonQuery(
                sql, CommandType.Text,
                new SqlParameter("@ShippingMethodKey", model.ShippingMethodKey),
                new SqlParameter("@TrackingUrl", model.TrackingUrl ?? (object)DBNull.Value)
                );
        }

    }
}
