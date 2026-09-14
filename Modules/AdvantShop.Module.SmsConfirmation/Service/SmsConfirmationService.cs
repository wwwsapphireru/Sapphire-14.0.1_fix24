using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Security;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.MyAccount;
using AdvantShop.Core.Services.Smses;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Module.SmsConfirmation.Handlers;
using AdvantShop.Module.SmsConfirmation.Models;
using AdvantShop.Orders;
using AdvantShop.Security;
using Newtonsoft.Json;

namespace AdvantShop.Module.SmsConfirmation.Service
{
    public class SmsConfirmationService
    {
        private const string AuthorizeSpliter = ":";

        #region initialize functions
        public static bool CreateTables()
        {
            var created = true;
            var query = string.Empty;

            try
            {
                if (!ModulesRepository.IsExistsModuleTable("Module", "SmsConfirmation"))
                {
                    //  CONSTRAINT [FK_Module_SmsConfirmation_CustomerID] REFERENCES [Customers].[Customer] ([CustomerID]) ON UPDATE CASCADE ON DELETE CASCADE
                    query = @"CREATE TABLE [Module].[SmsConfirmation]
                            (
                                [CustomerId] [uniqueidentifier] NOT NULL,
                                [Phone] nvarchar(20) NOT NULL,
                                [SmsCode] nvarchar(10) NOT NULL,
                                [PageType] [tinyint] NOT NULL,
                                [Attempts] [int] NOT NULL CONSTRAINT [DF_Module_SmsConfirmation_Attempts] DEFAULT ((0)),
                                [CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_Module_SmsConfirmation_CreatedAt] DEFAULT (GETDATE())
                            )";

                    ModulesRepository.ModuleExecuteNonQuery(query, CommandType.Text);
                }
            }
            catch(Exception ex)
            {
                Debug.Log.Error(ex);
                created = false;
            }

            return created;
        }

        public static bool Install()
        {
            return SmsConfirmationSettings.SetDefaultSettings() && CreateTables() && Update();
        }

        public static bool UnInstall()
        {
            return SmsConfirmationSettings.RemoveSettings();
        }

        public static bool Update()
        {
            try
            {
                if (!IsExistsSmsConfirmationColumn("Attempts"))
                {
                    ModulesRepository.ModuleExecuteNonQuery(
                        "ALTER TABLE [Module].[SmsConfirmation] ADD [Attempts] [int] NOT NULL CONSTRAINT [DF_Module_SmsConfirmation_Attempts] DEFAULT ((0))",
                        CommandType.Text);
                }

                if (!IsExistsSmsConfirmationColumn("CreatedAt"))
                {
                    ModulesRepository.ModuleExecuteNonQuery(
                        "ALTER TABLE [Module].[SmsConfirmation] ADD [CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_Module_SmsConfirmation_CreatedAt] DEFAULT (GETDATE())",
                        CommandType.Text);
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                return false;
            }

            return true;
        }

        private static bool IsExistsSmsConfirmationColumn(string columnName)
        {
            return !string.IsNullOrEmpty(ModulesRepository.ModuleExecuteScalar<string>(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Module' AND TABLE_NAME = 'SmsConfirmation' AND COLUMN_NAME = @ColumnName",
                CommandType.Text,
                new SqlParameter("@ColumnName", columnName)));
        }

        #endregion

        #region Banners Promo
        public static BannersSettingsModel GetPromoContentBannersData()
        {
            try
            {
                var url = $"{SmsConfirmationSettings.CrmPromoUrl}/PromoContentClient/GetBannersData";

                using (var webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.UTF8;

                    var response = webClient.DownloadString(url);
                    return JsonConvert.DeserializeObject<BannersSettingsModel>(response);
                }
            }
            catch (Exception ex)
            {
                Debug.Log.Error(ex);
                return null;
            }
        }

        #endregion

        #region sms confirmation code 

        private static SmsConfirmationCode GetSmsConfirmationCodeFromReader(SqlDataReader reader)
        {
            return new SmsConfirmationCode()
            {
                CustomerId = ModulesRepository.ConvertTo<Guid>(reader, "CustomerId"),
                Phone = ModulesRepository.ConvertTo<string>(reader, "Phone"),
                SmsCode = ModulesRepository.ConvertTo<string>(reader, "SmsCode"),
                PageType = ModulesRepository.ConvertTo<byte>(reader, "PageType"),
                Attempts = ModulesRepository.ConvertTo<int>(reader, "Attempts"),
                CreatedAt = ModulesRepository.ConvertTo<DateTime>(reader, "CreatedAt"),
            };
        }

        public static SmsConfirmationCode GetFullSmsConfirmationCode(Guid customerId, string phone, byte pageType)
        {
            return ModulesRepository.ModuleExecuteReadOne<SmsConfirmationCode>(
                "SELECT * FROM [Module].[SmsConfirmation] WHERE [CustomerId] = @CustomerId AND [Phone] = @Phone AND [PageType] = @PageType",
                CommandType.Text,
                GetSmsConfirmationCodeFromReader,
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@PageType", pageType));
        }

        public static SmsConfirmationCode GetSmsConfirmationCode(Guid customerId, byte pageType)
        {
            return ModulesRepository.ModuleExecuteReadOne<SmsConfirmationCode>(
                "SELECT * FROM [Module].[SmsConfirmation] WHERE [CustomerId] = @CustomerId AND [PageType] = @PageType",
                CommandType.Text,
                GetSmsConfirmationCodeFromReader,
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@PageType", pageType));
        }

        public static void AddSmsConfirmationCode(SmsConfirmationCode smsCode)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "INSERT INTO [Module].[SmsConfirmation] ([CustomerId], [Phone], [SmsCode], [PageType], [Attempts], [CreatedAt]) VALUES (@CustomerId, @Phone, @SmsCode, @PageType, @Attempts, @CreatedAt)",
                CommandType.Text,
                new SqlParameter("@CustomerId", smsCode.CustomerId),
                new SqlParameter("@Phone", smsCode.Phone),
                new SqlParameter("@SmsCode", smsCode.SmsCode),
                new SqlParameter("@PageType", smsCode.PageType),
                new SqlParameter("@Attempts", smsCode.Attempts),
                new SqlParameter("@CreatedAt", smsCode.CreatedAt));
        }

        public static void UpdateSmsConfirmationCode(SmsConfirmationCode smsCode)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "UPDATE [Module].[SmsConfirmation] SET [Phone] = @Phone, [SmsCode] = @SmsCode, [Attempts] = @Attempts, [CreatedAt] = @CreatedAt WHERE [CustomerId] = @CustomerId AND [PageType] = @PageType",
                CommandType.Text,
                new SqlParameter("@CustomerId", smsCode.CustomerId),
                new SqlParameter("@Phone", smsCode.Phone),
                new SqlParameter("@SmsCode", smsCode.SmsCode),
                new SqlParameter("@PageType", smsCode.PageType),
                new SqlParameter("@Attempts", smsCode.Attempts),
                new SqlParameter("@CreatedAt", smsCode.CreatedAt));
        }

        /// <summary>
        /// Увеличивает счётчик неудачных попыток и возвращает его новое значение.
        /// Инкремент делается на стороне БД, чтобы параллельные запросы не затирали друг друга.
        /// </summary>
        public static int IncrementSmsConfirmationCodeAttempts(Guid customerId, string phone, byte pageType)
        {
            return ModulesRepository.ModuleExecuteScalar<int>(
                @"UPDATE [Module].[SmsConfirmation] SET [Attempts] = [Attempts] + 1
                  OUTPUT INSERTED.[Attempts]
                  WHERE [CustomerId] = @CustomerId AND [Phone] = @Phone AND [PageType] = @PageType",
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@PageType", pageType));
        }

        public static void DeleteSmsConfirmationCode(Guid customerId, string phone, byte pageType)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "DELETE FROM [Module].[SmsConfirmation] WHERE [CustomerId] = @CustomerId AND [Phone] = @Phone AND [PageType] = @PageType",
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@PageType", pageType));
        }

        public static void DeleteSmsConfirmationCodes(Guid customerId)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "DELETE FROM [Module].[SmsConfirmation] WHERE [CustomerId] = @CustomerId", 
                CommandType.Text,
                new SqlParameter("@CustomerId", customerId));
        }

        #endregion

        private const int SmsCodeLength = 4;
        private const string SmsCodeChars = "0123456789";

        // после этого количества неудачных попыток код аннулируется и пользователь возвращается к вводу телефона
        public const int MaxConfirmAttempts = 5;

        // столько живёт выданный код, потом его нужно запрашивать заново
        public const int SmsCodeLifetimeMinutes = 10;

        public static bool IsSmsCodeExpired(SmsConfirmationCode smsCode)
        {
            return smsCode.CreatedAt.AddMinutes(SmsCodeLifetimeMinutes) < DateTime.Now;
        }

        private static string GenerateSmsCode()
        {
            var smsCode = string.Empty;
            var rnd = new Random();

            for (int i = 1; i <= SmsCodeLength; i++)
            {
                smsCode += SmsCodeChars[(int)(SmsCodeChars.Length * rnd.NextDouble())];
            }

            return smsCode;
        }

        public static string SendSmsCode(string phone, long standardPhone, byte pageType)
        {
            try
            {
                var customerId = CustomerContext.CurrentCustomer != null ? CustomerContext.CurrentCustomer.Id : Guid.Empty;

                var smsCode = GenerateSmsCode();
                var smsConfirmationCode = GetSmsConfirmationCode(customerId, pageType);
                if(smsConfirmationCode != null)
                {
                    smsConfirmationCode.Phone = phone;
                    smsConfirmationCode.SmsCode = smsCode;
                    smsConfirmationCode.Attempts = 0;
                    smsConfirmationCode.CreatedAt = DateTime.Now;
                    UpdateSmsConfirmationCode(smsConfirmationCode);
                }
                else
                {
                    smsConfirmationCode = new SmsConfirmationCode
                    {
                        CustomerId = customerId,
                        Phone = phone,
                        SmsCode = smsCode,
                        PageType = pageType,
                        CreatedAt = DateTime.Now
                    };

                    AddSmsConfirmationCode(smsConfirmationCode);
                }

                SmsNotifier.SendSms(standardPhone, /*GlorySoft_004 "Code: " +*/ smsCode);

                return smsCode;
            }
            catch(Exception ex)
            {
                Debug.Log.Error(ex);
                return null;
            }
        }

        #region autorize 

        public const string _emailDomain = "@sms.temp";
        public static string GetEmailByPhone(string phone)
        {
            var email = phone + _emailDomain;

            while (IsExistsCustomerEmail(email))
            {
                email = phone + "n" + _emailDomain;
            }

            return email;
        }

        public static bool IsExistsCustomerEmail(string email)
        {
            return ModulesRepository.ModuleExecuteScalar<int>(
                "SELECT COUNT([Email]) FROM [Customers].[Customer] WHERE [Email] = @Email",
                CommandType.Text,
                new SqlParameter("@Email", email)) > 0;
        }

        public static void UpdateCustomerEmailByCustomerId(Guid customerId, string email)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "UPDATE [Customers].[Customer] SET [Email] = @Email WHERE [CustomerID] = @CustomerID",
                CommandType.Text,
                new SqlParameter("@CustomerID", customerId),
                new SqlParameter("@Email", email));
        }

        public static bool AuthorizeUser(Customer customer, bool isHash, bool createPersistentCookie)
        {
            var oldCustomerId = CustomerContext.CurrentCustomer.Id;
            //Secure.AddUserLog(customer.EMail, true, customer.IsAdmin);
            ShoppingCartService.MergeShoppingCarts(oldCustomerId, customer.Id);
            CustomerContext.SetCustomerCookie(customer.Id);
            FormsAuthentication.SetAuthCookie(customer.EMail + AuthorizeSpliter + customer.Password, createPersistentCookie);

            return true;
        }

        public static List<MyAccountTab> GetMyAccountChangeEmailTabs()
        {
            var customer = CustomerContext.CurrentCustomer;
            if (!string.IsNullOrEmpty(customer.EMail) && !customer.EMail.Contains(_emailDomain))
                return new List<MyAccountTab>();

            return new List<MyAccountTab>()
            {
                new MyAccountTab
                {
                    Name = "Изменить Email",
                    TabName = "sms-confirmation-account",
                    ControllerName = "SmsConfirmationClient",
                    ActionName = "MyAccountChangeEmail"
                }
            };
        }

        #endregion

        public static void CheckOrderCustomer(IOrder order)
        {
            var orderCustomer = OrderService.GetOrderCustomer(order.OrderID);
            if (orderCustomer == null || !string.IsNullOrEmpty(orderCustomer.Email) || string.IsNullOrEmpty(orderCustomer.Phone))
                return;

            var customersByPhone = CustomerService.GetCustomersByPhone(orderCustomer.Phone);
            var customer = CustomerService.GetCustomer(orderCustomer.CustomerID); //customersByPhone.FirstOrDefault(x => !string.IsNullOrEmpty(x.EMail));
            if (customer == null)
                customer = customersByPhone.FirstOrDefault(x => !string.IsNullOrEmpty(x.EMail));
            else if (customer == null)
                customer = customersByPhone.FirstOrDefault();

            if (customer != null && string.IsNullOrEmpty(customer.EMail))
            {
                customer.EMail = GetEmailByPhone(orderCustomer.Phone);
                UpdateCustomerEmailByCustomerId(customer.Id, customer.EMail);
            }

            if (customer == null)
            {
                var smsConfirmationCode = new SmsConfirmationCode()
                {
                    CustomerId = orderCustomer.CustomerID,
                    PageType = (byte)ESmsConfirmationPageType.Checkout,
                    Phone = orderCustomer.Phone
                };

                var handler = new SmsConfirmationRegistrationHandler();
                customer = handler.Register(smsConfirmationCode);
            }

            orderCustomer.CustomerID = customer.Id;
            orderCustomer.Email = customer.EMail;

            OrderService.UpdateOrderCustomer(orderCustomer);

            AuthorizeService.SignOut();
            AuthorizeService.SignIn(customer.EMail, customer.Password, true, true);
        }

        #region UserEmailCheckoutSettings
        public static void SaveUserEmailCheckoutSettings()
        {
            SmsConfirmationSettings.IsShowEmailOnCheckout = SettingsCheckout.IsShowEmail;
            SmsConfirmationSettings.RequiredEmailOnCheckout = SettingsCheckout.IsRequiredEmail;
        }

        public static void SetUserEmailCheckoutSettings(bool? isShowEmailOnCheckout, bool? requiredEmailOnCheckout)        
        {
            if (isShowEmailOnCheckout.HasValue)
                SettingsCheckout.IsShowEmail = isShowEmailOnCheckout.Value;

            if (requiredEmailOnCheckout.HasValue)
                SettingsCheckout.IsRequiredEmail = requiredEmailOnCheckout.Value;
        }
        #endregion

        public static List<Customer> GetCustomersByPhone(string phone, bool? enabled, bool? confirmed)//GlorySoft_012
        {
            var phoneLong = Helpers.StringHelper.ConvertToStandardPhone(phone);
            return ModulesRepository.ModuleExecuteReadList<Customer>(
                "Select * from Customers.Customer where (Phone=@phone " + (phoneLong.HasValue ? "or StandardPhone=@phoneLong" : string.Empty) + ") And [CustomerType]=0" +
                (enabled.HasValue ? " And IsNull([Enabled], 0)=@enabled" : string.Empty) +
                (confirmed.HasValue ? " And IsNull(PhoneConfirmed, 0)=@confirmed" : string.Empty),
                CommandType.Text,
                CustomerService.GetFromSqlDataReader,
                new SqlParameter("@phone", phone),
                new SqlParameter("@phoneLong", phoneLong ?? (object)DBNull.Value),
                new SqlParameter("@enabled", enabled ?? (object)DBNull.Value),
                new SqlParameter("@confirmed", confirmed ?? (object)DBNull.Value)
                );
        }

        public static long? ConvertToStandardPhone(string phone, bool force = false, bool forceTrimEight = false, int? dcode = null)//GlorySoft_012
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var str = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");

            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (str.Substring(0, 2) != "79")
                return null;

            // <dialCode, length>
            var presets = new Dictionary<string, int>
            {
                { "7", 11 },    // Россия
                //{ "380", 12 },  // Украина
                //{ "375", 12 },  // Беларусь
                //{ "996", 12},   // Киргизия
            };

            if (presets.Keys.Any(dialCode => str.StartsWith(dialCode) && str.Length == presets[dialCode]))
                return str.TryParseLong(true);

            if (str.StartsWith("8") && (str.Length == 11 || forceTrimEight))
            {
                str = "7" + str.Remove(0, 1);
            }
            else
            {
                var dialCode = dcode ?? Repository.IpZoneContext.CurrentZone.DialCode;

                if (dialCode.HasValue && !str.StartsWith(dialCode.Value.ToString()) && !force)
                    str = dialCode.Value.ToString() + str;
            }


            return str.TryParseLong(true);
        }

    }
}
