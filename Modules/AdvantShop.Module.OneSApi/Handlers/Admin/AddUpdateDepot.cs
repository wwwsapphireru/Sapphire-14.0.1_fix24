using System;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Handlers;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class AddUpdateDepot : AbstractCommandHandler<bool>
    {
        private readonly DepotModel _depot;

        public AddUpdateDepot(DepotModel depot)
        {
            _depot = depot;
        }

        protected override bool Handle()
        {
            try
            {
                if (_depot.DepotId == 0)
                {
                    DepotAmountsService.AddDepot(_depot);
                }
                else
                {
                    DepotAmountsService.UpdateDepot(_depot);
                }
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
