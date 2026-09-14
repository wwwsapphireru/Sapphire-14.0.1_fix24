using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.Bonuses.Model;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Landing;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.Mails;
using AdvantShop.Module.OneSApi.Handlers.Client;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Orders;
using AdvantShop.Payment;
using AdvantShop.Repository;
using AdvantShop.Repository.Currencies;
using AdvantShop.Saas;
using AdvantShop.Security;
using AdvantShop.Taxes;
using AdvantShop.Trial;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AdvantShop.Module.OneSApi.Service
{
    public class CheckoutService
    {
        public static string GetOrderPay(Order order, int? paymentMethodId)
        {
            //var order =
            //    orderCode.IsNotEmpty()
            //        ? OrderService.GetOrderByCode(orderCode)
            //        : null;
            // order = order ??
            //         (model.OrderId.HasValue
            //             ? OrderService.GetOrder(model.OrderId.Value)
            //             : null);

            var paymentMethod =
                paymentMethodId.HasValue
                        ? PaymentService.GetPaymentMethod(paymentMethodId.Value)
                        : order?.PaymentMethod;

            if (
                order == null
                || order.Payed
                || order.OrderStatus.IsCanceled
                || paymentMethod is null
                || (SettingsCheckout.ManagerConfirmed && !order.ManagerConfirmed)
               )
                return null;

            if (paymentMethod is ICreditPaymentMethod creditPaymentMethod
                && creditPaymentMethod.ActiveCreditPayment
                && (creditPaymentMethod.MinimumPrice > order.Sum.ConvertCurrency(order.OrderCurrency, paymentMethod.PaymentCurrency ?? order.OrderCurrency)
                    || creditPaymentMethod.MaximumPrice < order.Sum.ConvertCurrency(order.OrderCurrency, paymentMethod.PaymentCurrency ?? order.OrderCurrency)))
                return null;


            if (paymentMethod.ProcessType == ProcessType.FormPost)
            {
                var paymentForm = paymentMethod.GetPaymentForm(order);
                var url = paymentForm.Url + "?";
                foreach (var key in paymentForm.InputValues.AllKeys)
                {
                    url += string.Format("{0}={1}&", key, HttpUtility.UrlEncode(paymentForm.InputValues[key]));
                }
                return url.TrimEnd('&');
            }

            if (paymentMethod.ProcessType == ProcessType.ServerRequest)
            {
                return UrlService.GetUrl("checkout/payredirect/" + order.Code);
            }

            //var viewModel =
            //    new OrderPayHandler(
            //            order,
            //            paymentMethod,
            //            model.PageWithPaymentButton,
            //            model.ValidationDisabled)
            //        .Execute();

            //if (viewModel == null)
            //    return null;

            ////if (
            ////    viewModel.ViewPath != null &&
            ////    ViewEngineCollection.FindPartialView(ControllerContext, viewModel.ViewPath)?.View != null
            ////    )
            ////    return PartialView(viewModel.ViewPath, viewModel);
            ////return PartialView("OrderPay/_Common", viewModel);

            return null;////
        }

        public static Order ProcessOrder(MyCheckout current, string customData, OrderType? orderType, bool isLanding)
        {
            ProcessUser(current.Data);

            var order = CreateOrder(current.Data, current.Cart, customData, orderType, isLanding);

            var certificate = current.Cart.Certificate;
            if (certificate != null)
            {
                certificate.ApplyOrderNumber = order.Number;
                certificate.Used = true;
                certificate.Enable = true;

                GiftCertificateService.DeleteCustomerCertificate(certificate.CertificateId);
                GiftCertificateService.UpdateCertificateById(certificate);
            }

            var coupon = current.Cart.Coupon;
            if (coupon != null && current.Cart.TotalPrice >= coupon.MinimalOrderPrice)
            {
                coupon.ActualUses += 1;
                CouponService.UpdateCoupon(coupon);
                CouponService.DeleteCustomerCoupon(coupon.CouponID);
            }

            ShoppingCartService.ClearShoppingCart(ShoppingCartType.ShoppingCart, current.Data.User.Id);
            ShoppingCartService.ClearShoppingCart(ShoppingCartType.ShoppingCart, CustomerContext.CustomerId);

            OrderConfirmationService.Delete(CustomerContext.CustomerId);

            return order;
        }

        public static void ProcessUser(CheckoutData Data)
        {
            var customer = CustomerContext.CurrentCustomer;

            if (customer.RegistredUser)
            {
                ProcessRegisteredUser(Data, customer);
                return;
            }

            var customerByEmail = !string.IsNullOrEmpty(Data.User.Email) ? CustomerService.GetCustomerByEmail(Data.User.Email) : null;
            if (customerByEmail != null && customerByEmail.Id != Guid.Empty)
            {
                //_linkedCustomerId = Data.User.Id = customerByEmail.Id;
                if (BonusSystem.IsActive)
                {
                    var bonusCard = BonusSystemService.GetCard(customerByEmail.Id);
                    if (bonusCard != null)
                        Data.User.BonusCardId = bonusCard.CardId;
                }
                return;
            }

            ProcessUnRegisteredUser(Data);
        }

        private static void ProcessUnRegisteredUser(CheckoutData Data)
        {
            try
            {
                if (!Data.User.WantRegist)
                    Data.User.Password = StringHelper.GeneratePassword(8);

                var settings = ModuleService.GetImportExportSettings();
                var customer = new Customer(settings.ImportCustomer.CustomerGroupId > 0 ? settings.ImportCustomer.CustomerGroupId : CustomerGroupService.DefaultCustomerGroup)
                {
                    Id = CustomerContext.CustomerId,
                    Password = Data.User.Password,
                    FirstName = Data.User.FirstName,
                    LastName = Data.User.LastName,
                    Patronymic = Data.User.Patronymic,
                    Phone = Data.User.Phone,
                    StandardPhone = StringHelper.ConvertToStandardPhone(Data.User.Phone),
                    SubscribedForNews = true,
                    EMail = Data.User.Email,
                    CustomerRole = Role.User,
                    BirthDay = SettingsCheckout.IsShowBirthDay ? Data.User.BirthDay : null,
                    CustomerType = Data.User.CustomerType,
                    IsAgreeForPromotionalNewsletter = Data.User.IsAgreeForPromotionalNewsletter ??
                                                      SettingsDesign.IsAgreeForPromotionalNewsletterDefaultCustomerValue
                };

                CustomerService.InsertNewCustomer(customer, Data.User.CustomerFields);
                if (customer.Id == Guid.Empty)
                    return;

                ModulesExecuter.Registration(customer);

                if (Data.User.WantRegist && BonusSystem.IsActive &&
                    (!SaasDataService.IsSaasEnabled || SaasDataService.CurrentSaasData.BonusSystem))
                {
                    CreateBonusCard(Data, customer);
                }

                Data.User.Id = customer.Id;

                AuthorizeService.SignIn(customer.EMail, customer.Password, false, true);

                var country = !string.IsNullOrWhiteSpace(Data.Contact.Country)
                    ? CountryService.GetCountryByName(Data.Contact.Country)
                    : null;

                var contact = new CustomerContact()
                {
                    Name = customer.GetFullName(),
                    Country = Data.Contact.Country,
                    CountryId = country?.CountryId ?? 0,
                    Region = Data.Contact.Region,
                    District = Data.Contact.District,
                    City = Data.Contact.City,
                    Zip = Data.Contact.Zip,

                    Street = Data.Contact.Street,
                    House = Data.Contact.House,
                    Apartment = Data.Contact.Apartment,
                    Structure = Data.Contact.Structure,
                    Entrance = Data.Contact.Entrance,
                    Floor = Data.Contact.Floor
                };

                CustomerService.AddContact(contact, customer.Id);

                if (Data.User.WantRegist && !string.IsNullOrEmpty(customer.EMail))
                {
                    var mail = new RegistrationMailTemplate(customer);

                    MailService.SendMailNow(CustomerContext.CustomerId, customer.EMail, mail);
                    MailService.SendMailNow(SettingsMail.EmailForRegReport, mail, replyTo: customer.EMail);
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        private static void ProcessRegisteredUser(CheckoutData Data, Customer customer)
        {
            try
            {
                var user = Data.User;

                if (string.IsNullOrEmpty(user.Email))
                    user.Email = customer.EMail;

                //var needUpdateCustomer = false;

                //if (!string.IsNullOrWhiteSpace(user.FirstName) && customer.FirstName != user.FirstName)
                //{
                //    customer.FirstName = user.FirstName;
                //    needUpdateCustomer = true;
                //}

                //if (!string.IsNullOrWhiteSpace(user.LastName) && customer.LastName != user.LastName)
                //{
                //    customer.LastName = user.LastName;
                //    needUpdateCustomer = true;
                //}

                //if (!string.IsNullOrWhiteSpace(user.Patronymic) && customer.Patronymic != user.Patronymic)
                //{
                //    customer.Patronymic = user.Patronymic;
                //    needUpdateCustomer = true;
                //}

                //if (!string.IsNullOrWhiteSpace(user.Phone) && customer.Phone != user.Phone)
                //{
                //    var standardPhone = !string.IsNullOrEmpty(user.Phone)
                //        ? StringHelper.ConvertToStandardPhone(user.Phone)
                //        : null;

                //    if (!CustomersService.IsPhoneExist(user.Phone, standardPhone))
                //    {
                //        customer.Phone = user.Phone;
                //        customer.StandardPhone = standardPhone;

                //        needUpdateCustomer = true;
                //    }
                //}

                //if (user.IsAgreeForPromotionalNewsletter != null && user.IsAgreeForPromotionalNewsletter.Value &&
                //    !customer.IsAgreeForPromotionalNewsletter)
                //{
                //    customer.IsAgreeForPromotionalNewsletter = true;
                //    needUpdateCustomer = true;
                //}

                //if (SettingsCheckout.IsShowBirthDay && user.BirthDay != null && user.BirthDay != customer.BirthDay)
                //{
                //    customer.BirthDay = user.BirthDay;
                //    needUpdateCustomer = true;
                //}

                //if (customer.BonusCardNumber == null && user.BonusCardId != null)
                //{
                //    var card = BonusSystemService.GetCard(user.BonusCardId);
                //    if (card != null && !card.Blocked)
                //    {
                //        customer.BonusCardNumber = card.CardNumber;
                //        needUpdateCustomer = true;
                //    }
                //}

                //if (needUpdateCustomer)
                //    CustomerService.UpdateCustomer(customer);

                var name = StringHelper.AggregateStrings(" ", user.LastName, user.FirstName, user.Patronymic);
                if (customer.Contacts.Count == 0 
                    //|| CustomerService.GetContactId(new CustomerContact
                    //    {
                    //        Name = name,
                    //        Country = Data.Contact.Country,
                    //        City = Data.Contact.City,
                    //        Region = Data.Contact.Region,
                    //        Zip = Data.Contact.Zip,
                    //        Street = Data.Contact.Street,
                    //        CustomerGuid = customer.Id
                    //    }).IsNullOrEmpty()
                        )
                {
                    var country = !string.IsNullOrWhiteSpace(Data.Contact.Country)
                        ? CountryService.GetCountryByName(Data.Contact.Country)
                        : null;

                    CustomerService.AddContact(new CustomerContact()
                    {
                        Name = name,
                        Country = Data.Contact.Country,
                        CountryId = country?.CountryId ?? 0,
                        Region = Data.Contact.Region,
                        City = Data.Contact.City,
                        District = Data.Contact.District,
                        Zip = Data.Contact.Zip,

                        Street = Data.Contact.Street,
                        House = Data.Contact.House,
                        Apartment = Data.Contact.Apartment,
                        Structure = Data.Contact.Structure,
                        Entrance = Data.Contact.Entrance,
                        Floor = Data.Contact.Floor
                    }, customer.Id);
                }

                if (user.CustomerFields != null)
                {
                    foreach (var customerField in user.CustomerFields)
                    {
                        CustomerFieldService.AddUpdateMap(customer.Id, customerField.Id, customerField.Value ?? "", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        private static Order CreateOrder(CheckoutData Data, ShoppingCart cart, string customData, OrderType? orderType, bool isLanding)
        {
            var currency = CurrencyService.CurrentCurrency;
            var orderSource = OrderSourceService.GetOrderSource(OrderType.ShoppingCart);

            if (orderType != null && orderType != OrderType.None)
                orderSource = OrderSourceService.GetOrderSource(orderType.Value);
            else if (Data.LpId != null)
            {
                var lp = new LpService().Get(Data.LpId.Value);
                LpSite site;
                if (lp != null && (site = new LpSiteService().Get(lp.LandingSiteId)) != null)
                    orderSource = OrderSourceService.GetOrderSource(OrderType.LandingPage, site.Id, site.Name);
                else
                    orderSource = OrderSourceService.GetOrderSource(OrderType.LandingPage);
            }
            else if (SettingsDesign.IsSocialTemplate)
                orderSource = OrderSourceService.GetOrderSource(OrderType.SocialNetworks);
            else if (SettingsDesign.IsMobileTemplate)
                orderSource = OrderSourceService.GetOrderSource(OrderType.Mobile);

            var customer = CustomerContext.CurrentCustomer;
            var c = CustomerService.GetCustomer(CustomerContext.CustomerId);

            var order = new Order
            {
                OrderCustomer = new OrderCustomer
                {
                    CustomerIP = HttpContext.Current.Request.UserHostAddress,
                    CustomerID = Data.User.Id,
                    FirstName = Data.User.FirstName,
                    LastName = Data.User.LastName,
                    Patronymic = Data.User.Patronymic,
                    Organization = Data.User.Organization,
                    Email = Data.User.Email,
                    Phone = Data.User.Phone,
                    StandardPhone =
                        !string.IsNullOrWhiteSpace(Data.User.Phone)
                            ? StringHelper.ConvertToStandardPhone(Data.User.Phone)
                            : null,
                    CustomerType = Data.User.CustomerType,

                    Country = Data.Contact.Country,
                    Region = Data.Contact.Region,
                    District = Data.Contact.District,
                    City = Data.Contact.City,
                    Zip = Data.Contact.Zip,
                    CustomField1 = Data.Contact.CustomField1,
                    CustomField2 = Data.Contact.CustomField2,
                    CustomField3 = Data.Contact.CustomField3,

                    Street = Data.Contact.Street,
                    House = Data.Contact.House,
                    Apartment = Data.Contact.Apartment,
                    Structure = Data.Contact.Structure,
                    Entrance = Data.Contact.Entrance,
                    Floor = Data.Contact.Floor
                },
                OrderCurrency = currency,
                OrderStatusId = OrderStatusService.DefaultOrderStatus,
                AffiliateID = 0,
                OrderDate = DateTime.Now,
                CustomerComment = Data.CustomerComment,
                ManagerId = customer.ManagerId,

                GroupName = (c ?? customer).CustomerGroup.GroupName,
                GroupDiscount = (c ?? customer).CustomerGroup.GroupDiscount,
                OrderDiscount = cart.DiscountPercentOnTotalPrice,
                OrderSourceId = orderSource.Id,
                CustomData = customData,
                LpId = Data.LpId,
                //LinkedCustomerId = _linkedCustomerId
                DontCallBack = Data.DontCallBack
            };

            foreach (var orderItem in cart.Select(item => (OrderItem)item))
            {
                order.OrderItems.Add(orderItem);
            }

            order.ShippingMethodId = Data.SelectShipping.MethodId;
            order.PaymentMethodId = Data.SelectPayment.Id;

            order.ArchivedShippingName = Data.SelectShipping.Name;
            order.ArchivedPaymentName = Data.SelectPayment.Name;

            order.OrderPickPoint = Data.SelectShipping.GetOrderPickPoint();

            order.PaymentDetails = Data.SelectPayment.GetDetails(order);

            order.AvailablePaymentCashOnDelivery = Data.SelectShipping.IsAvailablePaymentCashOnDelivery;
            order.AvailablePaymentPickPoint = Data.SelectShipping.IsAvailablePaymentPickPoint;

            if (Data.SelectShipping != null)
                if (Data.SelectShipping.DateOfDelivery != null)
                    order.DeliveryDate = Data.SelectShipping.DateOfDelivery;
            if (Data.SelectShipping.TimeOfDelivery == null && Data.SelectShipping.ShowSoonest)
                order.DeliveryTime = "Как можно скорее";
            else if (Data.SelectShipping.TimeOfDelivery.IsNotEmpty())
                order.DeliveryTime = string.Concat(
                    Data.SelectShipping.TimeOfDelivery,
                    Data.SelectShipping.TimeZoneOffset.HasValue
                        ? $"|{Data.SelectShipping.TimeZoneOffset.Value}"
                        : string.Empty);

            ProcessCertificate(cart, order);
            ProcessCoupon(cart, order);

            var shippingPrice = Data.SelectShipping.FinalRate;
            var paymentPrice = Data.SelectPayment.Rate;

            Card bonusCard = null;
            if (BonusSystem.IsActive)
            {
                bonusCard = BonusSystemService.GetCard(Data.User.BonusCardId);

                if (Data.Bonus.UseIt && bonusCard != null && bonusCard.BonusesTotalAmount > 0)
                {
                    order.BonusCost = BonusSystemService.GetBonusCost(bonusCard, cart, shippingPrice, Data.Bonus.AppliedBonuses).BonusPrice;
                }

                if (Data.User.WantBonusCard && bonusCard == null && customer.RegistredUser)
                {
                    CreateBonusCard(Data, customer);
                    bonusCard = BonusSystemService.GetCard(customer.Id);
                }
            }

            order.BonusCardNumber = bonusCard != null && !bonusCard.Blocked ? bonusCard.CardNumber : default(long?);

            order.ShippingCost = shippingPrice;
            var shippingTax = Data.SelectShipping.TaxId.HasValue ? TaxService.GetTax(Data.SelectShipping.TaxId.Value) : null;
            order.ShippingTaxType = shippingTax == null ? TaxType.None : shippingTax.TaxType;
            order.ShippingPaymentMethodType = Data.SelectShipping.PaymentMethodType;
            order.ShippingPaymentSubjectType = Data.SelectShipping.PaymentSubjectType;
            order.PaymentCost = paymentPrice;
            if (Data.User.IsAddRecipient)
                order.OrderRecipient = new OrderRecipient
                {
                    FirstName = Data.User.RecipientFirstName,
                    LastName = Data.User.RecipientLastName,
                    Patronymic = Data.User.RecipientPatronymic,
                    Phone = Data.User.RecipientPhone,
                    StandardPhone = StringHelper.ConvertToStandardPhone(Data.User.RecipientPhone)
                };

            order.OrderID = OrderService.AddOrder(order, new OrderChangedBy(customer));

            OrderStatusService.ChangeOrderStatusForNewOrder(order.OrderID);

            if (BonusSystem.IsActive && bonusCard != null && !bonusCard.Blocked)
            {
                BonusSystemService.MakeBonusPurchase(bonusCard.CardNumber, cart, shippingPrice, order);
            }

            PostProcessOrder(order);


            var lpUrl = new GetCrossSellLandingUrl(Data.LpUpId, order, isLanding).Execute();

            if (string.IsNullOrEmpty(lpUrl))
            {
                OrderMailService.SendMail(order, cart.TotalDiscount, Data.Bonus.BonusPlus, Data.SelectShipping.ForMailTemplate()/*, Data.SelectPayment.Name*/);
            }
            else
            {
                LandingHelper.LandingRedirectUrl = lpUrl;
                DeferredMailService.Add(new DeferredMail(order.OrderID, DeferredMailType.Order));
            }

            TrialService.TrackEvent(
                order.OrderItems.Any(x => x.Name.Contains("SM-G900F"))
                    ? TrialEvents.BuyTheProduct
                    : TrialEvents.CheckoutOrder, string.Empty);

            return order;
        }

        private static void ProcessCertificate(ShoppingCart Cart, Order order)
        {
            var certificate = Cart.Certificate;

            if (certificate != null)
            {
                order.Certificate = new OrderCertificate()
                {
                    Code = certificate.CertificateCode,
                    Price = certificate.Sum
                };
            }
        }

        private static void ProcessCoupon(ShoppingCart Cart, Order order)
        {
            var coupon = Cart.Coupon;

            order.Coupon = coupon != null && Cart.TotalPrice >= coupon.MinimalOrderPrice
                ? (OrderCoupon)coupon
                : null;
        }

        private static void CreateBonusCard(CheckoutData Data, Customer customer)
        {
            try
            {
                customer.BonusCardNumber = BonusSystemService.AddCard(new Card { CardId = customer.Id });
                CustomerService.UpdateCustomer(customer);

                if (customer.BonusCardNumber != null)
                {
                    Data.User.BonusCardId = customer.Id;

                    if (HttpContext.Current != null)
                        HttpContext.Current.Session["BonusesForNewCard"] = BonusSystem.BonusesForNewCard;
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
            }
        }

        private  static void PostProcessOrder(Order order)
        {
            if (order.Sum == 0)
                OrderService.PayOrder(order.OrderID, true);
        }

        public static MyCheckout Factory(Guid customerId, out CheckoutDataV8 data)
        {
            var model = new MyCheckout(ShoppingCartService.CurrentShoppingCart) { Data = OrderConfirmationService.Get(customerId) };
            data = Get(customerId);

            if (model.Data == null)
            {
                var customer = CustomerContext.CurrentCustomer;
                model.Data = new CheckoutData
                {
                    ShopCartHash = model.Cart.GetHashCode(),
                    User = { Id = customerId, CustomerType = customer.CustomerType }
                };
                OrderConfirmationService.Add(customer.Id, model.Data);
            }

            model.SetPriceRule();

            return model;
        }

        public static CheckoutDataV8 Get(Guid customerId)
        {
            return ModulesRepository.ModuleExecuteReadOne("Select * from [Order].OrderConfirmation where CustomerId=@CustomerId",
                CommandType.Text, GetFromReader,
                new SqlParameter("@CustomerId", customerId));
        }

        private static CheckoutDataV8 GetFromReader(SqlDataReader reader)
        {
            return
                JsonConvert.DeserializeObject<CheckoutDataV8>(
                    SQLDataHelper.GetString(reader, "OrderConfirmationData"),
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });

        }

    }
}
