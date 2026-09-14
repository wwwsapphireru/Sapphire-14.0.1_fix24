using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Module.OneSApi.Models.Client;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterSizeHandler
    {
        #region Fields

        private readonly int _categoryId;
        private readonly bool _indepth;

        private readonly bool _onlyAvaliable;

        private readonly List<int> _selectedSizeIds;
        private readonly List<int> _availableSizeIds;

        #endregion

        #region Constructor

        public FilterSizeHandler(int categoryId, bool indepth, List<int> selectedSizeIds, List<int> availableSizeIds, bool onlyAvaliable)
        {
            _categoryId = categoryId;
            _indepth = indepth;
            _selectedSizeIds = selectedSizeIds ?? new List<int>();
            _availableSizeIds = availableSizeIds;
            _onlyAvaliable = onlyAvaliable;
        }

        #endregion

        public FilterItemModel Get()
        {
            var sizes =
                SizeService.GetSizesByCategoryID(_categoryId, _indepth, _onlyAvaliable)
                    .Select(x => new FilterSize()
                    {
                        SizeId = x.SizeId,
                        SizeName = x.SizeName,
                        SortOrder = x.SortOrder,
                        Checked = _selectedSizeIds.Contains(x.SizeId)
                    }).ToList();

            if (sizes.Count == 0)
                return null;

            var model = new FilterItemModel()
            {
                Expanded = true,
                Type = "size",
                Title = SettingsCatalog.SizesHeader,
                Subtitle = "",
                Control = "checkbox"
            };

            foreach (var size in sizes)
            {
                model.Values.Add(new FilterListItemModel()
                {
                    Id = size.SizeId.ToString(),
                    Text = size.SizeName,
                    Selected = size.Checked,
                    Available = _availableSizeIds == null || _availableSizeIds.Any(x => x == size.SizeId) || _selectedSizeIds.Any()
                });
            }

            return model;
        }

        public Task<List<FilterItemModel>> GetAsync()
        {
            return Task.Run(() =>
            {
                Localization.Culture.InitializeCulture();

                return new List<FilterItemModel> {Get()};
            });
        }
    }
}