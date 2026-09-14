using System;
using System.Collections.Generic;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class CategoryFiltering
    {
        public CategoryFiltering()
        {
            PriceFrom = 0;
            PriceTo = Int32.MaxValue;

            BrandIds = new List<int>();
            SizeIds = new List<int>();
            ColorIds = new List<int>();
            WarehouseIds = new List<int>();
            PropertyIds = new List<int>();
            Sorting = ESortOrder.NoSorting;
            ViewMode = ProductViewMode.Tile.ToString().ToLower();

            Sortings = new List<SelectListItem>();
            foreach (ESortOrder item in Enum.GetValues(typeof(ESortOrder)))
            {
                if (item.Ignore())
                    continue;

                if (item == ESortOrder.DescByRatio && !SettingsCatalog.EnableProductRating)
                    continue;

                Sortings.Add(new SelectListItem() { Text = item.Localize(), Value = item.ToString() });
            }

            ColorsViewMode = SettingsCatalog.ColorsViewMode;
        }

        public CategoryFiltering(int categoryId, bool indepth) : this()
        {
            CategoryId = categoryId;
            Indepth = indepth;
        }

        public int CategoryId { get; set; }

        public bool Indepth { get; set; }

        public List<int> BrandIds { get; set; }
        public List<int> AvailableBrandIds { get; set; }

        public List<int> SizeIds { get; set; }
        public List<int> AvailableSizeIds { get; set; }

        public List<int> ColorIds { get; set; }
        public List<int> AvailableColorIds { get; set; }

        public List<int> WarehouseIds { get; set; }
        public List<int> AvailableWarehouseIds { get; set; }

        public List<int> PropertyIds { get; set; }
        public List<int> AvailablePropertyIds { get; set; }
        public Dictionary<int, KeyValuePair<float, float>> RangePropertyIds { get; set; }

        public float PriceFrom { get; set; }

        public float PriceTo { get; set; }

        public bool Available { get; set; }

        //public bool Preorder { get; set; }

        public ESortOrder Sorting { get; set; }
        public List<SelectListItem> Sortings { get; set; }

        public string ViewMode { get; set; }
        public bool AllowChangeViewMode { get; set; }

        public List<int> SearchItemsResult { get; set; }

        /// <summary>
        /// Фильтр применен
        /// </summary>
        public bool IsApplied
        {
            get
            {
                return BrandIds.Count > 0 || SizeIds.Count > 0 || ColorIds.Count > 0 || WarehouseIds.Count > 0 ||
                       PropertyIds.Count > 0 || (RangePropertyIds != null && RangePropertyIds.Count > 0) ||
                       PriceFrom > 0 || PriceTo < Int32.MaxValue ||
                       Available;
            }
        }

        public ColorsViewMode ColorsViewMode { get; set; }
    }
}