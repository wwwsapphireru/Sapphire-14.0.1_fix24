using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.ViewModel.Catalog;
using AdvantShop.Web.Infrastructure.Handlers;
using AdvantShop.Models.Catalog;
using AdvantShop.Saas;
using AdvantShop.SEO;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Web.Infrastructure.Extensions;

namespace AdvantShop.Handlers.Catalog
{
    public class GetProductListHandler : ICommandHandler<(ProductListViewModel, MetaInfo, string)>
    {
        private readonly EProductOnMain? _type;
        private EProductOnMain _currentType;
        private readonly CategoryModel _categoryModel;
        private readonly UrlHelper _urlHelper;
        
        private readonly string _list;
        private Tag _tag;
        private ProductList _productList;

        private bool _existsBest;
        private bool _existsNew;
        private bool _existsSale;
        private readonly int? _salesType;//GlorySoft_016

        private ProductListPagingModel _paging;

        private string _title;
        
        public GetProductListHandler(EProductOnMain? type, CategoryModel categoryModel, string list, UrlHelper urlHelper,/*GlorySoft_016*/ int? salesType)
        {
            _type = type;
            _categoryModel = categoryModel;
            _list = list;
            _urlHelper = urlHelper;
            _salesType = salesType;//GlorySoft_016
        }
        public (ProductListViewModel, MetaInfo, string) Execute()
        {
            Load();
            Validate();
            LoadPaging();
            ValidatePaging();
            BuildTitle();
            return Build();
        }

        private void Load()
        {
            _tag = TagService.GetByUrl(_categoryModel.TagUrl);
            _productList = ProductListService.GetByPath(_list);
            
            _existsBest = ProductOnMain.IsExistsProductByType(EProductOnMain.Best);
            _existsNew = ProductOnMain.IsExistsProductByType(EProductOnMain.New);
            _existsSale = ProductOnMain.IsExistsProductByType(EProductOnMain.Sale);
        }

        private void Validate()
        {
            if (_type == null || _type == EProductOnMain.None)
                throw new BlException("");
            else
                _currentType = _type.Value;

            if (_categoryModel.Page != null && _categoryModel.Page < 0)
                throw new BlException("");

            if (_currentType == EProductOnMain.List)
                ValidateTypeList();
            else
                ValidateType(_currentType);
        }
        
        private void LoadPaging() =>
            _paging = new ProductListHandler(
                _currentType, 
                true, 
                _categoryModel, 
                _productList?.Id, 
                false,/*GlorySoft_016*/ _salesType).Get();

        private (ProductListViewModel, MetaInfo, string) Build() => 
            (BuildModel(), BuildMeta(), _title);

        private void ValidateTypeList()
        {
            if (string.IsNullOrEmpty(_list))
                throw new BlException("");
                
            if (_productList == null || !_productList.Enabled)
                throw new BlException("");
        }

        private void ValidateType(EProductOnMain type)
        {
            switch (type)
            {
                case EProductOnMain.Best when !SettingsCatalog.BestEnabled 
                                              || !_existsBest:
                case EProductOnMain.New when !SettingsCatalog.NewEnabled 
                                             || !_existsNew:
                case EProductOnMain.NewArrivals when !SettingsCatalog.NewEnabled 
                                                     || !SettingsCatalog.DisplayLatestProductsInNewOnMainPage 
                                                     || _existsNew:
                case EProductOnMain.Sale when !SettingsCatalog.SalesEnabled
                                              || !_existsSale:
                    throw new BlException("");
            }
        }

        private void ValidatePaging()
        {
            if ((_paging.Pager.TotalPages < _paging.Pager.CurrentPage
                 && _paging.Pager.CurrentPage > 1)
                || _paging.Pager.CurrentPage < 0)
            {
                throw new BlException("");
            }
        }
        
        private ProductListViewModel BuildModel() => 
            new ProductListViewModel()
            {
                Type = _currentType,
                ShowBest = _existsBest && SettingsCatalog.BestEnabled,
                ShowNew = SettingsCatalog.NewEnabled &&
                          (_existsNew || SettingsCatalog.DisplayLatestProductsInNewOnMainPage),
                ShowSale = _existsSale && SettingsCatalog.SalesEnabled,
                NewArrivals = !_existsNew && SettingsCatalog.DisplayLatestProductsInNewOnMainPage,
                ProductLists = ProductListService.GetMainPageList(false),
                TagView = BuildTagView(),
                Tag = _tag != null ? InsertTag() : null,
                Pager = _paging.Pager,
                Products = _paging.Products,
                Filter = _paging.Filter,
                Description = BuildDescription(),
                ListId = _currentType == EProductOnMain.List && _productList != null 
                    ? _productList.Id 
                    : 0,
                BreadCrumbs = 
                    new List<BreadCrumbs>()
                    {
                        new BreadCrumbs(LocalizationService.GetResource("MainPage"), _urlHelper.AbsoluteRouteUrl("Home")),
                        new BreadCrumbs(_title, _urlHelper.AbsoluteRouteUrl("ProductList", new {type = _currentType.ToString()}))
                    },
                SalesType = _salesType//GlorySoft_016
            };

        private MetaInfo BuildMeta()
        {
            switch (_currentType)
            {
                case EProductOnMain.List when _productList != null:
                    return _productList.Meta;
                case EProductOnMain.List when _productList == null:
                    return MetaInfoService.GetDefaultMetaInfo(MetaType.MainPageProducts, _title);
                default:
                    var objType = 
                        _currentType == EProductOnMain.NewArrivals 
                            ? EProductOnMain.New 
                            : _currentType;
                    return MetaInfoService.GetMetaInfo(-1 * (int) objType, MetaType.MainPageProducts) ?? MetaInfoService.GetDefaultMetaInfo(MetaType.MainPageProducts, _title);
            }
        }

        private void BuildTitle()
        {
            switch (_currentType)
            {
                case EProductOnMain.Best:
                    _title = LocalizationService.GetResource("Catalog.ProductList.AllBestSellers");
                    break;
                case EProductOnMain.New:
                case EProductOnMain.NewArrivals:
                    _title = LocalizationService.GetResource("Catalog.ProductList.AllNewProducts");
                    break;
                case EProductOnMain.Sale:
                    if (_salesType == 1)/*GlorySoft_016*/
                        _title = LocalizationService.GetResource("Catalog.ProductList.AllSales");
                    else if (_salesType == 2)//GlorySoft_016
                        _title = LocalizationService.GetResource("Catalog.ProductList.AllSales2");
                    break;
                case EProductOnMain.List:
                    _title = _productList != null ? _productList.Name : string.Empty;
                    break;
                default:
                    _title = string.Empty;
                    break;
            }
        }
        
        private TagViewModel BuildTagView() =>
            !SaasDataService.IsSaasEnabled || (SaasDataService.IsSaasEnabled && SaasDataService.CurrentSaasData.HaveTags && _type.HasValue) 
                ? new TagViewModel()
                {
                    CategoryUrl = _currentType.ToString().ToLower(),
                    NonCategoryView = true,
                    Tags = TagService.GetTagsByProductOnMain(_currentType, _productList?.Id)
                        .Select(x => new TagView
                        {
                            Name = x.Name,
                            Url = x.UrlPath,
                            Selected = x.Id == (_tag?.Id ?? 0)
                        }).ToList()
                }
                : new TagViewModel()
                {
                    CategoryUrl = _currentType.ToString().ToLower(),
                    NonCategoryView = true,
                    Tags = new List<TagView>()
                };

        private Tag InsertTag()
        {
            _categoryModel.TagId = _tag.Id;
            return _tag;
        }

        private string BuildDescription()
        {
            switch (_currentType)
            {
                case EProductOnMain.Best:
                    return SettingsCatalog.BestDescription;
                case EProductOnMain.New:
                case EProductOnMain.NewArrivals:
                    return SettingsCatalog.NewDescription;
                case EProductOnMain.Sale:
                    if (_salesType == 1)/*GlorySoft_016*/
                        return SettingsCatalog.DiscountDescription;
                    else//GlorySoft_016
                        return string.Empty;
                case EProductOnMain.List when _productList != null:
                    return _productList.Description;
                default:
                    return string.Empty;
            }
        }
    }
}