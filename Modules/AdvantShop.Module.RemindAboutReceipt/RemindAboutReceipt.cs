using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Scheduler;
using AdvantShop.Web.Infrastructure.Extensions;
using System.Collections.Generic;
using System.Globalization;

namespace AdvantShop.Module.RemindAboutReceipt
{
    public class RemindAboutReceipt : IModule, IAdminModuleSettings, IRenderModuleByKey, IModuleTask,
        IModuleChangeActive, IModuleBundles, IAdminBundles
    {
        public static string ModuleName
        {
            get
            {
                return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ru"
                    ? "Узнать о поступлении/снижении цены"
                    : "Remind about receipt/discount";
            }
        }

        string IModule.ModuleName
        {
            get { return ModuleName; }
        }

        public static string ModuleStringId
        {
            get { return "RemindAboutReceipt"; }
        }

        string IModule.ModuleStringId
        {
            get { return ModuleStringId; }
        }

        public bool IsMobileAdminReady => false;

        public List<ModuleSettingTab> AdminSettings
        {
            get
            {
                return new List<ModuleSettingTab>
                {
                    new ModuleSettingTab
                    {
                        Title = "Список заявок на поступление товара",
                        Controller = "RARAdmin",
                        Action = "ClientsListTab"
                    },
                    new ModuleSettingTab
                    {
                        Title = "Поступление товара",
                        Controller = "RARAdmin",
                        Action = "ModuleSettingsReceipt"
                    },
                    new ModuleSettingTab
                    {
                        Title = "Список заявок на снижение цены",
                        Controller = "RARAdmin",
                        Action = "RadClientsListTab"
                    },
                    new ModuleSettingTab
                    {
                        Title = "Снижение цен",
                        Controller = "RARAdmin",
                        Action = "ModuleSettingsDiscount"
                    },
                    new ModuleSettingTab
                    {
                        Title = "Обратная связь",
                        Controller = "RARAdmin",
                        Action = "Feedback"
                    }
                };
            }
        }

        public bool CheckAlive()
        {
            return true;
        }

        #region IModuleChangeActive

        public void ModuleChangeActive(bool active)
        {
            if (!active)
            {
                var taskManager = TaskManager.TaskManagerInstance();
                foreach (var task in GetTasks())
                {
                    taskManager.RemoveModuleTask(task);
                }
            }
            else
            {
                TaskManager.TaskManagerInstance().ManagedTask(TaskSettings.Settings);
            }
        }

        #endregion

        public bool InstallModule()
        {
            return Service.ModuleService.Install();
        }

        public bool UninstallModule()
        {
            return Service.ModuleService.UnInstall();
        }

        public bool UpdateModule()
        {
            return true;
        }

        public List<ModuleRoute> GetModuleRoutes()
        {
            return new List<ModuleRoute>
            {
                new ModuleRoute
                {
                    Key = "product_info",
                    ControllerName = "LandingRARClient",
                    ActionName = "ShowNotificationForm"
                },
                new ModuleRoute
                {
                    Key = "landing_product_info",
                    ControllerName = "LandingRARClient",
                    ActionName = "ShowNotificationForm"
                },
                new ModuleRoute
                {
                    Key = "product_info",
                    ControllerName = "LandingRARClient",
                    ActionName = "RadShowNotificationForm"
                },
                new ModuleRoute
                {
                    Key = "landing_product_info",
                    ControllerName = "LandingRARClient",
                    ActionName = "RadShowNotificationForm"
                },
                new ModuleRoute
                {
                    Key = "landing_body_end",
                    ControllerName = "LandingRARClient",
                    ActionName = "LoadStyles"
                },
                new ModuleRoute
                {
                    Key = "product_view_empty_button",
                    ControllerName="LandingRARClient",
                    ActionName="ShowNotificationFormProductView"

                },
                new ModuleRoute//GlorySoft_012
                {
                    Key= "product_view_altbutton",
                    ControllerName="LandingRARClient",
                    ActionName="ProductView"
                },
                /*new ModuleRoute()
                {
                    Key = "body_end",
                    IsSimpleText = true,
                    Content = "<div data-oc-lazy-load=\"'modules/remindaboutreceipt/content/scripts/client-script.js?" + Service.ModuleSettings.Version + "'\"></div>" +
                               "<link rel=\"stylesheet\" href=\"modules/remindaboutreceipt/content/styles/client-style.css?" + Service.ModuleSettings.Version + "\" />"
                },
                new ModuleRoute()
                {
                    Key = "mobile_body_end",
                    IsSimpleText = true,
                    Content = "<div data-oc-lazy-load=\"'modules/remindaboutreceipt/content/scripts/client-script.js?" + Service.ModuleSettings.Version + "'\"></div>" +
                               "<link rel=\"stylesheet\" href=\"modules/remindaboutreceipt/content/styles/client-style.css?" + Service.ModuleSettings.Version + "\" />"
                }*/
            };
        }

        #region IModuleBundles

        public List<string> GetCssBundles()
        {
            return new List<string>()
            {
                "~/modules/remindaboutreceipt/content/styles/client-style.css?" +
                ModulesExtensions.GetModuleVersion(null, ModuleStringId)
            };
        }

        public List<string> GetJsBundles()
        {
            return new List<string>() { };
        }

        #endregion

        #region IAdminBundles

        public List<string> AdminCssBottom()
        {
            return new List<string>()
            {
                "~/modules/remindaboutreceipt/content/styles/admin-style.css?" +
                ModulesExtensions.GetModuleVersion(null, ModuleStringId)
            };
        }

        public List<string> AdminJsBottom()
        {
            return null;
        }

        #endregion

        public List<TaskSetting> GetTasks()
        {
            return new List<TaskSetting>()
            {
                new TaskSetting()
                {
                    Enabled = true,
                    JobType = typeof(Service.RemindAboutJob).FullName + "," +
                              typeof(Service.RemindAboutJob).Assembly.FullName,
                    TimeType = TimeIntervalType.Hours,
                    TimeInterval = 3
                }
            };
        }
    }
}