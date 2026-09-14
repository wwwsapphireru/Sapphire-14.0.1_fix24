using System.Collections.Generic;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Core.Models;
using AdvantShop.ViewModel.Catalog;
using AdvantShop.Core.Services.Catalog;

namespace AdvantShop.Models.Catalog
{
    public class SearchCatalogViewModel : BaseModel
    {
        public SearchCatalogViewModel(int categoryId)
        {
            Filter = new CategoryFiltering()
            {
                CategoryId = categoryId
            };
        }

        public SearchCatalogModel SearchCatalogModel { get; set; }

        public CategoryFiltering Filter { get; set; }

        public ProductViewModel Products { get; set; }

        public CategoryListViewModel Categories { get; set; }

        public Pager Pager { get; set; }

        public bool HasProducts => Products != null && Products.Products.Count > 0;

        public List<SelectListItem> SortingList { get; set; }
    }

    public class SearchCatalogModel
    {
        public string Q { get; set; }

        public string Brand { get; set; }

        public int? Page { get; set; }

        public ESortOrder? Sort { get; set; }

        public string ViewMode { get; set; }

        public int? CategoryId { get; set; }

        public float? PriceFrom { get; set; }

        public float? PriceTo { get; set; }

        public string Size { get; set; }

        public string Color { get; set; }

        public bool Available { get; set; }

        public string Warehouse { get; set; }

        public string Prop { get; set; }
        
        public List<CatalogFilterPropertyRange> PropertyRanges { get; set; }

        public string/*List<int>*/ ProductIds { get; set; }//GlorySoft_024
    }
}