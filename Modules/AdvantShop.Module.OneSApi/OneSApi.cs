using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Scheduler;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi
{
    public class OneSApi : CustomerAction, IModule, IAdminModuleSettings, IOrderChanged, IModuleTask, IAdminProductTabs, IRenderModuleByKey, IModuleUrlRewrite, ICustomerChange/*, IVirtualCategory/*, IShoppingCartDiscount*/
    {
        public const string ModuleStringId = "OneSApi";
        public const string ModuleName = "API 1С+";

        #region IModule

        string IModule.ModuleName
        {
            get
            {
                return ModuleName;
            }
        }

        string IModule.ModuleStringId { get { return ModuleStringId; } }

        public bool HasSettings
        {
            get { return true; }
        }

        public bool CheckAlive()
        {
            return ModulesRepository.IsInstallModule(ModuleStringId);
        }

        public bool InstallModule()
        {
            return ModuleService.InstallModule();
        }

        public bool UninstallModule()
        {
            return ModuleService.UninstallModule();
        }

        public bool UpdateModule()
        {
            return true;//ModuleService.UpdateModule();
        }

        #endregion

        #region IAdminModuleSettings

        public List<ModuleSettingTab> AdminSettings
        {
            get
            {
                return new List<ModuleSettingTab>
                {
                    new ModuleSettingTab
                    {
                        Title = "Настройки",
                        Controller = "OneSAdmin",
                        Action = "Settings"
                    },
                    new ModuleSettingTab
                    {
                        Title = "Данные к выгрузке",
                        Controller = "OneSAdmin",
                        Action = "Export"
                    },
                    new ModuleSettingTab
                    {
                        Title = "Логи обмена",
                        Controller = "OneSAdmin",
                        Action = "Logs"
                    },
                };
            }
        }

        public bool IsMobileAdminReady => false;

        #endregion

        #region IOrderChanged

        public void DoOrderAdded(IOrder order)
        {
            ExportService.DoOrderAdded(order, Models.Api.ExportType.Order);

            var ord = OrderService.GetOrder(order.OrderID);
            TriggerProcessService.ProcessEvent(ETriggerEventType.OrderCreated, ord);
        }

        public void DoOrderChangeStatus(IOrder order)
        {
            ExportService.DoOrderChangeStatus(order);

            var ord = OrderService.GetOrder(order.OrderID);
            TriggerProcessService.ProcessEvent(ETriggerEventType.OrderStatusChanged, ord);
        }

        /// <summary>
        /// Внимание! Данный метод может быть вызван несколько раз при одном изменении заказа
        /// </summary>
        public void DoOrderUpdated(IOrder order)
        {
            ExportService.DoOrderUpdated(order);
        }

        public void DoOrderDeleted(int orderId)
        {
        }

        public void PayOrder(int orderId, bool payed)
        {
            ExportService.PayOrder(orderId, payed);

            var ord = OrderService.GetOrder(orderId);
            TriggerProcessService.ProcessEvent(ETriggerEventType.OrderPaied, ord);
        }

        public void UpdateComments(int orderId)
        {
        }

        public void DoOrderItemAdded(IOrderItem item)
        {
        }

        public void DoOrderItemUpdated(IOrderItem item)
        {
        }

        public void DoOrderItemDeleted(IOrderItem item)
        {
        }

        #endregion

        #region IModuleTask

        public List<TaskSetting> GetTasks()
        {
            var tasks = new List<TaskSetting>();
            var settings = ModuleService.GetImportExportSettings();

            tasks.Add(new TaskSetting
            {
                Enabled = settings.JobClearLogs,
                JobType = typeof(ClearLogsJob).FullName + "," + typeof(ClearLogsJob).Assembly.FullName,
                TimeInterval = 1,
                TimeHours = 0,
                TimeMinutes = 15,
                TimeType = TimeIntervalType.Days
            });

            return tasks;
        }

        #endregion

        #region IAdminProductTabs

        public IList<AdminProductTabItem> GetAdminProductTabs(int productId)
        {
            return new List<AdminProductTabItem>()
            {
                new AdminProductTabItem("Остатки по филиалам", "ProductDepotsAmounts", "OneSAdmin"),
            };
        }

        #endregion

        #region IRenderModuleByKey

        public List<ModuleRoute> GetModuleRoutes()
        {
            return new List<ModuleRoute>
            {
                new ModuleRoute
                {
                    Key = "product_right_before",
                    ControllerName = "OneS",
                    ActionName = "ProductDepotsAmounts"
                },
                new ModuleRoute
                {
                    Key = "product_right_before_mobile",
                    ControllerName = "OneS",
                    ActionName = "ProductDepotsAmountsMobile"
                },
                new ModuleRoute
                {
                    Key = "product_view_status",
                    ControllerName = "OneS",
                    ActionName = "ProductViewStatus"
                },
                new ModuleRoute()
                {
                    Key = "admin_order_orderinfo",
                    ControllerName = "OneS",
                    ActionName = "AdminOrderInfo"
                },
                new ModuleRoute()
                {
                    Key = "admin_customer_viewinfo",
                    ControllerName = "OneS",
                    ActionName = "AdminCustomerViewInfo"
                },
            };
        }

        #endregion

        #region CustomerAction

        public override void Register(Customer customer)
        {
            var contacts = CustomerService.GetCustomerContacts(customer.Id);
            var country = CountryService.GetCountryByIso2("RU");
            foreach (var item in contacts)
            {
                if (item.CountryId != country.CountryId)
                {
                    item.CountryId = country.CountryId;
                    item.Country = country.Name;
                    CustomerService.UpdateContact(item, false);
                }
            }
            DiscountService.RegisterCustomer(customer);
        }

        public override int GetDefaultCustomerGroup()
        {
            var settings = ModuleService.GetImportExportSettings();
            return settings.ImportCustomer.CustomerGroupId > 0 ? settings.ImportCustomer.CustomerGroupId : CustomerGroupService.DefaultCustomerGroup;
        }

        #endregion

        #region IShoppingCartDiscount

        public float GetDiscount(ShoppingCart cart)
        {
            return DiscountService.GetDiscount(cart);
        }

        #endregion

        #region IModuleUrlRewrite

        public bool RewritePath(string rawUrl, ref string newUrl)
        {
            //if (rawUrl.Contains("feedback_attach"))////
            //{
            //    Debug.Log.Warn(rawUrl);
            //}
            var rawurl = rawUrl.ToLower();
            switch (rawurl)
            {
                //case "/user/registrationjson":
                //    newUrl = "/ones/userregistrationjson";
                //    return true;
                //case "/feedback/feedbackform":
                //    newUrl = "/ones/feedbackForm";
                //    return true;
                //case "/user/loginjson":
                //    newUrl = "/ones/userloginjson";
                //    return true;
                //case "":
                //    newUrl = "/ones/getcitiesautocomplete";
                //    return true;
                default:
                    //if (rawurl.Contains("/location/getcitiesautocomplete"))
                    //{
                    //    newUrl = rawurl.Replace("/location/", "/ones/");
                    //    return true;
                    //}
                    //else if (rawurl.Contains("/productext/getoffers"))
                    //{
                    //    newUrl = rawurl.Replace("/productext/", "/ones/");
                    //    return true;
                    //}
                    return false;
            }
        }

        #endregion

        #region ICustomerChange

        public void Add(Customer customer)
        { }

        public void Update(Customer customer)
        {
            ExportService.CustomerUpdate(customer);
        }

        public void Delete(Guid customerId)
        { }

        #endregion

    }
}
