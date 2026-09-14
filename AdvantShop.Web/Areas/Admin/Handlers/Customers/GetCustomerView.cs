using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Bonuses.Internal;
using AdvantShop.Core.Services.Booking;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Crm;
using AdvantShop.Core.Services.Crm.Facebook;
using AdvantShop.Core.Services.Crm.Ok;
using AdvantShop.Core.Services.Crm.Telegram;
using AdvantShop.Core.Services.Crm.Vk;
using AdvantShop.Core.Services.Customers;
using AdvantShop.Core.Services.Customers.AdminInformers;
using AdvantShop.Core.Services.CustomerSegments;
using AdvantShop.Core.Services.Partners;
using AdvantShop.Core.Services.Statistic;
using AdvantShop.Customers;
using AdvantShop.Orders;
using AdvantShop.Web.Admin.Models.Customers;

namespace AdvantShop.Web.Admin.Handlers.Customers
{
    public class GetCustomerView
    {
        private readonly Guid _customerId;
        private readonly string _clientCode;
        private readonly bool _isShort;

        public GetCustomerView(Guid customerId, string clientCode, bool isShort)
        {
            _customerId = customerId;
            _clientCode = clientCode;
            _isShort = isShort;
        }

        public CustomerViewModel Execute()
        {
            var customer = CustomerService.GetCustomer(_customerId);
            if (customer == null && _clientCode.IsNotEmpty())
            {
                customer = ClientCodeService.GetCustomerByCode(_clientCode, _customerId);
                customer.Code = _clientCode;
            }

            if (customer == null || !CustomerService.CheckAccess(customer))
                return null;

            AdminInformerService.SetSeen(customer.Id);

            var id = customer.Id;

            var managers = new List<SelectListItem>() { new SelectListItem() { Text = "-", Value = "" } };
            managers.AddRange(ManagerService.GetManagers(RoleAction.Customers).Select(x => new SelectListItem() { Text = x.FullName, Value = x.ManagerId.ToString() }));
            
            var managerId = customer != null && customer.Manager != null ? (int?)customer.Manager.ManagerId : null;

            var referralCustomerId = ReferralService.GetReferralCustomerId(id);
            
            if (_isShort)
            {
                return new CustomerViewModel()
                {
                    Customer = customer,

                    VkUser = VkService.GetUser(id),
                    FacebookUser = FacebookService.GetUser(id),
                    TelegramUser = new TelegramService().GetUser(id),
                    OkUser = OkService.GetUser(id),

                    ShoppingCart = ShoppingCartService.GetShoppingCartFromDb(ShoppingCartType.ShoppingCart, customer.Id, false),

                    BonusCard = BonusSystem.GetCard(customer),
                    InternalBonusCard = BonusSystem.IsInternal && BonusSystem.IsActive ? InternalBonusSystemService.GetCard(id) : null,

                    CustomerSegments = CustomerSegmentService.GetListByCustomerId(id),

                    ManagerList = managers,
                    ManagerId = managerId,

                    ReferralCustomer = referralCustomerId.HasValue ? CustomerService.GetCustomer(referralCustomerId.Value) : null
                };
            }

            var sw = new Stopwatch();
            sw.Start();

            var model = new CustomerViewModel
            {
                Customer = customer,

                CustomerFields = CustomerFieldService.GetMappedCustomerFieldsWithValue(id)
                    .Where(x => !string.IsNullOrEmpty(x.Value) && (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All)).ToList(),
                CustomerInterestingCategories = StatisticService.GetCustomerInterestingCategories(id),

                AllOrdersCount = StatisticService.GetCustomerOrdersCount(id, false),
                LeadsCount = LeadService.GetLeadsCount(id),
                BookingsCount = BookingService.GetCustomerBookingsCount(id, false),

                ManagerList = managers,
                ManagerId = managerId,

                ReferralCustomer = referralCustomerId.HasValue ? CustomerService.GetCustomer(referralCustomerId.Value) : null
            };

            var count = StatisticService.GetCustomerOrdersCount(id);
            var sum = count > 0 ? StatisticService.GetCustomerOrdersSum(id) : 0;

            model.OrdersSum = sum.FormatPrice();
            model.OrdersCount = count;
            model.AverageCheck = (count > 0 ? (sum / count) : 0).FormatPrice();

            model.DurationDate = customer.RegistrationDateTime.GetDurationString(DateTime.Now);

            var partnerBinding = PartnerService.GetBindedCustomer(id);
            model.PartnerInfo = partnerBinding == null ? null : new PartnerInfoModel
            {
                PartnerId = partnerBinding.PartnerId,
                PartnerName = partnerBinding.Partner.Name,
                PartnerEnabled = partnerBinding.Partner.Enabled,
                DateBinded = partnerBinding.DateCreated
            };

            model.CustomerFcmToken = CustomerService.GetFcmToken(id);

            sw.Stop();
            model.Elapsed = sw.Elapsed.TotalMilliseconds;

            //GlorySoft_020
            var hash = CustomerService.GetRegCode(customer.Id);
            //var c = CustomerService.GetCustomerFromDb(customer.Id);
            model.Enabled = customer.Enabled;
            if (!model.Enabled)
                model.RegConfirmUrl = Core.UrlRewriter.UrlService.GetUrl() + $"confirmregistration/{hash}";
            else
                model.PhoneConfirmed = customer.CustomerType == CustomerType.LegalEntity ? true : CustomerService.GetConfirmPhone(customer.Id.ToString());
            if (CustomerContext.CurrentCustomer?.IsAdmin == true)
            {
                var lk = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{customer.EMail}&&{customer.Password}"));
                model.AccountUrl = Core.UrlRewriter.UrlService.GetUrl() + $"accountforadmin/{lk}";
            }

            return model;
        }
    }
}
