using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Handlers.Catalog;
using AdvantShop.Models.Catalog;

namespace AdvantShop.Handlers.Search
{
    public class GetSearchFilterHandler
    {
        private readonly SearchCatalogModel _searchModel;

        public GetSearchFilterHandler(SearchCatalogModel searchModel)
        {
            _searchModel = searchModel;
        }

        public List<FilterItemModel> Execute()
        {
            var model = new SearchPagingHandler(_searchModel, false).GetForFilter();
            
            var filter = model.Filter;
            filter.Indepth = true;

            var productIds =
                model.Filter.SearchItemsResult != null && model.Filter.SearchItemsResult.Count > 0
                    ? model.Filter.SearchItemsResult.Take(1000).OrderBy(x => x).ToList()
                    : null;

            var tasks = new List<Task<List<FilterItemModel>>>
            {
                new FilterInputHandler(filter.CategoryId, false, _searchModel.Q).GetAsync(),
                new FilterSelectCategoryHandler(filter.CategoryId,/*GlorySoft_024*/ (_searchModel.ProductIds ?? "").Split(',').Select(x => x.TryParseInt()).ToList()).GetAsync()
            };

            if (productIds != null && productIds.Count > 0)
            {
                if (SettingsCatalog.ShowPriceFilter && !SettingsCatalog.HidePrice)
                {
                    tasks.Add(
                        new FilterPriceHandler(productIds, filter.PriceFrom, filter.PriceTo)
                            .GetAsync());
                }

                if (SettingsCatalog.ShowProducerFilter)
                {
                    tasks.Add(
                        new FilterBrandHandler(filter.CategoryId, filter.Indepth, filter.BrandIds,
                                filter.AvailableBrandIds, showOnlyAvailable: true,
                                productIds: productIds)
                            .GetAsync());
                }

                if (SettingsCatalog.ShowColorFilter &&/*GlorySoft_024*/ filter.CategoryId != 0)
                {
                    tasks.Add(
                        new FilterColorHandler(productIds,
                                filter.ColorIds, filter.AvailableColorIds,
                                SettingsCatalog.ShowOnlyAvalible || filter.Available,
                                filter.ColorsViewMode)
                            .GetAsync());
                }

                if (SettingsCatalog.ShowSizeFilter)
                {
                    tasks.Add(
                        new FilterSizeHandler(productIds, 
                                filter.SizeIds, filter.AvailableSizeIds,
                                SettingsCatalog.ShowOnlyAvalible || filter.Available)
                            .GetAsync());
                }

                if (SettingsCatalog.ShowWarehouseFilter)
                {
                    tasks.Add(
                        new FilterWarehouseHandler(filter.CategoryId, filter.Indepth, filter.WarehouseIds,
                                filter.AvailableWarehouseIds,
                                showOnlyAvailable: false, productIds: productIds)
                            .GetAsync());
                }

                if (filter.CategoryId != 0)//GlorySoft_024
                {
                    tasks.Add(
                    new FilterPropertyHandler(filter.CategoryId, filter.Indepth, filter.PropertyIds,
                            filter.AvailablePropertyIds, filter.RangePropertyIds,
                            productIds)
                        .GetAsync());
                }
            }

            var result = tasks.Select(x => x.Result)
                .SelectMany(x => x)
                .Where(x => x != null)
                .ToList();

            return result;
        }
    }
}