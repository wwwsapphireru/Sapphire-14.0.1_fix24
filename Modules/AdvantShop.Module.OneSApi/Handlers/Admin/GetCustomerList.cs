using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.SQL2;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Web.Infrastructure.Admin;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Customers
{
    public class GetCustomerList
    {
        private static readonly Type LocalizeAttributeType = typeof(LocalizeAttribute);
        private readonly CustomerListFilterModel _filterModel;
        private SqlPaging _paging;

        public GetCustomerList(CustomerListFilterModel filterModel)
        {
            _filterModel = filterModel;
        }

        public FilterResult<CustomerListModel> Execute()
        {
            var model = new FilterResult<CustomerListModel>();

            GetPaging();

            model.TotalItemsCount = _paging.TotalRowsCount;
            model.TotalPageCount = _paging.PageCount();
            model.TotalString = LocalizationService.GetResourceFormat("Admin.Grid.FildTotal", model.TotalItemsCount);

            if (model.TotalPageCount < _filterModel.Page && _filterModel.Page > 1)
            {
                return model;
            }

            model.DataItems = _paging.PageItemsList<CustomerListModel>();
            
            return model;
        }

        public List<int> GetItemsIds(string fieldName)
        {
            GetPaging();

            return _paging.ItemsIds<int>(fieldName);
        }

        private void GetPaging()
        {
            _paging = new SqlPaging()
            {
                ItemsPerPage = _filterModel.ItemsPerPage,
                CurrentPageIndex = _filterModel.Page
            };

            _paging.Select(
                "OneSApi_Customer.CustomerId",
                "(Select [Customer].Lastname + ' ' + [Customer].Firstname + ' ' + ISNULL([Customer].Patronymic,''))".AsSqlField("FullName"),
                "[Customer].Organization",
                "Changed"
            );

            _paging.From("[Module].[OneSApi_Customer]");
            _paging.Left_Join("[Customers].[Customer] ON [Customer].[CustomerId] = [OneSApi_Customer].[CustomerId]"); 

            Sorting();
            Filter();
        }

        private void Filter()
        {
            _paging.Where("ForExport = 1");
        }

        private void Sorting()
        {
            if (string.IsNullOrEmpty(_filterModel.Sorting) || _filterModel.SortingType == FilterSortingType.None)
            {
                _paging.OrderBy("Changed");
                return;
            }

            var sorting = _filterModel.Sorting.ToLower().Replace("formatted", "");

            var field = _paging.SelectFields().FirstOrDefault(x => x.FieldName == sorting);
            if (field != null)
            {
                if (_filterModel.SortingType == FilterSortingType.Asc)
                {
                    _paging.OrderBy(sorting);
                }
                else
                {
                    _paging.OrderByDesc(sorting);
                }
            }
        }
    }
}