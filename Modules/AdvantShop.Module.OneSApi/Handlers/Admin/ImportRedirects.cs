using AdvantShop.Catalog;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Diagnostics;
using AdvantShop.FilePath;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.SEO;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class ImportRedirects
    {
        private string _filename;

        public ImportRedirects(string filename)
        {
            _filename = filename;
        }

        public void Execute(bool withStatisctic)
        {
            var filePath = FoldersHelper.GetPathAbsolut(FolderType.UserFiles);
            var fullFileName = filePath + _filename + ".csv";

            if (withStatisctic)
            {
                long count = 0;
                using (var csvReader = new CsvReader(new StreamReader(fullFileName), new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                    while (csvReader.Read())
                        count++;
                }
                ModuleStatistic.TotalRow = count;
            }

            using (var csvReader = new CsvReader(new StreamReader(fullFileName), new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";" }))
            {
                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    if (withStatisctic)
                        ModuleStatistic.RowPosition++;
                    try
                    {
                        var currentRecord = new RedirectSeo
                        {
                            RedirectFrom = HttpUtility.UrlDecode(csvReader.GetField<string>("RedirectFrom").ToLower()),
                            //RedirectTo = HttpUtility.UrlDecode(csvReader.GetField<string>("RedirectTo").ToLower()),
                            ProductArtNo = csvReader.GetField<string>("ProductArtNo")
                        };
                        var externalId = HttpUtility.UrlDecode(csvReader.GetField<string>("RedirectTo"));

                        if (string.IsNullOrWhiteSpace(currentRecord.RedirectFrom) || currentRecord.RedirectFrom == "*")
                            continue;

                        var redirect = RedirectSeoService.GetRedirectsSeoByRedirectFrom(currentRecord.RedirectFrom);

                        if (redirect != null)
                            currentRecord.ID = redirect.ID;

                        if (currentRecord.ProductArtNo.IsNullOrEmpty())
                        {
                            var category = CategoryService.GetCategoryFromDbByExternalId(externalId);
                            if (category == null)
                                continue;
                            currentRecord.RedirectTo = "categories/" + category.UrlPath;
                        }
                        else
                        {
                            var productId = ImportService.GetIdByExternalId(externalId, "Product");
                            var product = ProductService.GetProduct(productId);
                            if (product == null)
                            {
                                product = ProductService.GetProduct(currentRecord.ProductArtNo);
                                if (product == null)
                                    continue;
                            }
                            else if (product.ArtNo != currentRecord.ProductArtNo)
                                currentRecord.ProductArtNo = product.ArtNo;
                            currentRecord.RedirectTo = "products/" + product.UrlPath;
                        }

                        if (RedirectSeoService.CheckOnSystemUrl(currentRecord.RedirectFrom) || RedirectSeoService.CheckOnSystemUrl(currentRecord.RedirectTo))
                        {
                            //Debug.Log.Warn(string.Format(T("Admin.Js.Settings.AddEdit301RedCtrl.SystemUrl"), csvReader.Row));
                            continue;
                        }

                        //if (RedirectSeoService.IsToManyRedirects(currentRecord))
                        //{
                        //    //Debug.Log.Warn(string.Format(T("Admin.Js.Settings.Import301RedCtrl.ErrorToManyRed"), csvReader.Row));
                        //    continue;
                        //}

                        if (redirect == null)
                            RedirectSeoService.AddRedirectSeo(currentRecord);
                        else
                            RedirectSeoService.UpdateRedirectSeo(currentRecord);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log.Warn(ex);
                    }
                }
            }
        }

    }
}
