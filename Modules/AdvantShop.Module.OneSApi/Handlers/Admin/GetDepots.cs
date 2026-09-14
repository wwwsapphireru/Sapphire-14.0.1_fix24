using System;
using System.Linq;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Admin;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class GetDepots
    {
        public FilterResult<DepotModel> Execute()
        {
            var depots = DepotAmountsService.GetDepots();
            
            var model = new FilterResult<DepotModel>
            {
                TotalItemsCount = depots.Count,
                TotalPageCount = 1,
                DataItems = depots,
                TotalString = "Всего: " + depots.Count
            };

            return model;
        }

    }
}
