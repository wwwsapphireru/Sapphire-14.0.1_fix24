using System.Collections.Generic;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Core.Models;
using AdvantShop.Core.Services.Catalog;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class SearchCatalogViewModel
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

        public string/*List<int>*/ ProductIds { get; set; }
    }

    public class CatalogFilterPropertyRange
    {
        public int Id { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
    }

    public class FilterItemModel
    {
        public FilterItemModel()
        {
            Values = new List<object>();
        }

        public bool Expanded { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Control { get; set; }

        public string Description { get; set; }

        public List<object> Values { get; set; }
    }

    public class FilterListItemModel
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public bool Selected { get; set; }

        public bool Available { get; set; }
    }

    public class FilterColor : Color
    {
        public bool Checked { get; set; }

    }
    public class FilterSize : Size
    {
        public bool Checked { get; set; }
    }

    public class FilterRangeItemModel
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public float CurrentMin { get; set; }
        public float CurrentMax { get; set; }
        public float Step { get; set; }
        public float DecimalPlaces { get; set; }
        public int Id { get; set; }
    }

    public class CategoryListViewModel 
    {
        public CategoryListViewModel()
        {
            Categories = new List<Category>();
            CategoriesWithProducts = new List<CategoryProductsViewModel>();
        }

        public bool DisplayProductCount { get; set; }

        public int CountCategoriesInLine { get; set; }

        public List<Category> Categories { get; set; }

        public List<CategoryProductsViewModel> CategoriesWithProducts { get; set; }

        public int PhotoWidth { get; set; }

        public int PhotoHeight { get; set; }

        public ECategoryDisplayStyle DisplayStyle { get; set; }
    }

    public class CategoryProductsViewModel : ProductViewModel
    {
        public CategoryProductsViewModel(List<ProductModel> products) : base(products)
        {
            DisplayComparison = false;
            DisplayPhotoPreviews = false;
        }

        public CategoryProductsViewModel(List<ProductModel> products, bool isMobile) : base(products, isMobile)
        {
            DisplayComparison = false;
            DisplayPhotoPreviews = false;
        }

        public string Url { get; set; }
        public int ProductsCount { get; set; }
    }

}