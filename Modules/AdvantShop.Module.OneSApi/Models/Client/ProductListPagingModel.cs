using AdvantShop.Core.Models;
using AdvantShop.Core.Services.Catalog;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class ProductListPagingModel
    {
        public ProductListPagingModel(bool indepth)
        {
            Filter = new CategoryFiltering() { Indepth = indepth };
        }
        
        public ProductViewModel Products { get; set; }

        public Pager Pager { get; set; }

        public CategoryFiltering Filter { get; set; }

        public List<int> ProductIds { get; set; }
    }
}