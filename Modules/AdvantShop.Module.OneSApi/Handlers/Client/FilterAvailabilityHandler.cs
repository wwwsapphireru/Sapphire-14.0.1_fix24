using System.Threading.Tasks;
using AdvantShop.Configuration;
using AdvantShop.Core.Services.Localization;
using System.Collections.Generic;
using AdvantShop.Module.OneSApi.Models.Client;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterAvailabilityHandler
    {
        private readonly bool _isSelectedAvailable;

        public FilterAvailabilityHandler(bool available)
        {
            _isSelectedAvailable = available;
        }


        public List<FilterItemModel> Get()
        {
            var model = new List<FilterItemModel>();

            if (!SettingsCatalog.AvaliableFilterEnabled)
                return model;

            var filter = new FilterItemModel()
            {
                Title = LocalizationService.GetResource("Catalog.Availability"),
                Control = "checkbox",
                Type = "available",
                Expanded = false,
                Values = new List<object>()
                {
                    new FilterListItemModel()
                    {
                        Id = "true",
                        Text = LocalizationService.GetResource("Catalog.InStock"),
                        Available = true,
                        Selected = _isSelectedAvailable
                    }
                }
            };

            model.Add(filter);
            return model;
        }


        public Task<List<FilterItemModel>> GetAsync()
        {
            return Task.Run(() =>
            {
                Localization.Culture.InitializeCulture();
                return Get();
            });
        }

    }
}