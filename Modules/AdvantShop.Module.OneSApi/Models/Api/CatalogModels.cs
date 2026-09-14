using AdvantShop.Catalog;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Api
{
    public class CatalogImportModel
    {
        public List<CategoryImportModel> Categories { get; set; }
        public List<PropertyImportModel> Properties { get; set; }
        public List<ProductImportModel> Products { get; set; }
        public List<ProductOffersImportModel> Offers { get; set; }
        public List<ProductPhotosImportModel> Photos { get; set; }
        public List<ProductFilesImportModel> Files { get; set; }
    }

    public class CategoryImportModel
    {
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public string FullName { get; set; }
        public string BriefDescription { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public PhotoImportModel MiniPicture { get; set; }
        public List<ProductCategoryModel> Category { get; set; }
    }

    public class PropertyImportModel
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string GroupName { get; set; }
        public int SortOrder { get; set; }
        public bool UseInFilter { get; set; }
        public bool UseInBrief { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRange { get; set; }
    }

    public class ProductImportModel
    {
        public string ExternalId { get; set; }
        public string ArtNo { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string BriefDescription { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public List<ProductCategoryModel> Category { get; set; }
        public string CategoryId { get; set; }
        public string Tax { get; set; }
        public ProductDiscountModel Discount { get; set; }
        public string Unit { get; set; }
        public float Weight { get; set; }
        public bool AllowPreOrder { get; set; }
        public bool Enabled { get; set; }
        public bool CategoryEnabled { get; set; }
        public bool PriceOnRequest { get; set; }
        public bool Recomended { get; set; }
        public bool Bestseller { get; set; }
        public bool DoNotApplyOtherDiscounts { get; set; }
        public float Multiplicity { get; set; }
        public string MainPhoto { get; set; }
        public short PriceNumber { get; set; }
        public bool Adult { get; set; }
        public ProductPropertiesModel Properties { get; set; }
        public List<RelatedProductModel> RelatedProducts { get; set; }
    }
    public class ProductDiscountModel
    {
        public float Percent { get; set; }
        public float Amount { get; set; }
        public DiscountType Type { get; set; }
    }
    public class ProductCategoryModel
    {
        public string ExternalId { get; set; }
        public string Name { get; set; }
        //public ProductCategoryModel Parent { get; set; }
    }
    public class ProductPropertiesModel
    {
        public string Group { get; set; }
        public List<ProductPropertyModel> Properties { get; set; }
    }
    public class ProductPropertyModel
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
        //public bool UseInFilter { get; set; }
        //public bool UseInBrief { get; set; }
        public string Value { get; set; }
        public float? RangeValue { get; set; }
        //public bool? IsDeleted { get; set; }
    }
    public class RelatedProductModel
    {
        public string ExternalId { get; set; }
        public int SortOrder { get; set; }
    }

    public class ProductOffersImportModel
    {
        public string ExternalId { get; set; }
        public List<OfferImportModel> Offers { get; set; }
        public List<DepotAmount> DepotAmounts { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public bool PriceOnRequest { get; set; }
        public short PriceNumber { get; set; }
    }
    public class OfferImportModel
    {
        public string Color { get; set; }
        public string Size { get; set; }
        public float Amount { get; set; }
        public float BasePrice { get; set; }
        public float SupplyPrice { get; set; }
        public string BarCode { get; set; }
        public float Weight { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
    public class DepotAmount
    {
        public string DepotCode { get; set; }
        public float Amount { get; set; }
    }

    public class ProductPhotosImportModel
    {
        public string ExternalId { get; set; }
        public List<PhotoImportModel> Photos { get; set; }
    }
    public class PhotoImportModel
    {
        public string/*byte[]*/ ValueStorage { get; set; }
        public string OriginName { get; set; }
        public string Color { get; set; }
        public bool Main { get; set; }
    }

    public class ProductFilesImportModel
    {
        public string ExternalId { get; set; }
        public List<FileImportModel> Files { get; set; }
    }
    public class FileImportModel
    {
        public string ExternalId { get; set; }
        public string/*byte[]*/ ValueStorage { get; set; }
        public string FileName { get; set; }
        public string TextLink { get; set; }
        public string FileType { get; set; }
        public string GroupName { get; set; }
        public int SortOrder { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
    }

    public class CatalogImportResultModel
    {
        public List<ImportResultModel> Categories { get; set; }
        public List<ImportResultModel> Properties { get; set; }
        public List<ImportResultModel> Products { get; set; }
        public List<ImportResultModel> Offers { get; set; }
        public List<ImportResultModel> RelatedProducts { get; set; }
        public List<ImportResultModel> Photos { get; set; }
        public List<ImportResultModel> Files { get; set; }
    }
    public class ImportResultModel
    {
        public string ExternalId { get; set; }
        //public string ArtNo { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Status { get; set; }
        public List<ImportDetailResultModel> Details { get; set; }
    }
    public class ImportDetailResultModel
    {
        public string Id { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class ProductExportModel
    {
        public static ProductExportModel FromProduct(Product product)
        {
            if (product == null)
                return null;

            var external = ExportService.GetExternalFromProductId(product.ProductId);

            return new ProductExportModel
            {
                ProductId = product.ProductId,
                ExternalId = external.ExternalId,
                ExportType = external.ExportType,
                Changed = external.Changed,
                Exported = external.Exported,
                ArtNo = product.ArtNo,
                Name = product.Name,
                Description = product.Description,
            };
        }

        public int ProductId { get; set; }
        public string ExternalId { get; set; }
        public int ExportType { get; set; }
        public DateTime Changed { get; set; }
        public DateTime? Exported { get; set; }
        public string ArtNo { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class FilterProductsModel : EntitiesFilterModel//, IValidatableObject
    {
        public FilterProductsModel()
        {
        }

        public int? ProductId { get; set; }
        public bool All { get; set; }

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    DateTime temp;
        //    if (!string.IsNullOrWhiteSpace(DateFrom) && !DateTime.TryParse(DateFrom, out temp))
        //        yield return new ValidationResult("Неудалось преобразовать дату параметра DateFrom");

        //    if (!string.IsNullOrWhiteSpace(DateTo) && !DateTime.TryParse(DateTo, out temp))
        //        yield return new ValidationResult("Неудалось преобразовать дату параметра DateTo");
        //}
    }

    public class ExternalProductModel
    {
        public string ExternalId { get; set; }
        public int ExportType { get; set; }
        public DateTime Changed { get; set; }
        public DateTime? Exported { get; set; }
    }

    public class ConfirmProductModel
    {
        public List<ConfirmProductModel_item> Items { get; set; }
        public string LogFile { get; set; }
    }
    public class ConfirmProductModel_item
    {
        public int ProductId { get; set; }
        public ExportType ExportType { get; set; }
        public string ExternalId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

}
