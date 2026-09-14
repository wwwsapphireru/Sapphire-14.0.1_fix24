using System;
using System.Linq;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Admin;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class GetShippingMethods
    {
        public FilterResult<ShippingMethodModel> Execute()
        {
            var methods = ShippingMethodsService.GetShippingMethods();
            
            var model = new FilterResult<ShippingMethodModel>
            {
                TotalItemsCount = methods.Count,
                TotalPageCount = 1,
                DataItems = methods,
                TotalString = "Всего: " + methods.Count
            };

            return model;
        }

    }
}
