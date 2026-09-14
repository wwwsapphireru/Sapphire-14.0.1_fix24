using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Core.Services.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Module.OneSApi.Service;
using System.Text;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterSelectCategoryHandler
    {
        private int _categoryId;
        private List<int> _productIds;

        public FilterSelectCategoryHandler(int categoryId, List<int> productIds)
        {
            _categoryId = categoryId;
            _productIds = productIds;
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

            List<Category> categories = new List<Category>();
            //if (_categoryId != 0)
            //    categories = CategoryService.GetChildCategoriesByCategoryId(0, false).Where(cat => cat.Enabled && !cat.Hidden).ToList();
            //else
            //{
                var catIds = ProductServiceV8.GetCategoriesIDsByProductIds(_productIds, true);
                if (catIds.Count > 0)
                    categories = CategoryService.GetCategoriesByCategoryIds(catIds).OrderBy(x => x.Name).ToList();
            //}

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

        private string GetParentCategoriesAsString(int childCategoryId)
        {
            var res = new StringBuilder();
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