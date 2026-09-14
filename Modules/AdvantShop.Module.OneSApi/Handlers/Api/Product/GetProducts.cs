using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AdvantShop.Core;
using AdvantShop.Core.SQL2;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Web.Infrastructure.Api;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Products
{
    public class GetProducts : EntitiesHandler<FilterProductsModel, ProductExportModel>
    {
        private readonly ImportExportSettingsModel _settings;

        public string LogFile { get; set; }

        public GetProducts(FilterProductsModel filterModel, ImportExportSettingsModel settings) : base(filterModel) 
        {
            _settings = settings;
        }

        protected override SqlPaging Select(SqlPaging paging)
        {
            paging.Select(
                "[Product].[ProductId]",
                "[Product].[ArtNo]",
                "[Product].[Name]",
                "[Product].[Description]",
                "[OneSApi_Product].[ExternalId]",
                "IsNull([OneSApi_ProductExport].[ExportType], 4)".AsSqlField("ExportType"),
                //"IsNull([OneSApi_ProductExport].[Changed], GetDate())".AsSqlField("Changed"),
                "IsNull([OneSApi_ProductExport].[Exported], GetDate())".AsSqlField("Exported")
                );

            if (FilterModel.All)
            {
                paging.From("[Module].[OneSApi_Product]");
                paging.Left_Join("[Module].[OneSApi_ProductExport] ON [OneSApi_Product].[ProductId]=[OneSApi_ProductExport].[ProductId]");
            }
            else
            {
                paging.From("[Module].[OneSApi_ProductExport]");
                paging.Left_Join("[Module].[OneSApi_Product] ON [OneSApi_Product].[ProductId]=[OneSApi_ProductExport].[ProductId]");
            }
            paging.Left_Join("[Catalog].[Product] ON [Product].[ProductId]=[OneSApi_Product].[ProductId]");

            return paging;
        }

        protected override SqlPaging Filter(SqlPaging paging)
        {
            paging.Where("[Product].[ProductId] Is Not NULL");
            if (!FilterModel.All)
                paging.Where("[OneSApi_ProductExport].[ForExport] = 1");

            if (FilterModel.ProductId.HasValue)
                paging.Where("[Product].[ProductId] = {0}", FilterModel.ProductId.Value);

            return paging;
        }

        protected override SqlPaging Sorting(SqlPaging paging)
        {
            if (string.IsNullOrEmpty(FilterModel.Sorting) || FilterModel.SortingType == FilterSortingType.None)
            {
                paging.OrderBy("[OneSApi_ProductExport].[Changed]");

                return paging;
            }

            /*var sorting = FilterModel.Sorting;

            var field = paging.SelectFields().FirstOrDefault(x => x.FieldName.Equals(sorting, StringComparison.OrdinalIgnoreCase));
            if (field != null)
            {
                if (FilterModel.SortingType == FilterSortingType.Asc)
                    paging.OrderBy(sorting);
                else
                    paging.OrderByDesc(sorting);
            }*/

            return paging;
        }

        protected override List<ProductExportModel> FillItems(SqlPaging paging)
        {
            return paging.PageItemsList<ProductExportModel>(GetFromReader);
        }

        private ProductExportModel GetFromReader(IDataReader reader)
        {
            var item = new ProductExportModel
            {
                ProductId = SQLDataHelper.GetInt(reader, "ProductId"),
                ArtNo = SQLDataHelper.GetString(reader, "ArtNo"),
                Name = SQLDataHelper.GetString(reader, "Name"),
                Description = SQLDataHelper.GetString(reader, "Description"),
                ExternalId = SQLDataHelper.GetString(reader, "ExternalId"),
                ExportType = SQLDataHelper.GetInt(reader, "ExportType"),

                Changed = SQLDataHelper.GetDateTime(reader, "Changed"),
                Exported = SQLDataHelper.GetNullableDateTime(reader, "Exported"),
            };

            return item;
        }
    }
}