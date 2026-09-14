//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdvantShop.Configuration;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.Core.Services.FullSearch;
using AdvantShop.Core.Services.Taxes;
using AdvantShop.Core.SQL;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.Saas;
using AdvantShop.SEO;
using AdvantShop.Core.Services.ChangeHistories;

namespace AdvantShop.Catalog
{
    public class ProductService
    {
        public const int MaxDescLength = 500000;

        #region Categories

        public static void SetProductHierarchicallyEnabled(int productId)
        {
            SQLDataAccess.ExecuteNonQuery("[Catalog].[SetProductHierarchicallyEnabled]", CommandType.StoredProcedure,
                                            new SqlParameter("@ProductId", productId));
        }

        public static int GetFirstCategoryIdByProductId(int productId)
        {
            return
                SQLDataHelper.GetInt(
                    SQLDataAccess.ExecuteScalar("[Catalog].[sp_GetCategoryIDByProductID]", CommandType.StoredProcedure, new SqlParameter("@ProductID", productId)),
                    CategoryService.DefaultNonCategoryId);
        }

        /// <summary>
        /// get categoryIds by productId
        /// </summary>
        public static IEnumerable<int> GetCategoriesIDsByProductId(int productId, bool onlyActive)
        {
            if (onlyActive)
            {
                return SQLDataAccess.ExecuteReadColumnIEnumerable<int>("SELECT Category.CategoryID FROM Catalog.ProductCategories " +
                                                                        "inner join Catalog.Category on Category.CategoryId = ProductCategories.CategoryId " +
                                                                        "WHERE ProductID = @ProductID  and Enabled = 1 and HirecalEnabled = 1 and Hidden = 0 " +
                                                                        "order by main desc",
                                                                       CommandType.Text,
                                                                       "CategoryID",
                                                                       new SqlParameter("@ProductID", productId));
            }
            else
            {
                return SQLDataAccess.ExecuteReadColumnIEnumerable<int>("SELECT CategoryID FROM Catalog.ProductCategories " +
                                                                        "WHERE ProductID = @ProductID " +
                                                                        "order by main desc",
                                                                      CommandType.Text,
                                                                      "CategoryID",
                                                                      new SqlParameter("@ProductID", productId));
            }
        }


        /// <summary>
        /// get categories by productId
        /// </summary>
        public static List<Category> GetCategoriesByProductId(int productId)
        {
            return SQLDataAccess.ExecuteReadList("[Catalog].[sp_GetProductCategories]", CommandType.StoredProcedure,
                CategoryService.GetCategoryFromReader, new SqlParameter("@ProductID", productId));
        }

        #endregion

        #region Related Products

        public static string LinkedProductToString(int productId, RelatedType related, string columSeparator)
        {
            var temp = SQLDataAccess.ExecuteReadList(
                "Select ArtNo from Catalog.Product inner join Catalog.RelatedProducts on " +
                "RelatedProducts.LinkedProductID=Product.ProductId where RelatedType=@type and RelatedProducts.ProductID=@productId " +
                "ORDER BY RelatedProducts.SortOrder",
                CommandType.Text,
                reader => SQLDataHelper.GetString(reader, "ArtNo"),
                new SqlParameter("@productId", productId),
                new SqlParameter("@type", (int)related));
            return temp.AggregateString(columSeparator);
        }

        public static void LinkedProductFromString(int productId, string linkproducts, RelatedType type, string columSeparator)
        {
            _LinkedProductFromString(productId, linkproducts, type, string.IsNullOrWhiteSpace(columSeparator) ? "," : columSeparator);
        }

        private static void _LinkedProductFromString(int productId, string linkproducts, RelatedType type, string columSeparator)
        {
            ClearRelatedProducts(productId, type);

            if (string.IsNullOrEmpty(linkproducts)) 
                return;
            
            var arrArt = linkproducts.Split(new[] { columSeparator}, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var t in arrArt)
            {
                var artNo = t.Trim();
                
                if (string.IsNullOrWhiteSpace(artNo)) 
                    continue;
                
                int linkProductId = GetProductId(artNo);
                if (linkProductId != 0)
                    AddRelatedProduct(productId, linkProductId, type);
            }
        }

        public static bool IsExistRelatedProduct(int productId, int relatedProductId, RelatedType relatedType)
        {
            return SQLDataAccess.ExecuteScalar<int>(
                "Select 1 from [Catalog].[RelatedProducts] where [ProductID]=@ProductID and [LinkedProductID]=@RelatedProductID and [RelatedType]=@RelatedType",
                CommandType.Text,
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@RelatedProductID", relatedProductId),
                new SqlParameter("@RelatedType", (int)relatedType)) > 0;

        }

        public static void AddRelatedProduct(int productId, int relatedProductId, RelatedType relatedType)
        {
            if (!IsExistRelatedProduct(productId, relatedProductId, relatedType))
            {
                SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_AddRelatedProduct]", CommandType.StoredProcedure,
                    new SqlParameter("@ProductID", productId),
                    new SqlParameter("@RelatedProductID", relatedProductId),
                    new SqlParameter("@RelatedType", (int)relatedType));
                CacheManager.RemoveByPattern(CacheNames.GetProductRelatedProductCacheObjectName(productId));
            }
        }

        public static void DeleteRelatedProduct(int productId, int relatedProductId, RelatedType relatedType)
        {
            SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_DeleteRelatedProduct]", CommandType.StoredProcedure,
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@RelatedProductID", relatedProductId),
                new SqlParameter("@RelatedType", (int)relatedType));
            CacheManager.RemoveByPattern(CacheNames.GetProductRelatedProductCacheObjectName(productId));
        }

        public static void DeleteRelatedProductsByProductId(int productId)
        {
            SQLDataAccess.ExecuteNonQuery(
                "Delete from catalog.RelatedProducts Where ProductId=@ProductID Or LinkedProductID=@ProductID",
                CommandType.Text, new SqlParameter("@ProductID", productId));
            CacheManager.RemoveByPattern(CacheNames.GetProductRelatedProductCacheObjectName(productId));
        }

        public static void ClearRelatedProducts(int productId, RelatedType relatedType)
        {
            SQLDataAccess.ExecuteNonQuery("Delete from catalog.RelatedProducts Where ProductId=@ProductID and RelatedType=@RelatedType",
                                            CommandType.Text,
                                            new SqlParameter("@ProductID", productId),
                                            new SqlParameter("@RelatedType", (int)relatedType));
            CacheManager.RemoveByPattern(CacheNames.GetProductRelatedProductCacheObjectName(productId));
        }

        public static void ChangeRelatedProductsSorting(int id, int? prevId, int? nextId)
        {
            if (!prevId.HasValue && !nextId.HasValue)
                return;
            SQLDataAccess.ExecuteNonQuery("Catalog.ChangeRelatedProductsSorting", CommandType.StoredProcedure,
                new SqlParameter("@Id", id),
                new SqlParameter("@prevId", prevId ?? (object)DBNull.Value),
                new SqlParameter("@nextId", nextId ?? (object)DBNull.Value));
            CacheManager.RemoveByPattern(CacheNames.ProductRelatedProducts);
        }

        public static List<ProductModel> GetRelatedProducts(int productId, RelatedType relatedType, List<int> warehouseIds = null) => GetRelatedProducts(productId, relatedType, noCache: false, warehouseIds);

        public static List<ProductModel> GetRelatedProducts(int productId, RelatedType relatedType, bool noCache, List<int> warehouseIds = null)
        {
            return noCache
                ? _getRelatedProducts(productId, relatedType, warehouseIds)
                : CacheManager.Get(
                    CacheNames.GetProductRelatedProductsCacheName(productId, relatedType, warehouseIds), 
                    () => _getRelatedProducts(productId, relatedType, warehouseIds));
        }

        private static List<ProductModel> _getRelatedProducts(int productId, RelatedType relatedType, List<int> warehouseIds)
        {
            return SQLDataAccess.Query<ProductModel>(
                    ";with cte (ProductId) as (select TOP (@Count) [Product].ProductId " +
                    "from catalog.Product " +
                    "inner join [Catalog].[ProductExt] on [ProductExt].ProductId = Product.ProductId " +
                    "Inner Join [Catalog].[RelatedProducts] ON [Product].[ProductID] = [RelatedProducts].[LinkedProductID] " +
                    "Where Product.Enabled = 1 and Product.Hidden = 0 and CategoryEnabled = 1 " +
                            "and RelatedProducts.ProductID = @ProductID " +
                            "and RelatedProducts.RelatedType = @RelatedType " +
                            "and Product.ProductID <> @ProductID " +
                    (SettingsCatalog.ShowOnlyAvalible
                        ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) "
                        : "") +
                    (warehouseIds != null
                        ? " AND ([Product].[AllowPreOrder] = 1 "
                          + "OR Exists(Select 1 from [Catalog].[Offer] "
                          + "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] "
                          + "where [WarehouseStocks].[WarehouseId] IN @warehouseIds "
                          + "     and Offer.ProductId = [Product].[ProductID] "
                          + "     AND [WarehouseStocks].[Quantity] > 0)) "
                        : "") +
                    "Order by (CASE WHEN PriceTemp=0 OR AmountSort=0 THEN AllowPreOrder ELSE 2 END) DESC, AmountSort desc, RelatedProducts.SortOrder) " +

                    "Select Product.ProductID, Product.ArtNo, Product.Name, Product.BriefDescription, Product.Recomended, Product.Bestseller, Product.New, Product.OnSale as Sale, " +
                    "Product.Enabled, Product.UrlPath, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.Discount, Product.DiscountAmount, Product.MinAmount, " +
                    "Product.MaxAmount, Product.DateAdded, Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity, " +
                    "Offer.OfferID, Offer.ColorID, MaxAvailable AS Amount, Offer.Amount AS AmountOffer, MinPrice as BasePrice, Colors, CurrencyValue, " +
                    "CountPhoto, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, NotSamePrices as MultiPrices, Gifts, Offer.BarCode , Units.DisplayName as UnitDisplayName, Units.Name as UnitName " +
                    
                    "From cte inner join [Catalog].[Product] on cte.ProductId = [Product].ProductId " +
                    "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                    "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                    "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                    "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] " +
                    "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id] ",
                    new
                    {
                        Count = SettingsCatalog.RelatedProductsMaxCount,
                        ProductId = productId,
                        RelatedType = (int)relatedType,
                        Type = PhotoType.Product.ToString(),
                        warehouseIds
                    })
                .ToList();
        }

        public static List<RelatedProductAdminModel> GetAllRelatedProducts(int productId, RelatedType relatedType)
        {
            return SQLDataAccess.Query<RelatedProductAdminModel>(
                "Select RelatedProducts.RelatedProductId, Product.ProductID, Product.ArtNo, Product.Name, Product.UrlPath, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription " +
                "From [Catalog].[Product] " +
                "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                "Inner Join [Catalog].[RelatedProducts] ON [Product].[ProductID] = [RelatedProducts].[LinkedProductID] " +
                "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] " +
                "Where [RelatedProducts].[ProductID] = @ProductID and RelatedProducts.RelatedType = @RelatedType and Product.ProductID<> @ProductID " +
                "Order by RelatedProducts.SortOrder",
                new
                {
                    ProductId = productId,
                    RelatedType = (int)relatedType,
                    Type = PhotoType.Product.ToString(),
                })
                .ToList();
        }

        public static List<ProductModel> GetRelatedProductsFromCategory(Product product, RelatedType relatedType, List<int> warehouseIds = null)
        {
            var categoryIds = new List<int>();
            var productCategories = GetCategoriesIDsByProductId(product.ProductId, true).ToList().Take(10).ToList();
            
            foreach (var productCategoryId in productCategories)
                categoryIds.AddRange(CategoryService.GetRelatedCategoryIds(productCategoryId, relatedType));
            
            categoryIds = categoryIds.Distinct().Take(10).ToList();

            if (categoryIds.Count <= 0)
                return null;

            var propValues = new List<string>();
            var propValuesNotSame = new List<string>();

            // Достаем значения свойств как у данного товара
            var propertyIds = productCategories.SelectMany(catId => CategoryService.GetRelatedPropertyIds(catId, relatedType, true)).Distinct().ToList();
            if (propertyIds.Count > 0)
            {
                var productPropertyValues =
                    product.ProductPropertyValues.Where(p => propertyIds.Contains(p.PropertyId)).ToList();

                var list = new List<int>();

                foreach (var productPropertyValue in productPropertyValues)
                {
                    var ids =
                        productPropertyValues.Where(
                            x =>
                                x.PropertyId == productPropertyValue.PropertyId &&
                                !list.Contains(productPropertyValue.PropertyValueId))
                            .Select(x => x.PropertyValueId)
                            .ToList();

                    if (ids.Any())
                    {
                        propValues.Add(String.Join(",", ids));
                        list.AddRange(ids);
                    }
                }
            }

            // Достаем значения свойств не как у данного товара
            var propertyNotSameIds = productCategories.SelectMany(catId => CategoryService.GetRelatedPropertyIds(catId, relatedType, false)).Distinct().ToList();
            if (propertyNotSameIds.Count > 0)
            {
                var productPropertyValues =
                    product.ProductPropertyValues.Where(p => propertyNotSameIds.Contains(p.PropertyId)).ToList();

                var list = new List<int>();

                foreach (var productPropertyValue in productPropertyValues)
                {
                    var ids =
                        productPropertyValues.Where(
                            x =>
                                x.PropertyId == productPropertyValue.PropertyId &&
                                !list.Contains(productPropertyValue.PropertyValueId))
                            .Select(x => x.PropertyValueId)
                            .ToList();

                    if (ids.Any())
                    {
                        propValuesNotSame.Add(String.Join(",", ids));
                        list.AddRange(ids);
                    }
                }
            }

            // Достаем значения свойств, которые выбрали
            var propertyValuesIds =
                productCategories.SelectMany(catId => CategoryService.GetRelatedPropertyValues(catId, relatedType))
                    .GroupBy(x => x.PropertyId)
                    .Select(g => String.Join(",", g.Select(x => x.PropertyValueId)))
                    .ToList();

            if (propertyValuesIds.Count > 0)
            {
                propValues = propValues.Union(propertyValuesIds).ToList();
            }

            if (propValues.Count > 0 || propValuesNotSame.Count > 0)
                return GetRelatedProductsByCategoryAndProperties(categoryIds, propValues, propValuesNotSame, relatedType, SettingsCatalog.RelatedProductsMaxCount);

            return GetRelatedProductsByCategory(categoryIds, relatedType, SettingsCatalog.RelatedProductsMaxCount, warehouseIds);
        }

        public static (List<ProductModel> products, string html) GetRelatedProductsByRelatedTypeSource(Product product, RelatedType relatedType, List<int> warehouseIds = null)
        {
            var sourceType = relatedType == RelatedType.Related ? SettingsDesign.RelatedProductSourceType : SettingsDesign.SimilarProductSourceType;
            var products = new List<ProductModel>();
            string html = null;
            switch (sourceType)
            {
                case SettingsDesign.eRelatedProductSourceType.Default:
                    return (GetRelatedProducts(product.ProductId, relatedType, warehouseIds), null);
                
                case SettingsDesign.eRelatedProductSourceType.FromCategory:
                    products = GetRelatedProducts(product.ProductId, relatedType);
                    if (products == null || products.Count == 0)
                        products = GetRelatedProductsFromCategory(product, relatedType, warehouseIds);
                    break;
                
                case SettingsDesign.eRelatedProductSourceType.FromModule:
                    var module = AttachedModules.GetModules<IModuleRelatedProducts>().FirstOrDefault();
                    if (module != null)
                    {
                        var instance = (IModuleRelatedProducts)Activator.CreateInstance(module);

                        products = instance.GetRelatedProducts(product, relatedType);

                        if (products == null || products.Count == 0)
                            html = instance.GetRelatedProductsHtml(product, relatedType);
                    }

                    // get default related products
                    if ((products == null || products.Count == 0) && html.IsNullOrEmpty())
                        products = GetRelatedProducts(product.ProductId, relatedType, warehouseIds);
                    break;
                
                case SettingsDesign.eRelatedProductSourceType.FromCurrentCategory:
                    products = GetRelatedProducts(product.ProductId, relatedType);
                    if (products == null || products.Count == 0)
                        products = GetRelatedProductsByCategory(new List<int> { product.CategoryId }, relatedType, SettingsCatalog.RelatedProductsMaxCount, warehouseIds);
                    break;
            }
            return (products, html);
        }
        

        private static List<ProductModel> GetRelatedProductsByCategory(IReadOnlyCollection<int> categoryIds, RelatedType relatedType, int count, List<int> warehouseIds = null)
        {
            var products = CacheManager.Get(CacheNames.GetProductRelatedProductsFromCategoryCacheName(categoryIds, relatedType, count, warehouseIds), () =>
                SQLDataAccess.Query<ProductModel>(
                        $@"Select Product.ProductID,
                               Product.ArtNo,
                               Product.Name,
                               Product.BriefDescription,
                               Product.Recomended as Recomend,
                               Product.Bestseller,
                               Product.New,
                               Product.OnSale as Sales,
                               Product.Enabled,
                               Product.UrlPath,
                               Product.AllowPreOrder,
                               Product.Ratio,
                               Product.ManualRatio,
                               Product.Discount,
                               Product.DiscountAmount,
                               Product.MinAmount,
                               Product.MaxAmount,
                               Product.DateAdded,
                               Product.DoNotApplyOtherDiscounts,
                               Product.MainCategoryId,
                               Product.Multiplicity,
                               Offer.OfferID,
                               Offer.ColorID,
                               Offer.Amount      AS AmountOffer,
                               MaxAvailable      AS Amount,
                               MinPrice          as BasePrice,
                               Colors,
                               CurrencyValue,
                               CountPhoto,
                               Photo.PhotoId,
                               PhotoName,
                               PhotoNameSize1,
                               PhotoNameSize2,
                               Photo.Description as PhotoDescription,
                               NotSamePrices     as MultiPrices,
                               Gifts,
                               Offer.BarCode,
                               Units.DisplayName as UnitDisplayName,
                               Units.Name        as UnitName
                        From [Catalog].[Product]
                                 Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID]
                                 Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID]
                                 Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId]
                                 Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID]
                                 Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id]
                        Where Product.ProductId in (Select top (@Count) ProductID
                                                    From (Select distinct Product.ProductID, AmountSort
                                                          From Catalog.ProductCategories
                                                                   Left Join catalog.Product On Product.ProductId = ProductCategories.ProductId
                                                                   Left Join [Catalog].[ProductExt]
                                                                             On [Product].[ProductID] = [ProductExt].[ProductID]
                                                          Where ProductCategories.CategoryID in ({string.Join(",", categoryIds)})
                                                            and Product.Enabled = 1
                                                            and CategoryEnabled = 1
                                                          {(SettingsCatalog.ShowOnlyAvalible
                                                              ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) "
                                                              : "")} 
                                                          {(warehouseIds != null 
                                                              ? @" AND ([Product].[AllowPreOrder] = 1
                                                                      OR Exists(Select 1
                                                                                from [Catalog].[Offer]
                                                                                         Inner Join [Catalog].[WarehouseStocks]
                                                                                                    ON [Offer].[OfferID] = [WarehouseStocks].[OfferId]
                                                                                where [WarehouseStocks].[WarehouseId] IN @warehouseIds
                                                                                  and Offer.ProductId = [Product].[ProductID]
                                                                                  AND [WarehouseStocks].[Quantity] > 0)) "
                                                              : "")}
                                                    ) as p
                                                    Order by p.AmountSort DESC, newid())
                        Order by (CASE WHEN Price = 0 THEN 0 ELSE 1 END) DESC, AmountSort DESC, AllowPreOrder DESC, newid()",
                        new
                        {
                            Count = count * 2,
                            RelatedType = (int)relatedType,
                            Type = PhotoType.Product.ToString(),
                            warehouseIds
                        })
                    .ToList());

            return products
                .OrderByDescending(x => x.BasePrice > 0 && x.Amount > 0 ? 1 : 0)
                .ThenByDescending(_ => Guid.NewGuid())
                .Take(count)
                .ToList();
        }

        private static List<ProductModel> GetRelatedProductsByCategoryAndProperties(List<int> categoryIds, List<string> propValues, List<string> propValuesNotSame, RelatedType relatedType, int count)
        {
            var categories = String.Join(",", categoryIds);
            var subQuery =
                "Select top(@Count)ProductID From (" +
                    "Select distinct Product.ProductID, AmountSort From Catalog.ProductCategories " +
                    "Left Join catalog.Product On Product.ProductId = ProductCategories.ProductId " +
                    "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                    "Where ProductCategories.CategoryID in (" + categories + ") and Product.Enabled=1 and CategoryEnabled=1" +
                    (SettingsCatalog.ShowOnlyAvalible ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) " : string.Empty) +
                    ") as p order by p.AmountSort DESC, newid()";

            if (propValues.Count > 0)
            {
                var queryProductIds = propValues.Aggregate("",
                    (current, propValue) =>
                        current +
                        ((current != "" ? " Intersect " : "") +
                         "Select distinct Product.ProductID, AmountSort " +
                         "From Catalog.ProductCategories " +
                         "Left Join catalog.Product On Product.ProductId = ProductCategories.ProductId " +
                         "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                         "Left Join Catalog.ProductPropertyValue On ProductPropertyValue.ProductId=ProductCategories.ProductId " +
                         "Where ProductCategories.CategoryID In (" + categories + ") and PropertyValueId In (" + propValue + ") and Product.Enabled=1 and CategoryEnabled=1 " +
                         (SettingsCatalog.ShowOnlyAvalible ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) " : string.Empty)));

                subQuery = "Select top(@Count)ProductID From (" + queryProductIds + ") as p order by p.AmountSort DESC, newid()";
            }

            var subQueryNotSame = "";

            if (propValuesNotSame.Count > 0)
            {
                subQueryNotSame = 
                    "Select distinct Product.ProductID " +
                    "From Catalog.ProductCategories " +
                    "Left Join catalog.Product On Product.ProductId = ProductCategories.ProductId " +
                    "Left Join [Catalog].[ProductExt]  On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                    "Left Join Catalog.ProductPropertyValue On ProductPropertyValue.ProductId=ProductCategories.ProductId " +
                    "Where ProductCategories.CategoryID In (" + categories + ") and PropertyValueId In (" + String.Join(",", propValuesNotSame) + ") and Product.Enabled=1 and CategoryEnabled=1 " +
                    (SettingsCatalog.ShowOnlyAvalible ? " AND (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) " : string.Empty);
            }

            return SQLDataAccess.Query<ProductModel>(
                "Select Product.ProductID, Product.ArtNo, Product.Name, Product.BriefDescription, Product.Recomended, Product.Bestseller, Product.New, Product.OnSale as Sale, " +
                "Product.Enabled, Product.UrlPath, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.Discount, Product.DiscountAmount, Product.MinAmount, Product.MaxAmount, " + 
                "Product.DateAdded, Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity, " +
                "Offer.OfferID, Offer.ColorID, Offer.Amount AS AmountOffer, MaxAvailable AS Amount, MinPrice as BasePrice, Colors, CurrencyValue, " +
                "CountPhoto, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, NotSamePrices as MultiPrices, Gifts, Offer.BarCode , Units.DisplayName as UnitDisplayName, Units.Name as UnitName " +

                "From [Catalog].[Product] " +
                "Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] " +
                "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id] " +
                "Where Product.ProductId in (" + subQuery + ") " +

                (subQueryNotSame != "" ? " and Product.ProductId not in (" + subQueryNotSame + ") " : "") +

                "Order by AmountSort DESC, (CASE WHEN Price=0 THEN 0 ELSE 1 END) DESC, AllowPreOrder DESC, ProductId",
                new
                {
                    Count = count,
                    RelatedType = (int)relatedType,
                    Type = PhotoType.Product.ToString()
                })
                .ToList();
        }

        public static List<ProductModel> GetProductsByIds(List<int> productIds, bool moveNotAvaliableToEnd = false, bool showOnlyAvailable = false)
        {
            return SQLDataAccess.Query<ProductModel>(
                "Select Product.ProductID, Product.ArtNo, Product.Name, Product.BriefDescription, Product.Recomended as Recomend, Product.Bestseller, Product.New, Product.OnSale as Sales, " +
                "Product.Enabled, Product.UrlPath, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.Discount, Product.DiscountAmount, Product.MinAmount, Product.MaxAmount, Product.DateAdded, Offer.OfferID, " +
                "Offer.ColorID, Offer.Amount AS AmountOffer, MaxAvailable AS Amount, MinPrice as BasePrice, Colors, CurrencyValue, Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity, " +
                "CountPhoto, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, NotSamePrices as MultiPrices, Gifts, Offer.BarCode , Units.DisplayName as UnitDisplayName, Units.Name as UnitName " +
                (moveNotAvaliableToEnd ? ",(CASE WHEN Price=0 THEN 0 ELSE 1 END) as TempSort, AmountSort as TempAmountSort " : "") +

                "From [Catalog].[Product] " +
                "Inner Join (Select item, sort From [Settings].[ParsingBySeperator](@productIds, ',')) pt On pt.item = [Product].[ProductId] " +
                "Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] " +
                "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id] " +
                "Where Product.Enabled=1 and CategoryEnabled=1 " +

                (showOnlyAvailable ? " and MaxAvailable > 0 " : "") +

                "ORDER BY " + (moveNotAvaliableToEnd ? "TempSort desc, TempAmountSort desc, " : "") + "pt.sort",

                new {productIds = string.Join(",", productIds)})
                .ToList();
        }

        public static List<ProductModel> GetAllProductsByIds(List<int> productIds)
        {
            return SQLDataAccess.Query<ProductModel>(
                "Select Product.ProductID, Product.ArtNo, Product.Name, Product.BriefDescription, Product.Recomended as Recomend, Product.Bestseller, Product.New, Product.OnSale as Sales, " +
                "Product.Enabled, Product.UrlPath, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.Discount, Product.DiscountAmount, Product.MinAmount, Product.MaxAmount, Product.DateAdded, Offer.OfferID, " +
                "Offer.ColorID, Offer.Amount AS AmountOffer, MaxAvailable AS Amount, MinPrice as BasePrice, Colors, CurrencyValue, Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity, " +
                "CountPhoto, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, NotSamePrices as MultiPrices, Gifts, Offer.BarCode , Units.DisplayName as UnitDisplayName, Units.Name as UnitName " +

                "From [Catalog].[Product] " +
                "Inner Join (Select item, sort From [Settings].[ParsingBySeperator](@productIds, ',')) pt On pt.item = [Product].[ProductId] " +
                "Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] " +
                "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id] " +
                "ORDER BY pt.sort",
                new
                {
                    productIds = string.Join(",", productIds)
                })
                .ToList();
        }
        
        public static List<ProductModel> GetProductsByOfferIds(List<int> offerIds, bool moveNotAvailableToEnd = false, bool showOnlyAvailable = false)
        {
            return SQLDataAccess.Query<ProductModel>(
                "Select Product.ProductID, Product.ArtNo, Product.Name, Product.BriefDescription, Product.Recomended as Recomend, Product.Bestseller, Product.New, Product.OnSale as Sales, " +
                "Product.Enabled, Product.UrlPath, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.Discount, Product.DiscountAmount, Product.MinAmount, Product.MaxAmount, Product.DateAdded, Offer.OfferID, " +
                "Offer.ColorID, Offer.Amount AS AmountOffer, MaxAvailable AS Amount, MinPrice as BasePrice, Colors, CurrencyValue, Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity, " +
                "CountPhoto, Photo.PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Photo.Description as PhotoDescription, NotSamePrices as MultiPrices, Gifts, Offer.BarCode , Units.DisplayName as UnitDisplayName, Units.Name as UnitName " +
                (moveNotAvailableToEnd ? ",(CASE WHEN Price=0 THEN 0 ELSE 1 END) as TempSort, AmountSort as TempAmountSort " : "") +

                "From [Catalog].[Product] " +
                "Inner Join [Catalog].[Offer] On [Product].[ProductId] = [Offer].[ProductId] " +
                "Inner Join (Select item, sort From [Settings].[ParsingBySeperator](@offerIds, ',')) pt On pt.item = [Offer].[OfferID] " +
                "Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID]  " +
                "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] " +
                "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id] " +
                
                "Where Product.Enabled=1 and CategoryEnabled=1 " +

                (showOnlyAvailable ? " and MaxAvailable > 0 " : "") +

                "ORDER BY " + (moveNotAvailableToEnd ? "TempSort desc, TempAmountSort desc, " : "") + "pt.sort",

                new { offerIds = string.Join(",", offerIds) })
                .ToList();
        }

        #endregion


        #region Get Add Update Delete

        public static bool DeleteProduct(int productId, bool sentToLuceneIndex, bool trackChanges = true, ChangedBy changedBy = null)
        {
            PhotoService.DeletePhotos(productId, PhotoType.Product);
            
            DeleteRelatedProductsByProductId(productId);
            
            ProductGiftService.ClearGifts(productId); // todo: clear for offer ids

            if (Settings1C.Enabled && IsExists(productId))
            {
                SQLDataAccess.ExecuteNonQuery(
                    "Insert Into [Catalog].[DeletedProducts] (ProductId,ArtNo,DateTime) Values (@ProductId, (Select top 1 ArtNo From Catalog.Product Where ProductId=@ProductId), GetDate())",
                    CommandType.Text, new SqlParameter("@ProductID", productId));
            }

            foreach (var category in GetCategoriesByProductId(productId))
                DeleteProductLink(productId, category.CategoryId);

            SizeChartService.DeleteMap(productId, ESizeChartEntityType.Product);

            SQLDataAccess.ExecuteNonQuery(
                "[Catalog].[sp_DeleteProduct]", CommandType.StoredProcedure, 
                new SqlParameter("@ProductID", productId));

            CategoryService.ClearCategoryCache();

            if (sentToLuceneIndex)
                ProductWriter.Delete(productId);
            
            if (trackChanges)
                ProductHistoryService.DeleteProduct(productId, changedBy);

            return true;
        }

        public static int AddProduct(Product product, bool updateIndexes, bool trackChanges = true, ChangedBy changedBy = null)
        {
            if (SaasDataService.IsSaasEnabled && GetProductsCount("[Enabled] = 1") >= SaasDataService.CurrentSaasData.ProductsCount)
                return 0;

            product.ProductId = SQLDataHelper.GetInt(SQLDataAccess.ExecuteScalar("[Catalog].[sp_AddProduct]",
                CommandType.StoredProcedure,
                new SqlParameter("@ArtNo", product.ArtNo != null ? product.ArtNo.Trim() : ""),
                new SqlParameter("@Name", product.Name),
                new SqlParameter("@Ratio", product.Ratio),
                new SqlParameter("@Discount", product.Discount.Percent),
                new SqlParameter("@DiscountAmount", product.Discount.Amount),
                new SqlParameter("@BriefDescription", (product.BriefDescription.IsLongerThan(MaxDescLength) ? null : product.BriefDescription) ?? (object)DBNull.Value),
                new SqlParameter("@Description", (product.Description.IsLongerThan(MaxDescLength) ? null : product.Description) ?? (object)DBNull.Value),
                new SqlParameter("@Enabled", product.Enabled),
                new SqlParameter("@Recomended", product.Recomended),
                new SqlParameter("@New", product.New),
                new SqlParameter("@BestSeller", product.BestSeller),
                new SqlParameter("@OnSale", product.OnSale),
                new SqlParameter("@AllowPreOrder", product.AllowPreOrder),
                new SqlParameter("@BrandID", product.BrandId != 0 ? product.BrandId : (object)DBNull.Value),
                new SqlParameter("@UrlPath", product.UrlPath),
                new SqlParameter("@Unit", product.UnitId ?? (object)DBNull.Value),
                new SqlParameter("@ShippingPrice", product.ShippingPrice ?? (object)DBNull.Value),
                new SqlParameter("@MinAmount", product.MinAmount ?? (object)DBNull.Value),
                new SqlParameter("@MaxAmount", product.MaxAmount ?? (object)DBNull.Value),
                new SqlParameter("@Multiplicity", product.Multiplicity),
                new SqlParameter("@HasMultiOffer", product.HasMultiOffer),
                new SqlParameter("@CurrencyID", product.CurrencyID),
                new SqlParameter("@ActiveView360", product.ActiveView360),
                new SqlParameter("@ModifiedBy", product.ModifiedBy),
                new SqlParameter("@AccrueBonuses", product.AccrueBonuses),
                new SqlParameter("@TaxId", product.TaxId ?? (object)DBNull.Value),
                new SqlParameter("@PaymentSubjectType", product.PaymentSubjectType),
                new SqlParameter("@PaymentMethodType", product.PaymentMethodType),
                new SqlParameter("@DateModified", DateTime.Now),
                new SqlParameter("@CreatedBy", product.CreatedBy ?? (object)DBNull.Value),
                new SqlParameter("@Hidden", product.Hidden),
                new SqlParameter("@ManualRatio", product.ManualRatio ?? (object)DBNull.Value),
                new SqlParameter("@IsMarkingRequired", product.IsMarkingRequired),
                new SqlParameter("@Comment", product.Comment ?? (object)DBNull.Value),
                new SqlParameter("@DoNotApplyOtherDiscounts", product.DoNotApplyOtherDiscounts),
                new SqlParameter("@IsDigital", product.IsDigital),
                new SqlParameter("@DownloadLink", product.DownloadLink ?? (object)DBNull.Value)
                ));

            if (product.ProductId == 0)
                return 0;

            // by default if artNo is Null db set productId
            if (string.IsNullOrEmpty(product.ArtNo))
            {
                product.ArtNo = GetProductArtNoByProductID(product.ProductId);
            }

            if (product.Offers != null && product.Offers.Count != 0)
            {
                foreach (var offer in product.Offers)
                {
                    if (offer.ArtNo.IsNullOrEmpty())
                        offer.ArtNo = product.ArtNo.Trim();

                    offer.ProductId = product.ProductId;
                    OfferService.AddOffer(offer);
                }
            }
            
            if (product.Meta != null
                && !product.Meta.IsDefaultMeta
                && product.Meta.IsNotEmpty())
            {
                MetaInfoService.InsertMetaInfo(product.Meta, product.ProductId);
            }
            
            if (product.Tags != null)
            {
                var tags = product.Tags;
                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = TagService.Get(tags[i].Name);
                    tags[i].Id = tag == null ? TagService.Add(tags[i]) : tag.Id;
                    TagService.AddMap(product.ProductId, tags[i].Id, ETagType.Product, i * 10);
                }
            }
            
            if (product.ExportOptions != null && product.ExportOptions.IsChanged)
                ProductExportOptionsService.AddOrUpdate(product.ProductId, product.ExportOptions, false);

            if (product.SizeChart != null)
                SizeChartService.AddUpdateMap(product.SizeChart.Id, product.ProductId, ESizeChartEntityType.Product);

            if (Settings1C.Enabled)
            {
                SQLDataAccess.ExecuteNonQuery("Delete From Catalog.DeletedProducts Where ArtNo = @ArtNo",
                    CommandType.Text, new SqlParameter("@ArtNo", product.ArtNo));
            }

            if (updateIndexes)
            {
                SetProductHierarchicallyEnabled(product.ProductId);
                ProductWriter.AddUpdate(product);
                PreCalcProductParams(product.ProductId);
                CategoryService.CalculateHasProductsForWarehousesInProductCategories(product.ProductId);
            }

            if (trackChanges)
                ProductHistoryService.NewProduct(product, changedBy);

            return product.ProductId;
        }

        public static string GetProductArtNoByProductID(int productId)
        {
            return SQLDataAccess.ExecuteScalar<string>("SELECT [ArtNo] FROM [Catalog].[Product] WHERE [ProductID] = @ProductID",
                                                                   CommandType.Text, new SqlParameter("@ProductID", productId));
        }
        
        public static string GetProductUrlByProductId(int productId)
        {
            return SQLDataAccess.ExecuteScalar<string>("SELECT [UrlPath] FROM [Catalog].[Product] WHERE [ProductID] = @ProductID",
                CommandType.Text, new SqlParameter("@ProductID", productId));
        }

        public static void UpdateProductByArtNo(Product product, bool sentToLuceneIndex)
        {
            product.ProductId = SQLDataAccess.ExecuteScalar<int>("SELECT [ProductID] FROM [Catalog].[Product] WHERE [ArtNo] = @ArtNo",
                                                                   CommandType.Text, new SqlParameter("@ArtNo", product.ArtNo));
            if (product.ProductId > 0)
                UpdateProduct(product, sentToLuceneIndex);
        }

        public static void UpdateProduct(Product product, bool updateIndexes, bool trackChanges = false, ChangedBy changedBy = null)
        {
            if (product.Offers != null &&
                (product.Offers.Count > 1 || product.Offers.Any(offer => offer.ColorID.HasValue || offer.SizeID.HasValue || offer.ArtNo != product.ArtNo)))
                product.HasMultiOffer = true;

            if (trackChanges)
                ProductHistoryService.TrackProductChanges(product, changedBy);

            SQLDataAccess.ExecuteNonQuery(
                @"DECLARE @MinPrice [FLOAT] = 
                    (SELECT MIN(o.Price) MinPrice
                     FROM [Catalog].Offer o
                     WHERE o.ProductID = @ProductID);

                UPDATE [Catalog].[Product] 
                SET [ArtNo]                    = @ArtNo
                  , [Name]                     = @Name
                  , [Ratio]                    = @Ratio
                  , [Discount]                 = @Discount
                  , [DiscountAmount]           = (CASE WHEN @DiscountAmount > @MinPrice THEN @MinPrice ELSE @DiscountAmount END)
                  , [BriefDescription]         = @BriefDescription
                  , [Description]              = @Description
                  , [Enabled]                  = @Enabled
                  , [Recomended]               = @Recomended
                  , [New]                      = @New
                  , [BestSeller]               = @BestSeller
                  , [OnSale]                   = @OnSale
                  , [DateModified]             = @DateModified
                  , [BrandID]                  = @BrandID
                  , [AllowPreOrder]            = @AllowPreOrder
                  , [UrlPath]                  = @UrlPath
                  , [Unit]                     = @Unit
                  , [ShippingPrice]            = @ShippingPrice
                  , [MinAmount]                = @MinAmount
                  , [MaxAmount]                = @MaxAmount
                  , [Multiplicity]             = @Multiplicity
                  , [HasMultiOffer]            = @HasMultiOffer
                  , [CurrencyID]               = @CurrencyID
                  , [ActiveView360]            = @ActiveView360
                  , [ModifiedBy]               = @ModifiedBy
                  , [AccrueBonuses]            = @AccrueBonuses
                  , [TaxId]                    = @TaxId
                  , [PaymentSubjectType]       = @PaymentSubjectType
                  , [PaymentMethodType]        = @PaymentMethodType
                  , [CreatedBy]                = @CreatedBy
                  , [Hidden]                   = @Hidden
                  , [Manualratio]              = @ManualRatio
                  , [IsMarkingRequired]        = @IsMarkingRequired
                  , [Comment]                  = @Comment
                  , [DoNotApplyOtherDiscounts] = @DoNotApplyOtherDiscounts
                  , [IsDigital]                = @IsDigital
                  , [DownloadLink]             = @DownloadLink                
                WHERE ProductID = @ProductID",
                CommandType.Text,
                new SqlParameter("@ArtNo", product.ArtNo != null ? product.ArtNo.Trim() : string.Empty),
                new SqlParameter("@Name", product.Name),
                new SqlParameter("@ProductID", product.ProductId),
                new SqlParameter("@Ratio", product.Ratio),
                new SqlParameter("@Discount", product.Discount.Percent),
                new SqlParameter("@DiscountAmount", product.Discount.Amount),
                new SqlParameter("@BriefDescription", product.BriefDescription ?? (object)DBNull.Value),
                new SqlParameter("@Description", product.Description ?? (object)DBNull.Value),
                new SqlParameter("@Enabled", product.Enabled),
                new SqlParameter("@AllowPreOrder", product.AllowPreOrder),
                new SqlParameter("@Recomended", product.Recomended),
                new SqlParameter("@New", product.New),
                new SqlParameter("@BestSeller", product.BestSeller),
                new SqlParameter("@OnSale", product.OnSale),
                new SqlParameter("@BrandID", product.BrandId != 0 ? product.BrandId : (object)DBNull.Value),
                new SqlParameter("@UrlPath", product.UrlPath),
                new SqlParameter("@Unit", product.UnitId ?? (object)DBNull.Value),
                new SqlParameter("@ShippingPrice", product.ShippingPrice ?? (object)DBNull.Value),
                new SqlParameter("@MinAmount", product.MinAmount ?? (object)DBNull.Value),
                new SqlParameter("@MaxAmount", product.MaxAmount ?? (object)DBNull.Value),
                new SqlParameter("@Multiplicity", product.Multiplicity),
                new SqlParameter("@HasMultiOffer", product.HasMultiOffer),
                new SqlParameter("@CurrencyID", product.CurrencyID),
                new SqlParameter("@ActiveView360", product.ActiveView360),
                new SqlParameter("@ModifiedBy", product.ModifiedBy),
                new SqlParameter("@AccrueBonuses", product.AccrueBonuses),
                new SqlParameter("@TaxId", product.TaxId ?? (object)DBNull.Value),
                new SqlParameter("@PaymentSubjectType", product.PaymentSubjectType),
                new SqlParameter("@PaymentMethodType", product.PaymentMethodType),
                new SqlParameter("@DateModified", DateTime.Now),
                new SqlParameter("@CreatedBy", product.CreatedBy ?? (object)DBNull.Value),
                new SqlParameter("@Hidden", product.Hidden),
                new SqlParameter("@ManualRatio", product.ManualRatio ?? (object)DBNull.Value),
                new SqlParameter("@IsMarkingRequired", product.IsMarkingRequired),
                new SqlParameter("@Comment", product.Comment ?? (object)DBNull.Value),
                new SqlParameter("@DoNotApplyOtherDiscounts", product.DoNotApplyOtherDiscounts),
                new SqlParameter("@IsDigital", product.IsDigital),
                new SqlParameter("@DownloadLink", product.DownloadLink ?? (object)DBNull.Value)
            );

            OfferService.DeleteOldOffers(product.ProductId, product.Offers, trackChanges, changedBy);

            if (product.Offers != null)
            {
                foreach (var offer in product.Offers)
                {
                    if (offer.OfferId <= 0)
                    {
                        OfferService.AddOffer(offer, trackChanges, changedBy);
                    }
                    else
                    {
                        OfferService.UpdateOffer(offer, trackChanges, changedBy);
                    }
                }
            }

            if (product.Meta != null)
            {
                if (product.Meta.IsNotEmpty())
                    MetaInfoService.SetMeta(product.Meta);
                else
                {
                    if (MetaInfoService.IsMetaExist(product.ProductId, MetaType.Product))
                        MetaInfoService.DeleteMetaInfo(product.ProductId, MetaType.Product);
                }
            }

            if (product.Tags != null)
            {
                var tags = product.Tags;
                TagService.DeleteMap(product.ProductId, ETagType.Product);

                var ids = new List<int>();
                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = TagService.Get(tags[i].Name);
                    tags[i].Id = tag == null ? TagService.Add(tags[i]) : tag.Id;

                    if (ids.Contains(tags[i].Id))
                        continue;

                    TagService.AddMap(product.ProductId, tags[i].Id, ETagType.Product, i * 10);

                    ids.Add(tags[i].Id);
                }
            }
            
            if (product.ExportOptions != null && product.ExportOptions.IsChanged)
                ProductExportOptionsService.AddOrUpdate(product.ProductId, product.ExportOptions, trackChanges, changedBy);

            if (product.SizeChart != null)
                SizeChartService.AddUpdateMap(product.SizeChart.Id, product.ProductId, ESizeChartEntityType.Product);
            else
                SizeChartService.DeleteMap(product.ProductId, ESizeChartEntityType.Product);

            if (updateIndexes)
            {
                SetProductHierarchicallyEnabled(product.ProductId);
                PreCalcProductParams(product.ProductId);
                CategoryService.CalculateHasProductsForWarehousesInProductCategories(product.ProductId);

                CacheManager.RemoveByPattern(CacheNames.BrandsInCategory);
                CacheManager.RemoveByPattern(CacheNames.PropertiesInCategory);

                ProductWriter.AddUpdate(product);
            }
        }

        public static void UpdateProductDiscount(int productId, float discount)
        {
            SQLDataAccess.ExecuteNonQuery("UPDATE [Catalog].[Product] SET [Discount] = @Discount WHERE [ProductId] = @ProductId", CommandType.Text, new SqlParameter("@Discount", discount), new SqlParameter("@ProductId", productId));
        }

        public static void UpdateProductArtNo(int productId, string artNo)
        {
            SQLDataAccess.ExecuteNonQuery("UPDATE [Catalog].[Product] SET [ArtNo] = @ArtNo WHERE [ProductId] = @ProductId", 
                CommandType.Text, new SqlParameter("@ArtNo", artNo), 
                new SqlParameter("@ProductId", productId));
        }

        public static Product GetProductFromReader(SqlDataReader reader)
        {
            var p = new Product
            {
                ProductId = SQLDataHelper.GetInt(reader, "ProductId"),
                ArtNo = SQLDataHelper.GetString(reader, "ArtNo"),
                Name = SQLDataHelper.GetString(reader, "Name"),
                BriefDescription = SQLDataHelper.GetString(reader, "BriefDescription", string.Empty),
                Description = SQLDataHelper.GetString(reader, "Description", string.Empty),
                Photo = SQLDataHelper.GetString(reader, "PhotoName"),
                Discount = new Discount(SQLDataHelper.GetFloat(reader, "Discount"), SQLDataHelper.GetFloat(reader, "DiscountAmount")),
                Ratio = SQLDataHelper.GetDouble(reader, "Ratio"),
                Enabled = SQLDataHelper.GetBoolean(reader, "Enabled", true),
                AllowPreOrder = SQLDataHelper.GetBoolean(reader, "AllowPreOrder"),
                Recomended = SQLDataHelper.GetBoolean(reader, "Recomended"),
                New = SQLDataHelper.GetBoolean(reader, "New"),
                BestSeller = SQLDataHelper.GetBoolean(reader, "Bestseller"),
                OnSale = SQLDataHelper.GetBoolean(reader, "OnSale"),
                BrandId = SQLDataHelper.GetInt(reader, "BrandID", 0),
                UrlPath = SQLDataHelper.GetString(reader, "UrlPath"),
                CategoryEnabled = SQLDataHelper.GetBoolean(reader, "CategoryEnabled"),
                UnitId = SQLDataHelper.GetNullableInt(reader, "Unit"),
                ShippingPrice = SQLDataHelper.GetNullableFloat(reader, "ShippingPrice"),
                Multiplicity = SQLDataHelper.GetFloat(reader, "Multiplicity"),
                MinAmount = SQLDataHelper.GetNullableFloat(reader, "MinAmount"),
                MaxAmount = SQLDataHelper.GetNullableFloat(reader, "MaxAmount"),
                HasMultiOffer = SQLDataHelper.GetBoolean(reader, "HasMultiOffer"),
                CurrencyID = SQLDataHelper.GetInt(reader, "CurrencyID"),
                ActiveView360 = SQLDataHelper.GetBoolean(reader, "ActiveView360"),
                ModifiedBy = SQLDataHelper.GetString(reader, "ModifiedBy"),
                AccrueBonuses = SQLDataHelper.GetBoolean(reader, "AccrueBonuses"),
                TaxId = SQLDataHelper.GetNullableInt(reader, "TaxId"),
                PaymentSubjectType = (ePaymentSubjectType)SQLDataHelper.GetInt(reader, "PaymentSubjectType"),
                PaymentMethodType = (ePaymentMethodType)SQLDataHelper.GetInt(reader, "PaymentMethodType"),
                CreatedBy = SQLDataHelper.GetString(reader, "CreatedBy"),
                Hidden = SQLDataHelper.GetBoolean(reader, "Hidden"),
                ManualRatio = SQLDataHelper.GetNullableFloat(reader, "ManualRatio"),
                IsMarkingRequired = SQLDataHelper.GetBoolean(reader, "IsMarkingRequired"),
                Comment = SQLDataHelper.GetString(reader, "Comment"),
                DoNotApplyOtherDiscounts = SQLDataHelper.GetBoolean(reader, "DoNotApplyOtherDiscounts"),
                IsDigital = SQLDataHelper.GetBoolean(reader, "IsDigital"),
                DownloadLink = SQLDataHelper.GetString(reader, "DownloadLink")
            };

            if (p.TaxId == null)
                p.TaxId = SettingsCatalog.DefaultTaxId;

            if (p.Multiplicity <= 0)
                p.Multiplicity = 1;
            
            p.SetMainCategoryId(SQLDataHelper.GetNullableInt(reader, "MainCategoryId"));
            
            return p;
        }

        public static Product GetProductFromReaderWithExportOptions(SqlDataReader reader)
        {
            var p = GetProductFromReader(reader);
            p.ExportOptions = ProductExportOptionsService.GetProductExportOptionsFromReader(reader);

            return p;
        }

        public static Product GetProduct(int productId)
        {
            return SQLDataAccess.ExecuteReadOne("[Catalog].[sp_GetProductById]", CommandType.StoredProcedure,
                GetProductFromReader,
                new SqlParameter("@ProductID", productId), 
                new SqlParameter("@Type", PhotoType.Product.ToString()));
        }

        public static Product GetProductWithExportOptions(int productId)
        {
            return SQLDataAccess.ExecuteReadOne(
                "SELECT TOP 1 * " +
                "FROM [Catalog].[Product] " +
                "LEFT JOIN [Catalog].[ProductExportOptions] ON [ProductExportOptions].[ProductId] = [Product].[ProductID] " +
                "LEFT JOIN [Catalog].[Photo] ON [Photo].[ObjId] = [Product].[ProductID] and [Type]=@Type AND [Main] = 1 " +
                "WHERE	[Product].[ProductID] = @ProductID ", 
                CommandType.Text,
                GetProductFromReaderWithExportOptions,
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@Type", PhotoType.Product.ToString()));
        }

        public static Product GetProduct(string artNo, bool includeOfferArtNo = false, bool withExportParams = false)
        {
            var productId = GetProductId(artNo);

            if (productId == 0 && includeOfferArtNo)
            {
                productId = GetProductIDByOfferArtNo(artNo);
            }

            return productId > 0 
                ? (!withExportParams ? GetProduct(productId) : GetProductWithExportOptions(productId))
                : null;
        }

        public static Product GetProductByUrl(string url)
        {
            return SQLDataAccess.ExecuteReadOne(
                "Select TOP 1 * From [Catalog].[Product] " +
                "LEFT JOIN [Catalog].[Photo] ON [Photo].[ObjId] = [Product].[ProductID] and [Type]=@Type AND [Main] = 1 " +
                "Where UrlPath=@UrlPath",
                CommandType.Text, GetProductFromReader,
                new SqlParameter("@UrlPath", url), new SqlParameter("@Type", PhotoType.Product.ToString()));
        }

        public static int GetProductIdByOfferId(int offerId)
        {
            return SQLDataAccess.ExecuteReadOne(
                "SELECT TOP 1 ProductID FROM [Catalog].[Offer] WHERE OfferID=@offerId",
                CommandType.Text, (reader) => SQLDataHelper.GetInt(reader, "ProductID"),
                new SqlParameter("@offerId", offerId));
        }

        public static Product GetProductByOfferId(int offerId)
        {
            var productId = GetProductIdByOfferId(offerId);
            if (productId == 0)
                return null;
            return GetProduct(productId);
        }

        public static bool IsUniqueArtNo(string artNo)
        {
            return !SQLDataAccess.ExecuteScalar<bool>("Select Top(1) ProductID FROM [Catalog].[Product] WHERE ArtNo=@artNo", CommandType.Text, new SqlParameter("@artNo", artNo));
        }


        public static int GetProductId(string artNo)
        {
            return SQLDataAccess.ExecuteScalar<int>("Select Top(1) ProductID FROM [Catalog].[Product] WHERE ArtNo=@artNo", CommandType.Text, new SqlParameter("@artNo", artNo));
        }


        public static int GetProductIdByName(string name)
        {
            return SQLDataAccess.ExecuteScalar<int>("Select Top(1) ProductID FROM [Catalog].[Product] WHERE Name=@Name", CommandType.Text, new SqlParameter("@Name", name));
        }

        public static Product GetFirstProduct()
        {
            var productId = SQLDataAccess.ExecuteScalar<int>("Select TOP(1) product.productId from Catalog.ProductCategories inner join catalog.product on ProductCategories.productid=product.Productid where Enabled=1 and categoryEnabled=1", CommandType.Text);
            return GetProduct(productId);
        }

        public static Product GetProductByName(string name)
        {
            var productId = GetProductIdByName(name);
            return productId > 0 ? GetProduct(productId) : null;
        }

        public static int GetProductIDByOfferArtNo(string artNo)
        {
            return SQLDataAccess.ExecuteScalar<int>("Select Top(1) ProductID FROM [Catalog].[Offer] WHERE ArtNo=@artNo", CommandType.Text, new SqlParameter("@artNo", artNo));
        }

        public static List<int> GetProductsIDs()
        {
            return SQLDataAccess.ExecuteReadColumnIEnumerable<int>("SELECT [ProductID] FROM [Catalog].[Product]", CommandType.Text, "ProductID").ToList();
        }
        #endregion

        #region ProductLinks


        public static int DeleteAllProductLink(int productId)
        {
            var categoryIds = 
                SQLDataAccess.ExecuteReadIEnumerable(
                "Select CategoryID FROM [Catalog].[ProductCategories] WHERE ProductID = @ProductId",
                CommandType.Text,
                reader => SQLDataHelper.GetInt(reader, "CategoryID"),
                new SqlParameter("@ProductID", productId));
            
            foreach (var categoryId in categoryIds)
                CacheManager.Remove(CacheNames.GetCategoryCacheObjectName(categoryId));

            SQLDataAccess.ExecuteNonQuery(
                "DELETE FROM [Catalog].[ProductCategories] WHERE [ProductID] = @ProductId",
                CommandType.Text, 
                new SqlParameter("@ProductID", productId));

            return 0;
        }

        public static int DeleteProductLink(int productId, int catId, bool decrementProductsCount = true, bool trackChanges = false, ChangedBy changedBy = null)
        {
            if (trackChanges)
                ProductHistoryService.DeleteCategory(productId, catId, changedBy);

            SQLDataAccess.ExecuteNonQuery(
                "[Catalog].[sp_RemoveProductFromCategory]", CommandType.StoredProcedure,
                new SqlParameter("@ProductID", productId), 
                new SqlParameter("@CategoryID", catId));

            if (decrementProductsCount)
                SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_DeIncCountProductInCategory]", CommandType.StoredProcedure,
                    new SqlParameter("@CategoryID", catId),
                    new SqlParameter("@client", false));
            
            CacheManager.Remove(CacheNames.GetCategoryCacheObjectName(catId));

            return 0;
        }

        public static void AddProductLink(int productId, int catId, int sortOrder, bool updatecache, bool mainCat = false, bool incrementProductsCount = true, bool trackChanges = false, ChangedBy changedBy = null)
        {
            AddProductLink(productId, catId, sortOrder, updatecache, mainCat, incrementProductsCount, true, trackChanges, changedBy);
        }

        public static void AddProductLink(int productId, int catId, int sortOrder, bool updatecache, bool mainCat, bool incrementProductsCount, bool useAutomap, bool trackChanges = false, ChangedBy changedBy = null)
        {
            if (!useAutomap)
            {
                SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_AddProductToCategory]", CommandType.StoredProcedure,
                    new SqlParameter("@ProductID", productId),
                    new SqlParameter("@CategoryID", catId),
                    new SqlParameter("@sortOrder", sortOrder),
                    new SqlParameter("@mainCategory", mainCat));

                if (incrementProductsCount)
                    SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_IncCountProductInCategory]", CommandType.StoredProcedure,
                        new SqlParameter("@CategoryID", catId),
                        new SqlParameter("@client", false));

                if (updatecache)
                    CategoryService.ClearCategoryCache();

                if (trackChanges)
                    ProductHistoryService.NewCategory(productId, catId, changedBy);

                return;
            }

            var automapAction = CategoryService.GetCategoryAutomapAction(catId);
            var automapCategories = CategoryService.GetAutomapCategories(catId);
            if (automapAction != ECategoryAutomapAction.Move || !automapCategories.Any())
                AddProductLink(productId, catId, sortOrder, updatecache, automapAction == ECategoryAutomapAction.CopyAndSetMain ? true : mainCat, incrementProductsCount, false, trackChanges, changedBy);

            if (automapAction != ECategoryAutomapAction.None && !automapAction.HasFlag(ECategoryAutomapAction.DoNothing))
            {
                if (automapAction == ECategoryAutomapAction.Move && automapCategories.Any())
                    DeleteProductLink(productId, catId, incrementProductsCount);

                foreach (var automapCategory in automapCategories)
                    AddProductLink(productId, automapCategory.NewCategoryId, sortOrder, updatecache, automapCategory.Main, incrementProductsCount, false, trackChanges, changedBy);
            }
        }

        public static void AddProductLinkByExternalCategoryId(int productId, string externalCategoryId, int sortOrder, bool updatecache, bool useAutomap = true)
        {
            if (externalCategoryId.IsNullOrEmpty() || externalCategoryId == "0")
                return;
            if (!useAutomap)
            {
                SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_AddProductToCategoryByExternalId]", CommandType.StoredProcedure,
                                            new SqlParameter("@ProductID", productId),
                                            new SqlParameter("@ExternalId", externalCategoryId),
                                            new SqlParameter("@SortOrder", sortOrder));
                if (updatecache)
                    CategoryService.ClearCategoryCache();
                return;
            }

            var category = CategoryService.GetCategoryFromDbByExternalId(externalCategoryId);
            if (category == null)
                return;

            var automapCategories = CategoryService.GetAutomapCategories(category.CategoryId);
            if (category.AutomapAction != ECategoryAutomapAction.Move || !automapCategories.Any())
                AddProductLink(productId, category.CategoryId, sortOrder, updatecache, category.AutomapAction == ECategoryAutomapAction.CopyAndSetMain, incrementProductsCount: false, useAutomap: false);

            if (category.AutomapAction != ECategoryAutomapAction.None && !category.AutomapAction.HasFlag(ECategoryAutomapAction.DoNothing))
            {
                if (category.AutomapAction == ECategoryAutomapAction.Move && automapCategories.Any())
                    DeleteProductLink(productId, category.CategoryId, false);

                foreach (var automapCategory in automapCategories)
                    AddProductLink(productId, automapCategory.NewCategoryId, sortOrder, updatecache, automapCategory.Main, incrementProductsCount: false, useAutomap: false);
            }
        }

        public static void UpdateProductLinkSort(int productId, int sort, int categoryId)
        {
            SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_UpdateProductLinkSort]", CommandType.StoredProcedure,
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@CategoryID", categoryId),
                new SqlParameter("@SortOrder", sort));
        }


        public static Dictionary<int, int> GetCategoriesSortingByProductId(int productid)
        {
            return SQLDataAccess.ExecuteReadDictionary<int, int>("Select CategoryId, SortOrder from catalog.ProductCategories where ProductId=@productId", CommandType.Text,
                                                            "CategoryId", "SortOrder",
                                                            new SqlParameter { ParameterName = "@ProductID", Value = productid }
                                            );
        }

        public static List<KeyValuePair<string, int>> GetExternalCategoriesSortingByProductId(int productid)
        {
            return SQLDataAccess.Query<KeyValuePair<string, int>>(
                "SELECT c.ExternalId as [Key], pc.SortOrder as [Value] FROM Catalog.ProductCategories pc INNER JOIN Catalog.Category c ON c.CategoryId = pc.CategoryId " +
                "WHERE ProductId = @ProductId AND c.ExternalId IS NOT NULL AND c.ExternalId <> ''",
                new { productid }).ToList();
        }

        public static void UpdateProductMainCategoryId(int categoryId)
        {
            SQLDataAccess.ExecuteNonQuery(
                "Update Catalog.Product " +
                "Set MainCategoryId = (Select top(1) pg.CategoryID FROM [Catalog].[ProductCategories] pg WHERE pg.ProductId = Product.ProductId Order By pg.Main desc) " +
                "From Catalog.Product " +
                "Where MainCategoryId = @CategoryID",
                CommandType.Text,
                new SqlParameter("@CategoryId", categoryId)
            );
        }

        #endregion

        #region Is Enabled
        
        public static bool IsProductEnabled(int productId)
        {
            var res = SQLDataAccess.ExecuteScalar<bool>("SELECT ([Enabled] & [CategoryEnabled]) as Enabled FROM [Catalog].[Product] WHERE [ProductID] = @id", CommandType.Text, new SqlParameter("@id", productId));

            return res;
        }

        [Obsolete]
        public static void DisableAllProducts()
        {
            SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_DisableAllProducts]", CommandType.StoredProcedure);
            CategoryService.ClearCategoryCache();
        }

        public static void ClearAmountAllProducts(IEnumerable<int> warehousesIds)
        {
            warehousesIds = warehousesIds ?? throw new ArgumentNullException(nameof(warehousesIds));
            var enumerable = warehousesIds as int[] ?? warehousesIds.ToArray();
            if (!enumerable.Any()) throw new ArgumentException($"Parameter \"{nameof(warehousesIds)}\" is empty", nameof(warehousesIds));

            SQLDataAccess.ExecuteNonQuery(
                $@"Update [Catalog].[WarehouseStocks]
                    SET [Quantity] = 0 
                    WHERE [WarehouseId] IN ({string.Join(",", enumerable)})",
                CommandType.Text,
                60*5);
            
            OfferService.RecalculateAmountAllOffer();
        }

        public static void ClearAmountAllProducts(DateTime startAt, string createdBy, params int[] warehousesIds)
            => ClearAmountAllProducts(startAt, createdBy, warehousesIds.AsEnumerable());
        
        public static void ClearAmountAllProducts(DateTime startAt, string createdBy, IEnumerable<int> warehousesIds)
        {
            warehousesIds = warehousesIds ?? throw new ArgumentNullException(nameof(warehousesIds));
            var enumerable = warehousesIds as int[] ?? warehousesIds.ToArray();
            if (!enumerable.Any()) throw new ArgumentException($"Parameter \"{nameof(warehousesIds)}\" is empty", nameof(warehousesIds));

            SQLDataAccess.ExecuteNonQuery(
                $@"Update whst 
                    SET whst.[Quantity] = 0 
                    FROM [Catalog].[WarehouseStocks] AS whst
                        INNER JOIN [Catalog].[Offer] AS ofr ON whst.OfferId = ofr.OfferID
                        INNER JOIN [Catalog].[Product] AS prd ON ofr.ProductId = prd.ProductId 
                    WHERE prd.DateModified < @startAt 
                        AND prd.CreatedBy = @createdBy
                        AND whst.[WarehouseId] IN ({string.Join(",", enumerable)})",
                CommandType.Text,
                60*5,
                new SqlParameter("startAt", startAt),
                new SqlParameter("createdBy", createdBy));
            
            OfferService.RecalculateAmountAllOffer(startAt, createdBy);
        }
        
        public static void DisableAllProducts(DateTime startAt)
        {
            SQLDataAccess.ExecuteNonQuery("UPDATE [Catalog].[Product] SET [Enabled] = 0 WHERE DateModified< @startAt ",
                                           CommandType.Text,
                                           new SqlParameter("startAt", startAt));
            CategoryService.ClearCategoryCache();
        }
        
        public static void DeleteAllProducts(DateTime startAt)
        {
            SQLDataAccess.ExecuteNonQuery("DELETE FROM [Catalog].[Product] WHERE DateModified< @startAt ",
                CommandType.Text,
                new SqlParameter("startAt", startAt));
            CategoryService.ClearCategoryCache();
        }
        
        public static void ResetToZeroAllProducts(DateTime startAt)
        {
            SQLDataAccess.ExecuteNonQuery(@"UPDATE [Catalog].[Offer] SET [Amount] = 0 
                                                            WHERE ProductID IN 
                                                            (SELECT ProductID FROM [Catalog].[Product]
                                                            WHERE DateModified< @startAt)",
                CommandType.Text,
                new SqlParameter("startAt", startAt));
            CategoryService.ClearCategoryCache();
        }

        public static void DisableAllProducts(DateTime startAt, string createdBy)
        {
            SQLDataAccess.ExecuteNonQuery("UPDATE [Catalog].[Product] SET [Enabled] = 0 WHERE DateModified < @startAt AND CreatedBy = @createdBy ",
                                           CommandType.Text,
                                           new SqlParameter("startAt", startAt),
                                           new SqlParameter("createdBy", createdBy));
            CategoryService.ClearCategoryCache();
        }
        
        public static void DeleteAllProducts(DateTime startAt, string createdBy)
        {
            SQLDataAccess.ExecuteNonQuery("DELETE FROM [Catalog].[Product] WHERE DateModified < @startAt AND CreatedBy = @createdBy ",
                CommandType.Text,
                new SqlParameter("startAt", startAt),
                new SqlParameter("createdBy", createdBy));
            CategoryService.ClearCategoryCache();
        }

        public static void DeleteProducts(DateTime startAt, string createdBy)
        {
            var productIds = SQLDataAccess.ExecuteReadColumn<int>("SELECT [ProductId] FROM [Catalog].[Product] WHERE DateModified< @startAt AND CreatedBy = @CreatedBy",
                                           CommandType.Text,
                                           "ProductId",
                                           new SqlParameter("startAt", startAt),
                                           new SqlParameter("CreatedBy", createdBy));

            foreach (var productId in productIds)
                DeleteProduct(productId, true);

            CategoryService.ClearCategoryCache();
        }

        #endregion

        #region Filtered Select
        ///// <summary>
        ///// get all products
        ///// </summary>
        ///// <returns></returns>
        //public static IEnumerable<Product> GetAllProducts()
        //{
        //    const string query = @"select * FROM [Catalog].[Product] LEFT JOIN [Catalog].[Photo] ON [Photo].[ObjId] = [Product].[ProductID] AND Type=@Type AND Photo.[Main] = 1";
        //    return SQLDataAccess.ExecuteReadIEnumerable<Product>(query, CommandType.Text, GetProductFromReader, new SqlParameter("@Type", PhotoType.Product.ToString()));
        //}

        public static List<int> GetAllProductIDs(bool onlyDemo = false)
        {
            return SQLDataAccess.ExecuteReadColumn<int>(
                "SELECT ProductId FROM [Catalog].[Product]" + (onlyDemo ? " WHERE IsDemo = 1" : string.Empty),
                CommandType.Text, "ProductId");
        }

        /// <summary>
        /// Get products without category
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> GetProductIDsWithoutCategory()
        {
            return SQLDataAccess.ExecuteReadColumnIEnumerable<int>(
                    "SELECT [Product].[ProductID] FROM [Catalog].[Product] WHERE [Product].[ProductID] not in (select distinct [ProductID] from [Catalog].[ProductCategories])",
                    CommandType.Text,
                    "ProductID");
        }

        /// <summary>
        /// get products in categories
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> GetProductIDsInCategories()
        {
            return SQLDataAccess.ExecuteReadColumnIEnumerable<int>(
                    "select distinct [ProductID] from [Catalog].[ProductCategories]",
                    CommandType.Text,
                    "ProductID");
        }



        #endregion

        #region Products Count and Existance
        /// <summary>
        /// get products count
        /// </summary>
        /// <returns></returns>
        public static int GetProductsCount(string condition = null, params SqlParameter[] parameters)
        {
            if (condition == null)
            {
                return SQLDataAccess.ExecuteScalar<int>("SELECT Count([ProductID]) FROM [Catalog].[Product]",
                                                           CommandType.Text);
            }
            return SQLDataAccess.ExecuteScalar<int>("SELECT Count([ProductID]) FROM [Catalog].[Product] WHERE " + condition,
                                                    CommandType.Text, parameters);
        }

        /// <summary>
        /// cheak exist product by productid
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static bool IsExists(int productId)
        {
            bool boolres = SQLDataAccess.ExecuteScalar<int>("[Catalog].[sp_GetProductCOUNTbyID]", CommandType.StoredProcedure, new SqlParameter("@ProductID", productId)) > 0;
            return boolres;
        }

        #endregion

        #region Photos
        public static void AddProductPhotoByArtNo(string artNo, string fullfileName, string description, bool isMain, int? colorId, bool skipOriginal = false)
        {
            var productId = GetProductId(artNo);
            if (productId == 0)
                return;
            
            AddProductPhotoByProductId(productId, fullfileName, description, isMain, colorId, skipOriginal);
        }

        public static void AddProductPhotoByProductId(int productId, string fullfilename, string description, bool isMain, int? colorId, bool skipOriginal = false)
        {
            if (string.IsNullOrWhiteSpace(fullfilename))  //  || (!IsExists(productId)) 
                return;

            var photo = new Photo(0, productId, PhotoType.Product)
            {
                Description = description,
                OriginName = Path.GetFileName(fullfilename),
                PhotoSortOrder = 0,
                ColorID = colorId
            };
            var tempName = PhotoService.AddPhoto(photo);

            if (string.IsNullOrWhiteSpace(tempName)) 
                return;

            try
            {
                using (var image = Image.FromFile(fullfilename))
                {
                    FileHelpers.SaveProductImageUseCompress(tempName, image, skipOriginal);
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                PhotoService.DeleteProductPhoto(photo.PhotoId, backuping: false);
                // throw ??
            }
        }

        public static void AddProductPhotoLinkByProductId(int productId, string uri, string description, bool isMain, int? colorId, bool skipOriginal = false)
        {
            if (string.IsNullOrWhiteSpace(uri) || (!IsExists(productId))) return;

            var id = PhotoService.AddPhotoWithOrignName(new Photo(0, productId, PhotoType.Product)
            {
                PhotoName = uri,
                Description = description,
                OriginName = uri,
                PhotoSortOrder = 0,
                ColorID = colorId,
                Main = isMain
            });

            if (id != 0) return;
        }

        public static void UpdateProductPhotoByProductId(int productId, string fullfilename, string description, bool isMain, int? colorId, bool skipOriginal = false)
        {
            if (string.IsNullOrWhiteSpace(fullfilename)) //  || (!IsExists(productId))
                return;

            //Если битая фотка - то не будет добавляться запись в базу, упадет на открытии файла
            try
            {
                var productPhoto = PhotoService.GetProductPhoto(productId, Path.GetFileName(fullfilename));
                if (productPhoto == null || !productPhoto.PhotoName.IsNotEmpty()) 
                    return;
                
                productPhoto.ColorID = colorId;
                
                using (var image = Image.FromFile(fullfilename))
                {
                    PhotoService.UpdatePhoto(productPhoto);
                    FileHelpers.SaveProductImageUseCompress(productPhoto.PhotoName, image, skipOriginal);
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }


        #endregion

        #region Product Price Change

        public static void IncrementProductsPrice(float value, bool bySupply, List<int> categoryIds, bool percent = true, bool allProducts = false)
        {
            ChangeProductsPriceByCategories(value, false, bySupply, categoryIds, percent, allProducts);
        }

        public static void DecrementProductsPrice(float value, bool bySupply, List<int> categoryIds, bool percent = true, bool allProducts = false)
        {
            ChangeProductsPriceByCategories(value, true, bySupply, categoryIds, percent, allProducts);
        }

        private static void ChangeProductsPriceByCategories(float value, bool negative, bool bySupply, List<int> categoryIds, bool percent = true, bool allProducts = false)
        {
            if (!allProducts && (categoryIds == null || !categoryIds.Any()))
                return;

            var price = bySupply ? "SupplyPrice" : "Price";

            var cmd = string.Format(
                "DECLARE @ProductId TABLE(id int); " +
                "Insert into @ProductId " +
                (allProducts
                    ? "Select ProductId From Catalog.Product"
                    : "Select ProductId FROM Catalog.ProductCategories WHERE CategoryID IN ({0}) AND Catalog.ProductCategories.Main = 1") +
                " " +
                "Update [Catalog].[Offer] set [Price] = [{1}] {2} {4} Where {5} {3} [ProductID] IN (SELECT id FROM @ProductId); " +
                "Update [Catalog].[Offer] set [Price] = 0 Where [Price] < 0; ",
                categoryIds != null ? categoryIds.AggregateString(",") : "",
                price,
                negative ? "-" : "+",
                bySupply ? "[SupplyPrice] > 0 and" : "",
                percent ? "([" + price + "] * @Value / 100)" : "@Value",
                bySupply ? " " : "[Price] > 0 and");

            SQLDataAccess.ExecuteNonQuery(cmd, CommandType.Text, new SqlParameter("@Value", value));
        }

        #endregion


        #region Product Discount Change

        public static void ChangeProductsDiscountByCategories(float value, List<int> categoryIds, bool percent = true, bool allProducts = false)
        {
            if (!allProducts && (categoryIds == null || !categoryIds.Any()))
                return;

            var cmd =
                "DECLARE @ProductId TABLE(id int); " +
                "Insert into @ProductId " +
                (allProducts
                    ? "Select ProductId From Catalog.Product"
                    : $"Select ProductId FROM Catalog.ProductCategories WHERE CategoryID IN ({(categoryIds != null ? categoryIds.AggregateString(",") : "")}) AND Catalog.ProductCategories.Main = 1") +
                " " +
                (percent
                    ? "Update [Catalog].[Product] set [Discount] = @Value, [DiscountAmount] = 0 Where [ProductID] IN (SELECT id FROM @ProductId); "
                    : "UPDATE [Catalog].[Product] SET [Discount] = 0, [DiscountAmount] = (" +
                      "CASE WHEN @Value > tempTable.MinPrice THEN tempTable.MinPrice ELSE @Value END) " +
                      "FROM [Catalog].[Product] " +
                      "LEFT JOIN ( " +
                      "SELECT o.ProductID, MIN(o.price) minPrice " +
                      "FROM [Catalog].Offer o GROUP BY o.ProductID " +
                      ")AS tempTable ON tempTable.ProductId = [Product].ProductId " +
                      "WHERE [Product].[ProductID] IN (SELECT id FROM @ProductId) ") +
                "Update [Catalog].[Product] set [Discount] = 0 Where [Discount] < 0; ";

            SQLDataAccess.ExecuteNonQuery(cmd, CommandType.Text, new SqlParameter("@Value", value));
        }

        public static void ChangeProductsDiscountByBrands(float value, List<int> brandIds, bool percent = true)
        {
            if (brandIds == null || brandIds.Count == 0)
                return;

            var cmd =
                "DECLARE @ProductId TABLE(id int); " +
                $"INSERT INTO @ProductId SELECT ProductId FROM [Catalog].[Product] WHERE [BrandID] In ({string.Join(",", brandIds)}); " +
                (percent 
                    ? "Update [Catalog].[Product] set [Discount] = @Value, [DiscountAmount] = 0 Where [ProductID] IN (SELECT id FROM @ProductId); "
                    : "UPDATE [Catalog].[Product] SET [Discount] = 0, [DiscountAmount] = (" +
                      "CASE WHEN @Value > tempTable.MinPrice THEN tempTable.MinPrice ELSE @Value END) " +
                      "FROM [Catalog].[Product] " +
                      "LEFT JOIN ( " +
                      "SELECT o.ProductID, MIN(o.price) minPrice " +
                      "FROM [Catalog].Offer o GROUP BY o.ProductID " +
                      ")AS tempTable ON tempTable.ProductId = [Product].ProductId " +
                      "WHERE [Product].[ProductID] IN (SELECT id FROM @ProductId) ") +
                "Update [Catalog].[Product] set [Discount] = 0 Where [Discount] < 0; ";

            SQLDataAccess.ExecuteNonQuery(cmd, CommandType.Text,
            new SqlParameter("@Value", value));
        }

        #endregion

        #region Ratio
        public static void UpdateProductManualRatioByProductId(int productId, double? manualRatio)
        {
            SQLDataAccess.ExecuteNonQuery(
                 "Update [Catalog].[Product] Set ManualRatio = @ManualRatio Where ProductId = @ProductId",
                 CommandType.Text,
                 new SqlParameter("@ManualRatio", manualRatio ?? (object)DBNull.Value),
                 new SqlParameter("@ProductId", productId));

            CacheManager.RemoveByPattern(CacheNames.SQLPagingCount);
            CacheManager.RemoveByPattern(CacheNames.SQLPagingItems);
        }
        public static void SetAllProductsManualRatio(double? manualRatio)
        {
            SQLDataAccess.ExecuteNonQuery(
                 "Update [Catalog].[Product] Set ManualRatio = @ManualRatio",
                 CommandType.Text,
                 new SqlParameter("@ManualRatio", manualRatio ?? (object)DBNull.Value));

            CacheManager.RemoveByPattern(CacheNames.SQLPagingCount);
            CacheManager.RemoveByPattern(CacheNames.SQLPagingItems);
        }
        #endregion

        public static void SetMainLink(int productId, int categoryId, bool trackChanges = false, ChangedBy changedBy = null)
        {
            SQLDataAccess.ExecuteNonQuery("[Catalog].[sp_SetMainCategoryLink]", CommandType.StoredProcedure,
                new SqlParameter("@ProductID", productId), new SqlParameter("@CategoryID", categoryId));

            if (trackChanges)
                ProductHistoryService.TrackProductMainCategoryChanges(productId, categoryId, changedBy);
        }

        public static bool IsMainLink(int productId, int categoryId)
        {
            return
                SQLDataAccess.ExecuteReadColumn<bool>(
                    "SELECT Main FROM [Catalog].[ProductCategories] WHERE [ProductID] = @ProductID AND [CategoryID] = @CategoryID",
                    CommandType.Text, "Main", new SqlParameter("@ProductID", productId),
                    new SqlParameter("@CategoryID", categoryId)).FirstOrDefault();
        }

        public static void SetActive(int productId, bool active)
        {
            SQLDataAccess.ExecuteNonQuery(
                 "Update [Catalog].[Product] Set Enabled = @Enabled Where ProductID = @ProductID",
                 CommandType.Text,
                 new SqlParameter("@ProductID", productId),
                 new SqlParameter("@Enabled", active));
        }

        public static void SetHidden(int productId, bool hidden)
        {
            SQLDataAccess.ExecuteNonQuery(
                 "UPDATE [Catalog].[Product] SET Hidden = @Hidden WHERE ProductID = @ProductID",
                 CommandType.Text,
                 new SqlParameter("@ProductID", productId),
                 new SqlParameter("@Hidden", hidden));
        }

        public static void SetProductsHidden(bool hidden, string createdBy, DateTime? modifiedTill = null)
        {
            SQLDataAccess.ExecuteNonQuery(
                 "UPDATE [Catalog].[Product] SET Hidden = @Hidden WHERE CreatedBy = @CreatedBy" +
                 (modifiedTill.HasValue ? " AND DateModified < @modifiedFrom" : string.Empty),
                 CommandType.Text,
                 new SqlParameter("@DateModified", modifiedTill ?? (object)DBNull.Value),
                 new SqlParameter("@Hidden", hidden),
                 new SqlParameter("@CreatedBy", createdBy));
        }

        public static void SetBrand(int productId, int brandId)
        {
            SQLDataAccess.ExecuteNonQuery("UPDATE [Catalog].[Product] SET BrandID = @BrandID WHERE ProductID = @ProductID", CommandType.Text, new SqlParameter("@ProductID", productId), new SqlParameter("@BrandID", brandId));
        }

        public static void DeleteBrand(int productId)
        {
            SQLDataAccess.ExecuteNonQuery("UPDATE [Catalog].[Product] SET BrandID = NULL Where ProductID = @ProductID", CommandType.Text, new SqlParameter("@ProductID", productId));
        }

        public static List<string> GetForAutoCompleteByIdsInAdmin(string productIds)
        {
            if (string.IsNullOrEmpty(productIds))
                return new List<string>();

            return
                SQLDataAccess.ExecuteReadList<string>(
                    "Select Product.ProductID, Product.Name, Product.ArtNo, Product.UrlPath from Catalog.Product " +
                    " Inner Join (select item, sort from [Settings].[ParsingBySeperator](@productIds,'/') ) as dtt on Product.ProductId=convert(int, dtt.item) " +
                    " Where Enabled = 1 And CategoryEnabled = 1 " +
                    " order by dtt.sort",
                    CommandType.Text,
                    reader =>
                        string.Format("<a href=\"{2}\">{0}<span>({1})</span></a>",
                            SQLDataHelper.GetString(reader, "Name"), SQLDataHelper.GetString(reader, "ArtNo"),
                            String.Format("Product.aspx?ProductID={0}", SQLDataHelper.GetInt(reader, "ProductID"))),
                    new SqlParameter("@productIds", productIds));
        }


        public static List<Product> GetForAutoCompleteProductsByIds(string productIds)
        {
            if (string.IsNullOrEmpty(productIds))
                return new List<Product>();

            return SQLDataAccess.ExecuteReadList<Product>(
                "Select Product.ProductID, Product.Name, Product.ArtNo from Catalog.Product "
                + "Inner Join (select item, sort from [Settings].[ParsingBySeperator](@productIds,'/') ) as dtt on Product.ProductId=convert(int, dtt.item) "
                + "order by dtt.sort",
                CommandType.Text,
                reader => new Product()
                {
                    ProductId = SQLDataHelper.GetInt(reader, "ProductID"),
                    ArtNo = SQLDataHelper.GetString(reader, "ArtNo"),
                    Name = SQLDataHelper.GetString(reader, "Name")
                },
                new SqlParameter("@productIds", productIds));
        }

        public static List<ProductModel> GetForAutoCompleteProducts(int count, string productIds, List<int> warehouseIds = null)
        {
            return
                SQLDataAccess
                    .Query<ProductModel>(
                        "Select TOP(@top) Product.ProductID, " +
                        "Photo.PhotoId, Photo.PhotoName, Photo.PhotoNameSize1, Photo.PhotoNameSize2, Photo.Description as PhotoDescription, " +
                        "Product.ArtNo, Product.Name, Product.Discount, Product.DiscountAmount, Product.MinAmount, Product.MaxAmount, " +
                        "Product.Enabled, Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.UrlPath," +
                        "Product.DoNotApplyOtherDiscounts, Product.MainCategoryId, Product.Multiplicity," +
                        "Offer.OfferID, Offer.Amount as AmountOffer, Offer.ArtNo as OfferArtNo, Offer.ColorID, Offer.SizeID," +
                        (SettingsCatalog.ComplexFilter 
                            ? "Colors, NotSamePrices as MultiPrices, MinPrice as BasePrice, " 
                            : "null as Colors, 0 as MultiPrices, Price as BasePrice, ") +
                        (warehouseIds != null ? "Offer.ColorID as PreSelectedColorId, " : "") +
                        "MaxAvailable as Amount, CurrencyValue, Gifts " +
                            
                        "From Catalog.Product " +
                        "Inner Join (select item, sort from [Settings].[ParsingBySeperator](@productIds,'/') ) as dtt on Product.ProductId=convert(int, dtt.item) " +
                        "Left Join [Catalog].[ProductExt] On [Product].[ProductID] = [ProductExt].[ProductID] " +
                        (warehouseIds is null
                            ? "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] And Type=@Type " +
                              "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] "
                            : "Outer Apply (Select top (1) Offer.OfferID, Amount, ArtNo, ColorID, SizeID, Price " +
                                            "From [Catalog].[Offer] " +
                                            "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] " +
                                            "Where Offer.ProductId = Product.ProductID " +
                                            "Order by (Case When [WarehouseStocks].[WarehouseId] in @warehouseIds AND [WarehouseStocks].[Quantity] > 0 Then 1 Else 0 End) desc, Main desc" +
                                            ") as Offer " +
                              "Outer Apply (Select top (1) PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Description " +
                                            "From Catalog.Photo " +
                                            "Where Photo.ObjId = Product.ProductId and (Photo.ColorID = Offer.ColorID OR Photo.ColorID IS NULL) AND Type = 'Product' " +
                                            "Order by [Photo].Main DESC, [Photo].[PhotoSortOrder], [PhotoId]" +
                                            ") as Photo "
                        ) +
                        "Inner Join [Catalog].[Currency] On [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                        
                        "Where Enabled = 1 and CategoryEnabled = 1 " +
                        (SettingsCatalog.ShowOnlyAvalible
                            ? "and (MaxAvailable>0 OR [Product].[AllowPreOrder] = 1) "
                            : "") +
                        (warehouseIds != null
                            ? "and (Product.AllowPreOrder = 1 " +
                                    "OR Exists(" +
                                        "Select 1 from [Catalog].[Offer] " + 
                                        "Inner Join [Catalog].[WarehouseStocks] on [Offer].[OfferID] = [WarehouseStocks].[OfferId] " + 
                                        "Where [WarehouseStocks].[WarehouseId] in @warehouseIds " + 
                                        "     AND Offer.ProductId = Product.ProductID " + 
                                        "     AND [WarehouseStocks].[Quantity] > 0)) "
                            : "") +
                        "Order by dtt.sort",
                        new
                        {
                            top = count, 
                            productIds,
                            Type = PhotoType.Product.ToString(),
                            warehouseIds
                        })
                    .ToList();
        }

        public static void MarkersFromString(Product product, string source, string columSeparator)
        {
            if (string.IsNullOrWhiteSpace(columSeparator))
                _MarkersFromString(product, source, ",");
            else
                _MarkersFromString(product, source, columSeparator);
        }

        private static void _MarkersFromString(Product product, string source, string columSeparator)
        {
            // b,n,r,s
            if (!string.IsNullOrWhiteSpace(source))
            {
                var items = source.Split(new[] { columSeparator }, StringSplitOptions.RemoveEmptyEntries);
                product.BestSeller = items.Contains("b");
                product.New = items.Contains("n");
                product.Recomended = items.Contains("r");
                product.OnSale = items.Contains("s");
            }
            else
            {
                product.BestSeller = product.New = product.Recomended = product.OnSale = false;
            }
        }

        public static string MarkersToString(Product product, string columSeparator)
        {
            // b,n,r,s
            string res = string.Empty;
            res += product.BestSeller ? "b" + columSeparator : string.Empty;
            res += product.New ? "n" + columSeparator : string.Empty;
            res += product.Recomended ? "r" + columSeparator : string.Empty;
            res += product.OnSale ? "s" + columSeparator : string.Empty;
            if (res.Length > 0)
                res = res.Remove(res.Length - 1, 1);
            return res;
        }

        public static string MarkersToString(bool isBestSeller, bool isNew, bool idRecomended, bool isOnSale, string columSeparator)
        {
            // b,n,r,s
            string res = string.Empty;
            res += isBestSeller ? "b" + columSeparator : string.Empty;
            res += isNew ? "n" + columSeparator : string.Empty;
            res += idRecomended ? "r" + columSeparator : string.Empty;
            res += isOnSale ? "s" + columSeparator : string.Empty;
            if (res.Length > 0)
                res = res.Remove(res.Length - 1, 1);
            return res;
        }

        public static void PreCalcProductParams(int productId)
        {
            SQLDataAccess.ExecuteNonQuery("[Catalog].[PreCalcProductParams]", CommandType.StoredProcedure, 60 * 3,
                                new SqlParameter("@ProductId", productId),
                                new SqlParameter("@ModerateReviews", SettingsCatalog.ModerateReviews),
                                new SqlParameter("@OnlyAvailable", SettingsCatalog.ShowOnlyAvalible),
                                new SqlParameter("@ComplexFilter", SettingsCatalog.ComplexFilter)
                                );

            CacheManager.RemoveByPattern(CacheNames.SQLPagingCount);
            CacheManager.RemoveByPattern(CacheNames.SQLPagingItems);
            CacheManager.RemoveByPattern(CacheNames.Category);
            CacheManager.RemoveByPattern(CacheNames.ProductRelatedProducts);
        }

        public static void PreCalcProductComments(int productId)
        {
            SQLDataAccess.ExecuteNonQuery(
                "Update Catalog.ProductExt Set Comments = (SELECT Count(ReviewId) From CMS.Review Where EntityId = @ProductId And ParentId = 0 And (Checked = 1 or @ModerateReviews = 0)) Where ProductId = @ProductId",
                CommandType.Text,
                180,
                new SqlParameter("@ProductId", productId),
                new SqlParameter("@ModerateReviews", SettingsCatalog.ModerateReviews));
        }

        public static void PreCalcProductColors(int? colorId = null)
        {
            var sqlParameters = new List<SqlParameter>();
            var condition = string.Empty;
            if (colorId.HasValue)
            {
                sqlParameters.Add(new SqlParameter("@ColorId", colorId));
                condition = "WHERE EXISTS (SELECT ProductId FROM [Catalog].[Offer] Where ColorID = @ColorId AND Offer.ProductId = ProductExt.ProductId)";
            }
            SQLDataAccess.ExecuteNonQuery(
                "UPDATE [Catalog].[ProductExt] SET Colors = (SELECT [Settings].[ProductColorsToString](ProductExt.ProductId, 0)) " + condition,
                CommandType.Text,
                180,
                sqlParameters.ToArray());
            CacheManager.RemoveByPattern(CacheNames.SQLPagingCount);
            CacheManager.RemoveByPattern(CacheNames.SQLPagingItems);
        }

        public static void PreCalcProductParamsMass()
        {
            try
            {
                using (var db = new SQLDataAccess())
                {
                    db.cmd.CommandText = "[Catalog].[PreCalcProductParamsMass]";
                    db.cmd.CommandType = CommandType.StoredProcedure;
                    db.cmd.CommandTimeout = 1000;
                    db.cmd.Parameters.Clear();
                    db.cmd.Parameters.Add(new SqlParameter("@ModerateReviews", SettingsCatalog.ModerateReviews));
                    db.cmd.Parameters.Add(new SqlParameter("@OnlyAvailable", SettingsCatalog.ShowOnlyAvalible));
                    db.cmd.Parameters.Add(new SqlParameter("@ComplexFilter", SettingsCatalog.ComplexFilter));
                    db.cnOpen();
                    db.cmd.ExecuteNonQuery();
                    db.cnClose();
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }

            CacheManager.RemoveByPattern(CacheNames.SQLPagingCount);
            CacheManager.RemoveByPattern(CacheNames.SQLPagingItems);
        }

        public static void PreCalcProductParamsMassInBackground()
        {
            Task.Factory.StartNew(PreCalcProductParamsMass, TaskCreationOptions.LongRunning);
        }

        public static List<ProductDiscount> GetDiscountList()
        {
            var discountModules = AttachedModules.GetModuleInstances<IDiscount>();
            if (discountModules == null || discountModules.Count == 0)
                return null;
            
            var productDiscounts = new List<ProductDiscount>();
            
            foreach (var discountModule in discountModules)
            {
                var list = discountModule.GetProductDiscountsList();
                
                if (list != null && list.Count > 0)
                    productDiscounts.AddRange(list);
            }
            
            return productDiscounts;
        }
        
        public static bool IgnoreProductDoNotApplyOtherDiscounts(int productId)
        {
            if (productId == 0)
                return false;

            var modules = AttachedModules.GetModuleInstances<IIgnoreProductDoNotApplyOtherDiscounts>();
            if (modules == null || modules.Count == 0)
                return false;
            
            foreach (var module in modules)
            {
                if (module.Ignore(productId))
                    return true;
            }
            return false;
        }

        public static List<ProductModel> GetProductsByCategory(int categoryId, int count, ESortOrder sort, 
                                                                bool indepth = false, List<int> warehouseIds = null)
        {
            const string cacheKeyPattern = "_products_{0}_{1}_{2}_{3}";
            var warehouseIdsCacheKey = warehouseIds is null ? "" : string.Join(",", warehouseIds);
            return CacheManager.Get(CacheNames.GetCategoryCacheObjectName(categoryId) +
                             string.Format(cacheKeyPattern, count, sort, indepth, warehouseIdsCacheKey),
                () => GetProductsByCategoryInternal(categoryId, count, sort, indepth, warehouseIds));
        }

        private static List<ProductModel> GetProductsByCategoryInternal(int categoryId, int count, ESortOrder sort, 
            bool indepth = false, List<int> warehouseIds = null)
        {
            var warehouseIdsSql = warehouseIds != null && warehouseIds.Count > 0
                ? string.Join(",", warehouseIds)
                : null;
            
            var sortItems = new List<string>();

            if (SettingsCatalog.MoveNotAvaliableToEnd)
            {
                sortItems.Add("(CASE WHEN Price=0 THEN 0 ELSE 1 END) DESC");
                sortItems.Add("AmountSort DESC");
            }

            switch (sort)
            {
                case ESortOrder.AscByName:
                    sortItems.Add("Product.Name ASC");
                    break;

                case ESortOrder.DescByName:
                    sortItems.Add("Product.Name DESC");
                    break;

                case ESortOrder.AscByPrice:
                    sortItems.Add("PriceTemp ASC");
                    break;

                case ESortOrder.DescByPrice:
                    sortItems.Add("PriceTemp DESC");
                    break;

                case ESortOrder.AscByRatio:
                    sortItems.Add("(CASE WHEN ManualRatio is NULL THEN Ratio ELSE ManualRatio END) ASC");
                    break;

                case ESortOrder.DescByRatio:
                    sortItems.Add("(CASE WHEN ManualRatio is NULL THEN Ratio ELSE ManualRatio END) DESC");
                    break;

                case ESortOrder.AscByAddingDate:
                    sortItems.Add("DateAdded ASC");
                    break;

                case ESortOrder.DescByAddingDate:
                    sortItems.Add("DateAdded DESC");
                    break;

                case ESortOrder.DescByDiscount:
                    sortItems.Add("Discount DESC");
                    sortItems.Add("DiscountAmount DESC");
                    break;
            }

            sortItems.Add("[ProductCategories].[SortOrder] ASC");

            return SQLDataAccess.Query<ProductModel>(
                "Select Top(@Count) Product.ProductID, CountPhoto, " +
                "Photo.PhotoId, Photo.PhotoNameSize1, Photo.PhotoNameSize2, Photo.PhotoName, Photo.Description as PhotoDescription, " +
                "Product.ArtNo, Product.Name, Product.Recomended as Recomend, Product.Bestseller, Product.New, Product.OnSale as Sales, " +
                "Product.Discount, Product.DiscountAmount, Product.BriefDescription, Product.MinAmount, Product.MaxAmount, Product.Enabled, " +
                "Product.AllowPreOrder, Product.Ratio, Product.ManualRatio, Product.UrlPath, Product.DateAdded, Product.DoNotApplyOtherDiscounts, " +
                "Product.MainCategoryId, Product.Multiplicity, " +
                "Offer.OfferID, Offer.Amount AS AmountOffer, Offer.ArtNo as OfferArtNo, Offer.ColorID, Offer.SizeID, " +
                "MaxAvailable AS Amount, Comments, CurrencyValue, Gifts, Units.DisplayName as UnitDisplayName, Units.Name as UnitName, ProductExt.AllowAddToCartInCatalog, " +
                (warehouseIdsSql != null ? "Offer.ColorID as PreSelectedColorId, " : "") +
                (SettingsCatalog.ComplexFilter
                    ? "Colors, NotSamePrices as MultiPrices, MinPrice as BasePrice "
                    : "null as Colors, 0 as MultiPrices, Price as BasePrice ") +
                " From [Catalog].[Product] " +
                "Left Join [Catalog].[ProductExt]  ON [Product].[ProductID] = [ProductExt].[ProductID]  " +
                (warehouseIdsSql is null
                    ? "Left Join [Catalog].[Photo] On [Photo].[PhotoId] = [ProductExt].[PhotoId] And Type=@Type " +
                      "Left Join [Catalog].[Offer] On [ProductExt].[OfferID] = [Offer].[OfferID] "
                    : "Outer Apply (Select top (1) Offer.OfferID, Amount, ArtNo, ColorID, SizeID, Price " +
                                    "From [Catalog].[Offer] " +
                                    "Inner Join [Catalog].[WarehouseStocks] ON [Offer].[OfferID] = [WarehouseStocks].[OfferId] " +
                                    "Where Offer.ProductId = Product.ProductID " +
                                    "Order by (Case When [WarehouseStocks].[WarehouseId] = @warehouseId AND [WarehouseStocks].[Quantity] > 0 Then 1 Else 0 End) desc, Main desc" +
                                    ") as Offer " +
                      "Outer Apply (Select top (1) PhotoId, PhotoName, PhotoNameSize1, PhotoNameSize2, Description " +
                                    "From Catalog.Photo " +
                                    "Where Photo.ObjId = Product.ProductId and (Photo.ColorID = Offer.ColorID OR Photo.ColorID IS NULL) AND Type = 'Product' " +
                                    "Order by [Photo].Main DESC, [Photo].[PhotoSortOrder], [PhotoId]" +
                                    ") as Photo "
                ) +
                "Inner Join [Catalog].[Currency] ON [Currency].[CurrencyID] = [Product].[CurrencyID] " +
                "Left Join [Catalog].[ProductCategories] On ProductCategories.ProductId = [Product].[ProductID] and ProductCategories.Main=1 " +
                "Left Join [Catalog].[Units] ON [Product].[Unit] = [Units].[Id] " +
                "Where Product.Enabled=1 and Product.Hidden=0 and CategoryEnabled=1 and " +
                (indepth
                    ? "Exists(select 1 from [Catalog].[ProductCategories] Inner Join [Settings].[GetChildCategoryByParent](@CategoryId) AS hCat ON hCat.id = [ProductCategories].[CategoryID] and ProductCategories.ProductId = [Product].[ProductID]) "
                    : "Exists(select 1 from [Catalog].[ProductCategories] Where [ProductCategories].[CategoryId] = @CategoryId and ProductCategories.ProductId = [Product].[ProductID]) ") +
                (SettingsCatalog.ShowOnlyAvalible ? " AND (MaxAvailable > 0 OR [Product].[AllowPreOrder] = 1) " : "") +
                (warehouseIdsSql != null
                    ? "and (Product.AllowPreOrder = 1 " +
                            "OR Exists(" +
                                "Select 1 from [Catalog].[Offer] " + 
                                "Inner Join [Catalog].[WarehouseStocks] on [Offer].[OfferID] = [WarehouseStocks].[OfferId] " + 
                                "Where [WarehouseStocks].[WarehouseId] in ("  + warehouseIdsSql + ") " + 
                                "     AND Offer.ProductId = Product.ProductID " + 
                                "     AND [WarehouseStocks].[Quantity] > 0)) "
                    : "") +
                (sortItems.Count > 0 ? $" Order by {string.Join(", ", sortItems)}" : ""),
                new
                {
                    Count = count,
                    Type = PhotoType.Product.ToString(),
                    CategoryId = categoryId,
                    warehouseId = warehouseIds != null && warehouseIds.Count > 0 ? warehouseIds[0] : default(int?)
                }).ToList();
        }

        public static List<ProductModel> GetProductsByBrand(int brandId)
        {
            return SQLDataAccess.Query<ProductModel>("SELECT * FROM [Catalog].[Product] WHERE [BrandId] = " + brandId).ToList();
        }

        public static List<int> GetProductIdsByOfferIds(List<int> offerIds)
        {
            return SQLDataAccess.ExecuteReadList(
                "Select Distinct ProductId From [Catalog].[Offer] Where OfferId in (" + String.Join(",", offerIds) + ")",
                CommandType.Text,
                reader => SQLDataHelper.GetInt(reader, "ProductId"));
        }

        public static List<string> GetDeletedProducts(DateTime? from, DateTime? to)
        {
            var query = "SELECT ArtNo FROM [Catalog].[DeletedProducts]";
            var queryParams = new List<SqlParameter>();

            if (from != null && to != null)
            {
                query += " Where [DateTime] >= @From and [DateTime] <= @To";
                queryParams.Add(new SqlParameter("@From", from));
                queryParams.Add(new SqlParameter("@To", to));
            }

            return SQLDataAccess.ExecuteReadList(query, CommandType.Text, reader => SQLDataHelper.GetString(reader, "ArtNo"), queryParams.ToArray());
        }

        #region Gift

        public static bool HasGifts(int productId)
        {
            return
                Convert.ToBoolean(
                    SQLDataAccess.ExecuteScalar(
                        "Select Gifts From [Catalog].[ProductExt] Where ProductId = @ProductId", CommandType.Text,
                        new SqlParameter("@ProductId", productId)));
        }

        #endregion

        public static ChangedBy GetModifiedDateByProduct(int productId)
        {
            return SQLDataAccess.ExecuteReadOne(
                "Select TOP 1 DateModified, ModifiedBy From Catalog.Product Where ProductId=@productId",
                CommandType.Text,
                reader =>
                {
                    var modifiedDate = SQLDataHelper.GetNullableDateTime(reader, "DateModified");
                    if (modifiedDate == null)
                        return null;

                    return new ChangedBy(SQLDataHelper.GetString(reader, "ModifiedBy"))
                    {
                        ModificationTime = modifiedDate.Value
                    };
                },
                new SqlParameter("@productId", productId));
        }


        public static void DeactivateGoodsMoreThan(int activeProductsCount)
        {
            if (activeProductsCount <= 0)
                return;

            SQLDataAccess.ExecuteNonQuery(
                @"if(Select (Count(ProductId)) From Catalog.Product Where Product.Enabled = 1 ) > @productsNumber
                  BEGIN
                    ;WITH productsToDeactivate AS 
                    ( 
	                    Select 
	                    Top(Select (Count(ProductId) - @productsNumber) From Catalog.Product Where Product.Enabled = 1) Product.ProductId 
	                    From Catalog.Product
	                    Where Product.Enabled = 1 
	                    Order by Product.DateAdded Desc
                    ) 
                    UPDATE Catalog.Product SET Enabled = 0 Where ProductId in (Select ProductId from productsToDeactivate)
                  END",
                CommandType.Text,
                new SqlParameter("@productsNumber", activeProductsCount));
        }

        public static void RecalcSortPopular(int productId)
        {
            SQLDataAccess.ExecuteNonQuery(
                @"Update Catalog.Product
                Set SortPopular = isNull((Select Sum([Amount]) From [Order].[OrderItems]
                                          Left Join [Order].[Order] On [Order].[OrderId] = [OrderItems].[OrderId]
                                          Where [OrderItems].[ProductId] = Product.ProductId and PaymentDate is not null), 0)
                Where ProductId = @ProductId",
                CommandType.Text,
                new SqlParameter("@ProductId", productId));
        }

        public static void RecalcSortPopularMass()
        {
            SQLDataAccess.ExecuteNonQuery(
                @"UPDATE [Catalog].[Product] 
                SET [SortPopular] = ISNULL((Select SUM([Amount]) FROM [Order].[OrderItems]
                                            LEFT JOIN [Order].[Order] ON [Order].[OrderId] = [OrderItems].[OrderId]
                                            WHERE [OrderItems].[ProductId] = Product.ProductId AND PaymentDate IS NOT NULL), 0)",
                CommandType.Text);
        }

        public static void RecalcSortPopularMassInBackground()
        {
            Task.Factory.StartNew(RecalcSortPopularMass, TaskCreationOptions.LongRunning);
        }

        public static List<string> GetCreatedByList()
        {
            return SQLDataAccess.ExecuteReadList(
                "SELECT DISTINCT CreatedBy FROM [Catalog].[Product] WHERE CreatedBy<> ''",
                CommandType.Text,
                reader => SQLDataHelper.GetString(reader, "CreatedBy"));
        }

        public static bool AllowAddProductToCart(int productId) =>
            SQLDataAccess.ExecuteReadOne(
                @"SELECT CASE
                           WHEN COUNT(DISTINCT SizeID) > 1
                               THEN 0
                           ELSE 1
                           END AS AllowAddProductToCart
                FROM [Catalog].[Offer]
                WHERE [ProductID] = @ProductId",
                CommandType.Text,
                reader => SQLDataHelper.GetBoolean(reader, "AllowAddProductToCart"),
                new SqlParameter("@ProductId", productId));

        public static List<int> GetCategoriesIDsByProductIds(List<int> productIds, bool onlyActive)//GlorySoft_023
        {
            if (productIds == null || productIds.Count == 0)
                return new List<int>();
            if (onlyActive)
            {
                return SQLDataAccess.ExecuteReadColumn<int>(
                    "SELECT Distinct Category.CategoryID FROM Catalog.ProductCategories " +
                    "inner join Catalog.Category on Category.CategoryId = ProductCategories.CategoryId " +
                    string.Format("WHERE ProductID In ({0}) and Enabled = 1 and HirecalEnabled = 1 and Hidden = 0 And Main = 1 ", productIds.AggregateString(',')),
                    //"order by main desc",
                    CommandType.Text, "CategoryID");
            }
            else
            {
                return SQLDataAccess.ExecuteReadColumn<int>(
                    "SELECT Distinct CategoryID FROM Catalog.ProductCategories " +
                    string.Format("WHERE ProductID In ({0}) And Main = 1 ", productIds.AggregateString(',')),
                    //"order by main desc",
                    CommandType.Text, "CategoryID");
            }
        }

    }
}
