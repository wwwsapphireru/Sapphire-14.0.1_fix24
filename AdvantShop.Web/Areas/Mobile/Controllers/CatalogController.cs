using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using AdvantShop.Areas.Mobile.Handlers.Catalog;
using AdvantShop.Areas.Mobile.Models.Catalog;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Statistic;
using AdvantShop.Customers;
using AdvantShop.Models.Catalog;
using AdvantShop.Saas;
using AdvantShop.SEO;
using AdvantShop.ViewModel.Catalog;
using AdvantShop.Core.Services.SEO;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.CMS;
using AdvantShop.Core;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Handlers.Catalog;
using AdvantShop.Handlers.Search;
using AdvantShop.Helpers;
using AdvantShop.Web.Infrastructure.Extensions;

namespace AdvantShop.Areas.Mobile.Controllers
{
    [SessionState(SessionStateBehavior.Disabled)]
    public class CatalogController : BaseMobileController
    {
        #region Catalog page

        // GET: Mobile/Catalog
        public ActionResult Index(CategoryModel categoryModel)
        {
            if ((string.IsNullOrWhiteSpace(categoryModel.Url) && categoryModel.CategoryId != 0) ||
                (categoryModel.Page != null && categoryModel.Page < 0))
                return Error404();

            if (categoryModel.Url.IsNotEmpty() && categoryModel.Url.Equals(Url.RouteUrl("CatalogRoot").TrimStart('/'),
                    StringComparison.OrdinalIgnoreCase))
                Response.Redirect(Url.RouteUrl("CatalogRoot"), true);

            var category = categoryModel.Url != null
                ? CategoryService.GetCategory(categoryModel.Url)
                : categoryModel.CategoryId.HasValue
                    ? CategoryService.GetCategory(categoryModel.CategoryId.Value)
                    : null;

            category = ModulesExecuter.GetVirtualCategory(category);
            categoryModel = (CategoryModel)ModulesExecuter.GetVirtualCategoryModel(categoryModel);

            if (category == null || !category.Enabled || !category.ParentsEnabled)
                return Error404();

            Request.RequestContext.HttpContext.Items["CurrentCategoryId"] = category.CategoryId;

            var tag = TagService.GetByUrl(categoryModel.TagUrl);

            var model = new CategoryMobileViewModel(category);

            model.BreadCrumbs =
                CategoryService.GetParentCategories(category.CategoryId)
                    .Reverse()
                    .Select(x => new BreadCrumbs(x.Name,
                        x.CategoryId == 0
                            ? Url.AbsoluteRouteUrl("CatalogRoot")
                            : Url.AbsoluteRouteUrl("Category", new { url = x.UrlPath })))
                    .ToList();

            model.BreadCrumbs.Insert(0, new BreadCrumbs(T("MainPage"), Url.AbsoluteRouteUrl("Home")));

            if (!SaasDataService.IsSaasEnabled ||
                (SaasDataService.IsSaasEnabled && SaasDataService.CurrentSaasData.HaveTags))
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
                model.TagView = new TagViewModel { CategoryUrl = category.UrlPath };
            }

            model.TagView.CategoryUrl = ModulesExecuter.GetUrlParentCategory(model.TagView.CategoryUrl);

            if (tag != null)
            {
                model.Tag = tag;
                categoryModel.TagId = tag.Id;
            }

            var indepth = categoryModel.Indepth || category.DisplayChildProducts;

            var productsCount = SettingsCatalog.ShowOnlyAvalible
                ? category.Available_Products_Count
                : (indepth
                    ? category.ProductsCount
                    : category.Current_Products_Count);

            if (productsCount > 0)
            {
                var paging = new CategoryProductPagingHandler(category, indepth, categoryModel, true).GetForCatalog();

                if ((paging.Pager.TotalPages < paging.Pager.CurrentPage && paging.Pager.CurrentPage > 1) ||
                    paging.Pager.CurrentPage < 0)
                {
                    return Error404();
                }

                model.Pager = paging.Pager;
                model.Products = paging.Products;
                model.Filter = paging.Filter;
            }

            model.SortingList = new List<SelectListItem>();
            foreach (ESortOrder sorting in Enum.GetValues(typeof(ESortOrder)))
            {
                if (sorting.Ignore())
                    continue;

                if (sorting == ESortOrder.DescByRatio && !SettingsCatalog.EnableProductRating)
                    continue;

                model.SortingList.Add(new SelectListItem()
                {
                    Text = sorting.Localize(),
                    Value = sorting.StrName(),
                    Selected = model.Filter != null && model.Filter.Sorting == sorting
                });
            }

            model.ParentCategory = model.Category.CategoryId == 0
                ? model.Category
                : CategoryService.GetCategory(model.Category.ParentCategoryId);

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
                        tagManager.ProdIds.Add(product.ArtNo);
                    }
                }
            }

            SetMobileTitle(CategoryService.GetCategory(0).Name);

            if (tag != null)
            {
                SetMetaInformation(tag.Meta, tag.Name, category.Name, page: categoryModel.Page ?? 1,
                    totalPages: model.Pager != null ? model.Pager.TotalPages : 0);
            }
            else
            {
                SetMetaInformation(category.Meta, category.Name, page: categoryModel.Page ?? 1,
                    tags: category.Tags.Select(x => x.Name).ToList(),
                    totalPages: model.Pager != null ? model.Pager.TotalPages : 0);
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult GetProductList(EProductOnMain type, CategoryModel categoryModel, int? list)
        {
            if (type == EProductOnMain.None)
                return Error404();

            if (categoryModel.Page != null && categoryModel.Page < 0)
                return Error404();

            ProductList productList = null;
            if (type == EProductOnMain.List)
            {
                if (list == null)
                    return Error404();

                productList = ProductListService.Get(list.Value);
                if (productList == null)
                    return Error404();
            }

            var paging = new ProductListHandler(type, true, categoryModel, list, true,/*GlorySoft_016*/ null).Get();

            if ((paging.Pager.TotalPages < paging.Pager.CurrentPage && paging.Pager.CurrentPage > 1) ||
                paging.Pager.CurrentPage < 0)
            {
                return Error404();
            }

            return PartialView("_ProductView", paging.Products);
        }

        [HttpGet]
        public ActionResult GetCategoryProductList(CategoryModel categoryModel)
        {
            if ((string.IsNullOrWhiteSpace(categoryModel.Url) && categoryModel.CategoryId != 0) ||
                (categoryModel.Page != null && categoryModel.Page < 0))
                return Error404();

            var category = categoryModel.CategoryId.HasValue
                ? CategoryService.GetCategory(categoryModel.CategoryId.Value)
                : categoryModel.Url != null
                    ? CategoryService.GetCategory(categoryModel.Url)
                    : null;

            var indepth = categoryModel.Indepth || category.DisplayChildProducts;

            var productsCount = SettingsCatalog.ShowOnlyAvalible
                ? category.Available_Products_Count
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

        [ChildActionOnly]
        public ActionResult CategoryList(int categoryId, ECategoryDisplayStyle? displayStyle = null)
        {
            if (displayStyle.HasValue && displayStyle.Value == ECategoryDisplayStyle.None)
            {
                return new EmptyResult();
            }

            var category = CategoryService.GetCategory(categoryId);
            var categories =
                CategoryService.GetChildCategoriesByCategoryId(categoryId,
                        warehouseIds: WarehouseContext.GetAvailableWarehouseIds())
                    .Where(cat => cat.Enabled && cat.ParentsEnabled && !cat.Hidden)
                    .ToList();

            if (categories.Count == 0)
                return new EmptyResult();

            var model = new CategoryListViewModel()
            {
                Categories = categories,
                DisplayProductCount = SettingsCatalog.ShowProductsCount,
                PhotoHeight = SettingsPictureSize.SmallCategoryImageHeight,
                DisplayStyle = displayStyle ?? category.DisplayStyle,
                PhotoWidth = SettingsPictureSize.SmallCategoryImageWidth,
            };

            return PartialView(model);
        }

        #endregion

        #region Product list

        public ActionResult ProductList(EProductOnMain? type, CategoryModel categoryModel, string list,/*GlorySoft_016*/ int? salesType)
        {
            try
            {
                var (model, meta) = new GetProductListMobileHandler(type, categoryModel, list,/*GlorySoft_016*/ salesType).Execute();

                SetMetaInformation(meta, model.Title, page: categoryModel.Page ?? 1, totalPages: model.Pager != null
                    ? model.Pager.TotalPages
                    : 0);
                SetNgController(Web.Infrastructure.Controllers.NgControllers.NgControllersTypes.ProductListCtrl);

                return View(model);
            }
            catch (BlException)
            {
                return Error404();
            }
        }

        #endregion

        #region Search

        public ActionResult Search(SearchCatalogModel model)
        {
            if (model.Page != null && model.Page < 1)
                return Error404();

            if (model.Q.IsNotEmpty())
                model.Q = HttpUtility.UrlDecode(model.Q);
            
            var viewModel = new SearchPagingHandler(model, true).Get();
            
            ModulesExecuter.Search(model.Q, viewModel.Pager.TotalItemsCount);

            if (model.Q.IsNotEmpty())
            {
                var url = Request.Url.ToString();
                url = url.Substring(url.LastIndexOf("/"), url.Length - url.LastIndexOf("/"));

                StatisticService.AddSearchStatistic(url, StringHelper.HtmlEncode(model.Q),
                    string.Format(T("Search.Index.SearchIn"), "", 0, "∞"),
                    viewModel.Pager.TotalItemsCount,
                    CustomerContext.CurrentCustomer.Id);
            }
            
            if ((viewModel.Pager.TotalPages < viewModel.Pager.CurrentPage && viewModel.Pager.CurrentPage > 1) ||
                viewModel.Pager.CurrentPage < 0)
            {
                return Error404();
            }
            
            var gtm = GoogleTagManagerContext.Current;
            if (gtm.Enabled)
            {
                gtm.PageType = ePageType.searchresults;
                gtm.ProdIds = new List<string>();
                if (viewModel.HasProducts)
                {
                    foreach (var item in viewModel.Products.Products)
                        gtm.ProdIds.Add(item.ArtNo);
                }
            }
            
            viewModel.SortingList = new List<SelectListItem>();
            foreach (ESortOrder sorting in Enum.GetValues(typeof(ESortOrder)))
            {
                if (sorting.Ignore() || sorting == ESortOrder.AscByAddingDate)
                    continue;

                viewModel.SortingList.Add(new SelectListItem()
                {
                    Text = sorting.Localize(),
                    Value = sorting.StrName(),
                    Selected = model.Sort == sorting
                });
            }

            SetMobileTitle(T("Search.Index.SearchTitle"));
            SetMetaInformation(
                new MetaInfo(string.Format("{0} - {1}", SettingsMain.ShopName, T("Search.Index.SearchTitle"))),
                model.Q, page: model.Page ?? 1);
            
            viewModel.SearchCatalogModel.Q = ClearSearch(viewModel.SearchCatalogModel.Q);

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult GetSearchProductList(SearchCatalogModel model)
        {
            if (model.Page != null && model.Page < 1)
                return Error404();

            var viewModel = new SearchPagingHandler(model, true).Get();

            viewModel.SortingList = new List<SelectListItem>();
            foreach (ESortOrder sorting in Enum.GetValues(typeof(ESortOrder)))
            {
                if (sorting.Ignore() || sorting == ESortOrder.AscByAddingDate)
                    continue;

                viewModel.SortingList.Add(new SelectListItem()
                {
                    Text = sorting.Localize(),
                    Value = sorting.StrName(),
                    Selected = model.Sort == sorting
                });
            }

            return PartialView("_ProductView", viewModel.Products);
        }

        #endregion

        public ActionResult CatalogMenu(int categoryId, bool showRoot = false, bool isRootItems = false)
        {
            var model = new GetCatalogMenu(categoryId, showRoot, isRootItems).Execute();
            return PartialView(model);
        }

        [ChildActionOnly]
        public ActionResult CatalogRootsOnMain(string viewMode = null)
        {
            SettingsMobile.eViewCategoriesOnMain viewCategories;

            if (!Enum.TryParse(viewMode ?? SettingsMobile.ViewCategoriesOnMain, out viewCategories))
            {
                viewCategories = SettingsMobile.eViewCategoriesOnMain.Default;
            }

            if (viewCategories == SettingsMobile.eViewCategoriesOnMain.None)
            {
                return new EmptyResult();
            }

            var model = new GetCatalogRoots(viewCategories,
                SettingsDesign.OnePageCatalog ? (int?)SettingsCatalog.MaximumItemsInMenu : null).Execute();
            return PartialView(model);
        }

        public JsonResult CatalogRoots(string viewMode = null)
        {
            SettingsMobile.eViewCategoriesOnMain viewCategories;

            if (!Enum.TryParse(viewMode ?? SettingsMobile.ViewCategoriesOnMain, out viewCategories))
            {
                viewCategories = SettingsMobile.eViewCategoriesOnMain.Default;
            }

            var model = new GetCatalogRoots(viewCategories,
                SettingsDesign.OnePageCatalog ? (int?)SettingsCatalog.MaximumItemsInMenu : null).Execute();

            return JsonOk(model);
        }

        #region ProductsByIds

        public ActionResult ProductsByOfferIds(string ids, string title, string type,
            int offerId = 0, bool? hideDescription = false, bool enabledCarousel = true)
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
                    ids.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
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

            var productIds = ProductService.GetProductIdsByOfferIds(offerIds).Where(x => x != excludeProductId)
                .ToList();

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

            var products = new ProductViewModel(productModels, true)
            {
                Title = title,
                ShowBriefDescription = SettingsMobile.ShowBriefDescription
            };

            var model = new ProductByIdMobileModel
            {
                Products = products,
                RelatedType = type,
                Title = title,
                EnabledCarousel = enabledCarousel
            };

            return PartialView(model);
        }

        #endregion
        
        private static string ClearSearch(string q) =>
            WebUtility.UrlDecode(q)
                ?.Trim('{').Trim('}')
                .TrimStart('$');
    }
}