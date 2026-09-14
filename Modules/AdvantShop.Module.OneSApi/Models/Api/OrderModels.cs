using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Core;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Configuration;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Core.Services.Webhook.Models.Api;
using AdvantShop.Customers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Web.Infrastructure.Api;
using Newtonsoft.Json;

namespace AdvantShop.Module.OneSApi.Models.Api
{
    public class OrderV8Model : OrderModel
    {
        public static OrderV8Model FromOrderV8(Order order)
        {
            if (order == null || order.IsDraft)
                return null;

            var settings = ModuleService.GetImportExportSettings();

            var paymentType = "";
            if (order.PaymentMethod != null)
                paymentType = (AdvantshopConfigService.GetDropdownPayments().FirstOrDefault(x => x.Value == order.PaymentMethod.PaymentKey) ?? new ListItemModel { Text = "" }).Text;

            //CustomerFieldWithValue inn = null, org = null;
            //if (order.OrderCustomer.CustomerType == CustomerType.LegalEntity)
            //{
            //    var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(order.OrderCustomer.CustomerID)
            //        .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
            //                    (x.CustomerType == order.OrderCustomer.CustomerType || x.CustomerType == CustomerType.All)).ToList();
            //    inn = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.INN);
            //    org = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.CompanyName);
            //}

            var external = ExportService.GetExternalFromOrderId(order.OrderID);

            return new OrderV8Model
            {
                Id = order.OrderID,
                Number = order.Number,
                Currency = order.OrderCurrency != null ? order.OrderCurrency.CurrencyCode : null,
                Sum = order.Sum,
                Date = order.OrderDate,

                CustomerComment = order.CustomerComment,
                AdminComment = order.AdminOrderComment,

                //PaymentId = order.PaymentMethodId,
                PaymentName = order.ArchivedPaymentName,
                PaymentCost = order.PaymentCost,
                PaymentType = paymentType,

                ShippingId = order.ShippingMethodId,
                ShippingName = order.ArchivedShippingName,
                ShippingCost = (decimal)((int)Math.Round(order.ShippingCost * 100f) / 100f),
                ShippingTaxName = order.ShippingTaxType.Localize(),
                TrackNumber = order.TrackNumber,
                DeliveryDate = order.DeliveryDate,
                DeliveryTime = order.DeliveryTime,

                OrderDiscount = order.OrderDiscount,
                OrderDiscountValue = order.OrderDiscountValue,

                BonusCardNumber = order.BonusCardNumber.TryParseLong(true),
                BonusCost = order.BonusCost,

                LpId = order.LpId,

                IsPaid = order.Payed,
                PaymentDate = order.PaymentDate,

                OrderCustomer = OrderCustomerV8Model.FromOrderCustomerV8(order.OrderCustomer/*, inn != null ? inn.Value : "", org != null ? org.Value : ""*/),
                CustomerAddress = order.OrderCustomer.GetCustomerAddress(),

                OrderRecipient = OrderRecipientModel.FromOrder(order),

                Status = OrderStatusV8Model.FromOrderStatusV8(order.OrderStatus),

                Source = OrderSourceModel.FromOrderSource(order.OrderSource),

                Items = order.OrderItems != null
                    ? order.OrderItems.Select(OrderItemV8Model.FromOrderItemV8).ToList()
                    : new List<OrderItemV8Model>(),

                //INN = inn != null ? inn.Value : "",//order.PaymentDetails != null ? order.PaymentDetails.INN : "",
                //CompanyName = org != null ? org.Value : "",//order.PaymentDetails != null ? order.PaymentDetails.CompanyName : "",

                ManagerId = order.ManagerId,
                ManagerFirstName = order.Manager != null ? order.Manager.FirstName : "",
                ManagerLastName = order.Manager != null ? order.Manager.LastName : "",
                //ManagerPatronymic = order.Manager != null ? order.Manager.Patronymic : "",
                ManagerEmail = order.Manager != null ? order.Manager.Email : "",
                ManagerPhone = order.Manager != null ? order.Manager.StandardPhone : null,

                PickPointId = order.OrderPickPoint != null ? order.OrderPickPoint.PickPointId : "",
                PickPointAddress = order.OrderPickPoint != null ? order.OrderPickPoint.PickPointAddress : "",

                ArtnoType = settings.ImportProduct.ImportArtnoType,
                ExternalId = external.ExternalId,
                ExportType = external.ExportType,
                Changed = external.Changed,
                Exported = external.Exported,
            };
        }

        public string ExternalId { get; set; }
        public int ExportType { get; set; }
        //public string INN { get; set; }
        //public string CompanyName { get; set; }
        public int? ManagerId { get; set; }
        public string ManagerFirstName { get; set; }
        public string ManagerLastName { get; set; }
        //public string ManagerPatronymic { get; set; }
        public string ManagerEmail { get; set; }
        public long? ManagerPhone { get; set; }
        public string PickPointId { get; set; }
        //public string PickPointAddress { get; set; }
        public int? ShippingId { get; set; }
        public string CustomerAddress { get; set; }
        //public int PaymentId { get; set; }
        //public DateTime? PaymentDate { get; set; }
        public string PaymentType { get; set; }

        public OrderCustomerV8Model OrderCustomer { get; set; }
        public new AddUpdateCustomerModel Customer { get; set; }
        public OrderRecipientModel OrderRecipient { get; set; }

        public float TotalWeight 
        { 
            get { return (Items ?? new List<OrderItemV8Model>()).Sum(x => x.Weight * (float)x.Amount); }
        }
        public OrdersStatuses OrderStatus { get; set; }
        public ImportArtnoType ArtnoType { get; set; }
        public DateTime Changed { get; set; }
        public DateTime? Exported { get; set; }
        public string BillOrg { get; set; }
        public new List<OrderItemV8Model> Items { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public new OrderStatusV8Model Status { get; set; }
        public string/*byte[]*/ BillStorage { get; set; }

        public new decimal ShippingCost { get; set; }

        public DateTime? KeepFreeUntil { get; set; }
        public string CardHolder { get; set; }
    }

    public class OrderItemV8Model : OrderItemModel
    {
        public static OrderItemV8Model FromOrderItemV8(OrderItem orderItem)
        {
            if (orderItem == null)
                return null;

            return new OrderItemV8Model
            {
                ArtNo = orderItem.ArtNo,
                Name = orderItem.Name,
                Color = orderItem.Color,
                Size = orderItem.Size,
                Price = (decimal)((int)Math.Round(orderItem.Price * 100f) / 100f),
                Amount = (decimal)((int)Math.Round(orderItem.Amount * 1000f) / 1000f),
                TaxRate = orderItem.TaxRate ?? 0,
                Unit = orderItem.Unit,
                ProductId = orderItem.ProductID ?? 0, 
                ExternalId = ImportService.GetExternalIdById(orderItem.ProductID ?? 0, "Product"),
                Weight =orderItem.Weight
            };
        }

        public string ExternalId { get; set; }
        //public int ProductId { get; set; }
        public string Code { get; set; }
        public float TaxRate { get; set; }
        public new string Unit { get; set; }
        public float Weight { get; set; }
        public new decimal Price { get; set; }
        public new decimal Amount { get; set; }
    }

    public class OrderCustomerV8Model : OrderCustomerModel
    {
        public static OrderCustomerV8Model FromOrderCustomerV8(OrderCustomer orderCustomer/*, string inn, string org*/)
        {
            if (orderCustomer == null)
                return null;

            //var customer = CustomerService.GetCustomer(orderCustomer.CustomerID);

            var model = new OrderCustomerV8Model
            {
                CustomerId = orderCustomer.CustomerID,
                FirstName = orderCustomer.FirstName,
                LastName = orderCustomer.LastName,
                Patronymic = orderCustomer.Patronymic,
                Organization = orderCustomer.Organization,
                Email = orderCustomer.Email,
                Phone = orderCustomer.Phone,
                Country = orderCustomer.Country,
                Region = orderCustomer.Region,
                District = orderCustomer.District,
                City = orderCustomer.City,
                Zip = orderCustomer.Zip,
                CustomField1 = orderCustomer.CustomField1,
                CustomField2 = orderCustomer.CustomField2,
                CustomField3 = orderCustomer.CustomField3,
                Street = orderCustomer.Street,
                House = orderCustomer.House,
                Apartment = orderCustomer.Apartment,
                Structure = orderCustomer.Structure,
                Entrance = orderCustomer.Entrance,
                Floor = orderCustomer.Floor,
                //RegistrationDate = customer != null ? customer.RegistrationDateTime : DateTime.Now,
                //Birthday = customer != null ? customer.BirthDay : null,
                //Subscribed = customer != null ? customer.SubscribedForNews : false,
                //Salt = customer != null ? customer.Password : null,
                //CustomerType = customer != null ? customer.CustomerType : CustomerType.PhysicalEntity,
                //CustomerFields = new Dictionary<string, string>(),
                //INN = inn,
                //CompanyName = org
            };

            //if (customer.CustomerGroup != null)
            //    model.DiscountPercent = customer.CustomerGroup.GroupDiscount;

            //var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id)
            //        .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
            //                    (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All)).ToList();
            //foreach (var field in customerFields)
            //    model.CustomerFields.Add(field.Name, field.ValueDateFormat ?? field.Value);

            return model;
        }

        //public DateTime RegistrationDate { get; set; }
        //public DateTime? Birthday { get; set; }
        //public bool Subscribed { get; set; }
        //public string Salt { get; set; }
        //public CustomerType CustomerType { get; set; }
        //public Dictionary<string, string> CustomerFields { get; set; }
        //public float DiscountPercent { get; set; }
        //public string INN { get; set; }
        //public string CompanyName { get; set; }
    }

    public class OrderRecipientModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public string Phone { get; set; }

        public static OrderRecipientModel FromOrder(Order order)
        {
            var orderRecipient = OrderService.GetOrderRecipient(order.OrderID);
            if (orderRecipient == null)
                return null;
            return new OrderRecipientModel
            {
                FirstName = orderRecipient.FirstName,
                LastName = orderRecipient.LastName,
                Patronymic = orderRecipient.Patronymic,
                Phone = orderRecipient.Phone,
            };
        }
    }

    public class OrderStatusV8Model : OrderStatusModel
    {
        public static OrderStatusV8Model FromOrderStatusV8(OrderStatus orderStatus)
        {
            if (orderStatus == null)
                return null;

            var settings = ModuleService.GetImportExportSettings();
            return new OrderStatusV8Model
            {
                Id = orderStatus.StatusID,
                Name = orderStatus.StatusName,
                Color = orderStatus.Color,
                IsCanceled = orderStatus.IsCanceled,
                IsCompleted = orderStatus.IsCompleted,
                Hidden = orderStatus.Hidden,
                IsLead = orderStatus.StatusID == settings.ExportOrder.StatusLead
            };
        }

        public bool IsLead { get; set; }
    }

    public class ExternalOrderModel
    {
        public string ExternalId { get; set; }
        public int ExportType { get; set; }
        public DateTime Changed { get; set; }
        public DateTime? Exported { get; set; }
    }

    public enum ExportType
    {
        Empty = 0,

        [Localize("Содержимое")]
        Order = 1,

        [Localize("Статус")]
        Status = 2,

        [Localize("Оплата")]
        Payed = 3,

        [Localize("Описание")]
        Description = 4,
    }

    public class ConfirmOrderModel
    {
        public List<ConfirmOrderModel_item> Items { get; set; }
        public string LogFile { get; set; }
    }
    public class ConfirmOrderModel_item
    {
        public int OrderId { get; set; }
        public ExportType ExportType { get; set; }
        public string CustomerId { get; set; }
        public string ExternalId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class FilterOrdersModel : EntitiesFilterModel//, IValidatableObject
    {
        public FilterOrdersModel()
        {
        }

        public int? OrderId { get; set; }

        private bool loadItems;
        public bool LoadItems
        {
            get
            {
                return loadItems;
            }
            set
            {
                loadItems = value;

                //if (loadItems)
                //{
                //    MaxItemsPerPage = 50;
                //    DefaultItemsPerPage = 50;
                //}
            }
        }

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    DateTime temp;
        //    if (!string.IsNullOrWhiteSpace(DateFrom) && !DateTime.TryParse(DateFrom, out temp))
        //        yield return new ValidationResult("Неудалось преобразовать дату параметра DateFrom");

        //    if (!string.IsNullOrWhiteSpace(DateTo) && !DateTime.TryParse(DateTo, out temp))
        //        yield return new ValidationResult("Неудалось преобразовать дату параметра DateTo");
        //}
    }

    public class OrderImportModel
    {
        public List<OrderV8Model> Orders { get; set; }
    }
    public class OrderImportResultModel
    {
        public List<ImportResultModel> Orders { get; set; }
    }

    public class BillOrg
    {
        public string CompanyName { get; set; }
        public string TransAccount { get; set; }
        public string CorAccount { get; set; }
        public string Address { get; set; }
        public string Telephone { get; set; }
        public string INN { get; set; }
        public string KPP { get; set; }
        public string BIK { get; set; }
        public string BankName { get; set; }
        public string Director { get; set; }
        public string PosDirector { get; set; }
        public string Accountant { get; set; }
        public string PosAccountant { get; set; }
        //public string Manager { get; set; }
        //public string PosManager { get; set; }
        //public string StampImageName { get; set; }
        //public bool ShowPaymentDetails { get; set; }
        //public bool RequiredPaymentDetails { get; set; }
        //public string CustomerCompanyNameField { get; set; }
        //public string CustomerINNField { get; set; }
    }

    public enum OrdersStatuses
    {
        New,
        [StringName("Confirmed")]
        Confirmed,
        [StringName("Shipped")]
        Shipped,
        [StringName("Ready")]
        Ready,
        [StringName("Done")]
        Done,
        [StringName("Waiting")]
        Waiting,
        [StringName("Building")]
        Building,
        [StringName("ReadyAtStore")]
        ReadyAtStore,
        [StringName("Cancelled")]
        Cancelled = 99,
    }

}
