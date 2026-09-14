using AdvantShop.Core.Modules;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class ProductOnMainService
    {
        public static bool IsExistsProductRecomended()
        {
            string sqlCmd = "if exists(select 1 from Catalog.Product where Enabled=1 and Hidden=0 and CategoryEnabled=1 and {0}) Select 1 else Select 0";
            sqlCmd = string.Format(sqlCmd, "Recomended=1");
            return ModulesRepository.ModuleExecuteScalar<bool>(sqlCmd, CommandType.Text);
        }
    }
}
