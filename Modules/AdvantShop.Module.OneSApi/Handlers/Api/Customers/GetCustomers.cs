using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.SQL2;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Web.Infrastructure.Api;
using Newtonsoft.Json;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Customers
{
    public class GetCustomers : EntitiesHandler<FilterCustomersModel, AddUpdateCustomerModel>
    {
        //private readonly ImportExportSettingsModel _settings;
        //private readonly List<ListItemModel> _payments;

        public string LogFile { get; set; }

        public GetCustomers(FilterCustomersModel filterModel) : base(filterModel) 
        {
            //_settings = settings;
            //_payments = AdvantshopConfigService.GetDropdownPayments();
        }

        protected override SqlPaging Select(SqlPaging paging)
        {
            paging.Select(
                "[Customer].[CustomerID]",
                "[Customer].[FirstName]",
                "[Customer].[LastName]",
                "[Customer].[Patronymic]",
                "[Customer].[Organization]",
                "[Customer].[Email]",
                "[Customer].[Phone]",

                "[Contact].[Country]",
                "[Region].[RegionName]".AsSqlField("Region"),
                "[Contact].[District]",
                "[Contact].[City]",
                "[Contact].[Zip]",
                //"[OrderCustomer].[CustomField1]",
                //"[OrderCustomer].[CustomField2]",
                //"[OrderCustomer].[CustomField3]",
                "[Contact].[Street]",
                "[Contact].[House]",
                "[Contact].[Apartment]",
                "[Contact].[Structure]",
                "[Contact].[Entrance]",
                "[Contact].[Floor]",

                "[Customer].[RegistrationDateTime]",
                "[Customer].[BirthDay]",
                "IsNull(Subscription.Subscribe, 0) As SubscribedForNews",
                "[Customer].[Password]",

                "[Customer].[ManagerId]",
                "[Manager].[FirstName]".AsSqlField("ManagerFirstName"),
                "[Manager].[LastName]".AsSqlField("ManagerLastName"),
                "[Manager].[Patronymic]".AsSqlField("ManagerPatronymic"),
                "[Manager].[Email]".AsSqlField("ManagerEmail"),
                "[Manager].[StandardPhone]".AsSqlField("ManagerPhone"),

                "[OneSApi_Customer].[ExternalId]",
                //"[OneSApi_Customer].[ExportType]",
                "[OneSApi_Customer].[Changed]",
                "[OneSApi_Customer].[Exported]",

                "[Customer].[CustomerType]",
                "[Customer].[ClientStatus]",
                "IsNull(CustomerGroup.GroupDiscount, 0) As DiscountPercent"
                );

            paging.From("[Module].[OneSApi_Customer]");
            paging.Left_Join("[Customers].[Customer] ON [Customer].[CustomerId]=[OneSApi_Customer].[CustomerId]");
            paging.Left_Join("[Customers].[Contact] ON [Customer].[CustomerId]=[Contact].[CustomerId]");
            paging.Left_Join("[Customers].[Region] ON [Region].[RegionId]=[Contact].[RegionId]");
            paging.Left_Join("[Customers].[Subscription] ON [Customer].[Email]=[Subscription].[Email]");
            paging.Left_Join("[Customers].[Managers] on [Customer].[ManagerId] = [Managers].[ManagerId]");
            paging.Left_Join("[Customers].[Customer] As [Manager] on [Managers].[CustomerId] = [Manager].[CustomerId]");
            paging.Left_Join("[Customers].[CustomerGroup] ON [Customer].[CustomerGroupId]=[CustomerGroup].[CustomerGroupId]");

            return paging;
        }

        protected override SqlPaging Filter(SqlPaging paging)
        {
            paging.Where("[Customer].[CustomerID] Is Not NULL");
            //paging.Where("[Customer].[CustomerID] IN (Select CustomerId From Module.OneSApi_Customer Where ForExport = 1)");
            paging.Where("[OneSApi_Customer].[ForExport] = 1");

            if (FilterModel.CustomerId.HasValue)
                paging.Where("[Customer].[CustomerID] = {0}", FilterModel.CustomerId.Value);

            return paging;
        }

        protected override SqlPaging Sorting(SqlPaging paging)
        {
            if (string.IsNullOrEmpty(FilterModel.Sorting) || FilterModel.SortingType == FilterSortingType.None)
            {
                paging.OrderBy("[OneSApi_Customer].[Changed]");

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

        protected override List<AddUpdateCustomerModel> FillItems(SqlPaging paging)
        {
            return paging.PageItemsList<AddUpdateCustomerModel>(GetCustomerFromReader);
        }

        private AddUpdateCustomerModel GetCustomerFromReader(IDataReader reader)
        {
            return GetFromReader(reader, "");
        }

        public AddUpdateCustomerModel GetOrderCustomerFromReader(IDataReader reader)
        {
            return GetFromReader(reader, "Customer");
        }

        private AddUpdateCustomerModel GetFromReader(IDataReader reader, string prefix)
        {
            var item = new AddUpdateCustomerModel
            {
                Id = SQLDataHelper.GetString(reader, prefix + "CustomerID"),
                FirstName = SQLDataHelper.GetString(reader, prefix + "FirstName"),
                LastName = SQLDataHelper.GetString(reader, prefix + "LastName"),
                Patronymic = SQLDataHelper.GetString(reader, prefix + "Patronymic"),
                Organization = SQLDataHelper.GetString(reader, prefix + "Organization"),
                Email = SQLDataHelper.GetString(reader, prefix + "Email"),
                Phone = SQLDataHelper.GetString(reader, prefix + "Phone"),
                //Country = SQLDataHelper.GetString(reader, "Country"),
                //Region = SQLDataHelper.GetString(reader, "Region"),
                //District = SQLDataHelper.GetString(reader, "District"),
                //City = SQLDataHelper.GetString(reader, "City"),
                //Zip = SQLDataHelper.GetString(reader, "Zip"),
                //CustomField1 = SQLDataHelper.GetString(reader, "CustomField1"),
                //CustomField2 = SQLDataHelper.GetString(reader, "CustomField2"),
                //CustomField3 = SQLDataHelper.GetString(reader, "CustomField3"),
                //Street = SQLDataHelper.GetString(reader, "Street"),
                //House = SQLDataHelper.GetString(reader, "House"),
                //Apartment = SQLDataHelper.GetString(reader, "Apartment"),
                //Structure = SQLDataHelper.GetString(reader, "Structure"),
                //Entrance = SQLDataHelper.GetString(reader, "Entrance"),
                //Floor = SQLDataHelper.GetString(reader, "Floor"),
                RegistrationDate = SQLDataHelper.GetDateTime(reader, prefix + "RegistrationDateTime"),
                Birthday = SQLDataHelper.GetNullableDateTime(reader, prefix + "BirthDay"),
                Subscribed = SQLDataHelper.GetBoolean(reader, prefix + "SubscribedForNews"),
                Salt = SQLDataHelper.GetString(reader, prefix + "Password"),
                CustomerType = (CustomerType)SQLDataHelper.GetInt(reader, prefix + "CustomerType"),
                ClientStatus = (CustomerClientStatus)SQLDataHelper.GetInt(reader, prefix + "ClientStatus"),
                DiscountPercent = SQLDataHelper.GetFloat(reader, prefix + "DiscountPercent"),
                Contact = new CustomerContactModel()
            };
            item.Contact.Country = SQLDataHelper.GetString(reader, prefix + "Country");
            item.Contact.Region = SQLDataHelper.GetString(reader, prefix + "Region");
            item.Contact.District = SQLDataHelper.GetString(reader, prefix + "District");
            item.Contact.City = SQLDataHelper.GetString(reader, prefix + "City");
            item.Contact.Zip = SQLDataHelper.GetString(reader, prefix + "Zip");
            item.Contact.Street = SQLDataHelper.GetString(reader, prefix + "Street");
            item.Contact.House = SQLDataHelper.GetString(reader, prefix + "House");
            item.Contact.Apartment = SQLDataHelper.GetString(reader, prefix + "Apartment");
            item.Contact.Structure = SQLDataHelper.GetString(reader, prefix + "Structure");
            item.Contact.Entrance = SQLDataHelper.GetString(reader, prefix + "Entrance");
            item.Contact.Floor = SQLDataHelper.GetString(reader, prefix + "Floor");

            //if (item.CustomerGroup != null)
            //    model.DiscountPercent = customer.CustomerGroup.GroupDiscount;

            var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(Guid.Parse(item.Id))
                .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
                            (x.CustomerType == item.CustomerType || x.CustomerType == CustomerType.All))
                .Where(x => (x.ValueDateFormat ?? x.Value).IsNotEmpty()).ToList();
            if (customerFields != null && customerFields.Count > 0)
            {
                item.CustomerFields = new List<CustomerFieldValueMapShort>();
                foreach (var field in customerFields)
                    item.CustomerFields.Add(new CustomerFieldValueMapShort { Name = field.Name, Value = field.ValueDateFormat ?? field.Value });
            }

            CustomerFieldWithValue inn = null, org = null;
            if (item.CustomerType == CustomerType.LegalEntity)
            {
                inn = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.INN);
                org = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.CompanyName);
                item.INN = inn != null ? inn.Value : "";
                item.CompanyName = org != null ? org.Value : "";
            }

            return item;
        }
    }
}