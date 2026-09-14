using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Products
{
    public class ConfirmProducts : ICommandHandler<ConfirmProductModel, bool>
    {
        public bool Execute(ConfirmProductModel model)
        {
            try
            {
                ExportService.ConfirmProducts(model.Items/*.Where(x => x.Success).ToList()*/);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return false;
            }

            ModuleService.AppendLog("ExportProducts", model.LogFile, JsonConvert.SerializeObject(model.Items, Formatting.Indented));

            return true;
        }
    }
}
