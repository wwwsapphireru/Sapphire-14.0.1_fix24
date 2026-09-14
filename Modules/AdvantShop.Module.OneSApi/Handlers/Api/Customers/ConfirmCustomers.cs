using AdvantShop.Core.Common.Extensions;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Customers
{
    public class ConfirmCustomers : ICommandHandler<ConfirmCustomerModel, bool>
    {
        public bool Execute(ConfirmCustomerModel model)
        {
            try
            {
                ExportService.ConfirmCustomers(model.Items/*.Where(x => x.Success).ToList()*/);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return false;
            }

            //ModuleService.AppendLog("GetOrders", model.LogFile, JsonConvert.SerializeObject(model, Formatting.Indented));

            return true;
        }
    }
}
