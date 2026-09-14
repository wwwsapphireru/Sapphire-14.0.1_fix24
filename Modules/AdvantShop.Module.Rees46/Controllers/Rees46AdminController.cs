using AdvantShop.Module.Rees46.Models;
using AdvantShop.Module.Rees46.Domain;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using System;
using System.Web.Mvc;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Configuration;
using AdvantShop.Helpers;
using System.IO;
using AdvantShop.Diagnostics;
using AdvantShop.Module.Rees46.Domain.PartnersApi;
using AdvantShop.Core.UrlRewriter;
using System.Linq;
using System.Web.UI.WebControls;
using AdvantShop.ExportImport;
using System.Collections.Generic;

namespace AdvantShop.Module.Rees46.Controllers
{
    public class Rees46AdminController : ModuleAdminController
    {
        [ChildActionOnly]
        public ActionResult Settings()
        {
            return PartialView("~/modules/" + Rees46.ModuleStringId + "/Views/Admin/_Settings.cshtml");
        }

        [HttpGet]
        public JsonResult GetSettings()
        {
            //GlorySoft_021
            var allFeeds = ExportFeedService.GetExportFeeds().Where(x => x.FeedType == EExportFeedType.YandexMarket);
            var feeds = new List<ExportFeed>() { new ExportFeed() { Id = 0, Name = "не выбран" } };
            feeds.AddRange(allFeeds);

            return JsonOk(new
            {
                /*GlorySoft_021*/Settings = new SettingsModel()
                {
                    // general settings
                    ShopKey = Rees46Settings.ShopKey,
                    Limit = Rees46Settings.Limit,
                    UseSuggestionsInSearch = Rees46Settings.UseSuggestionsInSearch,
                    SettingsUrl = UrlService.GetUrl("adminv3/settingstemplate#?settingsTab=product"),
                    // api
                    RelatedProductCode = Rees46Settings.RelatedProductCode,
                    AlternativeProductCode = Rees46Settings.AlternativeProductCode,
                    MainPageCode = Rees46Settings.MainPageCode,
                    CatalogTopCode = Rees46Settings.CatalogTopCode,
                    CatalogBottomCode = Rees46Settings.CatalogBottomCode,
                    CartCode = Rees46Settings.CartCode,

                    //GlorySoft_021
                    FeedId = Rees46Settings.FeedId,
                    Shedule = Rees46Settings.Shedule,
                },

                //GlorySoft_021
                Feeds = feeds,
                FeedIndex = feeds.FindIndex(x => x.Id == Rees46Settings.FeedId)
            }) ;

        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public ActionResult SaveSettings(SettingsModel model)
        {
            if (!ModelState.IsValid)
                return JsonError();

            // general settings
            Rees46Settings.ShopKey = model.ShopKey;
            Rees46Settings.Limit = model.Limit;
            Rees46Settings.UseSuggestionsInSearch = model.UseSuggestionsInSearch;
            // api
            Rees46Settings.RelatedProductCode = model.RelatedProductCode ?? "";
            Rees46Settings.AlternativeProductCode = model.AlternativeProductCode ?? "";
            Rees46Settings.MainPageCode = model.MainPageCode ?? ""; 
            Rees46Settings.CatalogTopCode = model.CatalogTopCode ?? "";
            Rees46Settings.CatalogBottomCode = model.CatalogBottomCode ?? "";
            Rees46Settings.CartCode = model.CartCode ?? "";

            //GlorySoft_021
            Rees46Settings.FeedId = model.FeedId;
            Rees46Settings.Shedule = model.Shedule;

            Rees46Repository.WriteManifestJson();
            return JsonOk();
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return JsonError();

            var registrationResult = Rees46Service.Registration(new Rees46Customer()
            {
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim()
            });

            if (registrationResult.status == false)
                return JsonError(registrationResult.message);
         
            return JsonOk();
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public ActionResult Export()//GlorySoft_021
        {
            var job = new ExportXmlJob();
            var id = Rees46Settings.FeedId;
            var exportFeed = ExportFeedService.GetExportFeed(id);
            var check = job.Check(exportFeed);
            if (check != string.Empty)
                return JsonError(check);
            job.Execute(null);
            return JsonOk();
        }

    }
}
