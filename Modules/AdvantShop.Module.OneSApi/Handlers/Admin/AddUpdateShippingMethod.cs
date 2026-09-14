using System;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Handlers;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class AddUpdateShippingMethod : AbstractCommandHandler<bool>
    {
        private readonly ShippingMethodModel _method;

        public AddUpdateShippingMethod(ShippingMethodModel method)
        {
            _method = method;
        }

        protected override bool Handle()
        {
            try
            {
                ShippingMethodsService.AddUpdateShippingMethod(_method);
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);

                return false;
            }

            return true;
        }

    }
}
