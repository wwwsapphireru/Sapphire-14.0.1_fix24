using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Diagnostics;
using AdvantShop.FilePath;
using AdvantShop.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class ProductServiceV8
    {
        public static List<ProductModel> GetForAutoCompleteProducts(string productIds)
        {
            return
                ModulesRepository.Query<ProductModel>(
                    "Select Product.ProductID, Product.Name, Product.ArtNo, Product.UrlPath, Product.Enabled, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, " +
                    "Product.Discount, Product.DiscountAmount, Product.MinAmount, Product.MaxAmount, MaxAvailable AS Amount, MinPrice as BasePrice, CurrencyValue, " +
                    "Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, Gifts, DoNotApplyOtherDiscounts " +
                    "From Catalog.Product " +
                    "Inner Join (select item, sort from [Settings].[ParsingBySeperator](@productIds,'/') ) as dtt on Product.ProductId=convert(int, dtt.item) " +
                    "Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID] " +
                    "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                    "Left Join [Catalog].[Photo]  On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                    "Where Enabled = 1 And CategoryEnabled = 1 " +
                    (SettingsCatalog.ShowOnlyAvalible ? "AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) " : "") +
                    "Order by dtt.sort",
                    new
                    {
                        productIds = productIds,
                        Type = PhotoType.Product.ToString(),
                    }).ToList();
        }

        public static void UpdateProductDescription(int productId, string description, string modifiedBy)
        {
            Debug.Log.Info("UpdateProductDescription");
            ModulesRepository.ModuleExecuteNonQuery(
                "Update Catalog.Product Set Description = @description, ModifiedBy = @modifiedBy Where ProductId = @productId ",
                CommandType.Text,
                new SqlParameter("@productId", productId),
                new SqlParameter("@description", description),
                new SqlParameter("@modifiedBy", modifiedBy)
                );
            ExportService.DoProductUpdated(productId, Models.Api.ExportType.Description);
        }

        public static List<int> GetCategoriesIDsByProductIds(List<int> productIds, bool onlyActive)
        {
            if (productIds == null || productIds.Count == 0)
                return new List<int>();
            if (onlyActive)
            {
                return ModulesRepository.ModuleExecuteReadColumn<int>(
                    "SELECT Distinct Category.CategoryID FROM Catalog.ProductCategories " +
                    "inner join Catalog.Category on Category.CategoryId = ProductCategories.CategoryId " +
                    string.Format("WHERE ProductID In ({0}) and Enabled = 1 and HirecalEnabled = 1 and Hidden = 0 And Main = 1 ", productIds.AggregateString(',')),
                    //"order by main desc",
                    CommandType.Text, "CategoryID");
            }
            else
            {
                return ModulesRepository.ModuleExecuteReadColumn<int>(
                    "SELECT Distinct CategoryID FROM Catalog.ProductCategories " +
                    string.Format("WHERE ProductID In ({0}) And Main = 1 ", productIds.AggregateString(',')),
                    //"order by main desc",
                    CommandType.Text, "CategoryID");
            }
        }

        public static Image ImageFromFile(string url, string artNo, out string tmpFilePath)
        {
            Image image = null;

            tmpFilePath = FoldersHelper.GetPathAbsolut(FolderType.ImageTemp, $"tmp-{artNo}.jpg");
            //FileHelpers.DeleteFile(tmpFilePath);
            if (FileHelpers.DownloadRemoteImageFile(url, tmpFilePath))
                image = Image.FromFile(tmpFilePath);
            else
            {
                int i = 0;
            }

            return image;
        }

    }
}
