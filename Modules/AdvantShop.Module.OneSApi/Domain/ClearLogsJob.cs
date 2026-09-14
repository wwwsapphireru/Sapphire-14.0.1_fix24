using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Scheduler;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.FilePath;
using AdvantShop.FullSearch;
using AdvantShop.Module.OneSApi.Service;
using Quartz;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace AdvantShop.Module.OneSApi.Domain
{
    [DisallowConcurrentExecution]
    public class ClearLogsJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            if (!context.CanStart())
                return;
            context.WriteLastRun();

            var settings = ModuleService.GetImportExportSettings();
            if (settings.ClearLogsDays == 0)
                return;

            var logPath = FoldersHelper.GetPathAbsolut(FolderType.UserFiles, "modules\\" + OneSApi.ModuleStringId);
            var folders = Directory.GetDirectories(logPath);
            var deadline = DateTime.Now.AddDays(-settings.ClearLogsDays);
            foreach (var folder in folders)
            {
                var files = Directory.GetFiles(folder, "*.json").
                    Where(x => DateTime.ParseExact((new FileInfo(x).Name).Replace(".json", ""), "yyyyMMddHHmmss", CultureInfo.InvariantCulture) < deadline);
                foreach (var file in files)
                    File.Delete(file);
            }

            var categoryIds = CategoryService.GetAllCategoryIDs();
            foreach (var categoryId in categoryIds)
                CategoryService.SetCategoryHierarchicallyEnabled(categoryId);

            var productIds = ProductService.GetAllProductIDs();
            foreach (var productId in productIds)
                ProductService.SetProductHierarchicallyEnabled(productId);

            CategoryService.RecalculateProductsCountManual();
            var root = CategoryService.GetChildIDsHierarchical(0);
            foreach (var catId in root)
            {
                var cat = CategoryService.GetCategory(catId);
                var count = SettingsCatalog.ShowOnlyAvalible ? cat.Available_Products_Count : cat.ProductsCount;
                if (count <= 0 && !cat.Hidden)
                {
                    cat.Hidden = true;
                    ImportService.SetCategoryHidden(cat.CategoryId, true);
                }
                else if (count > 0 && cat.Hidden)
                {
                    cat.Hidden = false;
                    ImportService.SetCategoryHidden(cat.CategoryId, false);
                }
            }

            ReIndexLucene(false);
            ReIndexLucene(true);

            var expired = ModuleService.GetExpiredConfirmRegistrartion();
            Debug.Log.Info(Newtonsoft.Json.JsonConvert.SerializeObject(expired));
            foreach (var guid in expired)
            {
                try
                {
                    CustomerService.DeleteCustomer(guid);
                }
                catch (Exception ex)
                {
                    Debug.Log.Error(ex);
                }
            }
        }

        private bool ReIndexLucene(bool secondTry = false)
        {
            try
            {
                if (secondTry)
                {
                    try
                    {
                        var dir = new DirectoryInfo(HostingEnvironment.MapPath("~/App_Data/Lucene"));
                        foreach (var file in dir.GetFiles())
                            file.Delete();
                        foreach (var directory in dir.GetDirectories())
                            directory.Delete(true);
                    }
                    catch
                    {
                    }
                }

                LuceneSearch.CreateAllIndex();
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                return false;
            }
            return true;
        }

    }
}
