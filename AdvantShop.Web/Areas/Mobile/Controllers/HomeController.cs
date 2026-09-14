using System.Web.Mvc;
using System.Web.SessionState;
using AdvantShop.Areas.Mobile.Handlers.Home;
using AdvantShop.Areas.Mobile.Models.Home;
using AdvantShop.Configuration;
using AdvantShop.Core.Services.CMS;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.FilePath;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.InplaceEditor;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Customers;
using AdvantShop.ViewModel.Home;

namespace AdvantShop.Areas.Mobile.Controllers
{
    [SessionState(SessionStateBehavior.Disabled)]
    public class HomeController : BaseMobileController
    {
        // GET: Mobile/Home
        public ActionResult Index()
        {
            var model = new HomeMobileHandler(SettingsDesign.OnePageCatalog).Execute();

            SetMobileTitle(T("MainPage") );
            SetMetaInformation(null, string.Empty);

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult Logo()
        {
            if (string.IsNullOrEmpty(SettingsMain.LogoImageName) && !SettingsMobile.IsMobileTemplateActive)
                return new EmptyResult();

            var width = 0;
            var height = 0;

            if (SettingsMobile.LogoType == SettingsMobile.eLogoType.Desktop.ToString())
            {
                width = SettingsMain.LogoImageWidth;
                height = SettingsMain.LogoImageHeight;
            }
            else if (SettingsMobile.LogoType == SettingsMobile.eLogoType.Mobile.ToString())
            {
                width = SettingsMobile.LogoImageWidth;
                height = SettingsMobile.LogoImageHeight;
            }

            var imgSource = SettingsMobile.LogoType == SettingsMobile.eLogoType.Desktop.ToString()
                ? SettingsMain.LogoImageName
                : SettingsMobile.LogoImageName;
            
            if (imgSource.IsNullOrEmpty() && InplaceEditorService.CanUseInplace(RoleAction.Settings))
            {
                imgSource = UrlService.GetUrl("images/nophoto-logo.png");
            } 
            
            var model = new LogoMobileModel
            {
                LogoAlt = SettingsMain.LogoImageAlt,
                Text = SettingsMobile.DisplayHeaderTitle ? SettingsMain.ShopName : SettingsMobile.HeaderCustomTitle,
                ImgSource = imgSource.IsNotEmpty() ? FoldersHelper.GetPath(FolderType.Pictures, imgSource, false) : null,
                Width = width,
                Height = height
            };

            var isParsedLogoType = SettingsMobile.eLogoType.TryParse(SettingsMobile.LogoType, out SettingsMobile.eLogoType logoType);

            model.LogoType = isParsedLogoType ? logoType : SettingsMobile.eLogoType.Text;

            return PartialView(model);
        }

        [ChildActionOnly]
        public ActionResult Carousel(CarouselOptions options)
        {
            if (!SettingsMobile.DisplaySlider)
                return new EmptyResult();

            var sliders = CarouselService.GetAllCarouselsMainPage(ECarouselPageMode.Mobile);
            if (sliders.Count == 0)
                return new EmptyResult();

            return PartialView("Carousel", new CarouselViewModel() 
            {
                Sliders = sliders,
                Options = options ?? new CarouselOptions()
            });
        }

        [ChildActionOnly]
        public ActionResult MainPageProducts(ProductViewModel products,/*GlorySoft_031*/ int? salesType)
        {
            var model = new MainPageProductsMobileViewModel()
            {
                Products = products,
                MainPageCatalogView = SettingsMobile.MainPageCatalogView,
                SalesType = salesType//GlorySoft_031
            };

            return PartialView("_MainPageProducts", model);
        }

        public ActionResult ToFullVersion()
        {
            // Todo: add cookie

            return RedirectToRoute("Home");
        }
    }
}