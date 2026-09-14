using System.Collections.Generic;
using AdvantShop.Core.Modules.Interfaces;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class ProductTabsViewModel 
    {
        public string CustomViewPath { get; set; }

        public ProductTabsViewModel()
        {
            Tabs = new List<BaseTab>();
        }

        public ProductDetailsViewModel ProductModel { get; set; }

        public List<BaseTab> Tabs { get; set; }

        public string AdditionalDescription { get; set; }

        public int ReviewsCount { get; set; }

        public int VideosCount { get; set; }

        public bool UseStandartReviews { get; set; }
    }
}
