using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Configuration.Settings;

namespace AdvantShop.Areas.Mobile.Models.Home
{
    public class MainPageProductsMobileViewModel
    {
        public ProductViewModel Products { get; set; }

        public SettingsMobile.eMainPageCatalogView MainPageCatalogView { get; set; }

        public int? SalesType { get; set; }//GlorySoft_031
    }
}