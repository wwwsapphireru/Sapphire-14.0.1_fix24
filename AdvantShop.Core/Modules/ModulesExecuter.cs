//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Core.Services.Bonuses.Internal.Model;
using AdvantShop.Core.Services.Crm;
using AdvantShop.Core.Services.Loging;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Orders;

namespace AdvantShop.Core.Modules
{
    public class ModulesExecuter
    {
        #region PictureModules

        public static void ProcessPhoto(Image image) =>
            TryProcessByModules<IProcessPhoto>(module => { module.DoProcessPhoto(image); });

        #endregion

        #region OrderModules

        public static void OrderAdded(IOrder order) =>
            TryProcessByModules<IOrderChanged>(module => { module.DoOrderAdded(order); });

        public static void OrderChangeStatus(int orderId)
        {
            var order = OrderService.GetOrder(orderId);

            TryProcessByModules<IOrderChanged>(module => { module.DoOrderChangeStatus(order); });
        }

        public static void OrderUpdated(IOrder order) =>
            TryProcessByModules<IOrderChanged>(module => { module.DoOrderUpdated(order); });

        public static void OrderDeleted(int orderId) =>
            TryProcessByModules<IOrderChanged>(module => { module.DoOrderDeleted(orderId); });


        public static void PayOrder(int orderId, bool payed) =>
            TryProcessByModules<IOrderChanged>(module => { module.PayOrder(orderId, payed); });

        public static void UpdateComments(int orderId) =>
            TryProcessByModules<IOrderChanged>(module => { module.UpdateComments(orderId); });

        public static void OrderItemAdded(IOrderItem item) =>
            TryProcessByModules<IOrderChanged>(module => { module.DoOrderItemAdded(item); });

        public static void OrderItemUpdated(IOrderItem item) =>
            TryProcessByModules<IOrderChanged>(module => { module.DoOrderItemUpdated(item); });

        public static void OrderItemDeleted(IOrderItem item) =>
            TryProcessByModules<IOrderChanged>(module => { module.DoOrderItemDeleted(item); });

        #endregion

        #region CustomerActions

        public static void AddToCart(ShoppingCartItem item, string url = "")
        {
            TryProcessByModules<ICustomerAction>(module => { module.AddToCart(item, url); });

            LoggingManager.GetCustomerActionLogger()?.AddToCart(item, url);
        }

        public static void AddToCompare(ShoppingCartItem item, string url = "")
        {
            TryProcessByModules<ICustomerAction>(module => { module.AddToCompare(item, url); });

            LoggingManager.GetCustomerActionLogger()?.AddToCompare(item, url);
        }

        public static void AddToWishList(ShoppingCartItem item, string url = "")
        {
            TryProcessByModules<ICustomerAction>(module => { module.AddToWishList(item, url); });

            LoggingManager.GetCustomerActionLogger()?.AddToWishList(item, url);
        }

        public static void Subscribe(Subscription subscription, string objectId = "")
        {
            TryProcessByModules<ICustomerAction>(module => { module.Subscribe(subscription.Email); });

            LoggingManager.GetCustomerActionLogger()?.Subscribe(subscription.Email);

            TryProcessByModules<ISendMails>(module =>
            {
                if (string.IsNullOrEmpty(objectId))
                    module.SubscribeEmail(subscription);
                else
                    module.SubscribeEmail(subscription, objectId);
            });
        }

        public static void UnSubscribe(string email)
        {
            TryProcessByModules<ICustomerAction>(module => { module.UnSubscribe(email); });

            LoggingManager.GetCustomerActionLogger()?.UnSubscribe(email);

            TryProcessByModules<ISendMails>(module => { module.UnsubscribeEmail(email); });
        }

        public static void Search(string searchTerm, int resultsCount)
        {
            TryProcessByModules<ICustomerAction>(module => { module.Search(searchTerm, resultsCount); });

            LoggingManager.GetCustomerActionLogger()?.Search(searchTerm, resultsCount);
        }

        public static void Registration(Customer customer)
        {
            TryProcessByModules<ICustomerAction>(module => { module.Register(customer); });

            LoggingManager.GetCustomerActionLogger()?.Register(customer);
        }

        public static void Login(Customer customer) =>
            TryProcessByModules<ICustomerAction>(module => { module.Login(customer); });

        public static void ViewMyAccount(Customer customer)
        {
            TryProcessByModules<ICustomerAction>(module => { module.ViewMyAccount(customer); });

            LoggingManager.GetCustomerActionLogger()?.ViewMyAccount(customer);
        }

        public static void FilterCatalog()
        {
            TryProcessByModules<ICustomerAction>(module => { module.FilterCatalog(); });

            LoggingManager.GetCustomerActionLogger()?.FilterCatalog();
        }

        public static void Vote()
        {
            TryProcessByModules<ICustomerAction>(module => { module.Vote(); });

            LoggingManager.GetCustomerActionLogger()?.Vote();
        }

        public static int GetDefaultCustomerGroup()//GlorySoft_003
        {
            var defaultCustomerGroup = CustomerGroupService.DefaultCustomerGroup;
            foreach (var cls in AttachedModules.GetModules<ICustomerAction>().Union(AttachedModules.GetCore<ICustomerAction>()))
            {
                var classInstance = (ICustomerAction)Activator.CreateInstance(cls, null);
                defaultCustomerGroup = classInstance.GetDefaultCustomerGroup();
            }
            return defaultCustomerGroup;
        }

        #endregion

        #region ISendOrderNotifications

        public static void SendNotificationsOnOrderAdded(IOrder order) =>
            TryProcessByModules<ISendOrderNotifications>(module => { module.SendOnOrderAdded(order); });

        public static void SendNotificationsOnOrderChangeStatus(IOrder order) =>
            TryProcessByModules<ISendOrderNotifications>(module => { module.SendOnOrderChangeStatus(order); });


        public static bool SendNotificationsHasTemplatesOnChangeStatus(int orderStatusId)
        {
            bool res = false;

            TryProcessByModules<ISendOrderNotifications>(module => { res |= module.HaveSmsTemplate(orderStatusId); });

            return res;
        }


        public static void SendNotificationsOnOrderUpdated(IOrder order) =>
            TryProcessByModules<ISendOrderNotifications>(module => { module.SendOnOrderUpdated(order); });

        public static void SendNotificationsOnOrderDeleted(int orderId) =>
            TryProcessByModules<ISendOrderNotifications>(module => { module.SendOnOrderDeleted(orderId); });

        public static void SendNotificationsOnPayOrder(int orderId, bool payed) =>
            TryProcessByModules<ISendOrderNotifications>(module => { module.SendOnPayOrder(orderId, payed); });

        #endregion

        #region ICustomerChange

        public static void AddCustomer(Customer customer) =>
            TryProcessByModules<ICustomerChange>(module => { module.Add(customer); }, true);

        public static void UpdateCustomer(Customer customer) =>
            TryProcessByModules<ICustomerChange>(module => { module.Update(customer); }, true);

        public static void UpdateCustomer(Guid customerId) =>
            TryProcessByModules<ICustomerChange>(module => { module.Update(CustomerService.GetCustomer(customerId)); },
                true);

        public static void DeleteCustomer(Guid customerId) =>
            TryProcessByModules<ICustomerChange>(module => { module.Delete(customerId); }, true);

        #endregion

        #region IContactChange

        public static void AddContact(CustomerContact contact) =>
            TryProcessByModules<IContactChange>(module => { module.Add(contact); }, true);

        public static void UpdateContact(CustomerContact contact) =>
            TryProcessByModules<IContactChange>(module => { module.Update(contact); }, true);

        public static void DeleteContact(Guid contactId) =>
            TryProcessByModules<IContactChange>(module => { module.Delete(contactId); }, true);

        #endregion

        #region Lead

        public static void LeadAdded(Lead lead) =>
            TryProcessByModules<ILeadChanged>(module => { module.LeadAdded(lead); });

        public static void LeadUpdated(Lead lead) =>
            TryProcessByModules<ILeadChanged>(module => { module.LeadUpdated(lead); });

        public static void LeadDeleted(int leadId) =>
            TryProcessByModules<ILeadChanged>(module => { module.LeadDeleted(leadId); });

        #endregion

        #region CheckInfo

        public static bool CheckInfo(
            HttpContext currentContext,
            ECheckType checkType,
            string senderEmail,
            string senderNickname,
            string message = "",
            string phone = ""
        )
        {
            var result = true;

            TryProcessByModules<ICheckInfo>(module =>
            {
                result &= module.CheckInfo(currentContext, checkType, senderEmail, senderNickname, message, phone);
            });

            return result;
        }

        #endregion

        #region VirtualCategory

        public static Category GetVirtualCategory(Category category)
        {
            foreach (var cls in AttachedModules.GetModules<IVirtualCategory>().Union(AttachedModules.GetCore<IVirtualCategory>()))
            {
                var classInstance = (IVirtualCategory)Activator.CreateInstance(cls);
                return classInstance.GetVirtualCategory(category);
            }
            return category;
        }

        public static ICategoryModel GetVirtualCategoryModel(ICategoryModel model)
        {
            foreach (var cls in AttachedModules.GetModules<IVirtualCategory>().Union(AttachedModules.GetCore<IVirtualCategory>()))
            {
                var classInstance = (IVirtualCategory)Activator.CreateInstance(cls);
                return classInstance.GetVirtualCategoryModel(model);
            }
            return model;
        }

        public static List<BreadCrumbs> GetVirtualCategoryBreadCrumbs(List<BreadCrumbs> breadCrumbs)
        {
            foreach (var cls in AttachedModules.GetModules<IVirtualCategory>().Union(AttachedModules.GetCore<IVirtualCategory>()))
            {
                var classInstance = (IVirtualCategory)Activator.CreateInstance(cls);
                return classInstance.GetVirtualCategoryBreadCrumbs(breadCrumbs);
            }
            return breadCrumbs;
        }

        public static Dictionary<int, KeyValuePair<float, float>> GetRangeIds(Dictionary<int, KeyValuePair<float, float>> rangeIds)
        {
            foreach (var cls in AttachedModules.GetModules<IVirtualCategory>().Union(AttachedModules.GetCore<IVirtualCategory>()))
            {
                var classInstance = (IVirtualCategory)Activator.CreateInstance(cls);
                return classInstance.GetRangeIds(rangeIds);
            }
            return rangeIds;
        }

        public static string GetUrlParentCategory(string url)
        {
            foreach (var cls in AttachedModules.GetModules<IVirtualCategory>().Union(AttachedModules.GetCore<IVirtualCategory>()))
            {
                var classInstance = (IVirtualCategory)Activator.CreateInstance(cls);
                return classInstance.GetUrlParentCategory(url);
            }
            return url;
        }

        #endregion

        #region IGeoIp

        public static List<Repository.IpZone> GetIpZonesAutocomplete(string q, bool inAdminPart = false)
        {
            foreach (var cls in AttachedModules.GetModules<IGeoIp>())
            {
                var classInstance = (IGeoIp)Activator.CreateInstance(cls, null);
                var ipZones = classInstance.GetIpZonesAutocomplete(q, inAdminPart);
                if (ipZones != null)
                    return ipZones;
            }
            return new List<Repository.IpZone>();
        }

        public static void OnSetZone(Repository.IpZone ipZone)
        {
            foreach (var cls in AttachedModules.GetModules<IGeoIp>())
            {
                var classInstance = (IGeoIp)Activator.CreateInstance(cls, null);
                classInstance.OnSetZone(ipZone);
            }
        }

        #endregion

        #region IGeocoder

        /// <summary>
        /// Прямое геокодирование адреса гео-объекта в координаты
        /// </summary>
        /// <remarks>Возвращает первый найденный результат</remarks>
        /// <param name="address">Адрес гео-объекта</param>
        /// <param name="yaMapsApiKey">Ключ яндекс карт, для использования встроенного геокодера</param>
        /// <returns></returns>
        public static GeocoderMetaData Geocode(string address, string yaMapsApiKey = null)
        {
            foreach (var cls in AttachedModules.GetModules<IGeocoder>()
                                               .Union(AttachedModules.GetCore<IGeocoder>()))
            {
                var classInstance = cls == typeof(Geocoder.Yandex.YandexIGeocoder)
                    ? (IGeocoder) Activator.CreateInstance(cls, yaMapsApiKey)
                    : (IGeocoder) Activator.CreateInstance(cls, null);

                var geocoderMetaData = classInstance.Geocode(address);

                if (geocoderMetaData != default)
                    return geocoderMetaData;
            }

            return default;
        }

        /// <summary>
        /// Прямое геокодирование адреса гео-объекта в координаты
        /// </summary>
        /// <remarks>
        /// Возвращает все найденные результаты.<br />
        /// Важно: как правило данный процесс является платным, лучше пользоваться методом <see cref="Geocode(string, string)"/>, <see cref="Geocode(string, Kind, string)"/>, <see cref="Geocode(string, Precision, string)"/> или <see cref="Geocode(string, Func&lt;GeocoderMetaData,bool&gt;, string)"/>
        /// </remarks>
        /// <param name="address">Адрес гео-объекта</param>
        /// <param name="yaMapsApiKey">Ключ яндекс карт, для использования встроенного геокодера</param>
        /// <returns></returns>
        public static List<GeocoderMetaData> GeocodeAll(string address, string yaMapsApiKey = null)
        {
            var results = new List<GeocoderMetaData>();
            foreach (var cls in AttachedModules.GetModules<IGeocoder>()
                                               .Union(AttachedModules.GetCore<IGeocoder>()))
            {
                var classInstance = cls == typeof(Geocoder.Yandex.YandexIGeocoder)
                    ? (IGeocoder) Activator.CreateInstance(cls, yaMapsApiKey)
                    : (IGeocoder) Activator.CreateInstance(cls, null);

                var geocoderMetaData = classInstance.Geocode(address);

                if (geocoderMetaData != default)
                    results.Add(geocoderMetaData);
            }

            return results;
        }

        /// <summary>
        /// Прямое геокодирование адреса гео-объекта в координаты
        /// </summary>
        /// <remarks>
        /// <para>Возвращает первый найденный результат соответствующий <paramref name="kindTo"/> не равный
        /// <see cref="AdvantShop.Core.Modules.Interfaces.Kind.None"/> и <see cref="AdvantShop.Core.Modules.Interfaces.Kind.Other"/>.</para>
        /// <para><paramref name="kindTo"/>:</para>
        /// <list type="bullet">
        ///    <item>
        ///        <term>меньше ли равно <see cref="AdvantShop.Core.Modules.Interfaces.Kind.Country"/> — возвращает либо меньший либо равный значению <paramref name="kindTo"/></term>
        ///    </item>
        ///    <item>
        ///        <term>другие значения — возвращает равный <paramref name="kindTo"/></term>
        ///    </item>
        ///</list>
        /// </remarks>
        /// <param name="address">Адрес гео-объекта</param>
        /// <param name="kindTo">Точность найденного гео-объекта.</param>
        /// <param name="yaMapsApiKey">Ключ яндекс карт, для использования встроенного геокодера</param>
        /// <returns></returns>
        public static GeocoderMetaData Geocode(string address, Kind kindTo, string yaMapsApiKey = null)
        {
            return Geocode(
                address,
                geocoderMetaData =>
                {
                    if (geocoderMetaData.Kind == Kind.None
                        || geocoderMetaData.Kind == Kind.Other)
                        return false;

                    if (kindTo <= Kind.Country
                        && geocoderMetaData.Kind > kindTo)
                        return false;

                    if (kindTo > Kind.Country
                        && geocoderMetaData.Kind != kindTo)
                        return false;

                    return true;

                },
                yaMapsApiKey);
        }

        /// <summary>
        /// Прямое геокодирование адреса гео-объекта в координаты
        /// </summary>
        /// <remarks>
        /// <para>Возвращает первый найденный результат равный <paramref name="precision"/> или более точный, но не равный
        /// <see cref="AdvantShop.Core.Modules.Interfaces.Precision.None"/>.</para>
        /// </remarks>
        /// <param name="address">Адрес гео-объекта</param>
        /// <param name="precision">Точность координат</param>
        /// <param name="yaMapsApiKey">Ключ яндекс карт, для использования встроенного геокодера</param>
        /// <returns></returns>
        public static GeocoderMetaData Geocode(string address, Precision precision, string yaMapsApiKey = null)
        {
            return Geocode(
                address,
                geocoderMetaData =>
                {
                    if (geocoderMetaData.Precision == Precision.None)
                        return false;

                    if (geocoderMetaData.Precision > precision)
                        return false;

                    return true;

                },
                yaMapsApiKey);
        }
        
        /// <summary>
        /// Прямое геокодирование адреса гео-объекта в координаты
        /// </summary>
        /// <remarks>
        /// <para>Возвращает первый найденный результат равный <paramref name="precision"/> или более точный, но не равный
        /// <see cref="AdvantShop.Core.Modules.Interfaces.Precision.None"/>.</para>
        /// </remarks>
        /// <param name="address">Адрес гео-объекта</param>
        /// <param name="filter">Функция фильтрующая необходимый результат (<see cref="bool.True"/> - подходит, <see cref="bool.False"/> - не подходит)</param>
        /// <param name="yaMapsApiKey">Ключ яндекс карт, для использования встроенного геокодера</param>
        /// <returns></returns>
        public static GeocoderMetaData Geocode(string address, Func<GeocoderMetaData,bool> filter, string yaMapsApiKey = null)
        {
            foreach (var cls in AttachedModules.GetModules<IGeocoder>()
                                               .Union(AttachedModules.GetCore<IGeocoder>()))
            {
                var classInstance = cls == typeof(Geocoder.Yandex.YandexIGeocoder)
                    ? (IGeocoder) Activator.CreateInstance(cls, yaMapsApiKey)
                    : (IGeocoder) Activator.CreateInstance(cls, null);

                var geocoderMetaData = classInstance.Geocode(address);

                if (geocoderMetaData != default)
                {
                    if (filter(geocoderMetaData))
                        return geocoderMetaData;
                }
            }

            return default;
        }


        /// <summary>
        /// Обратное геокодирование координат гео-объекта в адрес
        /// </summary>
        /// <remarks>Возвращает первый найденный результат</remarks>
        /// <param name="point">Координаты</param>
        /// <param name="yaMapsApiKey">Ключ яндекс карт, для использования встроенного геокодера</param>
        /// <returns>Данные адреса по координатам</returns>
        public static ReverseGeocoderData ReverseGeocode(Interfaces.Point point, string yaMapsApiKey = null)
        {
            foreach (var cls in AttachedModules.GetModules<IGeocoder>()
                                               .Union(AttachedModules.GetCore<IGeocoder>()))
            {
                var classInstance = cls == typeof(Geocoder.Yandex.YandexIGeocoder)
                    ? (IGeocoder) Activator.CreateInstance(cls, yaMapsApiKey)
                    : (IGeocoder) Activator.CreateInstance(cls, null);
                
                var geocoderMetaData = classInstance.ReverseGeocode(point);

                if (geocoderMetaData != default)
                    return geocoderMetaData;
            }

            return default;
        }

        #endregion IGeocoder

        #region ISuggestions

        public static void ProcessCheckoutAddress(CheckoutAddressQueryModel address) =>
            TryProcessByModules<ISuggestions>(module => { module.ProcessCheckoutAddress(address); });

        public static void ProcessAddress(SuggestAddressQueryModel model, bool inAdminPart = false) =>
            TryProcessByModules<ISuggestions>(module => { module.ProcessAddress(model, inAdminPart); });

        public static void GetSuggestionsHtmlAttributes(
            int customerFieldId,
            Dictionary<string, object> htmlAttributes,
            CustomerFieldAssignment fieldAssignment,
            string onSelectFunc
        ) => TryProcessByModules<ISuggestions>(module =>
        {
            module.GetSuggestionsHtmlAttributes(customerFieldId, htmlAttributes, fieldAssignment, onSelectFunc);
        });

        #endregion

        #region IIgnoreCheckoutShipping

        public static List<ShoppingCartItem> GetIgnoreShippingCartItems()
        {
            var result = new List<ShoppingCartItem>();

            TryProcessByModules<IIgnoreCheckoutShipping>(module =>
            {
                var ids = module.GetIgnoreShippingCartItems();
                if (ids != null && ids.Count > 0)
                    result.AddRange(ids);
            });

            return result;
        }

        #endregion

        #region IShippingMethod

        public static List<Services.Configuration.ListItemModel> GetDropdownShippings()
        {
            return Caching.CacheManager.Get("ModulesDropdownShippings", () =>
                AttachedModules.GetModules<IShippingMethod>(ignoreActive: true) // из-за ignoreActive можно в кэш закидывать
                .Where(module => module != null)
                .Select(module => (IShippingMethod)Activator.CreateInstance(module))
                .Select(moduleInstance =>
                    new Services.Configuration.ListItemModel
                    {
                        Value = moduleInstance.ShippingKey,
                        Text = moduleInstance.ShippingName
                    })
                .ToList());
        }

        #endregion

        #region IPaymentMethod

        public static List<Services.Configuration.ListItemModel> GetDropdownPayments()
        {
            return Caching.CacheManager.Get("ModulesDropdownPayments", () =>
                AttachedModules.GetModules<IPaymentMethod>(ignoreActive: true) // из-за ignoreActive можно в кэш закидывать
                .Where(module => module != null)
                .Select(module => (IPaymentMethod)Activator.CreateInstance(module))
                .Select(moduleInstance =>
                    new Services.Configuration.ListItemModel
                    {
                        Value = moduleInstance.PaymentKey,
                        Text = moduleInstance.PaymentName
                    })
                .ToList());
        }

        #endregion

        #region ITaskChanged

        public static void DoTaskAdded(ITask task, ICustomer managerToNotify) =>
            TryProcessByModules<ITaskChanged>(module => { module.DoTaskAdded(task, managerToNotify); });

        public static void DoTaskChanged(ITask oldTask, ITask newTask, ICustomer managerToNotify) =>
            TryProcessByModules<ITaskChanged>(module => { module.DoTaskChanged(oldTask, newTask, managerToNotify); });

        public static void DoTaskCommentAdded(ITask task, IAdminComment comment, ICustomer managerToNotify) =>
            TryProcessByModules<ITaskChanged>(module => { module.DoTaskCommentAdded(task, comment, managerToNotify); });

        #endregion

        #region IModuleKeyValue

        public static string GetModuleKeyValues(string key)
        {
            var result = new StringBuilder();
            foreach (var type in AttachedModules.GetModules<IModuleKeyValue>())
            {
                var module = (IModuleKeyValue)Activator.CreateInstance(type);
                var moduleKeyValues = module.GetKeyValues();
                if (moduleKeyValues != null)
                {
                    foreach (var keyValue in moduleKeyValues.Where(m => m.Key == key))
                    {
                        result.Append(keyValue.GetValue());
                    }
                }
            }

            return result.ToString();
        }

        #endregion

        #region IBonusesChanged

        public static void BonusCardAdded(Card card) =>
            TryProcessByModules<IBonusesChanged>(module => { module.DoBonusCardAdded(card); });

        public static void BonusCardUpdated(Card card) =>
            TryProcessByModules<IBonusesChanged>(module => { module.DoBonusCardUpdated(card); });

        public static void BonusCardDeleted(Guid cardId) =>
            TryProcessByModules<IBonusesChanged>(module => { module.DoBonusCardDeleted(cardId); });

        public static void CreateTransaction(Transaction transaction) =>
            TryProcessByModules<IBonusesChanged>(module => { module.DoCreateTransaction(transaction); });

        #endregion

        #region IMarketplaceModule

        public static bool ShowMarketplaceProductButton(int productId, bool inAdminPart = false)
        {
            foreach (var type in AttachedModules.GetModules<IMarketplaceModule>())
            {
                var module = (IMarketplaceModule)Activator.CreateInstance(type);
                var haveButton = module.IsHaveProductButton(productId, inAdminPart);
                if (haveButton)
                    return true;
            }

            return false;
        }

        #endregion

        #region IOrderManagement

        public static List<OrderGridAction> GetOrderGridActions()
        {
            var actions = new List<OrderGridAction>();

            TryProcessByModules<IOrderManagement>(module =>
            {
                var gridActions = module.GetOrderGridActions();
                if (gridActions != null)
                    actions.AddRange(gridActions);
            }, true);

            return actions;
        }

        #endregion

        #region IOnAppliedCustomerCoupon

        public static void AddedCustomerCoupon(int couponId, Guid customerId) =>
            TryProcessByModules<IOnAppliedCustomerCoupon>(module =>
            {
                module.AddedCustomerCoupon(couponId, customerId);
            });

        public static void DeletedCustomerCoupon(int couponId, Guid customerId) =>
            TryProcessByModules<IOnAppliedCustomerCoupon>(module =>
            {
                module.DeletedCustomerCoupon(couponId, customerId);
            });

        #endregion

        #region IOnSignIn

        public static void CustomerSignIn(Customer customer) =>
            TryProcessByModules<IOnAuthorization>(module =>
            {
                module.CustomerSignIn(customer);
            });

        #endregion

        private static void TryProcessByModules<T>(
            Action<T> action,
            bool makeInstancesWithCore = false,
            [CallerMemberName] string callerName = ""
        ) => ProcessByModules<T>(module =>
        {
            try
            {
                action(module);
            }
            catch (Exception exception)
            {
                var errorInfo = string.Format(
                    "Module: \"{0}\". An exception was thrown during action {1} of interface {2}: \"{3}\"",
                    module.GetType().FullName,
                    callerName,
                    action.Method.GetParameters()[0].ParameterType.Name,
                    exception.Message
                );

                Debug.Log.Error(errorInfo, exception);
            }
        }, makeInstancesWithCore);

        private static void ProcessByModules<T>(Action<T> action, bool makeInstancesWithCore = false) =>
            (makeInstancesWithCore
                ? AttachedModules.GetModuleInstancesWithCore<T>()
                : AttachedModules.GetModuleInstances<T>()
            )?.ForEach(action);
    }
}