using System.Collections.Generic;
using AdvantShop.Catalog;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class ProductPhotosViewModel 
    {
        public string CustomViewPath { get; set; }
        public Product Product { get; set; }
        public Discount Discount { get; set; }
        public ProductDetailsViewModel ProductModel { get; set; }

        public ProductPhoto MainPhoto { get; set; }

        public List<ProductPhoto> Photos { get; set; }

        public int CarouselPhotoHeight { get; set; }
        public int CarouselPhotoWidth { get; set; }

        public int PreviewPhotoWidth { get; set; }
        public int PreviewPhotoHeight { get; set; }

        public List<string> Labels { get; set; }

        public bool EnabledModalPreview { get; set; }

        public bool EnabledZoom { get; set; }

        public bool ActiveThreeSixtyView { get; set; }
        public List<ProductPhoto> Photos360 { get; set; }
        public string Photos360Ext { get; set; }

        public ProductVideo Video { get; set; }
    }
}