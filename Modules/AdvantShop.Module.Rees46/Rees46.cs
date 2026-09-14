//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.FullSearch;
using AdvantShop.Helpers;
using AdvantShop.Module.Rees46.Domain;
using AdvantShop.Module.Rees46.Domain.PartnersApi;
using AdvantShop.Configuration;
using AdvantShop.Diagnostics;
using AdvantShop.Core.Scheduler;
using AdvantShop.ExportImport;

namespace AdvantShop.Module.Rees46
{
    public class Rees46 : IModuleRelatedProducts, ISearch, IRenderModuleByKey, IModuleBundles, IAdminModuleSettings, IModuleTask
    {
        #region Module methods

        public const string ModuleStringId = "Rees46";

        string IModule.ModuleStringId
        {
            get { return ModuleStringId; }
        }

        public bool CheckAlive()
        {
            return true;
        }

        public bool InstallModule()
        {
            return Rees46Repository.InstallModule();
        }

        public bool UninstallModule()
        {
            return Rees46Repository.UninstallModule();
        }
        
        public bool UpdateModule()
        {          
            return Rees46Repository.UpdateModule();
        }
        
        public string ModuleName
        {
            get
            {
                switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
                {
                    case "ru":
                        return "Rees46 - Персональные рекомендации товаров";

                    case "en":
                        return "Rees46";

                    default:
                        return "Rees46";
                }
            }
        }
     

        #endregion

        #region IRenderModuleByKey

        public List<ModuleRoute> GetModuleRoutes()
        {
            return new List<ModuleRoute>()
            {
                new ModuleRoute()
                {
                    Key = "head",
                    ActionName = "GetScript",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "body_end",
                    ActionName = "SuggestionsInSearch",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "shoppingcart_after",
                    ActionName = "ShoppingcartAfter",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "order_success",
                    ActionName = "CheckoutFinalStep",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "product_right",
                    ActionName = "ProductRight",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "category_top",
                    ActionName = "CategoryTop",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "category_bottom",
                    ActionName = "CategoryBottom",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "mainpage_products",
                    ActionName = "MainPage",
                    ControllerName = "Rees46",
                },
                new ModuleRoute()
                {
                    Key = "search_page_top",
                    ActionName = "Search",
                    ControllerName = "Rees46",
                }
            };
        }

        #endregion
        
        #region IModuleRelatedProducts

        public string GetRelatedProductsHtml(Product product, RelatedType relatedType)
        {
            var offer = product.Offers.OrderByDescending(x => x.Main).FirstOrDefault();

            var pageType = relatedType == RelatedType.Related ? PageType.RelatedProduct : PageType.AlternativeProduct;

            return Rees46Service.GetRecomender(pageType, offer != null ? offer.OfferId : 0);           
        }

        public List<ProductModel> GetRelatedProducts(Product product, RelatedType relatedType)
        {
            return null;
        }

        #endregion
        
        #region ISearch

        public string RenderContent(string term)
        {
            return string.Empty;
        }

        public string RenderBottom(string term)
        {
            return Rees46Service.GetRecomender(PageType.Search, searchQuery: term);
        }

        public bool OverrideStandardSearch()
        {
            return false;
        }

        #endregion

        #region IModuleBundles

        public List<string> GetCssBundles()
        {
            return null;
        }

        public List<string> GetJsBundles()
        {
            return new List<string>() {"~/modules/rees46/js/lib.js"};
        }

        #endregion

        #region IAdminModuleSettings
        public bool IsMobileAdminReady => true;

        public List<ModuleSettingTab> AdminSettings
        {
            get
            {
                return new List<ModuleSettingTab>()
                {
                    new ModuleSettingTab()
                    {
                        Title = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ru" ? "Настройки" : "Settings",
                        Controller = "Rees46Admin",
                        Action = "Settings",
                        IsAdaptive = true
                    }
                };
            }
        }

        #endregion

        #region IModuleProductSearchProvaider GlorySoft_010

        public SearchResult Find(string term)
        {
            var result = Rees46PartnerService.GetSearch(term);
            SearchResult found = null;
            if (result?.products != null && result?.products_total > 0)
            {
                var offers = result.products.Select(x => OfferService.GetOffer(x.id.TryParseInt())).Where(x => x != null).ToList();
                found = new SearchResult
                {
                    SearchTerm = term,
                    SearchResultItems = offers.Select(x => new SearchResultItem { Id = x.ProductId }).ToList(),
                    Hits = result.products_total
                };
            }
            //if (found.Hits > 0 || found.SearchResultItems.Count > 0)
            //    return found;
            var defaultSearch = new FullSearch.LuceneProductSearch();
            var f = defaultSearch.Find(term);
            //SearchResult f = null;
            if (found == null)
            {
                found = f;// defaultSearch.Find(term);
            }
            else
            {
                var ff = found.SearchResultItems;
                //var oids = ProductService.GetProductIdsByOfferIds(ff.Select(x => x.Id).ToList());
                found = f;
                var ids = found.SearchResultItems.Select(x => x.Id);
                found.SearchResultItems.AddRange(ff.Where(x => !ids.Contains(x.Id)));
                found.Hits = found.SearchResultItems.Count;
            }
            return found;
        }

        #endregion

        #region IModuleTask GlorySoft_021

        public List<TaskSetting> GetTasks()
        {
            var tasks = new List<TaskSetting>();

            //var exportFeed = ExportFeedService.GetExportFeed(Rees46Settings.FeedId);
            var settings = ExportFeedSettingsProvider.GetSettings(Rees46Settings.FeedId);
            if (settings != null)
            {
                var task = new TaskSetting
                {
                    Enabled = Rees46Settings.Shedule,
                    JobType = typeof(ExportXmlJob).FullName + "," + typeof(ExportXmlJob).Assembly.FullName,
                    TimeType = settings.IntervalType,
                    TimeInterval = settings.Interval,
                    TimeHours = settings.JobStartTime.Hour,
                    TimeMinutes = settings.JobStartTime.Minute
                };
                tasks.Add(task);
            }

            return tasks;
        }

        #endregion

    }
}