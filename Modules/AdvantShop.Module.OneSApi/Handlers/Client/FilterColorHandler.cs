using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.FilePath;
using AdvantShop.Module.OneSApi.Models.Client;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterColorHandler
    {
        #region Fields

        private readonly int _categoryId;
        private readonly bool _indepth;
        private readonly bool _onlyAvailable;

        private readonly List<int> _selectedColorIds;
        private readonly List<int> _availableColorIds;

        private readonly ColorsViewMode _colorsViewMode;
        #endregion

        #region Constructor

        public FilterColorHandler(int categoryId, bool indepth, List<int> selectedColorIds, List<int> availableColorIds, bool onlyAvailable, ColorsViewMode colorsViewMode = ColorsViewMode.Icon)
        {
            _categoryId = categoryId;
            _indepth = indepth;
            _selectedColorIds = selectedColorIds ?? new List<int>();
            _availableColorIds = availableColorIds;
            _onlyAvailable = onlyAvailable;
            _colorsViewMode = colorsViewMode;
        }

        #endregion

        public FilterItemModel Get()
        {
            var colors = ColorService.GetColorsByCategoryId(_categoryId, _indepth, _onlyAvailable)
                            .Select(x => new FilterColor()
                            {
                                ColorId = x.ColorId,
                                ColorName = x.ColorName,
                                ColorCode = x.ColorCode,
                                IconFileName = x.IconFileName,
                                SortOrder = x.SortOrder,
                                Checked = _selectedColorIds.Contains(x.ColorId)
                            })
                            .ToList();

            if (colors.Count == 0)
                return null;
            
            var model = new FilterItemModel()
            {
                Expanded = true,
                Type = "color",
                Title = SettingsCatalog.ColorsHeader,
                Subtitle = "",
                Control = "color"
            };

            foreach (var color in colors)
            {
                model.Values.Add(new
                {
                    color.ColorName,
                    color.ColorCode,
                    Selected = color.Checked,
                    Text = color.ColorName,
                    Id = color.ColorId,
                    Available = _availableColorIds == null || _availableColorIds.Any(x => x == color.ColorId) || _selectedColorIds.Any(),
                    ImageHeight = color.IconFileName.ImageHeightCatalog,
                    ImageWidth = color.IconFileName.ImageWidthCatalog,
                    color.IconFileName.PhotoName,
                    ImageSrc = color.IconFileName.ImageSrc(ColorImageType.Catalog),
                    ColorsViewMode = _colorsViewMode.ToString().ToLower()
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