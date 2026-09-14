using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Configuration.Settings;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class MainPageProductsMobileViewModel
    {
        public ProductViewModel Products { get; set; }

        public SettingsMobile.eMainPageCatalogView MainPageCatalogView { get; set; }

        public int? SalesType { get; set; }
    }
}