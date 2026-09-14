//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using AdvantShop.Configuration;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.SQL;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;

namespace AdvantShop.Catalog
{
    public enum EProductOnMain
    {
        None = 0,
        Best = 1,
        New = 2,
        Sale = 3,
        List = 4,
        NewArrivals = 5,
        Recomended = 6//GlorySoft_023
    }

    public static class ProductOnMain
    {
        public static List<int> GetProductIdByType(EProductOnMain type, bool withPositiveSortOrder = false)
        {
            string sqlCmd;
            switch (type)
            {
                case EProductOnMain.Best:
                    sqlCmd = "select ProductId from Catalog.Product where Bestseller=1" + (withPositiveSortOrder ? " and SortBestseller>=0" : string.Empty);
                    break;
                case EProductOnMain.New:
                    sqlCmd = "select ProductId from Catalog.Product where New=1" + (withPositiveSortOrder ? " and SortNew>=0" : string.Empty);
                    break;
                case EProductOnMain.Sale:
                    sqlCmd = "select ProductId from Catalog.Product where (Discount > 0 or DiscountAmount > 0)" + (withPositiveSortOrder ? " and SortDiscount>=0" : string.Empty);
                    break;
                case EProductOnMain.Recomended://GlorySoft_023
                    sqlCmd = "select ProductId from Catalog.Product where Recomended=1";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return SQLDataAccess.ExecuteReadColumn<int>(sqlCmd, CommandType.Text, "ProductId", new SqlParameter("@type", (int)type));
        }

        public static List<int> GetProductIdByType(EProductOnMain type, int count)
        {
            var query =
                "Select Top(@Count) [Product].[ProductID] " +
                (SettingsCatalog.MoveNotAvaliableToEnd ? ",(CASE WHEN Price=0 THEN 0 ELSE 1 END) as TempSort, AmountSort as TempAmountSort " : "") +
                "From [Catalog].[Product] " +
                "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] " +

                "Where Product.Enabled=1 and Product.Hidden=0 and CategoryEnabled=1 {0}  " +
                (SettingsCatalog.ShowOnlyAvalible ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1)" : "") + " Order by {2}{1}";

            var moveNotAvailableToEnd = SettingsCatalog.MoveNotAvaliableToEnd ? "TempSort desc, TempAmountSort desc," : "";
            switch (type)
            {
                case EProductOnMain.Best:
                    query = string.Format(query, "and Bestseller=1", "SortBestseller, Product.ProductId desc", moveNotAvailableToEnd);
                    break;

                case EProductOnMain.New:
                    query = string.Format(query, "and New=1", "SortNew, Product.ProductId desc", moveNotAvailableToEnd);
                    break;
                case EProductOnMain.NewArrivals:
                    query = string.Format(query, "", "Product.ProductId desc", moveNotAvailableToEnd);
                    break;

                case EProductOnMain.Sale:
                    query = string.Format(query, "and (Discount>0 or DiscountAmount>0)", "SortDiscount, ProductId desc", moveNotAvailableToEnd);
                    break;

                case EProductOnMain.Recomended://GlorySoft_023
                    query = string.Format(query, "and Recomended=1", "Product.ProductId desc", moveNotAvailableToEnd);
                    break;

                default:
                    throw new NotImplementedException();
            }
            
            return CacheManager.Get(CacheNames.MainPageProductsCacheName(type.ToString(), count, query) + "_ids", 0, () =>
            {
                return
                    SQLDataAccess.ExecuteReadList(query, CommandType.Text,
                        reader => SQLDataHelper.GetInt(reader, "ProductId"), 
                        new SqlParameter("@Count", count));
            });
        }

        public static List<ProductModel> GetProductsByType(EProductOnMain type, int count, List<int> warehouseIds = null)
        {
            try
            {
                var query =
                    ";with cte (ProductId) as (select top(@Count) [Product].ProductId " +
                    "from catalog.Product " +
                    "inner join [Catalog].[ProductExt] on [ProductExt].ProductId = Product.ProductId " +
                    "where Product.Enabled = 1 and Product.Hidden = 0 and CategoryEnabled = 1 {0} " +
                    (SettingsCatalog.ShowOnlyAvalible ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) " : "") +
                    (warehouseIds != null 
                        ? " AND ([Product].[AllowPreOrder] = 1 " 
                              + "OR Exists(Select 1 from [Catalog].[Offer] "
                                          + "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] "
                                          + "where [WarehouseStocks].[WarehouseId] IN @warehouseIds "
                                          + "     and Offer.ProductId = [Product].[ProductID] "
                                          + "     AND [WarehouseStocks].[Quantity] > 0)) "
                        : "") +
                    "Order by {2}{1})" +

                    "Select [Product].[ProductID], Product.ArtNo, Product.Name, Recomended as Recomend, Bestseller, New, OnSale as Sales, Discount, DiscountAmount, " +
                    "Product.Enabled, Product.UrlPath, AllowPreOrder, Ratio, ManualRatio, Offer.OfferID, MaxAvailable AS Amount, MinAmount, MaxAmount, Offer.Amount AS AmountOffer, " +
                    "CountPhoto, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, Offer.ColorID, Product.DateAdded, " +
                    "null as AdditionalPhoto, Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity, Product.BriefDescription, " +
                    "Units.DisplayName as UnitDisplayName, Units.Name as UnitName, " +
                    (SettingsCatalog.ComplexFilter
                        ? @"Colors, 
                            NotSamePrices as MultiPrices, 
                            (CASE 
                                WHEN ProductExt.AllowAddToCartInCatalog = 1 
                                THEN Offer.Price
                                ELSE ProductExt.MinPrice 
                            END) as BasePrice, "
                        : "null as Colors, 0 as MultiPrices, Price as BasePrice, ") +
                    (warehouseIds != null ? "Offer.ColorID as PreSelectedColorId, " : "") +
                    "CurrencyValue, Comments, Gifts, ProductExt.AllowAddToCartInCatalog " +
                    "From cte inner join [Catalog].[Product] on cte.ProductId = [Product].ProductId " +
                    "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                    "Inner Join Catalog.Currency On Currency.CurrencyID = Product.CurrencyID " +
                    (warehouseIds is null
                        ? "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] And Type=@Type " +
                          "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] "
                        : "Outer Apply (Select top (1) Offer.OfferID, Amount, ArtNo, ColorID, SizeID, Price " +
                                        "From [Catalog].[Offer] " +
                                        "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] " +
                                        "Where Offer.ProductId = Product.ProductID " +
                                        "Order by (Case When [WarehouseStocks].[WarehouseId] IN @warehouseIds AND [WarehouseStocks].[Quantity] > 0 Then 1 Else 0 End) desc, Main desc" +
                                        ") as Offer " +
                          "Outer Apply (Select top (1) PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Description " +
                                        "From Catalog.Photo " +
                                        "Where Photo.ObjId = Product.ProductId and (Photo.ColorID = Offer.ColorID OR Photo.ColorID IS NULL) AND Type = 'Product' " +
                                        "Order by [Photo].Main DESC, [Photo].[PhotoSortOrder], [PhotoId]" +
                                        ") as Photo "
                    ) +
                    "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id]" +
                    "Order by {2}{1}";
                    

                    var moveNotAvailableToEnd = SettingsCatalog.MoveNotAvaliableToEnd ? "(CASE WHEN PriceTemp=0 THEN 0 ELSE 1 END) desc, AmountSort desc," : "";
                    switch (type)
                    {
                        case EProductOnMain.Best:
                            query = string.Format(query, "and Bestseller=1", "SortBestseller, Product.ProductId desc", moveNotAvailableToEnd);
                            break;

                        case EProductOnMain.New:
                            query = string.Format(query, "and New=1", "SortNew, Product.ProductId desc", moveNotAvailableToEnd);
                            break;
                        case EProductOnMain.NewArrivals:
                            query = string.Format(query, "", "Product.ProductId desc", moveNotAvailableToEnd);
                            break;

                        case EProductOnMain.Sale:
                            query = string.Format(query, "and (Discount>0 or DiscountAmount>0)", "SortDiscount, ProductId desc", moveNotAvailableToEnd);
                            break;

                        case EProductOnMain.Recomended://GlorySoft_023
                            query = string.Format(query, "and Recomended=1", "Product.ProductId desc", moveNotAvailableToEnd);
                            break;

                    default:
                            throw new NotImplementedException();
                    }


                    return CacheManager.Get(CacheNames.MainPageProductsCacheName(type.ToString(), count, query), 
                        () => 
                            SQLDataAccess.Query<ProductModel>(query, new
                                {
                                    count, 
                                    Type = PhotoType.Product.ToString(), 
                                    warehouseIds
                                }).ToList());
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }

            return null;
        }


        public static DataTable GetAdminProductsByType(EProductOnMain type, int count)
        {
            string sqlCmd;
            switch (type)
            {
                case EProductOnMain.Best:
                    sqlCmd = "select Top(@count) Product.ProductId, Name from Catalog.Product where Bestseller=1 order by SortBestseller";
                    break;
                case EProductOnMain.New:
                    sqlCmd = "select Top(@count) Product.ProductId, Name from Catalog.Product where New=1 order by SortNew";
                    break;
                case EProductOnMain.Sale:
                    sqlCmd = "select Top(@count) Product.ProductId, Name from Catalog.Product where (Discount > 0 or DiscountAmount > 0) order by SortDiscount";
                    break;
                case EProductOnMain.Recomended://GlorySoft_023
                    sqlCmd = "select Top(@count) Product.ProductId, Name from Catalog.Product where Recomended=1";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return SQLDataAccess.ExecuteTable(sqlCmd, CommandType.Text, new SqlParameter { ParameterName = "@count", Value = count });
        }

        public static int GetProductCountByType(EProductOnMain type, bool enabled = true)
        {
            return CacheManager.Get(CacheNames.MainPageProductsCountCacheName(type.ToString(), enabled), () =>
            {
                var sql = "select Count(ProductId) from Catalog.Product where " +
                          (enabled ? "Enabled=1 and Hidden=0 and CategoryEnabled=1 and" : "");

                switch (type)
                {
                    case EProductOnMain.Best:
                        sql += " bestseller=1";
                        break;
                    case EProductOnMain.New:
                        sql += " new=1";
                        break;
                    case EProductOnMain.NewArrivals:
                        sql += " new=0";
                        break;
                    case EProductOnMain.Sale:
                        sql += " (Discount > 0 or DiscountAmount > 0)";
                        break;
                    case EProductOnMain.Recomended://GlorySoft_023
                        sql += " Recomended=1";
                        break;
                    default:
                        throw new NotImplementedException();
                }
                return SQLDataAccess.ExecuteScalar<int>(sql, CommandType.Text);
            });
        }

        public static bool IsExistsProductByType(EProductOnMain type)
        {
            string sqlCmd = "if exists(select 1 from Catalog.Product where Enabled=1 and Hidden=0 and CategoryEnabled=1 and {0}) Select 1 else Select 0";
            switch (type)
            {
                case EProductOnMain.Best:
                    sqlCmd = string.Format(sqlCmd, "bestseller=1");
                    break;
                case EProductOnMain.New:
                    sqlCmd = string.Format(sqlCmd, "new=1");
                    break;
                case EProductOnMain.Sale:
                    sqlCmd = string.Format(sqlCmd, "(Discount > 0 or DiscountAmount > 0)");
                    break;
                case EProductOnMain.Recomended://GlorySoft_023
                    sqlCmd = string.Format(sqlCmd, "Recomended=1");
                    break;
                default:
                    throw new NotImplementedException();
            }
            return Convert.ToBoolean(SQLDataAccess.ExecuteScalar(sqlCmd, CommandType.Text));
        }


        public static void AddProductByType(int productId, EProductOnMain type)
        {
            string sqlCmd;
            switch (type)
            {
                case EProductOnMain.Best:
                    sqlCmd = "Update Catalog.Product set SortBestseller=(Select min(SortBestseller)-10 from Catalog.Product), Bestseller=1 where ProductId=@productId";
                    break;
                case EProductOnMain.New:
                    sqlCmd = "Update Catalog.Product set SortNew=(Select min(SortNew)-10 from Catalog.Product), New=1 where ProductId=@productId";
                    break;
                case EProductOnMain.Sale:
                    sqlCmd = "Update Catalog.Product set SortDiscount=(Select min(SortDiscount)-10 from Catalog.Product) where ProductId=@productId";
                    break;
                case EProductOnMain.Recomended://GlorySoft_023
                    sqlCmd = "Update Catalog.Product set Recomended=1 where ProductId=@productId";
                    break;
                default:
                    throw new NotImplementedException();
            }
            SQLDataAccess.ExecuteNonQuery(sqlCmd, CommandType.Text, new SqlParameter("@productId", productId));

            ClearCache();
        }

        public static void DeleteProductByType(int prodcutId, EProductOnMain type)
        {
            string sqlCmd;
            switch (type)
            {
                case EProductOnMain.Best:
                    sqlCmd = "Update Catalog.Product set SortBestseller=0, Bestseller=0 where ProductId=@productId";
                    break;
                case EProductOnMain.New:
                    sqlCmd = "Update Catalog.Product set SortNew=0, New=0 where ProductId=@productId";
                    break;
                case EProductOnMain.Sale:
                    sqlCmd = "Update Catalog.Product set SortDiscount=0, Discount=0, DiscountAmount=0 where ProductId=@productId";
                    break;
                case EProductOnMain.Recomended://GlorySoft_023
                    sqlCmd = "Update Catalog.Product set Recomended=0 where ProductId=@productId";
                    break;
                default:
                    throw new NotImplementedException();
            }

            SQLDataAccess.ExecuteNonQuery(sqlCmd, CommandType.Text, new SqlParameter("@productId", prodcutId));

            ClearCache();
        }

        public static void UpdateProductByType(int productId, int sortOrder, EProductOnMain type)
        {
            string sqlCmd;
            switch (type)
            {
                case EProductOnMain.Best:
                    sqlCmd = "Update Catalog.Product set SortBestseller=@sortOrder where ProductId=@productId and Bestseller=1";
                    break;
                case EProductOnMain.New:
                    sqlCmd = "Update Catalog.Product set SortNew=@sortOrder where ProductId=@productId and New=1";
                    break;
                case EProductOnMain.Sale:
                    sqlCmd = "Update Catalog.Product set SortDiscount=@sortOrder where ProductId=@productId";
                    break;
                default:
                    throw new NotImplementedException();
            }

            SQLDataAccess.ExecuteNonQuery(sqlCmd, CommandType.Text, new SqlParameter("@productId", productId), new SqlParameter("@sortOrder", sortOrder));

            ClearCache();
        }

        public static void ClearCache()
        {
            CacheManager.RemoveByPattern(CacheNames.SQLPagingItems);
            CacheManager.RemoveByPattern(CacheNames.SQLPagingCount);
        }

        public static void ShuffleLists()
        {
            if (SettingsCatalog.ShuffleBestOnMainPage)
            {
                ShuffleList(EProductOnMain.Best);
            }

            if (SettingsCatalog.ShuffleNewOnMainPage)
            {
                ShuffleList(EProductOnMain.New);
            }

            if (SettingsCatalog.ShuffleSalesOnMainPage)
            {
                ShuffleList(EProductOnMain.Sale);
            }

            foreach (var list in ProductListService.GetList().Where(x => x.ShuffleList))
            {
                ShuffleList(EProductOnMain.List, list.Id);
            }
        }

        private static void ShuffleList(EProductOnMain type, int? id = null)
        {
            var productIds = new List<int>();
            switch (type)
            {
                case EProductOnMain.Best:
                case EProductOnMain.New:
                case EProductOnMain.Sale:
                    productIds = GetProductIdByType(type, true);
                    break;

                case EProductOnMain.List:
                    if (id.HasValue)
                        productIds = ProductListService.GetProductIds(id.Value, true);
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (productIds.Count == 0)
                return;

            Random rnd = new Random();

            var sortOrder = 0;
            foreach (var productId in productIds.OrderBy(x => rnd.Next()))
            {
                switch (type)
                {
                    case EProductOnMain.Best:
                    case EProductOnMain.New:
                    case EProductOnMain.Sale:
                        UpdateProductByType(productId, sortOrder, type);
                        break;

                    case EProductOnMain.List:
                        if (id.HasValue)
                            ProductListService.UpdateProduct(id.Value, productId, sortOrder);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                sortOrder += 10;
            }
        }
    }
}