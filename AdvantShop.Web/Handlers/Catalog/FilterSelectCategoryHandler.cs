using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Models.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvantShop.Handlers.Catalog
{
    public class FilterSelectCategoryHandler
    {
        private int _categoryId;
        private List<int> _productIds;//GlorySoft_023

        public FilterSelectCategoryHandler(int categoryId,/*GlorySoft_023*/ List<int> productIds)
        {
            _categoryId = categoryId;
            _productIds = productIds;//GlorySoft_023
        }

        public FilterItemModel Get()
        {
            var model = new FilterItemModel()
            {
                Expanded = true,
                Type = "categoryId",
                Title = LocalizationService.GetResource("Catalog.FilterSelectCategory.FilterTitle"),
                Control = "selectSearch"
            };

            //var categories = CategoryService.GetChildCategoriesByCategoryId(0).Where(cat => cat.Enabled && !cat.Hidden);GlorySoft_023

            //foreach (var category in categories)GlorySoft_023
            //{
            //    model.Values.Add(new FilterListItemModel()
            //    {
            //        Id = category.CategoryId.ToString(),
            //        Text = category.Name,
            //        Selected = category.CategoryId == _categoryId
            //    });
            //}

            //GlorySoft_023
            List<Category> categories = new List<Category>();
            var catIds = ProductService.GetCategoriesIDsByProductIds(_productIds, true);
            if (catIds.Count > 0)
                categories = CategoryService.GetCategoriesByCategoryIds(catIds).OrderBy(x => x.Name).ToList();
            var dict = new Dictionary<Category, string>();
            foreach (var category in categories)
            {
                dict.Add(category, GetParentCategoriesAsString(category.CategoryId));
            }
            foreach (var category in dict.OrderBy(x => x.Value))
            {
                model.Values.Add(new FilterListItemModel()
                {
                    Id = category.Key.CategoryId.ToString(),
                    Text = category.Value,
                    Selected = category.Key.CategoryId == _categoryId
                });
            }

            return model;
        }

        public Task<List<FilterItemModel>> GetAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                Localization.Culture.InitializeCulture();
                return new List<FilterItemModel>
                {
                    Get()
                };
            });

        }

        private string GetParentCategoriesAsString(int childCategoryId)//GlorySoft_023
        {
            var res = new System.Text.StringBuilder();
            var categoies = CategoryService.GetParentCategories(childCategoryId).Where(x => x.CategoryId > 0).ToList();
            for (var i = categoies.Count - 1; i >= 0; i--)
            {
                if (i != categoies.Count - 1)
                {
                    res.Append(" / ");
                }
                res.Append(categoies[i].Name);
            }
            return res.ToString();
        }

    }
}