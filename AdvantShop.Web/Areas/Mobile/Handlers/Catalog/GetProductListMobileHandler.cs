using System;
using System.Collections.Generic;
using System.Web.Mvc;
using AdvantShop.Areas.Mobile.Models.Catalog;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Handlers.Catalog;
using AdvantShop.Models.Catalog;
using AdvantShop.SEO;
using AdvantShop.Web.Infrastructure.Handlers;

namespace AdvantShop.Areas.Mobile.Handlers.Catalog
{
    public class GetProductListMobileHandler : ICommandHandler<(ProductListMobileViewModel, MetaInfo)>
    {
        private readonly EProductOnMain? _type;
        private EProductOnMain _currentType;
        private readonly CategoryModel _categoryModel;
        
        private readonly string _list;
        private ProductList _productList;

        private bool _existsBest;
        private bool _existsNew;
        private bool _existsSale;
        private readonly int? _salesType;//GlorySoft_016


        private ProductListPagingModel _paging;

        private string _title;
        private readonly List<SelectListItem> _sorting = new List<SelectListItem>();
        
        public GetProductListMobileHandler(EProductOnMain? type, CategoryModel categoryModel, string list,/*GlorySoft_016*/ int? salesType)
        {
            _type = type;
            _categoryModel = categoryModel;
            _list = list;
            _salesType = salesType;//GlorySoft_016
        }

        public (ProductListMobileViewModel, MetaInfo) Execute()
        {
            Load();
            Validate();
            LoadPaging();
            ValidatePaging();
            BuildTitle();
            BuildSorting();
            return Build();
        }
        
        private void Load()
        {
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
                true,/*GlorySoft_016*/ _salesType).Get();
        
        private (ProductListMobileViewModel, MetaInfo) Build() => 
            (BuildModel(), BuildMeta());
        
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
        
        private ProductListMobileViewModel BuildModel() => 
            new ProductListMobileViewModel()
            {
                ListId = _currentType == EProductOnMain.List && _productList != null 
                    ? _productList.Id 
                    : 0,
                Title = _title,
                Type = _currentType.ToString().ToLower(),
                Products = _paging.Products,
                Pager = _paging.Pager,
                Filter = _paging.Filter,
                SortingList = _sorting,
                Description = BuildDescription(),
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

        private void BuildSorting()
        {
            foreach (ESortOrder sorting in Enum.GetValues(typeof(ESortOrder)))
            {
                if (sorting.Ignore() || sorting == ESortOrder.AscByAddingDate)
                    continue;

                _sorting.Add(new SelectListItem()
                {
                    Text = sorting.Localize(),
                    Value = sorting.StrName(),
                    Selected = string.Equals(_categoryModel.Sort.ToString(), sorting.StrName(), StringComparison.OrdinalIgnoreCase)
                });
            }
        }
    }
}