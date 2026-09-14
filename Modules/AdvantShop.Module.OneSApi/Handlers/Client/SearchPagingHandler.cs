using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Models;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.Core.Services.FullSearch;
using AdvantShop.Core.SQL2;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Repository.Currencies;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class SearchPagingHandler
    {
        #region Fields

        private readonly int _currentPageIndex;
        private readonly ISearchService _searchService;

        private readonly SearchCatalogModel _searchModel;
        private readonly bool _isMobile;
        private readonly SearchCatalogViewModel _model;

        private SqlPaging _paging;

        #endregion

        #region Constructor

        public SearchPagingHandler(SearchCatalogModel model, bool isMobile)
        {
            _searchModel = model;
            _isMobile = isMobile;
            _currentPageIndex = model.Page ?? 1;
            _searchService = new SearchService();

            _model = new SearchCatalogViewModel(model.CategoryId ?? 0)
            {
                SearchCatalogModel = _searchModel
            };
        }

        #endregion

        public SearchCatalogViewModel Get()
        {
            _model.Categories = SettingsCatalog.SearchByCategories ? GetCategories() : new CategoryListViewModel();

            GetProducts();

            return _model;
        }

        private void BuildPaging()
        {
            _paging = new SqlPaging()
                .Select(
                    "Product.ProductID",
                    "CountPhoto",
                    "Photo.PhotoId",
                    "Photo.PhotoName",
                    "Photo.PhotoNameSize1",
                    "Photo.PhotoNameSize2",
                    "Photo.Description".AsSqlField("PhotoDescription"),
                    "Product.ArtNo",
                    "Product.Name",
                    "Recomended".AsSqlField("Recomend"),
                    "Product.Bestseller",
                    "Product.New",
                    "Product.OnSale".AsSqlField("Sales"),
                    "Product.Discount",
                    "Product.DiscountAmount",
                    "Product.BriefDescription",
                    "Product.MinAmount",
                    "Product.MaxAmount",
                    "Product.Enabled",
                    "Product.AllowPreOrder",
                    "Product.Ratio",
                    "Product.ManualRatio",
                    "Product.UrlPath",
                    "Product.DateAdded",
                    "Product.DoNotApplyOtherDiscounts",
                    "Product.Multiplicity",
                    "Offer.OfferID",
                    "Offer.Amount".AsSqlField("AmountOffer"),
                    "Offer.ArtNo".AsSqlField("OfferArtNo"),
                    "Offer.ColorID",
                    "Offer.SizeID",
                    "MaxAvailable".AsSqlField("Amount"),
                    "Comments",
                    "CurrencyValue",
                    "Gifts"
                )
                .From("[Catalog].[Product]")
                .Left_Join("[Catalog].[ProductExt] ON [Product].[ProductID] = [ProductExt].[ProductID]")
                .Left_Join("[Catalog].[Photo] ON [Photo].[PhotoId] = [ProductExt].[PhotoId]")
                .Left_Join("[Catalog].[Offer] ON [ProductExt].[OfferID] = [Offer].[OfferID]")
                .Inner_Join("[Catalog].[Currency] ON [Currency].[CurrencyID] = [Product].[CurrencyID]");
            
            if (SettingsCatalog.ComplexFilter)
            {
                _paging.Select(
                    "Colors",
                    "NotSamePrices".AsSqlField("MultiPrices"),
                    "MinPrice".AsSqlField("BasePrice")
                );
            }
            else
            {
                _paging.Select(
                    "null".AsSqlField("Colors"),
                    "0".AsSqlField("MultiPrices"),
                    "Price".AsSqlField("BasePrice")
                );
            }

            BuildFilter();
            BuildSorting();
            
            _paging.ItemsPerPage = _currentPageIndex != 0 ? SettingsCatalog.ProductsPerPage : int.MaxValue;
            _paging.CurrentPageIndex = _currentPageIndex != 0 ? _currentPageIndex : 1;
        }

        private SqlPaging BuildSorting()
        {
            var sort = _searchModel.Sort ?? ESortOrder.NoSorting;
            _model.Filter.Sorting = sort;

            if (SettingsCatalog.MoveNotAvaliableToEnd)
            {
                _paging.OrderByDesc("(CASE WHEN Price=0 THEN 0 ELSE 1 END)".AsSqlField("TempSort"))
                       .OrderByDesc("AmountSort".AsSqlField("TempAmountSort"));
            }

            switch (sort)
            {
                case ESortOrder.AscByName:
                    _paging.OrderBy("Product.Name".AsSqlField("NameSort"));
                    break;
                case ESortOrder.DescByName:
                    _paging.OrderByDesc("Product.Name".AsSqlField("NameSort"));
                    break;

                case ESortOrder.AscByPrice:
                    _paging.OrderBy("PriceTemp");
                    break;

                case ESortOrder.DescByPrice:
                    _paging.OrderByDesc("PriceTemp");
                    break;

                case ESortOrder.AscByRatio:
                    _paging.OrderBy("(CASE WHEN ManualRatio is NULL THEN Ratio ELSE ManualRatio END)".AsSqlField("RatioSort"));
                    break;

                case ESortOrder.DescByRatio:
                    _paging.OrderByDesc("(CASE WHEN ManualRatio is NULL THEN Ratio ELSE ManualRatio END)".AsSqlField("RatioSort"));
                    break;

                case ESortOrder.AscByAddingDate:
                    _paging.OrderBy("DateAdded".AsSqlField("DateAddedSort"));
                    break;

                case ESortOrder.DescByAddingDate:
                    _paging.OrderByDesc("DateAdded".AsSqlField("DateAddedSort"));
                    break;
                    
                case ESortOrder.DescByPopular:
                    _paging.OrderByDesc("SortPopular");
                    break;
                    
                case ESortOrder.DescByDiscount:
                    _paging.OrderByDesc("Discount".AsSqlField("DiscountSorting"))
                           .OrderByDesc("DiscountAmount".AsSqlField("DiscountAmountSorting"));
                    break;
            }
            return _paging;
        }

        private void BuildFilter()
        {
            _paging.Where("Product.Enabled={0}", true)
                   .Where("AND CategoryEnabled={0}", true)
                   .Where("AND (Offer.Main={0} OR Offer.Main IS NULL)", true);

            if (!_searchModel.CategoryId.HasValue)
            {
                _paging.Where(
                    "AND Exists(Select 1 From [Catalog].[ProductCategories] Where ProductCategories.ProductId = [Product].[ProductID])");
            }
            else
            {
                _paging.Where(
                    "AND Exists(Select 1 From [Catalog].[ProductCategories] INNER JOIN [Settings].[GetChildCategoryByParent]({0}) AS hCat ON hCat.id = [ProductCategories].[CategoryID] and  ProductCategories.ProductId = [Product].[ProductID])",
                    _searchModel.CategoryId);
            }

            if (SettingsCatalog.ShowOnlyAvalible)
            {
                _paging.Where("AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1)");
            }

            if (!string.IsNullOrEmpty(_searchModel.Q))
            {
                _model.Filter.SearchItemsResult = _searchService.FindForPaging(_searchModel.Q, _paging, _searchModel.Sort ?? ESortOrder.NoSorting);
            }

            var currency = CurrencyService.CurrentCurrency;
            if (SettingsCatalog.DefaultCurrencyIso3 != currency.Iso3)
            {
                _paging.Where("", currency.Iso3);
            }

            if (_searchModel.PriceFrom.HasValue || _searchModel.PriceTo.HasValue)
            {
                var pricefrom = _searchModel.PriceFrom ?? 0;
                var priceto = _searchModel.PriceTo ?? int.MaxValue;

                _model.Filter.PriceFrom = pricefrom;
                _model.Filter.PriceTo = priceto;

                _paging.Where("AND (ProductExt.PriceTemp >= {0} ", pricefrom * currency.Rate);
                _paging.Where("AND  ProductExt.PriceTemp <= {0})", priceto * currency.Rate);
            }

            if (!string.IsNullOrEmpty(_searchModel.Brand))
            {
                var brandIds = _searchModel.Brand.Split(',').Select(x => x.TryParseInt()).Where(x => x != 0).ToList();
                if (brandIds.Count > 0)
                {
                    _model.Filter.BrandIds = brandIds;
                    _paging.Where("AND Product.BrandID IN ({0})", brandIds.ToArray());
                }
            }

            if (!string.IsNullOrEmpty(_searchModel.Size))
            {
                var sizeIds = _searchModel.Size.Split(',').Select(item => item.TryParseInt()).Where(id => id != 0).ToList();
                if (sizeIds.Count > 0)
                {
                    _model.Filter.SizeIds = sizeIds;
                    _paging.Where(
                        SettingsCatalog.ShowOnlyAvalible || _searchModel.Available
                            ? "and Exists(Select 1 from [Catalog].[Offer] where Offer.[SizeID] IN ({0}) and Offer.ProductId = [Product].[ProductID] AND (Offer.amount > 0 OR [Product].[AllowPreOrder] = 1))"
                            : "and Exists(Select 1 from [Catalog].[Offer] where Offer.[SizeID] IN ({0}) and Offer.ProductId = [Product].[ProductID])",
                        sizeIds.ToArray());
                }
            }

            if (!string.IsNullOrEmpty(_searchModel.Color))
            {
                var colorIds = _searchModel.Color.Split(',').Select(item => item.TryParseInt()).Where(id => id != 0).ToList();
                if (colorIds.Count > 0)
                {
                    _model.Filter.ColorIds = colorIds;
                    _paging.Where(
                         SettingsCatalog.ShowOnlyAvalible || _searchModel.Available
                            ? "and Exists(Select 1 from [Catalog].[Offer] where Offer.[ColorID] IN ({0}) and Offer.ProductId = [Product].[ProductID] AND (Offer.amount > 0 OR [Product].[AllowPreOrder] = 1))"
                            : "and Exists(Select 1 from [Catalog].[Offer] where Offer.[ColorID] IN ({0}) and Offer.ProductId = [Product].[ProductID])",
                        colorIds.ToArray());
                }

                if (SettingsCatalog.ComplexFilter)
                {
                    _paging.Select(
                        string.Format(
                            "(select Top 1 PhotoName from catalog.Photo inner join Catalog.Offer on Photo.objid=Offer.Productid and Type='product'" +
                            " Where Offer.ProductId=Product.ProductId and Photo.ColorID in({0}) Order by Photo.Main Desc, Photo.PhotoSortOrder)",
                            _model.Filter.ColorIds.AggregateString(",")).AsSqlField("AdditionalPhoto"));
                }
                else
                {
                    _paging.Select("null".AsSqlField("AdditionalPhoto"));
                }
            }
            else
            {
                _paging.Select("null".AsSqlField("AdditionalPhoto"));
            }

            if (!string.IsNullOrEmpty(_searchModel.Warehouse))
            {
                var warehouseIds = _searchModel.Warehouse
                                               .Split(',')
                                               .Select(item => item.TryParseInt())
                                               .Where(id => id != 0)
                                               .ToList();
                if (warehouseIds.Count > 0)
                {
                    _model.Filter.WarehouseIds = warehouseIds;
                    _paging.Where(
                        "and Exists(Select 1 from [Catalog].[Offer] "
                        + "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] "
                        + "where [WarehouseStocks].[WarehouseId] IN ({0}) "
                        + "     and Offer.ProductId = [Product].[ProductID] "
                        + "     AND [WarehouseStocks].[Quantity] > 0)", 
                        warehouseIds.ToArray());
                }
            }

            if (!string.IsNullOrEmpty(_searchModel.Prop))
            {
                var selectedPropertyIDs = new List<int>();
                var filterCollection = _searchModel.Prop.Split('-');
                foreach (var val in filterCollection)
                {
                    var tempListIds = new List<int>();
                    foreach (int id in val.Split(',').Select(item => item.TryParseInt()).Where(id => id != 0))
                    {
                        tempListIds.Add(id);
                        selectedPropertyIDs.Add(id);
                    }
                    if (tempListIds.Count > 0)
                        _paging.Where("AND Exists(Select 1 from [Catalog].[ProductPropertyValue] where [Product].[ProductID] = [ProductID] and PropertyValueID IN ({0}))", tempListIds.ToArray());
                }
                _model.Filter.PropertyIds = selectedPropertyIDs;
            }

            var rangeIds = new Dictionary<int, KeyValuePair<float, float>>();
            var rangeQueries =
                HttpContext.Current.Request.QueryString.AllKeys.Where(
                    p => p != null && p.StartsWith("prop_") && (p.EndsWith("_min") || p.EndsWith("_max"))).ToList();

            foreach (var rangeQuery in rangeQueries)
            {
                if (rangeQuery.EndsWith("_max"))
                    continue;

                var propertyId = rangeQuery.Split('_')[1].TryParseInt();
                if (propertyId == 0)
                    continue;

                var min = HttpContext.Current.Request.QueryString[rangeQuery].TryParseFloat();
                var max = HttpContext.Current.Request.QueryString[rangeQuery.Replace("min", "max")].TryParseFloat();

                rangeIds.Add(propertyId, new KeyValuePair<float, float>(min, max));
            }
            
            if (_searchModel.PropertyRanges != null && _searchModel.PropertyRanges.Count > 0)
            {
                foreach (var propertyRange in _searchModel.PropertyRanges)
                    rangeIds.Add(propertyRange.Id, new KeyValuePair<float, float>(propertyRange.Min, propertyRange.Max));
            }

            rangeIds = ModulesExecuter.GetRangeIds(rangeIds);
            if (rangeIds.Count > 0)
            {
                foreach (var id in rangeIds.Keys)
                {
                    _paging.Where(
                        "AND Exists( select 1 from [Catalog].[ProductPropertyValue] " +
                        "Inner Join [Catalog].[PropertyValue] on [PropertyValue].[PropertyValueID] = [ProductPropertyValue].[PropertyValueID] " +
                        "Where [Product].[ProductID] = [ProductID] and PropertyId = {0} and RangeValue >= {1} and RangeValue <= {2})",
                        id, rangeIds[id].Key, rangeIds[id].Value);
                }
            }
            _model.Filter.RangePropertyIds = rangeIds;

            _model.Filter.Available = _searchModel.Available;
        }

        private void BuildExcludingFilters()
        {
            if (!SettingsCatalog.ExcludingFilters)
                return;

            var tasks = new List<Task>();

            var task = Task.Run(() =>
            {
                _model.Filter.AvailablePropertyIds =
                    _paging.GetCustomData("PropertyValueID",
                        " AND PropertyValueID is not null",
                        reader => SQLDataHelper.GetInt(reader, "PropertyValueID"), true,
                        "Left JOIN [Catalog].[ProductPropertyValue] ON [Product].[ProductID] = [ProductPropertyValue].[ProductID]");
            });
            tasks.Add(task);

            if (SettingsCatalog.ShowProducerFilter)
            {
                task = Task.Run(() =>
                {
                    _model.Filter.AvailableBrandIds =
                        _paging.GetCustomData("Product.BrandID", " AND Product.BrandID is not null",
                            reader => SQLDataHelper.GetInt(reader, "BrandID"), true);
                });
                tasks.Add(task);
            }

            if (SettingsCatalog.ShowSizeFilter)
            {
                task = Task.Run(() =>
                {
                    _model.Filter.AvailableSizeIds =
                        _paging.GetCustomData("sizeOffer.SizeID", " AND sizeOffer.SizeID is not null",
                            reader => SQLDataHelper.GetInt(reader, "SizeID"), true,
                            "Left JOIN [Catalog].[Offer] as sizeOffer ON [Product].[ProductID] = [sizeOffer].[ProductID]");
                });
                tasks.Add(task);
            }

            if (SettingsCatalog.ShowColorFilter)
            {
                task = Task.Run(() =>
                {
                    _model.Filter.AvailableColorIds =
                        _paging.GetCustomData("colorOffer.ColorID", " AND colorOffer.ColorID is not null",
                            reader => SQLDataHelper.GetInt(reader, "ColorID"), true,
                            "Left JOIN [Catalog].[Offer] as colorOffer ON [Product].[ProductID] = [colorOffer].[ProductID]");
                });
                tasks.Add(task);
            }

            if (SettingsCatalog.ShowWarehouseFilter)
            {
                task = Task.Run(() =>
                {
                    _model.Filter.AvailableWarehouseIds =
                        _paging.GetCustomData(
                            "[WarehouseStocks].[WarehouseId]", 
                            " AND [WarehouseStocks].[Quantity] > 0",
                            reader => SQLDataHelper.GetInt(reader, "WarehouseId"), 
                            true,
                            "Left JOIN [Catalog].[Offer] as warehouseOffer ON [Product].[ProductID] = [warehouseOffer].[ProductID] "
                            + "LEFT JOIN [Catalog].[WarehouseStocks] ON warehouseOffer.[OfferID] = [WarehouseStocks].[OfferId]");
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray(), 1000);
        }

        private void GetViewMode()
        {
            if (!_isMobile)
            {
                var mode = SettingsCatalog.GetViewMode(SettingsCatalog.EnabledSearchViewChange, "search_viewmode", SettingsCatalog.DefaultSearchView, false);

                _model.Filter.ViewMode = mode.ToString().ToLower();
                _model.Filter.AllowChangeViewMode = SettingsCatalog.EnabledSearchViewChange;
            }
            else
            {
                var mode = SettingsCatalog.GetViewMode(SettingsMobile.EnableCatalogViewChange, "mobile_viewmode", SettingsMobile.DefaultCatalogView, true);

                _model.Filter.ViewMode = mode.ToString().ToLower();
                _model.Filter.AllowChangeViewMode = SettingsMobile.EnableCatalogViewChange;
            }
        }

        private void GetProducts()
        {
            if (string.IsNullOrWhiteSpace(_searchModel.Q))
            {
                _model.Pager = new Pager();
                _model.Products = new ProductViewModel(new List<ProductModel>(), _isMobile);
                GetViewMode();

                return;
            }

            BuildPaging();

            var totalCount = _paging.TotalRowsCount;
            var totalPages = _paging.PageCount(totalCount);

            _model.Pager = new Pager()
            {
                TotalItemsCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = _currentPageIndex
            };

            if ((totalPages < _currentPageIndex && _currentPageIndex > 1) || _currentPageIndex < 0)
                return;

            var products = _paging.PageItemsList<ProductModel>();

            _model.Products = new ProductViewModel(products, _isMobile);

            GetViewMode();

            // preset photo and color
            var selectedOffer = OfferService.GetOffer(_searchModel.Q);
            if (selectedOffer != null)
            {
                var p = products.Find(x => x.ProductId == selectedOffer.ProductId);
                if (p != null)
                {
                    p.SelectedColorId = selectedOffer.ColorID;
                    p.SelectedSizeId = selectedOffer.SizeID;
                }
            }
        }

        private CategoryListViewModel GetCategories()
        {
            var model = new CategoryListViewModel() { Categories = new List<Category>() };

            if (string.IsNullOrEmpty(_searchModel.Q))
                return model;
            
            var categoryIds = CategorySeacher.Search(_searchModel.Q).SearchResultItems.Select(x => x.Id).ToList();

            var translitQ = StringHelper.TranslitToRusKeyboard(_searchModel.Q);
            var translitCategoryIds = CategorySeacher.Search(translitQ).SearchResultItems.Select(x => x.Id).ToList();
            
            var categories =
                categoryIds.Union(translitCategoryIds)
                    .Distinct()
                    .Select(CategoryService.GetCategory)
                    .Where(cat => cat != null && cat.Enabled && cat.ParentsEnabled && !cat.Hidden)
                    .Take(10)
                    .ToList();

            model = new CategoryListViewModel()
            {
                Categories = categories,
                CountCategoriesInLine = SettingsDesign.CountCategoriesInLine,
                PhotoHeight = SettingsPictureSize.SmallCategoryImageHeight,
                PhotoWidth = SettingsPictureSize.SmallCategoryImageWidth,
                DisplayStyle = ECategoryDisplayStyle.List
            };

            return model;
        }

        public SearchCatalogViewModel GetForFilter()
        {
            BuildPaging();
            BuildExcludingFilters();

            return _model;
        }

        public Pager GetForFilterProductCount()
        {
            BuildPaging();

            var totalCount = _paging.TotalRowsCount;

            var model = new Pager
            {
                TotalItemsCount = totalCount,
                CurrentPage = _currentPageIndex
            };

            return model;
        }
    }
}