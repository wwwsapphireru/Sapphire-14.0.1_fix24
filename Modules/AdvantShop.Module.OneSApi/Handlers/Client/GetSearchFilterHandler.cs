using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.FullSearch;
using AdvantShop.Core.SQL2;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Repository.Currencies;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class GetSearchFilterHandler
    {
        private readonly SearchCatalogModel _searchModel;
        //private readonly ISearchService _searchService;
        //private SqlPaging _paging;

        public GetSearchFilterHandler(SearchCatalogModel searchModel)
        {
            _searchModel = searchModel;
            //_searchService = new SearchService();
            //BuildPaging();
            //searchModel.ProductIds = _searchService.FindForPaging(searchModel.Q, _paging, ESortOrder.NoSorting);
        }

        public List<FilterItemModel> Execute()
        {
            var model = new SearchPagingHandler(_searchModel, false).GetForFilter();

            var filter = model.Filter;
            filter.Indepth = true;

            var tasks = new List<Task<List<FilterItemModel>>>
            {
                new FilterInputHandler(filter.CategoryId, false, _searchModel.Q).GetAsync(),
                new FilterSelectCategoryHandler(filter.CategoryId, (_searchModel.ProductIds ?? "").Split(',').Select(x => x.TryParseInt()).ToList()).GetAsync()
            };
            
            if (SettingsCatalog.ShowPriceFilter && !SettingsCatalog.HidePrice)
            {
                tasks.Add(
                    new FilterPriceHandler(filter.CategoryId, filter.Indepth, filter.PriceFrom, filter.PriceTo)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowProducerFilter && model.Filter.SearchItemsResult != null && model.Filter.SearchItemsResult.Any())
            {
                tasks.Add(
                    new FilterBrandHandler(filter.CategoryId, filter.Indepth, filter.BrandIds,
                                                filter.AvailableBrandIds, showOnlyAvailable: true, 
                                                productIds: model.Filter.SearchItemsResult)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowColorFilter && filter.CategoryId != 0)
            {
                tasks.Add(
                    new FilterColorHandler(filter.CategoryId, filter.Indepth, filter.ColorIds,
                                                filter.AvailableColorIds, 
                                                SettingsCatalog.ShowOnlyAvalible || filter.Available, filter.ColorsViewMode)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowSizeFilter && filter.CategoryId != 0)
            {
                tasks.Add(
                    new FilterSizeHandler(filter.CategoryId, filter.Indepth, filter.SizeIds, filter.AvailableSizeIds,
                                            SettingsCatalog.ShowOnlyAvalible || filter.Available)
                        .GetAsync());
            }
            
            if (SettingsCatalog.ShowWarehouseFilter && model.Filter.SearchItemsResult != null && model.Filter.SearchItemsResult.Any())
            {
                tasks.Add(
                    new FilterWarehouseHandler(filter.CategoryId, filter.Indepth, filter.WarehouseIds, filter.AvailableWarehouseIds,
                            showOnlyAvailable: false, productIds: model.Filter.SearchItemsResult)
                       .GetAsync());
            }


            if (model.Filter.SearchItemsResult != null && model.Filter.SearchItemsResult.Any() && filter.CategoryId != 0)
            {
                tasks.Add(
                    new FilterPropertyHandler(filter.CategoryId, filter.Indepth, filter.PropertyIds,
                                                    filter.AvailablePropertyIds, filter.RangePropertyIds,
                                                    model.Filter.SearchItemsResult)
                        .GetAsync());
            }

            var result = tasks.Select(x => x.Result)
                .SelectMany(x => x)
                .Where(x => x != null)
                .ToList();

            return result;
        }

        #region SearchPagingHandler

        //private void BuildPaging()
        //{
        //    _paging = new SqlPaging()
        //        .Select(
        //            "Product.ProductID",
        //            "CountPhoto",
        //            "Photo.PhotoId",
        //            "Photo.PhotoName",
        //            "Photo.PhotoNameSize1",
        //            "Photo.PhotoNameSize2",
        //            "Photo.Description".AsSqlField("PhotoDescription"),
        //            "Product.ArtNo",
        //            "Product.Name",
        //            "Recomended".AsSqlField("Recomend"),
        //            "Product.Bestseller",
        //            "Product.New",
        //            "Product.OnSale".AsSqlField("Sales"),
        //            "Product.Discount",
        //            "Product.DiscountAmount",
        //            "Product.BriefDescription",
        //            "Product.MinAmount",
        //            "Product.MaxAmount",
        //            "Product.Enabled",
        //            "Product.AllowPreOrder",
        //            "Product.Ratio",
        //            "Product.ManualRatio",
        //            "Product.UrlPath",
        //            "Product.DateAdded",
        //            "Product.DoNotApplyOtherDiscounts",
        //            "Product.Multiplicity",
        //            "Offer.OfferID",
        //            "Offer.Amount".AsSqlField("AmountOffer"),
        //            "Offer.ArtNo".AsSqlField("OfferArtNo"),
        //            "Offer.ColorID",
        //            "Offer.SizeID",
        //            "MaxAvailable".AsSqlField("Amount"),
        //            "Comments",
        //            "CurrencyValue",
        //            "Gifts"
        //        )
        //        .From("[Catalog].[Product]")
        //        .Left_Join("[Catalog].[ProductExt] ON [Product].[ProductID] = [ProductExt].[ProductID]")
        //        .Left_Join("[Catalog].[Photo] ON [Photo].[PhotoId] = [ProductExt].[PhotoId]")
        //        .Left_Join("[Catalog].[Offer] ON [ProductExt].[OfferID] = [Offer].[OfferID]")
        //        .Inner_Join("[Catalog].[Currency] ON [Currency].[CurrencyID] = [Product].[CurrencyID]");

        //    if (SettingsCatalog.ComplexFilter)
        //    {
        //        _paging.Select(
        //            "Colors",
        //            "NotSamePrices".AsSqlField("MultiPrices"),
        //            "MinPrice".AsSqlField("BasePrice")
        //        );
        //    }
        //    else
        //    {
        //        _paging.Select(
        //            "null".AsSqlField("Colors"),
        //            "0".AsSqlField("MultiPrices"),
        //            "Price".AsSqlField("BasePrice")
        //        );
        //    }

        //    BuildFilter();
        //    //BuildSorting();

        //    _paging.ItemsPerPage = /*_currentPageIndex != 0 ?*/ SettingsCatalog.ProductsPerPage /*: int.MaxValue*/;
        //    _paging.CurrentPageIndex = /*_currentPageIndex != 0 ? _currentPageIndex :*/ 1;
        //}

        //private void BuildFilter()
        //{
        //    _paging.Where("Product.Enabled={0}", true)
        //           .Where("AND CategoryEnabled={0}", true)
        //           .Where("AND (Offer.Main={0} OR Offer.Main IS NULL)", true);

        //    if (!_searchModel.CategoryId.HasValue)
        //    {
        //        _paging.Where(
        //            "AND Exists(Select 1 From [Catalog].[ProductCategories] Where ProductCategories.ProductId = [Product].[ProductID])");
        //    }
        //    else
        //    {
        //        _paging.Where(
        //            "AND Exists(Select 1 From [Catalog].[ProductCategories] INNER JOIN [Settings].[GetChildCategoryByParent]({0}) AS hCat ON hCat.id = [ProductCategories].[CategoryID] and  ProductCategories.ProductId = [Product].[ProductID])",
        //            _searchModel.CategoryId);
        //    }

        //    if (SettingsCatalog.ShowOnlyAvalible)
        //    {
        //        _paging.Where("AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1)");
        //    }

        //    //if (!string.IsNullOrEmpty(_searchModel.Q))
        //    //{
        //    //    _model.Filter.SearchItemsResult = _searchService.FindForPaging(_searchModel.Q, _paging, _searchModel.Sort ?? ESortOrder.NoSorting);
        //    //}

        //    var currency = CurrencyService.CurrentCurrency;
        //    if (SettingsCatalog.DefaultCurrencyIso3 != currency.Iso3)
        //    {
        //        _paging.Where("", currency.Iso3);
        //    }

        //    if (_searchModel.PriceFrom.HasValue || _searchModel.PriceTo.HasValue)
        //    {
        //        var pricefrom = _searchModel.PriceFrom ?? 0;
        //        var priceto = _searchModel.PriceTo ?? int.MaxValue;

        //        //_model.Filter.PriceFrom = pricefrom;
        //        //_model.Filter.PriceTo = priceto;

        //        _paging.Where("AND (ProductExt.PriceTemp >= {0} ", pricefrom * currency.Rate);
        //        _paging.Where("AND  ProductExt.PriceTemp <= {0})", priceto * currency.Rate);
        //    }

        //    if (!string.IsNullOrEmpty(_searchModel.Brand))
        //    {
        //        var brandIds = _searchModel.Brand.Split(',').Select(x => x.TryParseInt()).Where(x => x != 0).ToList();
        //        if (brandIds.Count > 0)
        //        {
        //            //_model.Filter.BrandIds = brandIds;
        //            _paging.Where("AND Product.BrandID IN ({0})", brandIds.ToArray());
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(_searchModel.Size))
        //    {
        //        var sizeIds = _searchModel.Size.Split(',').Select(item => item.TryParseInt()).Where(id => id != 0).ToList();
        //        if (sizeIds.Count > 0)
        //        {
        //            //_model.Filter.SizeIds = sizeIds;
        //            _paging.Where(
        //                SettingsCatalog.ShowOnlyAvalible || _searchModel.Available
        //                    ? "and Exists(Select 1 from [Catalog].[Offer] where Offer.[SizeID] IN ({0}) and Offer.ProductId = [Product].[ProductID] AND (Offer.amount > 0 OR [Product].[AllowPreOrder] = 1))"
        //                    : "and Exists(Select 1 from [Catalog].[Offer] where Offer.[SizeID] IN ({0}) and Offer.ProductId = [Product].[ProductID])",
        //                sizeIds.ToArray());
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(_searchModel.Color))
        //    {
        //        var colorIds = _searchModel.Color.Split(',').Select(item => item.TryParseInt()).Where(id => id != 0).ToList();
        //        if (colorIds.Count > 0)
        //        {
        //            //_model.Filter.ColorIds = colorIds;
        //            _paging.Where(
        //                 SettingsCatalog.ShowOnlyAvalible || _searchModel.Available
        //                    ? "and Exists(Select 1 from [Catalog].[Offer] where Offer.[ColorID] IN ({0}) and Offer.ProductId = [Product].[ProductID] AND (Offer.amount > 0 OR [Product].[AllowPreOrder] = 1))"
        //                    : "and Exists(Select 1 from [Catalog].[Offer] where Offer.[ColorID] IN ({0}) and Offer.ProductId = [Product].[ProductID])",
        //                colorIds.ToArray());
        //        }

        //        if (SettingsCatalog.ComplexFilter)
        //        {
        //            _paging.Select(
        //                string.Format(
        //                    "(select Top 1 PhotoName from catalog.Photo inner join Catalog.Offer on Photo.objid=Offer.Productid and Type='product'" +
        //                    " Where Offer.ProductId=Product.ProductId and Photo.ColorID in({0}) Order by Photo.Main Desc, Photo.PhotoSortOrder)",
        //                    /*_model.Filter.C*/colorIds.AggregateString(",")).AsSqlField("AdditionalPhoto"));
        //        }
        //        else
        //        {
        //            _paging.Select("null".AsSqlField("AdditionalPhoto"));
        //        }
        //    }
        //    else
        //    {
        //        _paging.Select("null".AsSqlField("AdditionalPhoto"));
        //    }

        //    if (!string.IsNullOrEmpty(_searchModel.Warehouse))
        //    {
        //        var warehouseIds = _searchModel.Warehouse
        //                                       .Split(',')
        //                                       .Select(item => item.TryParseInt())
        //                                       .Where(id => id != 0)
        //                                       .ToList();
        //        if (warehouseIds.Count > 0)
        //        {
        //            //_model.Filter.WarehouseIds = warehouseIds;
        //            _paging.Where(
        //                "and Exists(Select 1 from [Catalog].[Offer] "
        //                + "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] "
        //                + "where [WarehouseStocks].[WarehouseId] IN ({0}) "
        //                + "     and Offer.ProductId = [Product].[ProductID] "
        //                + "     AND [WarehouseStocks].[Quantity] > 0)",
        //                warehouseIds.ToArray());
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(_searchModel.Prop))
        //    {
        //        var selectedPropertyIDs = new List<int>();
        //        var filterCollection = _searchModel.Prop.Split('-');
        //        foreach (var val in filterCollection)
        //        {
        //            var tempListIds = new List<int>();
        //            foreach (int id in val.Split(',').Select(item => item.TryParseInt()).Where(id => id != 0))
        //            {
        //                tempListIds.Add(id);
        //                selectedPropertyIDs.Add(id);
        //            }
        //            if (tempListIds.Count > 0)
        //                _paging.Where("AND Exists(Select 1 from [Catalog].[ProductPropertyValue] where [Product].[ProductID] = [ProductID] and PropertyValueID IN ({0}))", tempListIds.ToArray());
        //        }
        //        //_model.Filter.PropertyIds = selectedPropertyIDs;
        //    }

        //    var rangeIds = new Dictionary<int, KeyValuePair<float, float>>();
        //    var rangeQueries =
        //        HttpContext.Current.Request.QueryString.AllKeys.Where(
        //            p => p != null && p.StartsWith("prop_") && (p.EndsWith("_min") || p.EndsWith("_max"))).ToList();

        //    foreach (var rangeQuery in rangeQueries)
        //    {
        //        if (rangeQuery.EndsWith("_max"))
        //            continue;

        //        var propertyId = rangeQuery.Split('_')[1].TryParseInt();
        //        if (propertyId == 0)
        //            continue;

        //        var min = HttpContext.Current.Request.QueryString[rangeQuery].TryParseFloat();
        //        var max = HttpContext.Current.Request.QueryString[rangeQuery.Replace("min", "max")].TryParseFloat();

        //        rangeIds.Add(propertyId, new KeyValuePair<float, float>(min, max));
        //    }

        //    if (_searchModel.PropertyRanges != null && _searchModel.PropertyRanges.Count > 0)
        //    {
        //        foreach (var propertyRange in _searchModel.PropertyRanges)
        //            rangeIds.Add(propertyRange.Id, new KeyValuePair<float, float>(propertyRange.Min, propertyRange.Max));
        //    }

        //    rangeIds = ModulesExecuter.GetRangeIds(rangeIds);
        //    if (rangeIds.Count > 0)
        //    {
        //        foreach (var id in rangeIds.Keys)
        //        {
        //            _paging.Where(
        //                "AND Exists( select 1 from [Catalog].[ProductPropertyValue] " +
        //                "Inner Join [Catalog].[PropertyValue] on [PropertyValue].[PropertyValueID] = [ProductPropertyValue].[PropertyValueID] " +
        //                "Where [Product].[ProductID] = [ProductID] and PropertyId = {0} and RangeValue >= {1} and RangeValue <= {2})",
        //                id, rangeIds[id].Key, rangeIds[id].Value);
        //        }
        //    }
        //    //_model.Filter.RangePropertyIds = rangeIds;

        //    //_model.Filter.Available = _searchModel.Available;
        //}

        #endregion
    }
}