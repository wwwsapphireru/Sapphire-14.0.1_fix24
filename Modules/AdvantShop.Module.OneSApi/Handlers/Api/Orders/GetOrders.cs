using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Configuration;
using AdvantShop.Core.Services.Webhook.Models.Api;
using AdvantShop.Core.SQL2;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Extensions;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Api;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Orders
{
    public class GetOrders : EntitiesHandler<FilterOrdersModel, OrderV8Model>
    {
        private readonly ImportExportSettingsModel _settings;
        private readonly List<ListItemModel> _payments;

        public string LogFile { get; set; }

        public GetOrders(FilterOrdersModel filterModel, ImportExportSettingsModel settings) : base(filterModel) 
        {
            _settings = settings;
            _payments = AdvantshopConfigService.GetDropdownPayments();
        }

        protected override SqlPaging Select(SqlPaging paging)
        {
            paging.Select(
                "[Order].[OrderID]",
                "[Order].[Number]",
                "[OrderCurrency].[CurrencyCode]",
                "[Order].[Sum]",
                "[Order].[OrderDate]",

                "[Order].[CustomerComment]",
                "[Order].[AdminOrderComment]",

                "[Order].[PaymentMethodName]",
                "[Order].[PaymentCost]",

                "[Order].[ShippingMethodId]".AsSqlField("ShippingId"),
                "[Order].[ShippingMethodName]",
                "[Order].[ShippingCost]",
                "[Order].[ShippingTaxType]",
                "[Order].[TrackNumber]",
                "[Order].[DeliveryDate]",
                "[Order].[DeliveryTime]",

                "[Order].[OrderDiscount]",
                "[Order].[OrderDiscountValue]",

                "[Order].[BonusCardNumber]",
                "[Order].[BonusCost]",

                "[Order].[LpId]",

                "[Order].[PaymentDate]",
                "[Order].[IsDraft]",

                "[OrderCustomer].[CustomerID]",
                "[OrderCustomer].[FirstName]",
                "[OrderCustomer].[LastName]",
                "[OrderCustomer].[Patronymic]",
                "[OrderCustomer].[Organization]",
                "[OrderCustomer].[Email]",
                "[OrderCustomer].[Phone]",
                "[OrderCustomer].[Country]",
                "[OrderCustomer].[Region]",
                "[OrderCustomer].[District]",
                "[OrderCustomer].[City]",
                "[OrderCustomer].[Zip]",
                "[OrderCustomer].[CustomField1]",
                "[OrderCustomer].[CustomField2]",
                "[OrderCustomer].[CustomField3]",
                "[OrderCustomer].[Street]",
                "[OrderCustomer].[House]",
                "[OrderCustomer].[Apartment]",
                "[OrderCustomer].[Structure]",
                "[OrderCustomer].[Entrance]",
                "[OrderCustomer].[Floor]",

                //"[Customer].[RegistrationDateTime]",
                //"[Customer].[BirthDay]",
                //"IsNull(Subscription.Subscribe, 0) As SubscribedForNews",
                //"[Customer].[Password]",

                "[Order].[OrderStatusID]",
                "[OrderStatus].[StatusName]",
                "[OrderStatus].[IsCanceled]".AsSqlField("StatusIsCanceled"),
                "[OrderStatus].[IsCompleted]".AsSqlField("StatusIsCompleted"),
                "[OrderStatus].[Hidden]".AsSqlField("StatusHidden"),

                "[Order].[OrderSourceId]",
                "[OrderSource].[Name]".AsSqlField("SourceName"),
                "[OrderSource].[Main]".AsSqlField("SourceMain"),
                "[OrderSource].[Type]".AsSqlField("SourceType"),

                "[PaymentDetails].[INN]",
                "[PaymentDetails].[CompanyName]",

                "[Order].[ManagerId]",
                "[Manager].[FirstName]".AsSqlField("ManagerFirstName"),
                "[Manager].[LastName]".AsSqlField("ManagerLastName"),
                "[Manager].[Patronymic]".AsSqlField("ManagerPatronymic"),
                "[Manager].[Email]".AsSqlField("ManagerEmail"),
                "[Manager].[StandardPhone]".AsSqlField("ManagerPhone"),

                "[OrderPickPoint].[PickPointId]",
                "[OrderPickPoint].[PickPointAddress]",

                "IsNull(OrderAdditionalData.[Value], '')".AsSqlField("CardHolder"),

                Convert.ToInt32(_settings.ImportProduct.ImportArtnoType).ToString().AsSqlField("ArtnoType"),
                "[OneSApi_Order].[ExternalId]",
                "[OneSApi_Order].[ExportType]",
                "[OneSApi_Order].[Changed]",
                "[OneSApi_Order].[Exported]",

                "[PaymentMethod].[PaymentType]",

                //"[Customer].[CustomerType]"
                "IsNull([Customer].[CustomerID], '00000000-0000-0000-0000-000000000000')".AsSqlField("CustomerCustomerID"),
                "[Customer].[FirstName]".AsSqlField("CustomerFirstName"),
                "[Customer].[LastName]".AsSqlField("CustomerLastName"),
                "[Customer].[Patronymic]".AsSqlField("CustomerPatronymic"),
                "[Customer].[Organization]".AsSqlField("CustomerOrganization"),
                "[Customer].[Email]".AsSqlField("CustomerEmail"),
                "[Customer].[Phone]".AsSqlField("CustomerPhone"),

                "[Contact].[Country]".AsSqlField("CustomerCountry"),
                "[Region].[RegionName]".AsSqlField("CustomerRegion"),
                "[Contact].[District]".AsSqlField("CustomerDistrict"),
                "[Contact].[City]".AsSqlField("CustomerCity"),
                "[Contact].[Zip]".AsSqlField("CustomerZip"),
                "[Contact].[Street]".AsSqlField("CustomerStreet"),
                "[Contact].[House]".AsSqlField("CustomerHouse"),
                "[Contact].[Apartment]".AsSqlField("CustomerApartment"),
                "[Contact].[Structure]".AsSqlField("CustomerStructure"),
                "[Contact].[Entrance]".AsSqlField("CustomerEntrance"),
                "[Contact].[Floor]".AsSqlField("CustomerFloor"),

                "[OrderRecipient].[FirstName]".AsSqlField("RecipientFirstName"),
                "[OrderRecipient].[LastName]".AsSqlField("RecipientLastName"),
                "[OrderRecipient].[Patronymic]".AsSqlField("RecipientPatronymic"),
                "[OrderRecipient].[Phone]".AsSqlField("RecipientPhone"),

                "[Customer].[RegistrationDateTime]".AsSqlField("CustomerRegistrationDateTime"),
                "[Customer].[BirthDay]".AsSqlField("CustomerBirthDay"),
                "IsNull(Subscription.Subscribe, 0)".AsSqlField("CustomerSubscribedForNews"),
                "[Customer].[Password]".AsSqlField("CustomerPassword"),

                //"[Customer].[ManagerId]".AsSqlField("Customer"),
                //"[Manager].[FirstName]".AsSqlField("CustomerManagerFirstName"),
                //"[Manager].[LastName]".AsSqlField("CustomerManagerLastName"),
                //"[Manager].[Patronymic]".AsSqlField("CustomerManagerPatronymic"),
                //"[Manager].[Email]".AsSqlField("CustomerManagerEmail"),
                //"[Manager].[StandardPhone]".AsSqlField("CustomerManagerPhone"),

                "[Customer].[CustomerType]".AsSqlField("CustomerCustomerType"),
                "0".AsSqlField("CustomerClientStatus"),
                "IsNull(CustomerGroup.GroupDiscount, 0)".AsSqlField("CustomerDiscountPercent")
                );

            //paging.From("[Order].[Order]");
            paging.From("[Module].[OneSApi_Order]");
            //paging.Left_Join("[Module].[OneSApi_Order] ON [Order].[OrderID]=[OneSApi_Order].[OrderId]");
            paging.Left_Join("[Order].[Order] ON [Order].[OrderID]=[OneSApi_Order].[OrderId]");
            paging.Left_Join("[Order].[OrderCustomer] ON [Order].[OrderID]=[OrderCustomer].[OrderID]");
            paging.Left_Join("[Order].[OrderRecipient] ON [Order].[OrderID]=[OrderRecipient].[OrderID]");
            paging.Left_Join("[Order].[OrderStatus] ON [OrderStatus].[OrderStatusID]=[Order].[OrderStatusID]");
            paging.Left_Join("[Order].[OrderCurrency] ON [Order].[OrderID] = [OrderCurrency].[OrderID]");
            paging.Left_Join("[Order].[OrderSource] on [Order].[OrderSourceId] = [OrderSource].[Id]");
            paging.Left_Join("[Order].[PaymentDetails] on [Order].[OrderID] = [PaymentDetails].[OrderId]");
            paging.Left_Join("[Order].[OrderPickPoint] on [Order].[OrderID] = [OrderPickPoint].[OrderId]");
            paging.Left_Join("[Order].[PaymentMethod] on [Order].[PaymentMethodID] = [PaymentMethod].[PaymentMethodID]");
            paging.Left_Join("[Order].[OrderAdditionalData] on [OrderAdditionalData].[OrderId] = [OneSApi_Order].[OrderId] And [OrderAdditionalData].[Name] = 'CardHolder'");
            paging.Left_Join("[Customers].[Managers] on [Order].[ManagerId] = [Managers].[ManagerId]");
            paging.Left_Join("[Customers].[Customer] As [Manager] on [Managers].[CustomerId] = [Manager].[CustomerId]");

            paging.Left_Join("[Customers].[Customer] ON [Customer].[CustomerId]=[OrderCustomer].[CustomerId]");
            paging.Left_Join("[Customers].[Subscription] ON [Customer].[Email]=[Subscription].[Email]");
            paging.Left_Join("[Customers].[Contact] ON [Customer].[CustomerId]=[Contact].[CustomerId]");
            paging.Left_Join("[Customers].[Region] ON [Region].[RegionId]=[Contact].[RegionId]");
            paging.Left_Join("[Customers].[CustomerGroup] ON [Customer].[CustomerGroupId]=[CustomerGroup].[CustomerGroupId]");

            return paging;
        }

        protected override SqlPaging Filter(SqlPaging paging)
        {
            paging.Where("[Order].[OrderID] Is Not NULL");
            paging.Where("[Order].[IsDraft] != 1");
            //paging.Where("[Order].[OrderID] IN (Select OrderId From Module.OneSApi_Order Where ForExport = 1)");
            paging.Where("[OneSApi_Order].[ForExport] = 1");

            if (FilterModel.OrderId.HasValue)
                paging.Where("[Order].[OrderID] = {0}", FilterModel.OrderId.Value);

            return paging;
        }

        protected override SqlPaging Sorting(SqlPaging paging)
        {
            if (string.IsNullOrEmpty(FilterModel.Sorting) || FilterModel.SortingType == FilterSortingType.None)
            {
                paging.OrderBy("[OneSApi_Order].[Changed]");

                return paging;
            }

            /*var sorting = FilterModel.Sorting;

            var field = paging.SelectFields().FirstOrDefault(x => x.FieldName.Equals(sorting, StringComparison.OrdinalIgnoreCase));
            if (field != null)
            {
                if (FilterModel.SortingType == FilterSortingType.Asc)
                    paging.OrderBy(sorting);
                else
                    paging.OrderByDesc(sorting);
            }*/

            return paging;
        }

        protected override List<OrderV8Model> FillItems(SqlPaging paging)
        {
            return paging.PageItemsList<OrderV8Model>(GetFromReader);
        }

        private OrderV8Model GetFromReader(IDataReader reader)
        {
            var item = new OrderV8Model
            {
                Id = SQLDataHelper.GetInt(reader, "OrderID"),
                Number = SQLDataHelper.GetString(reader, "Number"),
                Currency = SQLDataHelper.GetString(reader, "CurrencyCode"),
                Sum = SQLDataHelper.GetFloat(reader, "Sum"),
                Date = SQLDataHelper.GetDateTime(reader, "OrderDate"),

                CustomerComment = SQLDataHelper.GetString(reader, "CustomerComment"),
                AdminComment = SQLDataHelper.GetString(reader, "AdminOrderComment"),

                PaymentName = SQLDataHelper.GetString(reader, "PaymentMethodName"),
                PaymentCost = SQLDataHelper.GetFloat(reader, "PaymentCost"),

                ShippingId = SQLDataHelper.GetInt(reader, "ShippingId"),
                ShippingName = SQLDataHelper.GetString(reader, "ShippingMethodName"),
                ShippingCost = Math.Round(SQLDataHelper.GetDecimal(reader, "ShippingCost"), 2),
                ShippingTaxName = ((Taxes.TaxType)SQLDataHelper.GetInt(reader, "ShippingTaxType")).Localize(),
                TrackNumber = SQLDataHelper.GetString(reader, "TrackNumber"),
                DeliveryDate = SQLDataHelper.GetNullableDateTime(reader, "DeliveryDate"),
                DeliveryTime = SQLDataHelper.GetString(reader, "DeliveryTime"),

                OrderDiscount = SQLDataHelper.GetFloat(reader, "OrderDiscount"),
                OrderDiscountValue = SQLDataHelper.GetFloat(reader, "OrderDiscountValue"),

                BonusCardNumber = SQLDataHelper.GetNullableLong(reader, "BonusCardNumber"),
                BonusCost = SQLDataHelper.GetFloat(reader, "BonusCost"),

                LpId = SQLDataHelper.GetNullableInt(reader, "LpId"),

                IsPaid = SQLDataHelper.GetNullableDateTime(reader, "PaymentDate").HasValue,
                PaymentDate = SQLDataHelper.GetNullableDateTime(reader, "PaymentDate"),

                //INN = "",//SQLDataHelper.GetString(reader, "INN"),
                //CompanyName = "",//SQLDataHelper.GetString(reader, "CompanyName"),

                ManagerId = SQLDataHelper.GetNullableInt(reader, "ManagerId"),
                ManagerFirstName = SQLDataHelper.GetString(reader, "ManagerFirstName"),
                ManagerLastName = SQLDataHelper.GetString(reader, "ManagerLastName"),
                //ManagerPatronymic = SQLDataHelper.GetString(reader, "ManagerPatronymic"),
                ManagerEmail = SQLDataHelper.GetString(reader, "ManagerEmail"),
                ManagerPhone = SQLDataHelper.GetNullableLong(reader, "ManagerPhone"),

                PickPointId = SQLDataHelper.GetString(reader, "PickPointId"),
                PickPointAddress = SQLDataHelper.GetString(reader, "PickPointAddress"),

                ArtnoType = (ImportArtnoType)SQLDataHelper.GetInt(reader, "ArtnoType"),
                ExternalId = SQLDataHelper.GetString(reader, "ExternalId"),
                ExportType = SQLDataHelper.GetInt(reader, "ExportType"),
                PaymentType = SQLDataHelper.GetString(reader, "PaymentType"),//(_payments.FirstOrDefault(x => x.Value == SQLDataHelper.GetString(reader, "PaymentType")) ?? new ListItemModel { Text = "" }).Text,

                Changed = SQLDataHelper.GetDateTime(reader, "Changed"),
                Exported = SQLDataHelper.GetNullableDateTime(reader, "Exported"),
                CardHolder = SQLDataHelper.GetString(reader, "CardHolder"),
            };
            
            var statusId = SQLDataHelper.GetInt(reader, "OrderStatusID");
            var sourceId = SQLDataHelper.GetInt(reader, "OrderSourceId");

            item.OrderCustomer = new OrderCustomerV8Model
            {
                CustomerId = SQLDataHelper.GetGuid(reader, "CustomerID"),
                FirstName = SQLDataHelper.GetString(reader, "FirstName"),
                LastName = SQLDataHelper.GetString(reader, "LastName"),
                Patronymic = SQLDataHelper.GetString(reader, "Patronymic"),
                Organization = SQLDataHelper.GetString(reader, "Organization"),
                Email = SQLDataHelper.GetString(reader, "Email"),
                Phone = SQLDataHelper.GetString(reader, "Phone"),
                Country = SQLDataHelper.GetString(reader, "Country"),
                Region = SQLDataHelper.GetString(reader, "Region"),
                District = SQLDataHelper.GetString(reader, "District"),
                City = SQLDataHelper.GetString(reader, "City"),
                Zip = SQLDataHelper.GetString(reader, "Zip"),
                CustomField1 = SQLDataHelper.GetString(reader, "CustomField1"),
                CustomField2 = SQLDataHelper.GetString(reader, "CustomField2"),
                CustomField3 = SQLDataHelper.GetString(reader, "CustomField3"),
                Street = SQLDataHelper.GetString(reader, "Street"),
                House = SQLDataHelper.GetString(reader, "House"),
                Apartment = SQLDataHelper.GetString(reader, "Apartment"),
                Structure = SQLDataHelper.GetString(reader, "Structure"),
                Entrance = SQLDataHelper.GetString(reader, "Entrance"),
                Floor = SQLDataHelper.GetString(reader, "Floor")
                //RegistrationDate = SQLDataHelper.GetDateTime(reader, "RegistrationDateTime"),
                //Birthday = SQLDataHelper.GetNullableDateTime(reader, "BirthDay"),
                //Subscribed = SQLDataHelper.GetBoolean(reader, "SubscribedForNews"),
                //Salt = SQLDataHelper.GetString(reader, "Password"),
                //CustomerType = (CustomerType)SQLDataHelper.GetInt(reader, "CustomerType"),
            };

            item.OrderRecipient = new OrderRecipientModel
            {
                FirstName = SQLDataHelper.GetString(reader, "RecipientFirstName"),
                LastName = SQLDataHelper.GetString(reader, "RecipientLastName"),
                Patronymic = SQLDataHelper.GetString(reader, "RecipientPatronymic"),
                Phone = SQLDataHelper.GetString(reader, "RecipientPhone"),
            };

            item.CustomerAddress = item.OrderCustomer.GetCustomerAddress();

            //if (item.Customer.CustomerType == CustomerType.LegalEntity && item.Customer.Id.IsNotEmpty())
            //{
            //    var customer = CustomerService.GetCustomer(Guid.Parse(item.Customer.Id));
            //    if (customer != null)
            //    {
            //        var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id)
            //            .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
            //                        (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All)).ToList();
            //        var field = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.INN);
            //        if (field != null)
            //            item.INN = field.Value;
            //        field = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.CompanyName);
            //        if (field != null)
            //            item.CompanyName = field.Value;
            //    }
            //}
            item.Customer = new Customers.GetCustomers(null).GetOrderCustomerFromReader(reader);

            var settings = ModuleService.GetImportExportSettings();
            item.Status = statusId > 0
                ? new OrderStatusV8Model
                    {
                        Id = statusId,
                        Name = SQLDataHelper.GetString(reader, "StatusName"),
                        IsCanceled = SQLDataHelper.GetBoolean(reader, "StatusIsCanceled"),
                        IsCompleted = SQLDataHelper.GetBoolean(reader, "StatusIsCompleted"),
                        Hidden = SQLDataHelper.GetBoolean(reader, "StatusHidden"),
                        IsLead = statusId == settings.ExportOrder.StatusLead
                    }
                : null;

            item.Source = sourceId > 0
                ? new OrderSourceModel
                    {
                        Id = sourceId,
                        Name = SQLDataHelper.GetString(reader, "SourceName"),
                        Main = SQLDataHelper.GetBoolean(reader, "SourceMain"),
                        Type = SQLDataHelper.GetString(reader, "SourceType").TryParseEnum<Core.Services.Orders.OrderType>().ToString(),
                    }
                : null;

            if (FilterModel.LoadItems)
                item.Items = ExportService.GetOrderItems(item.Id);
            else
                item.Items = null;

            return item;
        }
    }
}