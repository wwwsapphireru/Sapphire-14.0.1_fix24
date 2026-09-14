using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AdvantShop.Catalog;
using AdvantShop.Core.SQL;
using AdvantShop.Helpers;
using AdvantShop.Module.Rees46.Models;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Service
{
    public class ExportCategoriesService
    {
        private readonly int _exportFeedId;

        public ExportCategoriesService(int exportFeedId)
        {
            _exportFeedId = exportFeedId;
        }

        public List<ExportCategoryModel> GetCategories(bool exportNotAvailable)
        {
            return SQLDataAccess.ExecuteReadList("[Settings].[sp_GetExportFeedCategories]",
                CommandType.StoredProcedure,
                reader =>
                {
                    var id = SQLDataHelper.GetInt(reader, "CategoryID");
                    var cat = CategoryService.GetCategory(id);
                    return new ExportCategoryModel
                    {
                        Id = id,
                        ParentCategory = SQLDataHelper.GetInt(reader, "ParentCategory"),
                        Name = SQLDataHelper.GetString(reader, "Name"),
                        UrlPath = cat.UrlPath,
                        Enabled = cat.Enabled && cat.ParentsEnabled
                    };
                },
                new SqlParameter("@exportFeedId", _exportFeedId),
                new SqlParameter("@onlyCount", false),
                new SqlParameter("@exportNotAvailable", exportNotAvailable));
        }

        public int GetCategoriesCount(bool exportNotAvailable)
        {
            return SQLDataAccess.ExecuteScalar<int>("[Settings].[sp_GetExportFeedCategories]",
                CommandType.StoredProcedure,
                60 * 3,
                new SqlParameter("@exportFeedId", _exportFeedId),
                new SqlParameter("@onlyCount", true),
                new SqlParameter("@exportNotAvailable", exportNotAvailable));
        }
    }
}