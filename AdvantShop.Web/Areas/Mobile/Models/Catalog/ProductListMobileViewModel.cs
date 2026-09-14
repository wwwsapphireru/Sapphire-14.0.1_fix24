using System.Collections.Generic;
using System.Web.Mvc;
using AdvantShop.Core.Models;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Models.Catalog;

namespace AdvantShop.Areas.Mobile.Models.Catalog
{
    public class ProductListMobileViewModel
    {
        public int ListId { get; set; }
        public string Title { get; set; }

        public string Type { get; set; }

        public ProductViewModel Products { get; set; }

        public Pager Pager { get; set; }

        public CategoryFiltering Filter { get; set; }

        public List<SelectListItem> SortingList { get; set; }

        public string Description { get; set; }

        public bool HasProducts
        {
            get { return Filter != null && Products != null && Products.Products.Count > 0; }
        }

        public int? SalesType { get; set; }//GlorySoft_016
    }
}