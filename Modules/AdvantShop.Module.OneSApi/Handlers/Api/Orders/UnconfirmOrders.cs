using AdvantShop.Core.Common.Extensions;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Orders
{
    public class UnconfirmOrders : ICommandHandler<ConfirmOrderModel, bool>
    {
        public bool Execute(ConfirmOrderModel model)
        {
            try
            {
                foreach (var item in model.Items)
                {
                    ExportService.SendOrder(item.OrderId, item.ExportType);
                }
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return false;
            }

            //ModuleService.AppendLog("GetOrders", model.LogFile, JsonConvert.SerializeObject(model.Items, Formatting.Indented));

            return true;
        }
    }
}
