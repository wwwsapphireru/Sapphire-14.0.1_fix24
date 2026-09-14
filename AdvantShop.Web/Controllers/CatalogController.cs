using System.Threading.Tasks;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.SEO;
using AdvantShop.Handlers.Catalog;
using AdvantShop.Handlers.Menu;
using AdvantShop.Models.Catalog;
using AdvantShop.Saas;
using AdvantShop.ViewModel.Catalog;
using AdvantShop.ViewModel.Common;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Extensions;
using AdvantShop.Core.Services.SEO.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.SessionState;
using AdvantShop.App.Landing.Handlers.Products;
using AdvantShop.App.Landing.Models.Catalogs;
using AdvantShop.Core;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Configuration;
using AdvantShop.Core.Services.Diagnostics;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Web.Infrastructure.Filters;

namespace AdvantShop.Controllers
{
    [SessionState(SessionStateBehavior.Disabled)]
    public partial class CatalogController : BaseClientController
    {
        #region Category page
      
        // GET: Category page
        [AccessByChannel(EProviderSetting.StoreActive)]
        public ActionResult Index(CategoryModel categoryModel)
        {
            if ((string.IsNullOrWhiteSpace(categoryModel.Url) && categoryModel.CategoryId != 0) || (categoryModel.Page != null && categoryModel.Page < 0))
                return Error404();

            if (categoryModel.Url.IsNotEmpty() && categoryModel.Url.Equals(Url.RouteUrl("CatalogRoot").TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect(Url.RouteUrl("CatalogRoot"), false);
                return new EmptyResult();
            }

            var category = categoryModel.Url != null
                                ? CategoryService.GetCategory(categoryModel.Url)
                                : categoryModel.CategoryId.HasValue ? CategoryService.GetCategory(categoryModel.CategoryId.Value) : null;
            
            category = ModulesExecuter.GetVirtualCategory(category);
            categoryModel = (CategoryModel)ModulesExecuter.GetVirtualCategoryModel(categoryModel);

            //if (category == null || !category.Enabled || !category.ParentsEnabled)GlorySoft_025
            //    return Error404();

            //GlorySoft_025
            if (category == null)
            {
                Response.Redirect(Url.RouteUrl("CatalogRoot"), false);
                return new EmptyResult();
            }
            if (!category.Enabled || !category.ParentsEnabled)
            {
                while (category.CategoryId > 0)
                {
                    category = category.ParentCategory;
                    if (category.Enabled && category.ParentsEnabled)
                        break;
                }
                if (category.ParentCategoryId == 0)
                {
                    Response.Redirect(Url.RouteUrl("CatalogRoot"), false);
                    return new EmptyResult();
                }
            }

            var indepth = categoryModel.Indepth || category.DisplayChildProducts;

            Request.RequestContext.HttpContext.Items["CurrentCategoryId"] = category.CategoryId;

            var childCategories =
                     CategoryService.GetChildCategoriesByCategoryId(
                             category.ID,
                             warehouseIds: WarehouseContext.GetAvailableWarehouseIds())
                         .Where(x => x.Enabled && x.ParentsEnabled && !x.Hidden)
                         .ToList();

            var model = new CategoryViewModel(category)
            {
                ChildCategories = childCategories
            };

            if (!string.IsNullOrWhiteSpace(categoryModel.TagUrl))
            {
                var tags = TagService.GetCategoryTags(category.ID);
                if (!tags.Any(x => x.UrlPath == categoryModel.TagUrl)) 
                    return Error404();
            }

            var tag = TagService.GetByUrl(categoryModel.TagUrl);
            if (tag != null && !tag.Enabled)
            {
                Response.Redirect("/categories/" + category.UrlPath, true);
                return new EmptyResult();
            }

            if (!SaasDataService.IsSaasEnabled || (SaasDataService.IsSaasEnabled && SaasDataService.CurrentSaasData.HaveTags))
            {
                category.Tags = TagService.GetCategoryTags(category.CategoryId);

                model.TagView = new TagViewModel
                {
                    CategoryUrl = category.UrlPath,
                    Tags = category.Tags.Where(x => x.Enabled && x.VisibilityForUsers).Select(x => new TagView
                    {
                        Name = x.Name,
                        Url = x.UrlPath,
                        Selected = x.Id == (tag != null ? tag.Id : 0)
                    }).ToList()
                };
            }
            else
            {
                category.Tags = new List<Tag>();
                model.TagView = new TagViewModel() {CategoryUrl = category.UrlPath};
            }

            model.TagView.CategoryUrl = ModulesExecuter.GetUrlParentCategory(model.TagView.CategoryUrl);

            if (tag != null)
            {
                model.Tag = tag;
                categoryModel.TagId = tag.Id;
            }
            
            var productsCount = SettingsCatalog.ShowOnlyAvalible
                ? category.Available_Products_Count
                : indepth
                    ? category.ProductsCount
                    : category.Current_Products_Count;

            if (productsCount > 0)
            {
                var paging = new CategoryProductPagingHandler(category, indepth, categoryModel, false).GetForCatalog();

                if ((paging.Pager.TotalPages < paging.Pager.CurrentPage && paging.Pager.CurrentPage > 1) ||
                    paging.Pager.CurrentPage < 0)
                {
                    return Error404();
                }

                model.Pager = paging.Pager;
                model.Products = paging.Products;
                model.Filter = paging.Filter;
            }

            model.BreadCrumbs =
                CategoryService.GetParentCategories(category.CategoryId)
                    .Select(x => new BreadCrumbs(x.Name, x.CategoryId == 0 ? Url.AbsoluteRouteUrl("CatalogRoot") : Url.AbsoluteRouteUrl("Category", new { url = x.UrlPath })))
                    .Reverse()
                    .ToList();
            model.BreadCrumbs.Insert(0, new BreadCrumbs(T("MainPage"), Url.AbsoluteRouteUrl("Home")));

            model.BreadCrumbs = ModulesExecuter.GetVirtualCategoryBreadCrumbs(model.BreadCrumbs);

            var tagManager = GoogleTagManagerContext.Current;
            if (tagManager.Enabled)
            {
                tagManager.PageType = ePageType.category;
                tagManager.CatCurrentId = category.ID;
                tagManager.CatCurrentName = category.Name;
                tagManager.CatParentId = category.ParentCategory != null ? category.ParentCategory.ID : 0;
                tagManager.CatParentName = category.ParentCategory != null ? category.ParentCategory.Name : "";

                tagManager.ProdIds = new List<string>();
                if (model.Products != null)
                {
                    foreach (var product in model.Products.Products)
                    {
                        tagManager.ProdIds.Add(product.OfferArtNo);
                    }
                }
            }

            SetNgController(NgControllers.NgControllersTypes.CatalogCtrl);

            if (tag != null)
            {
                model.BreadCrumbs.Add(new BreadCrumbs(tag.Name, string.Empty));
                SetMetaInformation(tag.Meta, tag.Name, category.Name, page: categoryModel.Page ?? 1, totalPages: model.Pager != null ? model.Pager.TotalPages : 0);
            }
            else
            {
                SetMetaInformation(category.Meta, category.Name, page: categoryModel.Page ?? 1,
                    tags: category.Tags.Select(x => x.Name).ToList(), 
                    totalPages: model.Pager != null ? model.Pager.TotalPages : 0);
            }

            var og = new OpenGraphModel();
            if (!string.IsNullOrEmpty(category.Picture.PhotoName))
            {
                og.Image = category.Picture.ImageSrcSmall();
            }
            else if (!string.IsNullOrEmpty(category.MiniPicture.PhotoName))
            {
                og.Image = category.MiniPicture.ImageSrcSmall();
            }

            MetaDataContext.CurrentObject = og;

            if (DebugMode.IsDebugMode(eDebugMode.CriticalCss) && categoryModel.ViewMode.IsNotEmpty() &&
                Enum.IsDefined(typeof(ProductViewMode), categoryModel.ViewMode) && model.Filter != null)
            {
                model.Filter.ViewMode = categoryModel.ViewMode.ToLower();
            }

            WriteLog(category.Name, Url.AbsoluteRouteUrl("Category", new { url = category.UrlPath }), ePageType.category);
            
            return View(model);
        }

        private Category GetCategoryWithProducts(Category category)
        {
            var childCategory =
                CategoryService.GetChildCategoriesByCategoryId(
                        category.ID,
                        true,
                        warehouseIds: WarehouseContext.GetAvailableWarehouseIds())
                    .FirstOrDefault(x => x.Enabled && x.ParentsEnabled && !x.Hidden);

            return
                childCategory == null || childCategory.Current_Products_Count > 0
                    ? childCategory
                    : GetCategoryWithProducts(childCategory);
        }

        

        [HttpGet]
        public JsonResult Filter(CategoryModel categoryModel)
        {
            if (categoryModel.Page != null && categoryModel.Page < 0)
                return Json(null);

            var category = categoryModel.CategoryId.HasValue ? CategoryService.GetCategory(categoryModel.CategoryId.Value) : null;

            var indepth = categoryModel.Indepth || category.DisplayChildProducts;

            if (categoryModel.CategoryId == 0 && !indepth)
                return Json(null);

            category = ModulesExecuter.GetVirtualCategory(category);
            categoryModel = (CategoryModel)ModulesExecuter.GetVirtualCategoryModel(categoryModel);

            if (category == null)
                return Json(null);
            
            var tag = TagService.GetByUrl(categoryModel.TagUrl);
            if (tag != null)
                categoryModel.TagId = tag.Id;

            var paging = new CategoryProductPagingHandler(category, indepth, categoryModel, false).GetForFilter();
            var filter = paging.Filter;

            var sqlTasks = new List<Task<List<FilterItemModel>>>();

            if (SettingsCatalog.ShowPriceFilter && !SettingsCatalog.HidePrice)
            {
                sqlTasks.Add(new FilterPriceHandler(filter.CategoryId, filter.Indepth, filter.PriceFrom, filter.PriceTo).GetAsync());
            }

            if (SettingsCatalog.ShowProducerFilter)
            {
                sqlTasks.Add(new FilterBrandHandler(filter.CategoryId, filter.Indepth, filter.BrandIds, filter.AvailableBrandIds).GetAsync());
            }

            if (SettingsCatalog.ShowColorFilter)
            {
                sqlTasks.Add(new FilterColorHandler(filter.CategoryId, filter.Indepth, filter.ColorIds, filter.AvailableColorIds, SettingsCatalog.ShowOnlyAvalible || filter.Available, filter.ColorsViewMode).GetAsync());
            }

            if (SettingsCatalog.ShowSizeFilter)
            {
                sqlTasks.Add(new FilterSizeHandler(filter.CategoryId, filter.Indepth, filter.SizeIds, filter.AvailableSizeIds, SettingsCatalog.ShowOnlyAvalible || filter.Available).GetAsync());
            }

            if (SettingsCatalog.ShowWarehouseFilter)
            {
                sqlTasks.Add(new FilterWarehouseHandler(filter.CategoryId, filter.Indepth, filter.WarehouseIds, filter.AvailableWarehouseIds).GetAsync());
            }
            
            if (SettingsCatalog.ShowPropertiesFilterInParentCategories)
            {
                sqlTasks.Add(new FilterPropertyHandler(filter.CategoryId, filter.Indepth, filter.PropertyIds, filter.AvailablePropertyIds, filter.RangePropertyIds).GetAsync());
            }
            else
            {
                var hasNoSubCategories = CategoryService.GetChildCategoriesByCategoryId(category.CategoryId, warehouseIds: WarehouseContext.GetAvailableWarehouseIds())
                    .Where(cat => cat.Enabled && !cat.Hidden)
                    .ToList()
                    .Count == 0;
                
                if (hasNoSubCategories)
                    sqlTasks.Add(new FilterPropertyHandler(filter.CategoryId, filter.Indepth, filter.PropertyIds, filter.AvailablePropertyIds, filter.RangePropertyIds).GetAsync());
            }


            sqlTasks.Add(new FilterAvailabilityHandler(filter.Available).GetAsync());

            var resultFilter = sqlTasks.Select(x => x.Result).SelectMany(x => x).Where(x => x != null).ToList();

            ModulesExecuter.FilterCatalog();
            
            return Json(resultFilter);
        }

        [HttpGet]
        public JsonResult FilterProductCount(CategoryModel categoryModel)
        {
            if (categoryModel.CategoryId == 0 || (categoryModel.Page != null && categoryModel.Page < 0))
                return Json(null);

            var category = categoryModel.CategoryId.HasValue ? CategoryService.GetCategory(categoryModel.CategoryId.Value) : null;
            if (category == null)
                return Json(null);

            var tag = TagService.GetByUrl(categoryModel.TagUrl);
            if (tag != null)
                categoryModel.TagId = tag.Id;

            var indepth = categoryModel.Indepth || category.DisplayChildProducts;
            var paging = new CategoryProductPagingHandler(category, indepth, categoryModel, false).GetForFilterProductCount();
            if (paging.Filter == null || paging.Pager == null)
                return Json(null);

            return Json(paging.Pager.TotalItemsCount);
        }

        [HttpGet]
        public JsonResult LpFilter(ProductsByCategoryModel categoryModel)
        {
            if (categoryModel.CategoryId == 0 || categoryModel.Page < 0)
                return Json(null);

            var category = CategoryService.GetCategory(categoryModel.CategoryId);
            if (category == null)
                return Json(null);

            var indepth = categoryModel.Indepth != null || 
                          CategoryService.GetAllChildCategoriesIdsByCategoryId(categoryModel.CategoryId).Any(x => x != categoryModel.CategoryId);

            var paging = new CatalogProductPaging(categoryModel, indepth).GetForFilter();
            var filter = paging.Filter;

            var sqlTasks = new List<Task<List<FilterItemModel>>>();

            if (!categoryModel.HideFilterByPrice)
            {
                sqlTasks.Add(new FilterPriceHandler(filter.CategoryId, filter.Indepth, filter.PriceFrom, filter.PriceTo).GetAsync());
            }

            if (!categoryModel.HideFilterByBrand)
            {
                sqlTasks.Add(new FilterBrandHandler(filter.CategoryId, filter.Indepth, filter.BrandIds, filter.AvailableBrandIds).GetAsync());
            }

            if (!categoryModel.HideFilterByColor)
            {
                sqlTasks.Add(new FilterColorHandler(filter.CategoryId, filter.Indepth, filter.ColorIds, filter.AvailableColorIds, SettingsCatalog.ShowOnlyAvalible || filter.Available, filter.ColorsViewMode).GetAsync());
            }

            if (!categoryModel.HideFilterBySize)
            {
                sqlTasks.Add(new FilterSizeHandler(filter.CategoryId, filter.Indepth, filter.SizeIds, filter.AvailableSizeIds, SettingsCatalog.ShowOnlyAvalible || filter.Available).GetAsync());
            }

            if (!categoryModel.HideFilterByProperty)
            {
                sqlTasks.Add(new FilterPropertyHandler(filter.CategoryId, filter.Indepth, filter.PropertyIds, filter.AvailablePropertyIds, filter.RangePropertyIds).GetAsync());
            }

            var results = sqlTasks.Select(x => x.Result).SelectMany(x => x).Where(x => x != null).ToList();
            
            return Json(results);
        }

        [ChildActionOnly]
        public ActionResult CategoryList(int categoryId, ECategoryDisplayStyle type, int? countProductsInLine, int? countProductsInSection)
        {
            if (type == ECategoryDisplayStyle.None)
                return new EmptyResult();

            var categories =
                CategoryService.GetChildCategoriesByCategoryId(
                        categoryId, 
                        warehouseIds: WarehouseContext.GetAvailableWarehouseIds())
                    .Where(x => x.Enabled && x.ParentsEnabled && !x.Hidden)
                    .ToList();

            if (categories.Count == 0)
                return new EmptyResult();

            if (type == ECategoryDisplayStyle.List)
            {
                var modelWithProducts = new CategoryListViewModel()
                {
                    CategoriesWithProducts = new List<CategoryProductsViewModel>(),
                    DisplayProductCount = SettingsCatalog.ShowProductsCount
                };

                var countInLine = countProductsInLine ?? SettingsDesign.CountCatalogProductInLine;
                var countInBlock = countProductsInSection ?? SettingsDesign.CountCatalogProductInLine;

                foreach (var category in categories)
                {
                    var products = ProductService.GetProductsByCategory(
                        category.CategoryId,
                        countInBlock,
                        category.Sorting,
                        category.HasChild,
                        WarehouseContext.GetAvailableWarehouseIds());
                    
                    var categoryModel = new CategoryProductsViewModel(products)
                    {
                        ProductsCount = SettingsCatalog.ShowOnlyAvalible ? category.Available_Products_Count : category.ProductsCount,
                        Title = category.Name,
                        Url = Url.RouteUrl("Category", new { url = category.UrlPath }),
                        CountProductsInLine = countInLine,
                        ShowBriefDescription = SettingsMain.MainPageVisibleBriefDescription
                    };
                    modelWithProducts.CategoriesWithProducts.Add(categoryModel);
                }

                return PartialView("CategoryListWithProducts", modelWithProducts);
            }

            var model = new CategoryListViewModel()
            {
                Categories = categories,
                DisplayProductCount = SettingsCatalog.ShowProductsCount,
                CountCategoriesInLine = SettingsDesign.CountCategoriesInLine,
                PhotoWidth = SettingsPictureSize.SmallCategoryImageWidth,
                PhotoHeight = SettingsPictureSize.SmallCategoryImageHeight
            };

            return PartialView(model);
        }

        #endregion

        #region Product list page

        public ActionResult ProductList(EProductOnMain? type, CategoryModel categoryModel, string list,/*GlorySoft_016*/ int? salesType)
        {
            try
            {
                var (model, meta, title) = new GetProductListHandler(type, categoryModel, list, 
                        new UrlHelper(HttpContext.Request.RequestContext),/*GlorySoft_016*/ salesType).Execute();
                
                SetNgController(NgControllers.NgControllersTypes.ProductListCtrl);
                SetMetaInformation(meta, title, page: categoryModel.Page ?? 1, totalPages: model.Pager != null 
                                                                                   ? model.Pager.TotalPages 
                                                                                   : 0);
                return View(model);
            }
            catch (BlException)
            {
                return Error404();
            }
        }

        public ActionResult ChangeMode(string name, string viewMode)
        {
            var model = new ChangeModeViewModel()
            {
                Name = name,
                ViewMode = viewMode
            };

            return PartialView(model);
        }

        public JsonResult FilterProductList(EProductOnMain type, CategoryModel modelIn, int? list,/*GlorySoft_016*/ int? salesType)
        {
            if (modelIn.Page != null && modelIn.Page < 0)
                return Json(null);

            var tag = TagService.GetByUrl(modelIn.TagUrl);
            if (tag != null)
                modelIn.TagId = tag.Id;

            var paging = new ProductListHandler(type, true, modelIn, list, false,/*GlorySoft_016*/ salesType).GetForFilter();
            var filter = paging.Filter;

            var sqlTasks = new List<Task<List<FilterItemModel>>>
            {
                new FilterSelectCategoryHandler(modelIn.CategoryId ?? 0,/*GlorySoft_023*/ paging.ProductIds.ToList()).GetAsync()
            };

            if (SettingsCatalog.ShowPriceFilter && !SettingsCatalog.HidePrice)
            {
                sqlTasks.Add(
                    new FilterPriceHandler(type, list, filter.PriceFrom, filter.PriceTo)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowProducerFilter)
            {
                sqlTasks.Add(
                    new FilterBrandHandler(0, true, filter.BrandIds, filter.AvailableBrandIds, type, list)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowColorFilter)
            {
                sqlTasks.Add(
                    new FilterColorHandler(type, list,
                            filter.ColorIds, filter.AvailableColorIds,
                            SettingsCatalog.ShowOnlyAvalible || filter.Available, filter.ColorsViewMode)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowSizeFilter)
            {
                sqlTasks.Add(
                    new FilterSizeHandler(type, list,
                            filter.SizeIds, filter.AvailableSizeIds,
                            SettingsCatalog.ShowOnlyAvalible || filter.Available)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowWarehouseFilter)
            {
                sqlTasks.Add(
                    new FilterWarehouseHandler(0, true, filter.WarehouseIds, filter.AvailableWarehouseIds,
                            type, list)
                        .GetAsync());
            }

            if (SettingsCatalog.ShowPropertiesFilterInProductList &&/*GlorySoft_023*/ (modelIn.CategoryId ?? 0) != 0)
            {
                sqlTasks.Add(
                    new FilterPropertyHandler(modelIn.CategoryId.Value/*GlorySoft_023 filter.CategoryId*/, filter.Indepth, filter.PropertyIds,
                            filter.AvailablePropertyIds, filter.RangePropertyIds, null, type, list)
                        .GetAsync());
            }

            var resultFilter = sqlTasks.Select(x => x.Result).SelectMany(x => x).Where(x => x != null).ToList();

            return Json(resultFilter);
        }

        public JsonResult FilterProductListCount(EProductOnMain type, CategoryModel modelIn, int? list,/*GlorySoft_016*/ int? salesType)
        {
            if (modelIn.Page != null && modelIn.Page < 0)
                return Json(null);
                       

            var tag = TagService.GetByUrl(modelIn.TagUrl);
            if (tag != null)
                modelIn.TagId = tag.Id;

            var paging = new ProductListHandler(type, true, modelIn, list, false,/*GlorySoft_016*/ salesType).GetForFilterProductCount();
            if (paging.Filter == null || paging.Pager == null)
                return Json(null);

            return Json(paging.Pager.TotalItemsCount);
        }


        [HttpGet]
        public ActionResult GetCategoryProductList(CategoryModel categoryModel)
        {
            if ((string.IsNullOrWhiteSpace(categoryModel.Url) && categoryModel.CategoryId != 0) || (categoryModel.Page != null && categoryModel.Page < 0))
                return Error404();

            var category = categoryModel.CategoryId.HasValue
                    ? CategoryService.GetCategory(categoryModel.CategoryId.Value)
                    : categoryModel.Url != null ? CategoryService.GetCategory(categoryModel.Url) : null;

            var indepth = categoryModel.Indepth || category.DisplayChildProducts;

            var productsCount = SettingsCatalog.ShowOnlyAvalible ? category.Available_Products_Count
                                                    : (indepth
                                                        ? category.ProductsCount
                                                        : category.Current_Products_Count);

            if (productsCount <= 0)
                return JsonError();

            var paging = new CategoryProductPagingHandler(category, indepth, categoryModel, true).GetForCatalog();

            if ((paging.Pager.TotalPages < paging.Pager.CurrentPage && paging.Pager.CurrentPage > 1) ||
                paging.Pager.CurrentPage < 0)
            {
                return Error404();
            }

            return PartialView("_ProductView", paging.Products);
        }


        #endregion

        #region ProductsByIds
        [HttpPost]
        public ActionResult ProductsByIds(ProductByIdViewModel options)
        {
            if (string.IsNullOrWhiteSpace(options.Ids))
                return new EmptyResult();

            var productIds =
                options.Ids.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.TryParseInt())
                    .Where(x => x != 0)
                    .Take(12)
                    .ToList();

            if (productIds.Count == 0)
                return new EmptyResult();

            var moveNotAvaliableToEnd = (options.NotSort == null || !options.NotSort.Value) && SettingsCatalog.MoveNotAvaliableToEnd;

            var productModels = ProductService.GetProductsByIds(productIds, moveNotAvaliableToEnd);
            if (productModels == null || productModels.Count == 0)
                return new EmptyResult();

            if (options.HideDescription != null && options.HideDescription.Value)
            {
                foreach (var productModel in productModels)
                    productModel.BriefDescription = null;
            }

            var products = new ProductViewModel(productModels, SettingsDesign.IsMobileTemplate)
            {
                Title = options.Title,
                DisplayPhotoPreviews = false
            };

            var model = new ProductByIdViewModel()
            {
                Products = products,
                RelatedType = options.Type,
                Title = options.Title,
                VisibleItems = options.VisibleItems > 0 ? options.VisibleItems : SettingsDesign.CountCatalogProductInLine,
                EnabledCarousel = options.EnabledCarousel,
                CarouselResponsive = options.CarouselResponsive
            };

            if (model.Products != null)
                model.Products.CountProductsInLine = model.VisibleItems;

            model.Products.LazyLoadType = model.EnabledCarousel ? eLazyLoadType.Carousel : eLazyLoadType.Default;

            return PartialView(model);
        }

        public ActionResult ProductsByOfferIds(string ids, string title, string type, int visibleItems = 0, int offerId = 0, bool? hideDescription = false)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return new EmptyResult();

            var offerIds =
                ids.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.TryParseInt())
                    .Where(x => x != 0)
                    .Take(12)
                    .ToList();

            if (offerIds.Count == 0) // пришли артикулы вместо offerId
            {
                offerIds =
                    ids.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                        .Take(12)
                        .Select(OfferService.GetOffer)
                        .Where(x => x != null)
                        .Select(x => x.OfferId)
                        .ToList();
            }

            if (offerIds.Count == 0)
                return new EmptyResult();

            var excludeProductId = offerId != 0
                                    ? ProductService.GetProductIdsByOfferIds(new List<int> { offerId }).FirstOrDefault()
                                    : 0;

            var productIds = ProductService.GetProductIdsByOfferIds(offerIds).Where(x => x != excludeProductId).ToList();

            if (productIds.Count == 0)
                return new EmptyResult();

            var productModels = ProductService.GetProductsByIds(productIds);
            if (productModels == null || productModels.Count == 0)
                return new EmptyResult();

            if (hideDescription != null && hideDescription.Value)
            {
                foreach (var productModel in productModels)
                    productModel.BriefDescription = null;
            }

            var products = new ProductViewModel(productModels)
            {
                Title = title,
                DisplayPhotoPreviews = false
            };

            var model = new ProductByIdViewModel()
            {
                Products = products,
                RelatedType = type,
                Title = title,
                VisibleItems = visibleItems > 0 ? visibleItems : SettingsDesign.CountCatalogProductInLine,
                EnabledCarousel = !SettingsDesign.IsMobileTemplate
            };

            if (model.Products != null)
                model.Products.CountProductsInLine = model.VisibleItems;

            return PartialView("ProductsByIds", model);
        }

        #endregion

        #region MenuCatalog

        [ChildActionOnly]
        public ActionResult MenuCatalog(MenuCatalogViewModel menuModel)
        {
            var routeData = Request.RequestContext.RouteData;

            var isRoot =
                (string)routeData.Values["controller"] == "Home" && (string)routeData.Values["action"] == "Index" ||
                (string)routeData.Values["controller"] == "Catalog" && (string)routeData.Values["action"] == "Index" &&
                (int?)Request.RequestContext.HttpContext.Items["CurrentCategoryId"] == 0;
            
            var viewModel = new MenuViewModel
            {
                IsExpanded = menuModel.IsExpanded ?? isRoot,
                InLayout = menuModel.InLayout,
                ViewMode = !menuModel.InLayout
                    ? SettingsDesign.MenuStyle
                    : SettingsDesign.MenuStyle == SettingsDesign.eMenuStyle.Modern
                        ? SettingsDesign.eMenuStyle.Modern
                        : SettingsDesign.eMenuStyle.Classic
            };

            if (viewModel.IsExpanded && !viewModel.InLayout || !viewModel.IsExpanded)
            {
                var categoryId = (int?)RouteData.Values["CategoryId"];
                
                var menuItemModel = new MenuHandler().GetCatalogMenuItems(categoryId ?? 0, limited: true, SettingsDesign.OnePageCatalog);
                viewModel.MenuItems = menuItemModel.SubItems;
                viewModel.RootUrlPath = menuItemModel.UrlPath;
                viewModel.MenuItemsLimited = menuItemModel.SubItemsLimited;

                viewModel.DisplayProductsCount = SettingsCatalog.ShowProductsCount;
                viewModel.CountColsProductsInRow = menuModel.CountColsProductsInRow ?? 4;
                viewModel.LimitedCategoryMenu = SettingsCatalog.LimitedCategoryMenu;
                viewModel.OnePageCatalog = SettingsDesign.OnePageCatalog;
            }

            return PartialView("MenuCatalog", viewModel);
        }

        #endregion
    }
}