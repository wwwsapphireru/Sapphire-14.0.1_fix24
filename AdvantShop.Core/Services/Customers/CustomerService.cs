//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using AdvantShop.Configuration;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Attachments;
using AdvantShop.Core.Services.Bonuses;
using AdvantShop.Core.Services.ChangeHistories;
using AdvantShop.Core.Services.Customers;
using AdvantShop.Core.Services.Helpers;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.Services.Loging;
using AdvantShop.Core.Services.Loging.Calls;
using AdvantShop.Core.Services.Loging.Emails;
using AdvantShop.Core.Services.Loging.Events;
using AdvantShop.Core.Services.Loging.Push;
using AdvantShop.Core.Services.Loging.Smses;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Core.Services.Partners;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Core.Services.Triggers.DeferredDatas;
using AdvantShop.Core.SQL;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;

namespace AdvantShop.Customers
{
    public class CustomerService
    {
        private const string GuestCookieName = "customer_guest";

        private static IEmailLogger EmailLogger => LoggingManager.GetEmailLogger();
        private static ISmsLogger SmsLogger => LoggingManager.GetSmsLogger();
        private static IEventLogger EventLogger => LoggingManager.GetEventLogger();
        private static ICallLogger CallLogger => LoggingManager.GetCallLogger();
        private static IPushLogger PushLogger => LoggingManager.GetPushLogger();

        public static void DeleteCustomer(Guid customerId, bool trackChanges = true, ChangedBy changedBy = null)
        {
            var customer = GetCustomer(customerId);

            SubscriptionService.Unsubscribe(customer.EMail);
            BonusSystem.DeleteCard(customer);
            SQLDataAccess.ExecuteNonQuery("[Customers].[sp_DeleteCustomer]", CommandType.StoredProcedure,
                new SqlParameter("@CustomerID", customerId));

            ModulesExecuter.DeleteCustomer(customerId);

            TriggerDeferredDataService.Delete(ETriggerObjectType.Customer, customer.InnerId);

            AttachmentService.DeleteAttachments<CheckoutAttachment>(customerId.GetHashCode());

            CacheManager.RemoveByPattern(CacheNames.Customer);
            
            if (trackChanges)
                CustomerHistoryService.DeleteCustomer(customer, changedBy);
        }

        public static void DeleteContact(Guid contactId, bool trackChanges = true, ChangedBy changedBy = null, 
            bool executeModules = true)
        {
            var contact = GetCustomerContact(contactId);
            if (contact == null)
                return;
            
            SQLDataAccess.ExecuteNonQuery("[Customers].[sp_DeleteCustomerContact]", CommandType.StoredProcedure,
                new SqlParameter("@ContactID", contactId));
            
            SetMainContact(false, contact.CustomerGuid, contact.ContactId);

            if (executeModules)
                ModulesExecuter.DeleteContact(contactId);
            
            CacheManager.RemoveByPattern(CacheNames.Customer);
            
            if (trackChanges)
                CustomerHistoryService.DeleteContact(contactId, changedBy);
        }

        public static int GetCustomerGroupId(Guid customerId)
        {
            return
                SQLDataHelper.GetInt(
                    SQLDataAccess.ExecuteScalar(
                        "SELECT [CustomerGroupId] FROM [Customers].[Customer] WHERE [CustomerID] = @CustomerID",
                        CommandType.Text, new SqlParameter { ParameterName = "@CustomerID", Value = customerId }),
                    CustomerGroupService.DefaultCustomerGroup);
        }

        public static Customer GetCustomer(Guid customerId)
        {
            var cacheName = CacheNames.Customer + customerId;

            if (!CacheManager.TryGetValue(cacheName, out Customer customer))
            {
                customer = GetCustomerFromDb(customerId);
                CacheManager.Insert(cacheName, customer ?? new Customer());
            }

            return customer != null && customer.Id != Guid.Empty ? customer : null;
        }

        public static Customer GetCustomerFromDb(Guid customerId)
        {
            return SQLDataAccess.ExecuteReadOne<Customer>(
                    "SELECT TOP 1 * FROM [Customers].[Customer] WHERE [CustomerID] = @CustomerID",
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@CustomerID", customerId));
        }

        public static Customer GetCustomer(int innerId)
        {
            return
                SQLDataAccess.ExecuteReadOne<Customer>(
                    "SELECT TOP 1 * FROM [Customers].[Customer] WHERE [InnerId] = @InnerId",
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@InnerId", innerId));
        }

        public static List<Customer> GetCustomersByRole(Role role)
        {
            return
                SQLDataAccess.ExecuteReadList<Customer>(
                    "SELECT * FROM [Customers].[Customer] WHERE [CustomerRole] = @CustomerRole",
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@CustomerRole", ((int)role).ToString()));
        }

        public static List<Customer> GetCustomersByRoles(Role role, params Role[] roles)
        {
            var rolesList = new List<int>() { (int)role };
            rolesList.AddRange(roles.Where(x => role != x).Select(x => (int)x));
            return
                SQLDataAccess.ExecuteReadList<Customer>(
                    string.Format("SELECT * FROM [Customers].[Customer] WHERE [CustomerRole] in ({0})", rolesList.AggregateString(",")),
                    CommandType.Text,
                    GetFromSqlDataReader);
        }

        public static List<Customer> GetCustomersForAutocomplete(string query, int? limit = null)
        {
            var top = limit != null && limit > 0 ? "top(" + limit.Value + ")" : ""; 
            
            if (query.IsDecimal())
            {
                return
                    SQLDataAccess.ExecuteReadList(
                        "SELECT " + top + " * FROM [Customers].[Customer] " +
                                       "WHERE [CustomerRole] = @CustomerRole AND (" +

                                       (query.Length >= 6
                                           ? "[Phone] like '%' + @q + '%' " +
                                             "OR [StandardPhone] like '%' + @q + '%' " +
                                             "OR "
                                           : "") +

                                       "[Email] like @q + '%')",
                        CommandType.Text,
                        GetFromSqlDataReader,
                        new SqlParameter("@q", query),
                        new SqlParameter("@CustomerRole", ((int) Role.User).ToString()));
            }

            var translitKeyboard = StringHelper.TranslitToRusKeyboard(query);

            var customerType = string.Empty;
            if (SettingsCustomers.IsRegistrationAsLegalEntity)
                customerType = !SettingsCustomers.IsRegistrationAsPhysicalEntity ? " AND CustomerType = " + (int)CustomerType.LegalEntity : string.Empty;
            else
                customerType = " AND CustomerType = " + (int)CustomerType.PhysicalEntity;

            return
                SQLDataAccess.ExecuteReadList(
                    "SELECT " + top + " * FROM [Customers].[Customer] " +
                    "WHERE [CustomerRole] = @CustomerRole AND " +
                    "([FirstName] like @q + '%' OR [FirstName] like @qtr + '%' " +
                    "OR [LastName] like @q + '%' OR [LastName] like @qtr + '%' " +
                    "OR [Organization] like @q + '%' OR [Organization] like @qtr + '%' " +
                    "OR [Phone] like '%' + @q + '%' " +
                    "OR [Email] like @q + '%')" + customerType,
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@q", query),
                    new SqlParameter("@qtr", translitKeyboard),
                    new SqlParameter("@CustomerRole", ((int)Role.User).ToString()));
            
        }

        public static List<Customer> GetCustomersForAutocomplete(string firstName, string lastName, string patronymic, int? limit = null)
        {
            var top = limit > 0 ? $"top({limit.Value})" : string.Empty; 

            var firstNameTranslitKeyboard = StringHelper.TranslitToRusKeyboard(firstName);
            var lastNameTranslitKeyboard = StringHelper.TranslitToRusKeyboard(lastName);
            var patronymicTranslitKeyboard = StringHelper.TranslitToRusKeyboard(patronymic);
            
            var whereList = new List<string>();
            if (firstName.IsNotEmpty())
                whereList.Add("([FirstName] like @FirstName + '%' OR [FirstName] like @FirstNameTr + '%')");
            if (lastName.IsNotEmpty())
                whereList.Add("([LastName] like @LastName + '%' OR [LastName] like @LastNameTr + '%')");
            if (patronymic.IsNotEmpty())
                whereList.Add("([Patronymic] like @Patronymic + '%' OR [Patronymic] like @PatronymicTr + '%')");

            if (SettingsCustomers.IsRegistrationAsLegalEntity)
            {
                if(!SettingsCustomers.IsRegistrationAsPhysicalEntity)
                    whereList.Add("CustomerType = " + (int) CustomerType.LegalEntity);
            }
            else
                whereList.Add("CustomerType = " + (int)CustomerType.PhysicalEntity);

            return
                SQLDataAccess.ExecuteReadList(
                    "SELECT " + top + " * FROM [Customers].[Customer] " +
                    "WHERE [CustomerRole] = @CustomerRole AND "  + string.Join(" AND ", whereList),
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@FirstName", firstName ?? (object) DBNull.Value),
                    new SqlParameter("@LastName", lastName ?? (object) DBNull.Value),
                    new SqlParameter("@Patronymic", patronymic ?? (object) DBNull.Value),
                    new SqlParameter("@FirstNameTr", firstNameTranslitKeyboard ?? (object) DBNull.Value),
                    new SqlParameter("@LastNameTr", lastNameTranslitKeyboard ?? (object) DBNull.Value),
                    new SqlParameter("@PatronymicTr", patronymicTranslitKeyboard ?? (object) DBNull.Value),
                    new SqlParameter("@CustomerRole", ((int)Role.User).ToString()));
        }

        public static List<Customer> GetCustomersByPhoneForAutocomplete(string query, int? limit = null)
        {
            var top = limit != null && limit > 0 ? "top(" + limit.Value + ")" : "";

            var standardPhone = StringHelper.ConvertToStandardPhone(query, true, true);

            return
                SQLDataAccess.ExecuteReadList(
                    "SELECT " + top + " * FROM [Customers].[Customer] " +
                    "WHERE [CustomerRole] = @CustomerRole AND (" +
                    "Phone like @q + '%' " +
                    (standardPhone != null ? "OR convert(nvarchar, StandardPhone) like @standardPhone + '%'" : "") +
                    ")",
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@q", query),
                    new SqlParameter("@standardPhone", standardPhone?.ToString() ?? (object) DBNull.Value),
                    new SqlParameter("@CustomerRole", ((int) Role.User).ToString()));
        }

        public static bool ExistsCustomer(Guid customerId)
        {
            bool isExist;
            var cacheName = CacheNames.Customer + customerId + "_ExistsCustomer";

            if (!CacheManager.TryGetValue(cacheName, out isExist))
            {
                isExist = SQLDataAccess.ExecuteScalar(
                              "SELECT [CustomerID] FROM [Customers].[Customer] WHERE [CustomerID] = @CustomerID",
                              CommandType.Text,
                              new SqlParameter("@CustomerID", customerId)) != null;

                CacheManager.Insert(cacheName, isExist, 1);
            }
            return isExist;
        }

        public static Customer GetCustomerByEmail(string email)
        {
            return SQLDataAccess.ExecuteReadOne<Customer>(
                "[Customers].[sp_GetCustomerByEmail]", CommandType.StoredProcedure,
                GetFromSqlDataReader, new SqlParameter("@Email", email));
        }

        public static List<Customer> GetCustomersByPhone(string phone)
        {
            var phoneLong = StringHelper.ConvertToStandardPhone(phone);
            return SQLDataAccess.ExecuteReadList<Customer>(
                "Select * from Customers.Customer where Phone=@phone " + (phoneLong.HasValue ? "or StandardPhone=@phoneLong" : string.Empty), CommandType.Text,
                GetFromSqlDataReader,
                new SqlParameter("@phone", phone),
                new SqlParameter("@phoneLong", phoneLong ?? (object)DBNull.Value)
                );
        }

        public static Customer GetCustomerByPhone(string phone, long? standardPhone)
        {
            return SQLDataAccess.ExecuteReadOne(
                "Select top(1) * " +
                "From Customers.Customer " +
                "Where Phone=@Phone " + (standardPhone != null && standardPhone != 0 ? "or StandardPhone=@StandardPhone" : ""),
                CommandType.Text,
                GetFromSqlDataReader,
                new SqlParameter("@Phone", phone),
                new SqlParameter("@StandardPhone", standardPhone ?? (object)DBNull.Value));
        }


        public static Customer GetCustomerByEmailAndPhone(string email, string phone, long? standardPhone)
        {
            return SQLDataAccess.ExecuteReadOne(
                "Select top(1) * " +
                "From Customers.Customer " +
                "Where Email = @Email And (Phone=@Phone " + (standardPhone != null && standardPhone != 0 ? "or StandardPhone=@StandardPhone" : "") + ")",
                CommandType.Text,
                GetFromSqlDataReader,
                new SqlParameter("@Email", email),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@StandardPhone", standardPhone ?? (object) DBNull.Value));
        }

        public static List<Customer> GetCustomerBirthdayInTheNextWeek()
        {
            var dateFrom = DateTime.Today.AddDays(-3);
            var dateTo = DateTime.Today.AddDays(7);
            var list =
                SQLDataAccess.ExecuteReadList<Customer>(
                    "SELECT TOP(5) * " +
                    "FROM [Customers].[Customer] " +
                    "Where Enabled = 1 and [BirthDay] is not null and " +
                    "(CustomerRole = " + (int)Role.Administrator + " or CustomerRole = " + (int)Role.Moderator + ") and " +
                    "( " +
                    "Convert(Date, '" + dateTo.Year + "' + '-' + Convert(Varchar, Month(BirthDay)) + '-' + Convert(Varchar,Day(BirthDay))) Between @DateFrom And @DateTo " +
                    (dateFrom.Year != dateTo.Year
                        ? "or Convert(Date, '" + dateFrom.Year + "' + '-'  + Convert(Varchar, Month(BirthDay)) + '-' + Convert(Varchar,Day(BirthDay))) Between @DateFrom And @DateTo "
                        : "") +
                    ") " +
                    "Order by Month(BirthDay), Day(BirthDay)",
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@DateFrom", dateFrom),
                    new SqlParameter("@DateTo", dateTo));

            return list;
        }

        public static Customer GetCustomerByRole(Role role)
        {
            return SQLDataAccess.ExecuteReadOne<Customer>(
                "Select top(1) * from Customers.Customer where CustomerRole=@CustomerRole", CommandType.Text,
                GetFromSqlDataReader, new SqlParameter { ParameterName = "@CustomerRole", Value = role });
        }


        public static Customer GetCustomerByOpenAuthIdentifier(string identifier)
        {
            return SQLDataAccess.ExecuteReadOne<Customer>(
                "[Customers].[sp_GetCustomerByOpenAuthIdentifier]", CommandType.StoredProcedure,
                GetFromSqlDataReader, new SqlParameter { ParameterName = "@Identifier", Value = identifier });
        }

        public static Customer GetFromSqlDataReader(SqlDataReader reader)
        {
            var customer = new Customer(true)
            {
                Id = SQLDataHelper.GetGuid(reader, "CustomerID"),
                CustomerGroupId = SQLDataHelper.GetInt(reader, "CustomerGroupId", 0),
                EMail = SQLDataHelper.GetString(reader, "EMail"),
                FirstName = SQLDataHelper.GetString(reader, "FirstName"),
                LastName = SQLDataHelper.GetString(reader, "LastName"),
                Patronymic = SQLDataHelper.GetString(reader, "Patronymic"),
                RegistrationDateTime = SQLDataHelper.GetDateTime(reader, "RegistrationDateTime"),
                Phone = SQLDataHelper.GetString(reader, "Phone"),
                StandardPhone = SQLDataHelper.GetNullableLong(reader, "StandardPhone"),
                Password = SQLDataHelper.GetString(reader, "Password"),
                CustomerRole = (Role)SQLDataHelper.GetInt(reader, "CustomerRole"),
                AdminComment = SQLDataHelper.GetString(reader, "AdminComment"),
                ManagerId = SQLDataHelper.GetNullableInt(reader, "ManagerId"),
                Rating = SQLDataHelper.GetInt(reader, "Rating"),
                Avatar = SQLDataHelper.GetString(reader, "Avatar"),
                Enabled = SQLDataHelper.GetBoolean(reader, "Enabled"),
                HeadCustomerId = SQLDataHelper.GetNullableGuid(reader, "HeadCustomerId"),
                BirthDay = SQLDataHelper.GetNullableDateTime(reader, "BirthDay"),
                City = SQLDataHelper.GetString(reader, "City"),
                InnerId = SQLDataHelper.GetInt(reader, "InnerId"),
                SortOrder = SQLDataHelper.GetInt(reader, "SortOrder"),
                Organization = SQLDataHelper.GetString(reader, "Organization"),
                ClientStatus = (CustomerClientStatus)SQLDataHelper.GetInt(reader, "ClientStatus"),
                RegisteredFrom = SQLDataHelper.GetString(reader, "RegisteredFrom"),
                RegisteredFromIp = SQLDataHelper.GetString(reader, "RegisteredFromIp"),
                CustomerType = (CustomerType)SQLDataHelper.GetInt(reader, "CustomerType"),
            };

            return customer;
        }

        public static CustomerContact GetContactFromSqlDataReader(SqlDataReader reader)
        {
            var contact = new CustomerContact
            {
                ContactId = SQLDataHelper.GetGuid(reader, "ContactID"),
                CustomerGuid = SQLDataHelper.GetGuid(reader, "CustomerID"),
                Name = SQLDataHelper.GetString(reader, "Name"),
                City = SQLDataHelper.GetString(reader, "City"),
                District = SQLDataHelper.GetString(reader, "District"),
                Country = SQLDataHelper.GetString(reader, "Country"),
                Zip = SQLDataHelper.GetString(reader, "Zip"),
                Region = SQLDataHelper.GetString(reader, "Zone"),
                CountryId = SQLDataHelper.GetInt(reader, "CountryID"),
                RegionId = SQLDataHelper.GetNullableInt(reader, "RegionID"),

                Street = SQLDataHelper.GetString(reader, "Street"),
                House = SQLDataHelper.GetString(reader, "House"),
                Apartment = SQLDataHelper.GetString(reader, "Apartment"),
                Structure = SQLDataHelper.GetString(reader, "Structure"),
                Entrance = SQLDataHelper.GetString(reader, "Entrance"),
                Floor = SQLDataHelper.GetString(reader, "Floor"),
                DadataJson = SQLDataHelper.GetString(reader, "DadataJson", null),
                IsMain = SQLDataHelper.GetBoolean(reader, "IsMain")
            };

            return contact;
        }

        public static CustomerContact GetCustomerContact(string contactId)
        {
            var id = Guid.Empty;
            if (Guid.TryParse(contactId, out id))
            {
                return GetCustomerContact(id);
            }
            return null;
        }

        public static CustomerContact GetCustomerContact(Guid contactId)
        {
            var contact = SQLDataAccess.ExecuteReadOne(
                "SELECT TOP 1 * FROM [Customers].[Contact] WHERE [ContactID] = @id",
                CommandType.Text,
                GetContactFromSqlDataReader,
                new SqlParameter("@id", contactId));

            return contact;
        }

        public static List<CustomerContact> GetCustomerContacts(Guid customerId)
        {
            if (CustomerContext.CurrentCustomer?.Id == customerId 
                && CustomerContext.CurrentCustomer?.CustomerRole == Role.Guest)
                return new List<CustomerContact>();
            
            return SQLDataAccess.ExecuteReadList(
                @"SELECT * 
                FROM [Customers].[Contact] 
                WHERE CustomerId = @CustomerId 
                ORDER BY IsMain DESC",
                CommandType.Text, 
                GetContactFromSqlDataReader,
                new SqlParameter("@CustomerId", customerId)
            );
        }

        public static IList<Customer> GetCustomers()
        {
            return SQLDataAccess.ExecuteReadList<Customer>("SELECT * FROM [Customers].[Customer]", CommandType.Text,
                GetFromSqlDataReader);
        }

        public static IList<Customer> GetCustomers(DateTime from, DateTime to, int? topNum = null)
        {
            return SQLDataAccess.ExecuteReadList<Customer>(
                topNum.HasValue
                    ? "SELECT TOP(" + topNum.Value + ") * FROM [Customers].[Customer] Where RegistrationDateTime >= @from and RegistrationDateTime <= @to"
                    : "SELECT * FROM [Customers].[Customer] Where RegistrationDateTime >= @from and RegistrationDateTime <= @to",
                CommandType.Text,
                GetFromSqlDataReader,
                new SqlParameter("@from", from),
                new SqlParameter("@to", to));
        }

        public static Dictionary<Guid, long> GetCustomersPhones()
        {
            var dict = new Dictionary<Guid, long>();
            dict.AddRange(
                SQLDataAccess.ExecuteReadIEnumerable<KeyValuePair<Guid, long>>(
                    "SELECT distinct CustomerId, StandardPhone FROM [Customers].[Customer] where StandardPhone is not null",
                    CommandType.Text,
                    reader =>
                        new KeyValuePair<Guid, long>(SQLDataHelper.GetGuid(reader, "CustomerID"),
                            SQLDataHelper.GetLong(reader, "StandardPhone"))));
            return dict;
        }

        public static Dictionary<Guid, long> GetSubscribedCustomersPhones()
        {
            var dict = new Dictionary<Guid, long>();
            dict.AddRange(
                SQLDataAccess.ExecuteReadIEnumerable<KeyValuePair<Guid, long>>(
                    "SELECT distinct CustomerId, StandardPhone FROM [Customers].[Customer] " +
                    "INNER JOIN [Customers].[Subscription] ON [Subscription].[Email] = [Customer].[Email] " +
                    "where StandardPhone is not null AND Subscribe = 1",
                    CommandType.Text,
                    reader =>
                        new KeyValuePair<Guid, long>(SQLDataHelper.GetGuid(reader, "CustomerID"),
                            SQLDataHelper.GetLong(reader, "StandardPhone"))));
            return dict;
        }


        public static Guid AddContact(CustomerContact contact, Guid customerId, bool executeModules = true)
        {
            var id = SQLDataAccess.ExecuteScalar(
                "INSERT INTO [Customers].[Contact] " +
                "(CustomerID, Name, Country, City, Zone, Zip, CountryID, RegionID, Street, House, Apartment, Structure, Entrance, Floor, District, DadataJson, IsMain) OUTPUT Inserted.ContactID VALUES " +
                "(@CustomerID, @Name, @Country, @City, @Zone, @Zip, @CountryID, @RegionID, @Street, @House, @Apartment, @Structure, @Entrance, @Floor, @District, @DadataJson, @IsMain); ",
                CommandType.Text,

                new SqlParameter("@CustomerID", customerId),
                new SqlParameter("@Name", contact.Name.Default("").Reduce(150)),
                new SqlParameter("@Country", contact.Country.Default("").Reduce(70)),
                new SqlParameter("@City", contact.City.Default("").Reduce(70)),
                new SqlParameter("@District", contact.District.Default("").Reduce(70)),
                new SqlParameter("@Zone", contact.Region.Default("").Reduce(70)),
                new SqlParameter("@Zip", contact.Zip.Default("").Reduce(70)),
                new SqlParameter("@CountryID", contact.CountryId != 0 ? contact.CountryId : (object) DBNull.Value),
                new SqlParameter("@RegionID", contact.RegionId.HasValue && contact.RegionId > 0 ? contact.RegionId : (object) DBNull.Value),

                new SqlParameter("@Street", contact.Street.Default("").Reduce(255)),
                new SqlParameter("@House", contact.House.Default("").Reduce(50)),
                new SqlParameter("@Apartment", contact.Apartment.Default("").Reduce(50)),
                new SqlParameter("@Structure", contact.Structure.Default("").Reduce(10)),
                new SqlParameter("@Entrance", contact.Entrance.Default("").Reduce(10)),
                new SqlParameter("@Floor", contact.Floor.Default("").Reduce(10)),
                new SqlParameter("@DadataJson", contact.DadataJson ?? (object) DBNull.Value),
                new SqlParameter("@IsMain", contact.IsMain)
            );

            contact.CustomerGuid = customerId;
            contact.ContactId = SQLDataHelper.GetGuid(id);

            SetMainContact(contact.IsMain, contact.CustomerGuid, contact.ContactId);

            if (executeModules)
                ModulesExecuter.AddContact(contact);

            CacheManager.RemoveByPattern(CacheNames.Customer + customerId);

            return contact.ContactId;
        }

        public static void UpdateContact(CustomerContact contact, bool trackChanges = true, ChangedBy changedBy = null, 
            bool executeModules = true)
        {
            if (trackChanges)
               CustomerHistoryService.TrackContactChanges(contact, changedBy);
            
            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Contact] Set " +
                "Name=@Name, Country=@Country, City=@City, Zone=@Zone, Zip=@Zip, CountryID=@CountryID, RegionID=@RegionID, " +
                "Street=@Street, House=@House, Apartment=@Apartment, Structure=@Structure, Entrance=@Entrance, Floor=@Floor, " +
                "District=@District, DadataJson=@DadataJson, IsMain=@IsMain " +
                "WHERE ContactID = @ContactID ",
                CommandType.Text,

                new SqlParameter("@ContactID", contact.ContactId),
                new SqlParameter("@Name", contact.Name.Default("").Reduce(150)),
                new SqlParameter("@Country", contact.Country.Default("").Reduce(70)),
                new SqlParameter("@City", contact.City.Default("").Reduce(70)),
                new SqlParameter("@District", contact.District.Default("").Reduce(70)),
                new SqlParameter("@Zone", contact.Region.Default("").Reduce(70)),
                new SqlParameter("@Zip", contact.Zip.Default("").Reduce(70)),
                new SqlParameter("@CountryID", contact.CountryId != 0 ? contact.CountryId : (object)DBNull.Value),
                new SqlParameter("@RegionID", contact.RegionId.HasValue && contact.RegionId > 0 ? contact.RegionId : (object)DBNull.Value),

                new SqlParameter("@Street", contact.Street.Default("").Reduce(255)),
                new SqlParameter("@House", contact.House.Default("").Reduce(50)),
                new SqlParameter("@Apartment", contact.Apartment.Default("").Reduce(50)),
                new SqlParameter("@Structure", contact.Structure.Default("").Reduce(10)),
                new SqlParameter("@Entrance", contact.Entrance.Default("").Reduce(10)),
                new SqlParameter("@Floor", contact.Floor.Default("").Reduce(10)),
                new SqlParameter("@DadataJson", contact.DadataJson ?? (object) DBNull.Value),
                new SqlParameter("@IsMain", contact.IsMain)
            );
            
            SetMainContact(contact.IsMain, contact.CustomerGuid, contact.ContactId);

            if (executeModules)
                ModulesExecuter.UpdateContact(contact);

            CacheManager.RemoveByPattern(CacheNames.Customer + contact.CustomerGuid);
        }

        public static void SetMainContact(bool isMain, Guid customerId, Guid contactId)
        {
            SQLDataAccess.ExecuteNonQuery(
                "if (@IsMain = 1) " +
                "begin " +
                    "Update [Customers].[Contact] Set IsMain = 0 Where CustomerID=@CustomerID and ContactID <> @ContactID " +
                    "Update [Customers].[Contact] Set IsMain = 1 Where CustomerID=@CustomerID and ContactID = @ContactID " +
                "end " +
                "else if not exists (Select 1 From [Customers].[Contact] Where CustomerID=@CustomerID and IsMain=1) " +
                "begin " +
                    "if exists (Select 1 From [Customers].[Contact] Where CustomerID=@CustomerID and ContactID <> @ContactID) " +
                        "Update [Customers].[Contact] Set IsMain = 1 Where ContactId = (Select top(1) ContactId From [Customers].[Contact] Where CustomerID=@CustomerID and ContactID <> @ContactID) " +
                    "else if exists (Select 1 From [Customers].[Contact] Where CustomerID=@CustomerID) " +
                        "Update [Customers].[Contact] Set IsMain = 1 Where ContactId = (Select top(1) ContactId From [Customers].[Contact] Where CustomerID=@CustomerID) " +
                "end",
                CommandType.Text,
                new SqlParameter("@IsMain", isMain),
                new SqlParameter("@CustomerID", customerId),
                new SqlParameter("@ContactID", contactId)
            );
            
            CacheManager.RemoveByPattern(CacheNames.Customer + customerId);
        }

        public static bool UpdateCustomer(Customer customer, bool trackChanges = true, ChangedBy changedBy = null)
        {
            if (customer == null)
                return false;
            
            if (trackChanges)
                CustomerHistoryService.TrackChanges(customer, changedBy);

            SQLDataAccess.ExecuteNonQuery(
                @"UPDATE [Customers].[Customer]
                SET [FirstName]       = @FirstName,
                    [LastName]        = @LastName,
                    [Patronymic]      = @Patronymic,
                    [Phone]           = @Phone,
                    [StandardPhone]   = @StandardPhone,
                    [Email]           = @Email,
                    [CustomerGroupId] = @CustomerGroupId,
                    [CustomerRole]    = @CustomerRole,
                    [AdminComment]    = @AdminComment,
                    [ManagerId]       = @ManagerId,
                    [Rating]          = @Rating,
                    [Avatar]          = @Avatar,
                    [Enabled]         = @Enabled,
                    [HeadCustomerId]  = @HeadCustomerId,
                    [BirthDay]        = @BirthDay,
                    [City]            = @City,
                    [SortOrder]       = @SortOrder,
                    [Organization]    = @Organization,
                    [ClientStatus]    = @ClientStatus,
                    [CustomerType]    = @CustomerType
                WHERE customerid = @customerid",
                CommandType.Text,
                new SqlParameter("@CustomerID", customer.Id),
                new SqlParameter("@FirstName", customer.FirstName.Default(string.Empty).Reduce(70)),
                new SqlParameter("@LastName", customer.LastName.Default(string.Empty).Reduce(70)),
                new SqlParameter("@Patronymic", customer.Patronymic.Default(string.Empty).Reduce(70)),
                new SqlParameter("@Phone", customer.Phone.Default(string.Empty)),
                new SqlParameter("@StandardPhone", customer.StandardPhone ?? (object)DBNull.Value),
                new SqlParameter("@Email", customer.EMail.Default(string.Empty).Reduce(100)),
                new SqlParameter("@CustomerGroupId", customer.CustomerGroupId == 0 
                    ? (object)DBNull.Value 
                    : customer.CustomerGroupId
                ),
                new SqlParameter("@CustomerRole", customer.CustomerRole),
                new SqlParameter("@AdminComment", customer.AdminComment ?? (object)DBNull.Value),
                new SqlParameter("@ManagerId", customer.ManagerId ?? (object)DBNull.Value),
                new SqlParameter("@Rating", customer.Rating),
                new SqlParameter("@Avatar", customer.Avatar?.Reduce(100) ?? (object)DBNull.Value),
                new SqlParameter("@Enabled", customer.Enabled),
                new SqlParameter("@HeadCustomerId", customer.HeadCustomerId ?? (object)DBNull.Value),
                new SqlParameter("@BirthDay", customer.BirthDay ?? (object)DBNull.Value),
                new SqlParameter("@City", customer.City?.Reduce(70) ?? (object)DBNull.Value),
                new SqlParameter("@SortOrder", customer.SortOrder),
                new SqlParameter("@Organization", customer.Organization?.Reduce(250) ?? (object)DBNull.Value),
                new SqlParameter("@ClientStatus", (int)customer.ClientStatus),
                new SqlParameter("@CustomerType", customer.CustomerType)
            );

            if (customer.EMail.IsNotEmpty() &&
                SubscriptionService.IsSubscribe(customer.EMail) != customer.IsAgreeForPromotionalNewsletter)
            {
                if (customer.IsAgreeForPromotionalNewsletter)
                {
                    SubscriptionService.Subscribe(customer.EMail);
                }
                else
                {
                    SubscriptionService.Unsubscribe(customer.EMail);
                }
            }

            var tags = customer.Tags;
            TagService.DeleteMap(customer.Id);
            if (tags != null && tags.Count != 0)
            {
                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = TagService.Get(tags[i].Name);
                    tags[i].Id = tag == null ? TagService.Add(tags[i]) : tag.Id;
                    TagService.AddMap(customer.Id, tags[i].Id, i * 10);
                }
            }

            ModulesExecuter.UpdateCustomer(customer);

            CacheManager.RemoveByPattern(CacheNames.Customer + customer.Id);

            return true;
        }

        public static void UpdateCustomerEmail(Guid id, string email, bool trackChanges = true, ChangedBy changedBy = null)
        {
            if (trackChanges)
                CustomerHistoryService.TrackEmailChanges(id, email, changedBy);
            
            SQLDataAccess.ExecuteNonQuery("Update Customers.Customer Set Email = @Email Where CustomerID = @CustomerID",
                CommandType.Text, 
                new SqlParameter("@CustomerID", id), 
                new SqlParameter("@Email", email));

            ModulesExecuter.UpdateCustomer(id);

            CacheManager.RemoveByPattern(CacheNames.Customer + id);
        }

        public static void UpdateCustomerPassword(Guid id, string password, bool trackChanges = true, ChangedBy changedBy = null)
        {
            if (trackChanges)
                CustomerHistoryService.TrackEmailChanges(id, password, changedBy);

            SQLDataAccess.ExecuteNonQuery("Update Customers.Customer Set Password = @Password Where CustomerID = @CustomerID",
                CommandType.Text,
                new SqlParameter("@CustomerID", id),
                new SqlParameter("@Password", password));

            ModulesExecuter.UpdateCustomer(id);

            CacheManager.RemoveByPattern(CacheNames.Customer + id);
        }

        public static Customer GetCustomerByEmailAndPassword(string email, string password, bool isHash)
        {
            return SQLDataAccess.ExecuteReadOne("[Customers].[sp_GetCustomerByEmailAndPassword]",
                CommandType.StoredProcedure, GetFromSqlDataReader,
                new SqlParameter("@Email", email),
                new SqlParameter("@Password", isHash ? password : SecurityHelper.GetPasswordHash(password)));
        }
        
        public static Customer GetCustomerByStandardPhoneAndPassword(long standardPhone, string password, bool isHash)
        {
            return SQLDataAccess.ExecuteReadOne("SELECT * FROM [Customers].[Customer] WHERE ([StandardPhone] = @StandardPhone) AND ([Password] = @Password)",
                CommandType.Text, GetFromSqlDataReader,
                new SqlParameter("@StandardPhone", standardPhone),
                new SqlParameter("@Password", isHash ? password : SecurityHelper.GetPasswordHash(password)));
        }
        
        public static Customer GetCustomerByIdAndPassword(Guid id, string password, bool isHash)
        {
            return SQLDataAccess.ExecuteReadOne("SELECT * FROM [Customers].[Customer] WHERE ([CustomerID] = @CustomerID) AND ([Password] = @Password)",
                CommandType.Text, GetFromSqlDataReader,
                new SqlParameter("@CustomerID", id),
                new SqlParameter("@Password", isHash ? password : SecurityHelper.GetPasswordHash(password)));
        }

        [Obsolete("This method is obsolete. Use ConvertAddressToString instead.", false)]
        public static string ConvertToLinedAddress(CustomerContact cc)
        {
            return ConvertAddressToString(cc,
            f => f.Country,
            f => f.Region,
            f => f.District,
            f => f.City,
            f => f.Zip,
            f => f.Street,
            f => f.House,
            f => f.Structure,
            f => f.Apartment,
            f => f.Entrance,
            f => f.Floor);
        }

        /// <summary>
        /// Converts <see cref="CustomerContact"/> to a localized string
        /// </summary>
        /// <param name="customerContact">customer contact object to be converted</param>
        /// <param name="fields">
        /// lambda expression of customer contacts fields, which one should be used.
        /// <b>Order matters!</b>
        /// </param>
        /// <returns>a localized address string</returns>
        /// <exception cref="ArgumentNullException">throws if fields params not set</exception>
        public static string ConvertAddressToString(CustomerContact customerContact, params Expression<Func<CustomerContact, object>>[] fields)
        {
            if (fields is null || fields.Length == 0)
                throw new ArgumentNullException(nameof(fields));

            List<string> addressList = new List<string>();

            foreach (var field in fields)
            {
                var body = (MemberExpression)field.Body;
                var value = field.Compile().Invoke(customerContact);

                var strValue = value?.ToString();
                if (string.IsNullOrWhiteSpace(strValue?.Trim()))
                    continue;

                var attributeData = body.Member.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(LocalizeAttribute));
                var resourceKey = attributeData?.ConstructorArguments.FirstOrDefault();

                addressList.Add(resourceKey is null
                    ? strValue
                    : $"{LocalizationService.GetResource((string)resourceKey.Value.Value)} {strValue}");
            }

            return string.Join(", ", addressList);
        }
        
        public static bool ExistsEmail(string strUserEmail)
        {
            if (String.IsNullOrEmpty(strUserEmail))
            {
                return false;
            }

            bool boolRes =
                SQLDataAccess.ExecuteScalar<int>(
                    "SELECT COUNT(CustomerID) FROM [Customers].[Customer] WHERE [Email] = @Email;", CommandType.Text,
                    new SqlParameter("@Email", strUserEmail)) > 0;

            return boolRes;
        }

        public static void ChangePassword(Guid customerId, string strNewPassword, bool isPassHashed, bool trackChanges = true, ChangedBy changedBy = null)
        {
            SQLDataAccess.ExecuteNonQuery(
                "[Customers].[sp_ChangePassword]",
                CommandType.StoredProcedure,
                new SqlParameter("@CustomerID", customerId),
                new SqlParameter("@Password", isPassHashed ? strNewPassword : SecurityHelper.GetPasswordHash(strNewPassword)));

            if (trackChanges)
                CustomerHistoryService.TrackPasswordChanges(customerId, changedBy);

            CacheManager.RemoveByPattern(CacheNames.Customer + customerId);            

            if (CustomerContext.CustomerId == customerId)
                Security.AuthorizeService.SignIn(CustomerContext.CurrentCustomer.EMail, strNewPassword, false, true);
        }

        public static bool IsNewCustomerValid(Customer customer)
        {
            if (!string.IsNullOrEmpty(customer.EMail) && IsEmailExist(customer.EMail))
                return false;

            if ((!string.IsNullOrEmpty(customer.Phone) || (customer.StandardPhone != null && customer.StandardPhone != 0)) &&
                IsPhoneExist(customer.Phone, customer.StandardPhone))
            {
                return false;
            }

            return true;
        }

        public static Guid InsertNewCustomer(
            Customer customer, 
            List<CustomerFieldWithValue> customerFields = null, 
            bool trackChanges = true, 
            ChangedBy changedBy = null, 
            bool processTriggers = true
        )
        {
            if (!IsNewCustomerValid(customer))
                return customer.Id = Guid.Empty;
            
            var regFromUri = HttpContext.Current != null 
                             && HttpContext.Current.TryGetRequest(out var request) 
                             && CustomerContext.CurrentCustomer != null 
                             && !CustomerContext.CurrentCustomer.IsAdmin 
                             && !CustomerContext.CurrentCustomer.IsModerator
                ? request.GetUrlReferrer() ?? request.Url
                : null;
            var regFromIp = HttpContext.Current != null 
                            && CustomerContext.CurrentCustomer != null 
                            && !CustomerContext.CurrentCustomer.IsAdmin 
                            && !CustomerContext.CurrentCustomer.IsModerator 
                ? HttpContext.Current.TryGetIp()
                : null;

            if (!string.IsNullOrEmpty(customer.EMail))
                customer.EMail = customer.EMail.ToLower();
            
            if (!string.IsNullOrEmpty(customer.Phone) && !customer.StandardPhone.HasValue)
                customer.StandardPhone = StringHelper.ConvertToStandardPhone(customer.Phone);

            var temp = SQLDataAccess.ExecuteReadOne(
                @"IF @CustomerID IS NULL
                    SET @CustomerID = NEWID()

                INSERT INTO [Customers].[Customer]( [CustomerID]
                                                  , [CustomerGroupID]
                                                  , [Password]
                                                  , [FirstName]
                                                  , [LastName]
                                                  , [Phone]
                                                  , [StandardPhone]
                                                  , [RegistrationDateTime]
                                                  , [Email]
                                                  , [CustomerRole]
                                                  , [Patronymic]
                                                  , [AdminComment]
                                                  , [ManagerId]
                                                  , [Rating]
                                                  , [Enabled]
                                                  , [HeadCustomerId]
                                                  , [BirthDay]
                                                  , [City]
                                                  , [Organization]
                                                  , [ClientStatus]
                                                  , [RegisteredFrom]
                                                  , [CustomerType]
                                                  , [RegisteredFromIp])
                VALUES ( @CustomerID
                       , @CustomerGroupID
                       , @Password
                       , @FirstName
                       , @LastName
                       , @Phone
                       , @StandardPhone
                       , @RegistrationDateTime
                       , @Email
                       , @CustomerRole
                       , @Patronymic
                       , @AdminComment
                       , @ManagerId
                       , @Rating
                       , @Enabled
                       , @HeadCustomerId
                       , @BirthDay
                       , @City
                       , @Organization
                       , @ClientStatus
                       , @RegisteredFrom
                       , @CustomerType
                       , @RegisteredFromIp);

                SELECT CustomerID, InnerId
                From [Customers].[Customer]
                WHERE CustomerId = @CustomerID;",
                CommandType.Text,
                reader => new KeyValuePair<Guid, int>(
                    SQLDataHelper.GetGuid(reader, "CustomerID"), 
                    SQLDataHelper.GetInt(reader, "InnerId")
                ),

                new SqlParameter("@CustomerID", customer.Id != Guid.Empty ? customer.Id : (object)DBNull.Value),
                new SqlParameter("@CustomerGroupID", customer.CustomerGroupId),
                new SqlParameter("@Password", SecurityHelper.GetPasswordHash(customer.Password)),
                new SqlParameter("@FirstName", customer.FirstName.Default(string.Empty).Reduce(70)),
                new SqlParameter("@LastName", customer.LastName.Default(string.Empty).Reduce(70)),
                new SqlParameter("@Patronymic", customer.Patronymic.Default(string.Empty).Reduce(70)),
                new SqlParameter("@Phone", 
                    string.IsNullOrEmpty(customer.Phone) ? (object) DBNull.Value : customer.Phone
                ),
                new SqlParameter("@StandardPhone", customer.StandardPhone ?? (object) DBNull.Value),
                new SqlParameter("@RegistrationDateTime",
                    customer.RegistrationDateTime == DateTime.MinValue
                        ? DateTime.Now
                        : customer.RegistrationDateTime
                ),
                new SqlParameter("@Email", customer.EMail.Default(string.Empty)),
                new SqlParameter("@CustomerRole", customer.CustomerRole),
                new SqlParameter("@AdminComment", customer.AdminComment ?? (object)DBNull.Value),
                new SqlParameter("@ManagerId", customer.ManagerId ?? (object)DBNull.Value),
                new SqlParameter("@Rating", customer.Rating),
                new SqlParameter("@Enabled", customer.Enabled),
                new SqlParameter("@HeadCustomerId", customer.HeadCustomerId ?? (object)DBNull.Value),
                new SqlParameter("@BirthDay", customer.BirthDay ?? (object)DBNull.Value),
                new SqlParameter("@City", customer.City?.Reduce(70) ?? (object)DBNull.Value),
                new SqlParameter("@Organization", customer.Organization?.Reduce(250) ?? (object)DBNull.Value),
                new SqlParameter("@ClientStatus", (int) customer.ClientStatus),
                new SqlParameter("@RegisteredFrom",
                    regFromUri != null
                        ? regFromUri.GetLeftPart(UriPartial.Path).Reduce(500)
                        : (object) DBNull.Value
                ),
                new SqlParameter("@CustomerType", customer.CustomerType),
                new SqlParameter("@RegisteredFromIp", regFromIp?.Reduce(200) ?? (object)DBNull.Value)
            );

            if (customer.IsAgreeForPromotionalNewsletter)
                SubscriptionService.Subscribe(customer.EMail);

            customer.Id = temp.Key;
            customer.InnerId = temp.Value;
            customer.SetRegistered();

            var tags = customer.Tags;

            if (tags != null && tags.Count != 0)
            {
                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = TagService.Get(tags[i].Name);
                    tags[i].Id = tag?.Id ?? TagService.Add(tags[i]);
                    TagService.AddMap(customer.Id, tags[i].Id, i * 10);
                }
            }

            if (customerFields != null && customerFields.Count > 0)
            {
                foreach (var customerField in customerFields)
                {
                    CustomerFieldService.AddUpdateMap(customer.Id, customerField.Id, customerField.Value ?? "", true);
                }
            }
            
            if (trackChanges)
                CustomerHistoryService.NewCustomer(customer, changedBy);

            ModulesExecuter.AddCustomer(customer);
            
            ReferralService.BindNewCustomer(customer);
            
            if (processTriggers)
                TriggerProcessService.ProcessEvent(ETriggerEventType.CustomerCreated, customer);
            
            PartnerService.BindNewCustomer(customer);

            CacheManager.RemoveByPattern(CacheNames.Customer);

            if (customer.IsAdmin || customer.IsModerator || customer.IsManager)
                CustomerAdminPushNotificationService.AddNew(customer.Id);

            return customer.Id;
        }

        public static string GetContactId(CustomerContact contact)
        {
            var res =
                SQLDataHelper.GetNullableGuid(SQLDataAccess.ExecuteScalar("[Customers].[sp_GetContactIDByContent]",
                    CommandType.StoredProcedure,
                    new SqlParameter("@Name", contact.Name),
                    new SqlParameter("@Country", contact.Country),
                    new SqlParameter("@City", contact.City),
                    new SqlParameter("@Zone", contact.Region ?? ""),
                    new SqlParameter("@Zip", contact.Zip ?? ""),
                    new SqlParameter("@Street", contact.Street ?? ""),
                    new SqlParameter("@CustomerID", contact.CustomerGuid)
                    ));
            return res == null ? null : res.ToString();
        }

        public static bool IsEmailExist(string email)
        {
            return SQLDataAccess.ExecuteScalar<int>(
                "SELECT COUNT([CustomerID]) FROM [Customers].[Customer] WHERE [Email] = @Email",
                CommandType.Text, new SqlParameter("@Email", email)) != 0;
        }

        public static bool IsPhoneExist(string phone, long? standardPhone,/*GlorySoft_030*/ CustomerType? type = null)
        {
            return
                SQLDataAccess.ExecuteScalar<int>(
                    "Select Count(CustomerId) " +
                    "From Customers.Customer " +
                    "Where Phone=@Phone " + (standardPhone != null && standardPhone != 0 ? "or StandardPhone=@StandardPhone" : ")") +/*GlorySoft_030 string.Empty),*/
                    (type.HasValue ? " And [CustomerType]=@type" : ""),//GlorySoft_030
                    CommandType.Text,
                    new SqlParameter("@Phone", phone),
                    new SqlParameter("@type", type.HasValue ? (int)type.Value : (object)DBNull.Value),//GlorySoft_030
                    new SqlParameter("@StandardPhone", standardPhone ?? (object) DBNull.Value)) != 0;
        }

        public static bool AddOpenIdLinkCustomer(Guid customerGuid, string identifier)
        {
            return SQLDataHelper.GetInt(SQLDataAccess.ExecuteScalar(
                "Insert Into [Customers].[OpenIdLinkCustomer] (CustomerID, OpenIdIdentifier) Values (@CustomerID, @OpenIdIdentifier)",
                CommandType.Text,
                new SqlParameter("@CustomerID", customerGuid),
                new SqlParameter("@OpenIdIdentifier", identifier))) != 0;
        }

        public static bool IsExistOpenIdLinkCustomer(string identifier)
        {
            return SQLDataAccess.ExecuteScalar<int>(
                "SELECT COUNT([CustomerID]) FROM [Customers].[OpenIdLinkCustomer] WHERE [OpenIdIdentifier] = @OpenIdIdentifier",
                CommandType.Text,
                new SqlParameter("@OpenIdIdentifier", identifier)) != 0;
        }

        public static void ChangeCustomerGroup(Guid customerId, int customerGroupId)
        {
            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Customer] Set CustomerGroupId = @CustomerGroupId WHERE CustomerID = @CustomerID",
                CommandType.Text, new SqlParameter("@CustomerID", customerId),
                new SqlParameter("@CustomerGroupId", customerGroupId));

            ModulesExecuter.UpdateCustomer(customerId);

            CacheManager.RemoveByPattern(CacheNames.Customer + customerId);
        }

        // public static void UpdateAdminComment(Guid customerId, string comment)
        // {
        //     SQLDataAccess.ExecuteNonQuery(
        //         "Update [Customers].[Customer] Set AdminComment = @AdminComment WHERE CustomerID = @CustomerID",
        //         CommandType.Text,
        //         new SqlParameter("@CustomerID", customerId),
        //         new SqlParameter("@AdminComment", comment));
        //
        //     ModulesExecuter.UpdateCustomer(customerId);
        //
        //     CacheManager.RemoveByPattern(CacheNames.Customer + customerId);
        // }

        // public static void UpdateCustomerRating(Guid customerId, int rating)
        // {
        //     SQLDataAccess.ExecuteNonQuery(
        //         "Update [Customers].[Customer] Set [Rating] = @Rating WHERE CustomerID = @CustomerID",
        //         CommandType.Text,
        //         new SqlParameter("@CustomerID", customerId),
        //         new SqlParameter("@Rating", rating));
        //
        //     ModulesExecuter.UpdateCustomer(customerId);
        //
        //     CacheManager.RemoveByPattern(CacheNames.Customer + customerId);
        // }

        public static void ChangeCustomerManager(Guid customerId, int? managerId, bool trackChanges = true, ChangedBy changedBy = null)
        {
            if (trackChanges)
                CustomerHistoryService.TrackManagerChanges(customerId, managerId, changedBy);
            
            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Customer] Set ManagerId = @ManagerId WHERE CustomerID = @CustomerID",
                CommandType.Text,
                new SqlParameter("@CustomerID", customerId),
                new SqlParameter("@ManagerId", managerId ?? (object)DBNull.Value));

            ModulesExecuter.UpdateCustomer(customerId);

            CacheManager.RemoveByPattern(CacheNames.Customer + customerId);
        }

        public static string GetCurrentCustomerManager()
        {
            return SQLDataAccess.ExecuteScalar<string>(
                "Select [Customer].FirstName + ' ' + [Customer].[LastName] From [Customers].[Customer] LEFT JOIN [Customers].[Managers] On [Customer].ManagerId = [Managers].ManagerId WHERE [Customer].CustomerID = @CustomerID",
                CommandType.Text,
                new SqlParameter("@CustomerID", CustomerContext.CurrentCustomer.Id));
        }

        public static bool CanDelete(Guid customerId)
        {
            List<string> messages;
            return CanDelete(customerId, out messages);
        }

        public static bool CanDelete(Guid customerId, out List<string> messages)
        {
            messages = new List<string>();
            var currentCustomer = CustomerContext.CurrentCustomer;
            if (currentCustomer == null)
                return false;
            var customer = GetCustomer(customerId);
            if (customer == null)
            {
                messages.Add(LocalizationService.GetResource("Core.Customers.ErrorDeleteCustomer.NotFound"));
                return false;
            }

            if (customer.Id == currentCustomer.Id)
                messages.Add(LocalizationService.GetResource("Core.Customers.ErrorDeleteCustomer.SelfDelete"));
            if (customer.IsAdmin && currentCustomer.IsModerator)
                messages.Add(LocalizationService.GetResource("Core.Customers.ErrorDeleteCustomer.IsAdmin"));

            Manager manager;
            if (customer.IsManager && (manager = ManagerService.GetManager(customer.Id)) != null)
            {
                string managerMessage;
                if (!ManagerService.CanDelete(manager.ManagerId, out managerMessage))
                    messages.Add(managerMessage);
            }

            return !messages.Any();
        }

        /// <summary>
        /// Get sended by site emails
        /// </summary>
        public static List<EmailLogItem> GetEmails(Guid customerId, string email)
        {
            try
            {
                var emails = MailService.GetLast(customerId) ?? new List<EmailLogItem>();

                if (!string.IsNullOrEmpty(email))
                {
                    var emailsFromActivity = EmailLogger.GetEmails(customerId, email);

                    if (emailsFromActivity != null && emailsFromActivity.Count > 0)
                        emails.AddRange(emailsFromActivity.Select(x => new EmailLogItem()
                        {
                            CustomerId = x.CustomerId,
                            EmailAddress = x.Email,
                            Subject = x.Subject,
                            Body = x.Body,
                            Status = x.Status,
                            CreateOn = x.CreatedOnUtc.ToLocalTime(),
                            ShopId = SettingsLic.LicKey
                        }));
                }
                return emails;
            }
            catch (Exception ex)
            {
                Debug.Log.Warn(ex);
            }
            return null;
        }

        /// <summary>
        /// Get emails from imap host
        /// </summary>
        public static List<EmailImap> GetEmails(string email)
        {
            return CacheManager.Get("GetEmailsByImap" + email, 0.3, () => new ImapMailService().GetEmails(email));
        }

        public static EmailImap GetEmailImap(string uid, string folder)
        {
            var imapService = new ImapMailService();
            return imapService.GetEmail(uid, folder);
        }


        public static List<SmsLogDto> GetSms(Guid customerId, long phone)
        {
            return SmsLogger.GetSms(customerId, phone);
        }

        public static List<Event> GetEvent(Guid customerId)
        {
            return EventLogger.GetEvents(customerId);
        }

        public static List<Call> GetCalls(Guid customerId, string phone)
        {
            return CallLogger.GetCalls(customerId, phone);
        }

        public static bool CheckAccess(Customer customer)
        {
            var currentCustomer = CustomerContext.CurrentCustomer;

            if (currentCustomer.IsAdmin || currentCustomer.IsVirtual)
                return true;

            if (CustomerContext.CurrentCustomer.IsModerator)
            {
                var manager = ManagerService.GetManager(CustomerContext.CurrentCustomer.Id);
                if (manager != null && manager.Enabled)
                {
                    if (SettingsManager.ManagersCustomerConstraint == ManagersCustomerConstraint.Assigned &&
                        customer.ManagerId != manager.ManagerId)
                        return false;

                    if (SettingsManager.ManagersCustomerConstraint == ManagersCustomerConstraint.AssignedAndFree &&
                        customer.ManagerId != manager.ManagerId && customer.ManagerId != null)
                        return false;

                    return true;
                }
            }
            return false;
        }

        public static List<CustomerWithLastOrderDto> GetCustomersByDaysFromLastOrder(int days)
        {
            return SQLDataAccess.ExecuteReadList(
                @"SELECT c.*, o.OrderID 
                FROM [Customers].[Customer] as c 
                CROSS APPLY (Select top(1) [Order].OrderID, OrderDate 
			                 From [Order].[Order] 
			                 Inner Join [Order].[OrderCustomer] On [OrderCustomer].[OrderId] = [Order].[OrderId] 
			                 Where [OrderCustomer].[CustomerId] = c.CustomerId 
			                 Order by [OrderDate] desc) as o
                Where DATEDIFF(day, o.OrderDate, getdate()) = @days",
                CommandType.Text,
                reader =>
                {
                    var customer = GetFromSqlDataReader(reader);
                    var c = new CustomerWithLastOrderDto(customer)
                    {
                        LastOrderId = SQLDataHelper.GetInt(reader, "OrderID")
                    };
                    return c;
                },
                new SqlParameter("@days", days));
        }

        public static DateTime GetCustomerLastOrderDate(Guid customerId)
        {
            return SQLDataAccess.ExecuteScalar<DateTime>(
                "Select Top 1 o.[OrderDate] from [Order].[Order] o " +
                "Inner Join [Order].[OrderCustomer] oc On oc.[OrderId] = o.[OrderID] " +
                "Where oc.[CustomerID] = @customerId",
                CommandType.Text,
                new SqlParameter("@customerId", customerId));
        }

        public static List<Customer> GetCustomersByTriggersDateParams(DateTime date, bool ignoreYear, bool isBirthDay, int? customFieldId)
        {
            var sql = "";

            if (isBirthDay)
            {
                sql = ignoreYear
                    ? "SELECT * FROM [Customers].[Customer] Where BirthDay is not null and MONTH(BirthDay)=@dateMonth and DAY(BirthDay)=@dateDay"
                    : "SELECT * FROM [Customers].[Customer] Where BirthDay is not null and MONTH(BirthDay)=@dateMonth and DAY(BirthDay)=@dateDay and YEAR(BirthDay)=@dateYear";
            }
            else
            {
                sql = ignoreYear
                    ? "SELECT c.* FROM [Customers].[Customer] as c " +
                      "Inner Join [Customers].[CustomerFieldValuesMap] vm On c.CustomerId = vm.CustomerId " +
                      "Where vm.CustomerFieldId = @CustomFieldId and vm.[Value] is not null and ISDATE(vm.[Value]) = 1 and " +
                      "MONTH(convert(varchar,vm.[Value],120))=@dateMonth and DAY(convert(varchar,vm.[Value],120))=@dateDay"

                    : "SELECT c.* FROM [Customers].[Customer] as c " +
                      "Inner Join [Customers].[CustomerFieldValuesMap] vm On c.CustomerId = vm.CustomerId " +
                      "Where vm.CustomerFieldId = @CustomFieldId and vm.[Value] is not null and ISDATE(vm.[Value]) = 1 and " +
                      "MONTH(convert(varchar,vm.[Value],120))=@dateMonth and DAY(convert(varchar,vm.[Value],120))=@dateDay and YEAR(convert(varchar,vm.[Value],120))=@dateYear";
            }

            return SQLDataAccess.ExecuteReadList(sql, CommandType.Text, GetFromSqlDataReader,
                new SqlParameter("@dateMonth", date.Month),
                new SqlParameter("@dateDay", date.Day),
                new SqlParameter("@dateYear", date.Year),
                new SqlParameter("@CustomFieldId", customFieldId));
        }

        public static int GetCustomersCountByOrderDate(DateTime orderDateFrom, DateTime orderDateTo)
        {
            return SQLDataAccess.ExecuteScalar<int>(
                "SELECT COUNT(DISTINCT [Customer].[CustomerID]) FROM [Customers].[Customer] " +
                "INNER JOIN [Order].[OrderCustomer] ON [OrderCustomer].[CustomerID] = [Customer].[CustomerID] " +
                "INNER JOIN [Order].[Order] ON [Order].[OrderID] = [OrderCustomer].[OrderId] " +
                "WHERE [Order].[OrderDate] >= @orderDateFrom AND [Order].[OrderDate] <= @orderDateTo",
                CommandType.Text,
                new SqlParameter("@orderDateFrom", orderDateFrom),
                new SqlParameter("@orderDateTo", orderDateTo));
        }

        public static bool IsTechDomainGuest()
        {
            var cookie = CommonHelper.GetCookie(GuestCookieName);
            return cookie != null && cookie.Value != null;
        }

        public static void SetTechDomainGuest()
        {
            CommonHelper.SetCookie(GuestCookieName, "true", true, true, true);
        }
        
        public static List<object> GetCustomerTypesSelectOptions()
        {
            return Enum.GetValues(typeof(CustomerType)).Cast<CustomerType>().Where(x => x != CustomerType.All).Select(x => (object)new
            {
                label = x.Localize(),
                value = (int)x,
            }).ToList();
        }

        public static string GetFcmToken(Guid customerId)
        {
            return SQLDataAccess
                .Query<string>("select FcmToken From [Customers].[Customer] Where CustomerId=@customerId",
                    new {customerId}).FirstOrDefault();
        }

        public static void UpdateFcmToken(Guid customerId, string fcmToken)
        {
            var oldFcmToken = GetFcmToken(customerId);

            if (!string.IsNullOrWhiteSpace(fcmToken))
            {
                SQLDataAccess.ExecuteNonQuery(
                    "Update [Customers].[Customer] Set FcmToken = null Where FcmToken=@FcmToken",
                    CommandType.Text,
                    new SqlParameter("@FcmToken", fcmToken));
            }

            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Customer] Set FcmToken=@FcmToken Where CustomerId=@CustomerId",
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@FcmToken", fcmToken ?? (object) DBNull.Value));
            
            if (fcmToken.IsNotEmpty() && oldFcmToken.IsNullOrEmpty())
                TriggerProcessService.ProcessEvent(ETriggerEventType.InstallMobileApp, GetCustomer(customerId));
        }

        public static void UpdateAdminFcmToken(Guid customerId, string fcmToken)
        {
            if (!string.IsNullOrWhiteSpace(fcmToken))
            {
                SQLDataAccess.ExecuteNonQuery(
                    "Update [Customers].[CustomerAdminPushNotificationSettings] Set FcmToken = null Where FcmToken=@FcmToken",
                    CommandType.Text,
                    new SqlParameter("@FcmToken", fcmToken));
            }

            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[CustomerAdminPushNotificationSettings] Set FcmToken=@FcmToken Where CustomerId=@CustomerId",
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@FcmToken", fcmToken ?? (object)DBNull.Value));
        }
        
        public static List<PushLog> GetPushNotifications(Guid customerId) => PushLogger.GetPushLogs(customerId);

        #region ReferralCode

        public static string GetReferralCode(Guid customerId) =>
            SQLDataAccess.Query<string>("SELECT ReferralCode FROM [Customers].[Customer] WHERE CustomerId=@customerId",
                    new {customerId}).FirstOrDefault();
        
        public static void SetReferralCode(string referralCode, Guid customerId) =>
            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Customer] Set ReferralCode=@ReferralCode Where CustomerID=@CustomerId",
                CommandType.Text, 
                new SqlParameter("@ReferralCode", referralCode),
                new SqlParameter("@CustomerId", customerId));
        
        public static bool IsReferralCodeExist(string referralCode) =>
            SQLDataAccess.ExecuteScalar<bool>("SELECT CASE WHEN EXISTS (SELECT 1 FROM [Customers].[Customer] " +
                                              "WHERE ReferralCode=@ReferralCode) THEN 1 ELSE 0 END",
                    CommandType.Text,
                    new SqlParameter("@ReferralCode", referralCode));
        
        public static Customer GetCustomerByReferralCode(string referralCode) =>
            SQLDataAccess.ExecuteReadOne<Customer>(
                    "SELECT TOP 1 * FROM [Customers].[Customer] WHERE ReferralCode=@ReferralCode",
                    CommandType.Text,
                    GetFromSqlDataReader,
                    new SqlParameter("@ReferralCode", referralCode));

        #endregion
        
        #region DelayedTriggersStart

        public static bool GetDelayedTriggersStart(Guid customerId) =>
            SQLDataAccess
                .Query<bool?>("SELECT DelayedTriggersStart FROM [Customers].[Customer] WHERE CustomerId=@customerId",
                    new { customerId })
                .FirstOrDefault() ?? false;
        
        public static void SetDelayedTriggersStart(Guid customerId, bool delayedTriggersStart) =>
            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Customer] Set DelayedTriggersStart=@DelayedTriggersStart Where CustomerID=@CustomerId",
                CommandType.Text, 
                new SqlParameter("@DelayedTriggersStart", delayedTriggersStart),
                new SqlParameter("@CustomerId", customerId));
        
        #endregion

        public static CustomerType DefaultCustomerType
            => SettingsCustomers.IsRegistrationAsLegalEntity
               && SettingsCustomers.IsRegistrationAsPhysicalEntity == false
                ? CustomerType.LegalEntity
                : CustomerType.PhysicalEntity;

        public static string GetRegCode(Guid customerId)//GlorySoft_020
        {
            return SQLDataAccess.ExecuteScalar<string>(
                "Select RegCodeHash From [Customers].[Customer] Where CustomerID=@customerId",
                CommandType.Text, new SqlParameter("@customerId", customerId));
        }

        public static bool GetConfirmPhone(string customerId)//GlorySoft_020
        {
            return SQLDataAccess.ExecuteScalar<bool>(
                "Select IsNull(PhoneConfirmed, 0) From [Customers].[Customer] Where CustomerID=@customerId",
                CommandType.Text, new SqlParameter("@customerId", customerId));
        }

        public static Customer CheckRegCode(string hash)//GlorySoft_028
        {
            var c = SQLDataAccess.ExecuteReadOne(
                "Select CustomerID, RegCodeExpired From [Customers].[Customer] Where RegCodeHash=@regCode", CommandType.Text,
                reader => (SQLDataHelper.GetString(reader, "CustomerID"), SQLDataHelper.GetNullableDateTime(reader, "RegCodeExpired")),
                new SqlParameter("@regCode", hash));
            if (c.Item1.IsNotEmpty() && c.Item2.HasValue && c.Item2 >= DateTime.Now)
            {
                var customerId = Guid.Parse(c.Item1);
                SQLDataAccess.ExecuteNonQuery(
                    "Update [Customers].[Customer] Set [Enabled]=1, RegCodeHash=NULL, RegCodeExpired=NULL Where CustomerID=@customerId",
                    CommandType.Text,
                    new SqlParameter("@customerId", customerId));
                return GetCustomer(customerId);
            }
            else
                return null;
        }

        public static void SetCustomerEnabled(Guid id, bool enabled)//GlorySoft_028
        {
            SQLDataAccess.ExecuteNonQuery(
                "Update Customers.Customer Set [Enabled] = @enabled Where CustomerID = @id",
                CommandType.Text,
                new SqlParameter("@id", id), new SqlParameter("@enabled", enabled));
        }

        public static Customer GetCustomerByPhone(string phone, long? standardPhone, CustomerType? type = null)//GlorySoft_031
        {
            return SQLDataAccess.ExecuteReadOne(
                "Select top(1) * " +
                "From Customers.Customer " +
                "Where (Phone=@Phone" + (standardPhone != null && standardPhone != 0 ? " or StandardPhone=@StandardPhone)" : ")") +
                (type.HasValue ? " And [CustomerType]=@type" : ""),
                CommandType.Text,
                CustomerService.GetFromSqlDataReader,
                new SqlParameter("@Phone", phone),
                new SqlParameter("@StandardPhone", standardPhone ?? (object)DBNull.Value),
                new SqlParameter("@type", type.HasValue ? (int)type.Value : (object)DBNull.Value));
        }

        public static void InplaceSave(Customer customer, string field, string value, bool additional, bool subscribe)//GlorySoft_031
        {
            if (!additional)
                SQLDataAccess.ExecuteNonQuery(
                    string.Format("Update Customers.Customer Set [{0}] = @value Where CustomerID = @id", field),
                    CommandType.Text,
                    new SqlParameter("@id", customer.Id),
                    new SqlParameter("@value", value));
            else
            {
                var customerField = GetCustomerField(field, customer.CustomerType);
                CustomerFieldService.AddUpdateMap(customer.Id, customerField.Id, value);
            }
            if (subscribe)
            {
                var s = SubscriptionService.GetSubscription(customer.EMail);
                var v = Convert.ToBoolean(value.ToLower());
                if ((s?.Subscribe ?? false) != v)
                {
                    if (v)
                        SubscriptionService.Subscribe(customer.EMail);
                    else
                        SubscriptionService.Unsubscribe(customer.EMail);
                }
            }
        }

        private static CustomerField GetCustomerField(string name, CustomerType type)//GlorySoft_031
        {
            return SQLDataAccess.Query<CustomerField>("SELECT * FROM Customers.CustomerField WHERE [Name] = @name And [CustomerType] = @type", new { name, type }).FirstOrDefault();
        }

        public static void SetConfirmPhone(string customerId, string phone)//GlorySoft_031
        {
            var standartPhone = StringHelper.ConvertToStandardPhone(phone, true, true);
            SQLDataAccess.ExecuteNonQuery(
                "Update [Customers].[Customer] Set Phone=@phone, StandardPhone=@standartPhone, PhoneConfirmed=1 Where CustomerID=@customerId",
                CommandType.Text,
                new SqlParameter("@phone", phone),
                new SqlParameter("@standartPhone", standartPhone),
                new SqlParameter("@customerId", customerId));

        }

        public static void SetRegCode(Guid customerId, string regCode)//GlorySoft_031
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Customers].[Customer] Set RegCodeHash=@regCode, RegCodeExpired=@regCodeExpired Where CustomerID=@customerId",
                CommandType.Text,
                new SqlParameter("@regCode", regCode),
                new SqlParameter("@regCodeExpired", DateTime.Now.AddDays(1)),
                new SqlParameter("@customerId", customerId));
        }

    }
}