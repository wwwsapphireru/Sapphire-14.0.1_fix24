using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Api.Product
{
    public class ProductImportModel
    {
        public string ExternalId { get; set; }
        public string ArtNo { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string BriefDescription { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public ProductCategoryModel Category { get; set; }
        public string Tax { get; set; }
        public string Unit { get; set; }
        //public string Weight { get; set; }
        //public string BarCode { get; set; }
        public bool Enabled { get; set; }
        public List<ProductPropertyModel> Properties { get; set; }
        public List<string> Photos { get; set; }
        public List<ProductOfferModel> Offers { get; set; }
    }
    public class ProductCategoryModel
    {
        public string Name { get; set; }
        public ProductCategoryModel Parent { get; set; }
    }
    public class ProductPropertyModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public float? RangeValue { get; set; }
    }
    public class ProductOfferModel
    {
        public string ArtNo { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public float Price { get; set; }
        public float Amount { get; set; }
        public string BarCode { get; set; }
    }

    public class ProductImportResultModel
    {
        public string ExternalId { get; set; }
        //public string ArtNo { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

}
