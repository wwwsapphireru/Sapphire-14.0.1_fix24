using AdvantShop.Core.Caching;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.ChangeHistories;
using AdvantShop.Core.Services.Customers;
using AdvantShop.Core.Services.Partners;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Core.SQL;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AdvantShop.Module.OneSApi.Service
{
    public class CustomersService
    {
        public static bool IsNewCustomerValid(Customer customer)
        {
            if (!string.IsNullOrEmpty(customer.EMail) && CustomerService.IsEmailExist(customer.EMail))
                return false;

            if (customer.CustomerType != CustomerType.LegalEntity
                && (!string.IsNullOrEmpty(customer.Phone) || (customer.StandardPhone != null && customer.StandardPhone != 0)) 
                && IsPhoneExist(customer.Phone, customer.StandardPhone, CustomerType.PhysicalEntity))
                return false;

            return true;
        }

        public static bool IsPhoneExist(string phone, long? standardPhone, CustomerType? type = null)
        {
            return
                SQLDataAccess.ExecuteScalar<int>(
                    "Select Count(CustomerId) " +
                    "From Customers.Customer " +
                    "Where (Phone=@Phone" + (standardPhone != null && standardPhone != 0 ? " or StandardPhone=@StandardPhone)" : ")") +
                    (type.HasValue ? " And [CustomerType]=@type" : ""),
                    CommandType.Text,
                    new SqlParameter("@Phone", phone),
                    new SqlParameter("@StandardPhone", standardPhone ?? (object)DBNull.Value),
                    new SqlParameter("@type", type.HasValue ? (int)type.Value : (object)DBNull.Value)) != 0;
        }

        public static CustomerField GetCustomerField(string name, CustomerType type)
        {
            return SQLDataAccess.Query<CustomerField>("SELECT * FROM Customers.CustomerField WHERE [Name] = @name And [CustomerType] = @type", new { name, type }).FirstOrDefault();
        }

        public static CustomerContact GetCustomerContact(Guid customerId)
        {
            var contact = SQLDataAccess.ExecuteReadOne(
                "SELECT TOP 1 * FROM [Customers].[Contact] WHERE [CustomerID] = @id",
                CommandType.Text,
                CustomerService.GetContactFromSqlDataReader,
                new SqlParameter("@id", customerId));

            return contact;
        }

        public static void DeleteManyContacts(Guid customerId)
        {
            var c = SQLDataAccess.ExecuteScalar<int>(
                "SELECT Count(ContactId) FROM [Customers].[Contact] WHERE [CustomerID] = @id",
                CommandType.Text,
                new SqlParameter("@id", customerId));
            if (c > 1)
                SQLDataAccess.ExecuteNonQuery("Delete From Customers.Contact Where CustomerID = @id",
                    CommandType.Text,
                    new SqlParameter("@id", customerId));
        }
    }
}
