using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.FilePath;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Repository;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
	public class ModuleService
	{
		public static bool InstallModule()
		{
			if (!ModulesRepository.IsExistsModuleTable("Module", "OneSApi_Order"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.OneSApi_Order
			        (
			            [OrderId] [int] NOT NULL,
			            [ExportType] [int] NOT NULL,
			            [ForExport] [bit] NOT NULL,
			            [Changed] [datetime] NOT NULL,
			            [Exported] [datetime] NULL,
						[ExternalId] [nvarchar](36) NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.OneSApi_Order ADD CONSTRAINT
			        PK_OneSApi_Order PRIMARY KEY CLUSTERED 
			        (
			            [OrderId] ASC, 
						[ExportType] ASC
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			if (!ModulesRepository.IsExistsModuleTable("Module", "OneSApi_Product"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.OneSApi_Product
			        (
						[ProductId] [int] NOT NULL,
						[ExternalId] [nvarchar](73) NOT NULL,
						[ExpectedDate] [datetime] NULL,
						[PriceOnRequest] [bit] NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.OneSApi_Product ADD CONSTRAINT
			        PK_OneSApi_Product PRIMARY KEY CLUSTERED 
			        (
			            [ProductId] ASC 
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			if (!ModulesRepository.IsExistsModuleTable("Module", "OneSApi_Offer"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.OneSApi_Offer
			        (
						[OfferId] [int] NOT NULL,
						[PriceOnRequest] [float] NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.OneSApi_Offer ADD CONSTRAINT
			        PK_OneSApi_Offer PRIMARY KEY CLUSTERED 
			        (
			            [OfferId] ASC 
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			if (!ModulesRepository.IsExistsModuleTable("Module", "DepotAmounts_Depot"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.DepotAmounts_Depot
			        (
						[DepotId] [int] IDENTITY(1,1) NOT NULL,
						[Code] [nvarchar](9) NOT NULL,
						[Name] [nvarchar](50) NOT NULL,
						[Title] [nvarchar](100) NULL,
						[Active] [bit] NOT NULL,
						[SortOrder] [int] NOT NULL,
						[Hint] [nvarchar](max) NULL,
						[DepartmentId] [int] NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.DepotAmounts_Depot ADD CONSTRAINT
			        PK_DepotAmounts_Depot PRIMARY KEY CLUSTERED 
			        (
			            [DepotId] ASC 
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			if (!ModulesRepository.IsExistsModuleTable("Module", "DepotAmounts_Product"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.DepotAmounts_Product
			        (
						[ProductId] [int] NOT NULL,
						[DepotId] [int] NOT NULL,
						[Amount] [float] NOT NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.DepotAmounts_Product ADD CONSTRAINT
			        PK_DepotAmounts_Product PRIMARY KEY CLUSTERED 
			        (
						[ProductId] ASC,
						[DepotId] ASC
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			if (!ModulesRepository.IsExistsModuleTable("Module", "OneSApi_ShippingMethod"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.OneSApi_ShippingMethod
			        (
						[ShippingMethodKey] [nvarchar](50) NOT NULL,
						[TrackingUrl] [nvarchar](max) NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.OneSApi_ShippingMethod ADD CONSTRAINT
			        PK_OneSApi_ShippingMethod PRIMARY KEY CLUSTERED 
			        (
						[ShippingMethodKey] ASC
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			if (!ModulesRepository.IsExistsModuleTable("Module", "OneSApi_ProductExport"))
			{
				ModulesRepository.ModuleExecuteNonQuery(
				@"CREATE TABLE Module.OneSApi_ProductExport
			        (
						[ProductId] [int] NOT NULL,
			            [ExportType] [int] NOT NULL,
			            [ForExport] [bit] NOT NULL,
			            [Changed] [datetime] NOT NULL,
			            [Exported] [datetime] NULL,
			        )  ON [PRIMARY]

			        ALTER TABLE Module.OneSApi_ProductExport ADD CONSTRAINT
			        PK_OneSApi_ProductExport PRIMARY KEY CLUSTERED 
			        (
			            [ProductId] ASC 
			        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

				CommandType.Text);
			}

			return UpdateModule();
		}

		public static bool UpdateModule()
		{
			//ModulesRepository.ModuleExecuteNonQuery(
			//	"If not Exists(Select * From INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OneSApi_Order' AND COLUMN_NAME = 'Changed') " +
			//	"Begin " +
			//		"ALTER TABLE Module.OneSApi_Order ADD [Changed] [datetime] NULL " +
			//	"End",
			//	CommandType.Text);
			//ModulesRepository.ModuleExecuteNonQuery(
			//	"If not Exists(Select * From INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OneSApi_Order' AND COLUMN_NAME = 'ExportType') " +
			//	"Begin " +
			//		"ALTER TABLE Module.OneSApi_Order ADD [ExportType] [int] NULL " +
			//	"End",
			//	CommandType.Text);

			//if (!ModulesRepository.IsExistsModuleTable("Module", "OneSApi_ProductExport"))
			//{
			//	ModulesRepository.ModuleExecuteNonQuery(
			//	@"CREATE TABLE Module.OneSApi_ProductExport
			//        (
			//			[ProductId] [int] NOT NULL,
			//            [ExportType] [int] NOT NULL,
			//            [ForExport] [bit] NOT NULL,
			//            [Changed] [datetime] NOT NULL,
			//            [Exported] [datetime] NULL,
			//        )  ON [PRIMARY]

			//        ALTER TABLE Module.OneSApi_ProductExport ADD CONSTRAINT
			//        PK_OneSApi_ProductExport PRIMARY KEY CLUSTERED 
			//        (
			//            [ProductId] ASC 
			//        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]",

			//	CommandType.Text);
			//}
			////ModulesRepository.ModuleExecuteNonQuery(
			////	"If not Exists(Select * From INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OneSApi_Product' AND COLUMN_NAME = 'ExportType') " +
			////	"Begin " +
			////		"ALTER TABLE Module.OneSApi_Product ADD [ExportType] [int] NULL " +
			////	"End",
			////	CommandType.Text);
			////ModulesRepository.ModuleExecuteNonQuery(
			////	"If not Exists(Select * From INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OneSApi_Product' AND COLUMN_NAME = 'ForExport') " +
			////	"Begin " +
			////		"ALTER TABLE Module.OneSApi_Product ADD [ForExport] [bit] NULL " +
			////	"End",
			////	CommandType.Text);
			////ModulesRepository.ModuleExecuteNonQuery(
			////	"If not Exists(Select * From INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OneSApi_Product' AND COLUMN_NAME = 'Changed') " +
			////	"Begin " +
			////		"ALTER TABLE Module.OneSApi_Product ADD [Changed] [datetime] NULL " +
			////	"End",
			////	CommandType.Text);
			////ModulesRepository.ModuleExecuteNonQuery(
			////	"If not Exists(Select * From INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OneSApi_Product' AND COLUMN_NAME = 'Exported') " +
			////	"Begin " +
			////		"ALTER TABLE Module.OneSApi_Product ADD [Exported] [datetime] NULL " +
			////	"End",
			////	CommandType.Text);

			return true;
		}

		public static bool UninstallModule()
		{
			if (ModulesRepository.IsExistsModuleTable("Module", "OneSApi_Order"))
			{
				ModulesRepository.ModuleExecuteNonQuery("DROP TABLE Module.OneSApi_Order",
				CommandType.Text);
			}

			return true;
		}

		public static ImportExportSettingsModel GetImportExportSettings()
		{
			var s = ModuleSettingsProvider.GetSettingValue<string>("ImportExportSettings", OneSApi.ModuleStringId);
			ImportExportSettingsModel settings;
			try
			{
				settings = JsonConvert.DeserializeObject<ImportExportSettingsModel>(s);
			}
			catch
			{
				settings = new ImportExportSettingsModel()
				{
					ImportProduct = new ImportProduct
					{
						ImportHierarhy = true,
						ImportArtnoType = ImportArtnoType.ArtNo,
						ImportNameType = ImportNameType.FullName
					},
					ExportOrder = new ExportOrder
					{
						ItemsPerPage = 10,
					},
					ImportOrder = new ImportOrder { },
					ImportCustomer = new ImportCustomer { }
				};
			}

			if (settings.ImportProduct == null)
			{
				settings.ImportProduct = new ImportProduct
				{
					AddCategory = settings.AddCategory,
					CreateProducts = settings.CreateProducts,
					CreateProperties = settings.CreateProperties,
					DefaultCategoryId = settings.DefaultCategoryId,
					ImportArtnoType = settings.ImportArtnoType,
					ImportHierarhy = settings.ImportHierarhy,
					ImportToDefaultCategory = settings.ImportToDefaultCategory,
					UpdateArtNo = settings.UpdateArtNo,
					UpdateBrand = settings.UpdateBrand,
					UpdateBriefDescription = settings.UpdateBriefDescription,
					UpdateCategory = settings.UpdateCategory,
					UpdateDescription = settings.UpdateDescription,
					UpdateEnabled = settings.UpdateEnabled,
					UpdateName = settings.UpdateName,
					UpdatePhotos = settings.UpdatePhotos,
					UpdateProperties = settings.UpdateProperties,
					UpdateTax = settings.UpdateTax,
					UpdateUnit = settings.UpdateUnit
				};
			}
			if (settings.ExportOrder == null)
			{
				settings.ExportOrder = new ExportOrder
				{
					ExportChangeStatus = settings.ExportChangeStatus,
					ExportPayed = settings.ExportPayed,
					ExportUpdated = settings.ExportUpdated,
					ItemsPerPage = settings.ItemsPerPage,
					UseIn1C = settings.UseIn1C
				};
			}

			return settings;
		}

		public static string WriteLog(string folder, string json, string result, DateTime? start = null, bool finish = true, string error = null)
		{
			var logPath = FoldersHelper.GetPathAbsolut(FolderType.UserFiles, string.Format("modules\\{0}\\{1}", OneSApi.ModuleStringId, folder));
			FileHelpers.CreateDirectory(logPath);

			if (!start.HasValue) start = DateTime.Now;
			var filename = start.Value.ToString("yyyyMMddHHmmss") + ".json";

			using (var wr = new StreamWriter(logPath + "\\" + filename))
			{
				wr.WriteLine(string.Format("Start: {0}", start.Value.ToString("dd.MM.yyyy HH:mm:ss")));

				if (json.IsNotEmpty())
				{
					wr.WriteLine();
					wr.WriteLine("Request:");
					wr.WriteLine(json);
				}

				if (result.IsNotEmpty())
				{
					wr.WriteLine();
					wr.WriteLine("Result:");
					wr.WriteLine(result);
				}

				if (error.IsNotEmpty())
				{
					wr.WriteLine();
					wr.WriteLine("Error:");
					wr.WriteLine(error);
				}

				if (finish)
				{
					wr.WriteLine();
					wr.WriteLine(string.Format("Finish: {0}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")));
				}
			}

			return filename;
		}

		public static void AppendLog(string folder, string filename, string result, bool finish = true)
		{
			var logPath = FoldersHelper.GetPathAbsolut(FolderType.UserFiles, string.Format("modules\\{0}\\{1}", OneSApi.ModuleStringId, folder));

			using (var wr = new StreamWriter(logPath + "\\" + filename, true))
			{
				if (result.IsNotEmpty())
				{
					wr.WriteLine();
					wr.WriteLine("Result:");
					wr.WriteLine(result);
				}

				if (finish)
				{
					wr.WriteLine();
					wr.WriteLine(string.Format("Finish: {0}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")));
				}
			}
		}

		public static void TryAction(Action action, string message = null)
		{
			try
			{
				action();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.Log.Error(ex);
			}
		}

		public static bool IsAllowSiteBrowsingByIp(string ip)
		{
			//if (!BrowsersHelper.IsBot())
			//	return true;
			if (ip == "::1")
				return true;
			int? countryId = GetCountryIdByIp(ip);
			//if (!countryId.HasValue || countryId == 0)
			//	return true;
			Debug.Log.Warn($"IsAllowSiteBrowsingByIp ip: {ip}  country: {countryId}");

			return countryId == SettingsMain.SellerCountryId;
		}

		public static int? GetCountryIdByIp(string ip)
		{
			if (ip.IsLocalIP())
				return null;

			var modules = GetModuleInstances<IGeoIp>();
			if (modules != null && modules.Count != 0)
			{
				foreach (var module in modules)
				{
					var moduleZone = module.GetIpZone(ip);
					if (moduleZone != null && moduleZone.CountryId != 0)
						return moduleZone.CountryId;
				}
			}

			var geoIpData = GeoIpService.GetGeoIpData(ip);
			if (geoIpData != null && geoIpData.Country.IsNotEmpty())
			{
				var country = CountryService.GetCountryByIso2(geoIpData.Country);
				return country?.CountryId;
			}
			return null;
		}

		public static List<T> GetModuleInstances<T>()
		{
			var types = AttachedModules.GetModules<T>(false);
			if (types == null || types.Count == 0)
				return null;

			var result = new List<T>();

			for (var i = 0; i < types.Count; i++)
			{
				if (types[i] == null)
					continue;

				var instance = Activator.CreateInstance(types[i]);
				if (instance == null)
					continue;

				result.Add((T)instance);
			}

			return result;
		}

		public static string GenerateRandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var random = new Random();
			char[] result = new char[length];
			for (int i = 0; i < length; i++)
			{
				result[i] = chars[random.Next(chars.Length)];
			}
			return new string(result);
		}

		public static void SetRegCode(Guid customerId, string regCode)
		{
			ModulesRepository.ModuleExecuteNonQuery(
				"Update [Customers].[Customer] Set RegCodeHash=@regCode, RegCodeExpired=@regCodeExpired Where CustomerID=@customerId",
				CommandType.Text,
				new SqlParameter("@regCode", regCode),
				new SqlParameter("@regCodeExpired", DateTime.Now.AddDays(1)),
				new SqlParameter("@customerId", customerId));
		}

		public static string GetRegCode(Guid customerId)
		{
			return ModulesRepository.ModuleExecuteScalar<string>(
				"Select RegCodeHash From [Customers].[Customer] Where CustomerID=@customerId",
				CommandType.Text, new SqlParameter("@customerId", customerId));
		}

		public static Customer CheckRegCode(string hash)
		{
			var c = ModulesRepository.ModuleExecuteReadOne(
				"Select CustomerID, RegCodeExpired From [Customers].[Customer] Where RegCodeHash=@regCode", CommandType.Text,
				reader => (SQLDataHelper.GetString(reader, "CustomerID"), SQLDataHelper.GetNullableDateTime(reader, "RegCodeExpired")),
				new SqlParameter("@regCode", hash));
			if (c.Item1.IsNotEmpty() && c.Item2.HasValue && c.Item2 >= DateTime.Now)
			{
				var customerId = Guid.Parse(c.Item1);
				ModulesRepository.ModuleExecuteNonQuery(
					"Update [Customers].[Customer] Set [Enabled]=1, RegCodeHash=NULL, RegCodeExpired=NULL Where CustomerID=@customerId",
					CommandType.Text,
					new SqlParameter("@customerId", customerId));
				return CustomerService.GetCustomer(customerId);
			}
			else
				return null;
		}

		public static bool GetConfirmPhone(string customerId)
		{
			return ModulesRepository.ModuleExecuteScalar<bool>(
				"Select IsNull(PhoneConfirmed, 0) From [Customers].[Customer] Where CustomerID=@customerId",
				CommandType.Text, new SqlParameter("@customerId", customerId));
		}

		public static void SetConfirmPhone(string customerId, string phone)
		{
			var standartPhone = StringHelper.ConvertToStandardPhone(phone, true, true);
			ModulesRepository.ModuleExecuteNonQuery(
				"Update [Customers].[Customer] Set Phone=@phone, StandardPhone=@standartPhone, PhoneConfirmed=1 Where CustomerID=@customerId",
				CommandType.Text,
				new SqlParameter("@phone", phone),
				new SqlParameter("@standartPhone", standartPhone),
				new SqlParameter("@customerId", customerId));

		}

		public static List<Guid> GetExpiredConfirmRegistrartion()
		{
			var result = new List<Guid>();
			var all = ModulesRepository.ModuleExecuteReadList<(Guid, DateTime?)>(
				"Select CustomerID, RegCodeExpired From [Customers].[Customer] Where Enabled = 0",
				CommandType.Text,
				reader => (SQLDataHelper.GetGuid(reader, "CustomerID"), SQLDataHelper.GetNullableDateTime(reader, "RegCodeExpired")));
            foreach (var item in all)
            {
				if (!item.Item2.HasValue || item.Item2 < DateTime.Now)
					result.Add(item.Item1);
			}
			return result;
		}

	}
}