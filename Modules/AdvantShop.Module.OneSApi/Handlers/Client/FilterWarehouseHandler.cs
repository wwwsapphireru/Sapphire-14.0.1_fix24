using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Module.OneSApi.Models.Client;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterWarehouseHandler
    {
        #region Fields

        private readonly int _categoryId;
        private readonly bool _indepth;
        private readonly EProductOnMain _flag;
        private readonly int? _listId;
        private readonly bool _showOnlyAvailable;
        private readonly List<int> _productIds;

        private readonly List<int> _selectedWarehouseIds;
        private readonly List<int> _availableWarehouseIds;

        #endregion

        #region Constructor

        public FilterWarehouseHandler(int categoryId, bool indepth, List<int> selectedWarehouseIds, List<int> availableWarehouseIds,
            EProductOnMain flag = EProductOnMain.None, int? listId = null, bool showOnlyAvailable = false, 
            List<int> productIds = null)
        {
            _categoryId = categoryId;
            _indepth = indepth;
            _flag = flag;
            _selectedWarehouseIds = selectedWarehouseIds ?? new List<int>();
            _availableWarehouseIds = availableWarehouseIds;
            _listId = listId;
            _showOnlyAvailable = showOnlyAvailable;
            _productIds = productIds;
        }

        #endregion

        public FilterItemModel Get()
        {
            // var warehouses = _flag == EProductOnMain.None
            //
            //     ? CacheManager.Get(CacheNames.BrandsInCategoryCacheName(_categoryId, _indepth, (int) _flag, 0, _productIds, SettingsCatalog.ShowOnlyAvalible),
            //         () => BrandService.GetBrandsByCategoryId(_categoryId, _indepth, _productIds, SettingsCatalog.ShowOnlyAvalible))
            //
            //     : CacheManager.Get(CacheNames.BrandsInCategoryCacheName(0, false, (int) _flag, _listId ?? 0),
            //         () => BrandService.GetBrandsByProductOnMain(_flag, _listId));

            var warehouses = WarehouseService.GetList();

            if (warehouses == null || warehouses.Count == 0)
                return null;

            var filterWarehouse = new FilterItemModel()
            {
                Expanded = true,
                Type = "warehouse",
                Title = LocalizationService.GetResource("Catalog.FilterWarehouse.Warehouses"),
                Subtitle = "",
                Control = "checkbox"
            };

            foreach (var warehouse in warehouses)
            {
                if (_showOnlyAvailable && !(_availableWarehouseIds == null || _availableWarehouseIds.Contains(warehouse.Id)))
                    continue;

                var selected = _selectedWarehouseIds.Contains(warehouse.Id);

                filterWarehouse.Values.Add(new FilterListItemModel()
                {
                    Id = warehouse.Id.ToString(),
                    Text = warehouse.Name,
                    Selected = selected,
                    Available = selected || _availableWarehouseIds == null || _availableWarehouseIds.Contains(warehouse.Id) || _selectedWarehouseIds.Any()
                });
            }

            if (filterWarehouse.Values == null || filterWarehouse.Values.Count == 0)
                return null;

            return filterWarehouse;
        }

        public Task<List<FilterItemModel>> GetAsync()
        {
            return Task.Run(() =>
            {
                Localization.Culture.InitializeCulture();

                return new List<FilterItemModel> {Get()};
            });
        }
    }
}