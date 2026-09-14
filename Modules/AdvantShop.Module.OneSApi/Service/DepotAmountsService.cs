using AdvantShop.Core.Modules;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class DepotAmountsService
    {

        #region Install

        public static bool InstallModule()
        {
            if (!ModulesRepository.IsExistsModuleTable("Module", "DepotAmounts_Depot"))
            {
                ModulesRepository.ModuleExecuteNonQuery(
                @"CREATE TABLE [Module].[DepotAmounts_Depot](
	                [DepotId] [int] IDENTITY(1,1) NOT NULL,
	                [Code] [nvarchar](9) NOT NULL,
	                [Name] [nvarchar](50) NOT NULL,
	                [Title] [nvarchar](100) NULL,
	                [Active] [bit] NOT NULL,
	                [SortOrder] [int] NOT NULL,
	                [Hint] [nvarchar](max) NULL,
	                [DepartmentId] [int] NULL,
                 CONSTRAINT [PK_DepotAmounts_Depot] PRIMARY KEY CLUSTERED 
                (
	                [DepotId] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
                ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]",

                CommandType.Text);
            }

            if (!ModulesRepository.IsExistsModuleTable("Module", "DepotAmounts_Product"))
            {
                ModulesRepository.ModuleExecuteNonQuery(
                @"CREATE TABLE [Module].[DepotAmounts_Product](
	                [ProductId] [int] NOT NULL,
	                [DepotId] [int] NOT NULL,
	                [Amount] [float] NOT NULL,
                 CONSTRAINT [PK_DepotAmounts_Offer] PRIMARY KEY CLUSTERED 
                (
	                [ProductId] ASC,
	                [DepotId] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
                ) ON [PRIMARY]",

                CommandType.Text);
            }

            return true;
        }

        #endregion

        public static List<DepotModel> GetDepots()
        {
            return ModulesRepository.ModuleExecuteReadList<DepotModel>(
                @"SELECT * FROM [Module].[DepotAmounts_Depot] Order By [SortOrder]",
                CommandType.Text,
                reader => GetDepotFromReader(reader));
        }

        private static DepotModel GetDepotFromReader(SqlDataReader reader)
        {
            return new DepotModel
            {
                DepotId = SQLDataHelper.GetInt(reader, "DepotId"),
                Code = SQLDataHelper.GetString(reader, "Code"),
                Name = SQLDataHelper.GetString(reader, "Name"),
                Title = SQLDataHelper.GetString(reader, "Title"),
                Hint = SQLDataHelper.GetString(reader, "Hint"),
                SortOrder = SQLDataHelper.GetInt(reader, "SortOrder"),
                Active = SQLDataHelper.GetBoolean(reader, "Active"),
                DepartmentId = SQLDataHelper.GetNullableInt(reader, "DepartmentId")
            };
        }

        public static DepotModel GetDepot(int id)
        {
            var result = ModulesRepository.ModuleExecuteReadOne(
                @"SELECT * FROM [Module].[DepotAmounts_Depot] Where [DepotId]=@id",
                CommandType.Text,
                reader => GetDepotFromReader(reader),
                new SqlParameter("@id", id));

            return result;
        }

        public static void AddDepot(DepotModel model)
        {
            //if (model.Main)
            //    ModulesRepository.ModuleExecuteNonQuery(
            //        @"UPDATE [Module].[DepotAmounts_Depot] Set Main=0 ",
            //        CommandType.Text
            //        );
            ModulesRepository.ModuleExecuteNonQuery(
                @"INSERT INTO [Module].[DepotAmounts_Depot] (Code, Name, Title, Hint, SortOrder, Active, DepartmentId) Values (@Code, @Name, @Title, @Hint, @SortOrder, @Active, @DepartmentId) ",
                CommandType.Text,
                new SqlParameter("@Code", model.Code),
                new SqlParameter("@Name", model.Name),
                new SqlParameter("@Title", model.Title ?? (object)DBNull.Value),
                new SqlParameter("@Hint", model.Hint ?? (object)DBNull.Value),
                new SqlParameter("@SortOrder", model.SortOrder),
                new SqlParameter("@Active", model.Active),
                new SqlParameter("@DepartmentId", model.DepartmentId ?? (object)DBNull.Value)
                );
        }

        public static void UpdateDepot(DepotModel model)
        {
            //if (model.Main)
            //    ModulesRepository.ModuleExecuteNonQuery(
            //        @"UPDATE [Module].[DepotAmounts_Depot] Set Main=0 ",
            //        CommandType.Text
            //        );
            ModulesRepository.ModuleExecuteNonQuery(
                @"UPDATE [Module].[DepotAmounts_Depot] Set Code=@Code, Name=@Name, Title=@Title, Hint=@Hint, SortOrder=@SortOrder, Active=@Active, DepartmentId=@DepartmentId Where [DepotId]=@id ",
                CommandType.Text,
                new SqlParameter("@id", model.DepotId),
                new SqlParameter("@Code", model.Code),
                new SqlParameter("@Name", model.Name),
                new SqlParameter("@Title", model.Title ?? (object)DBNull.Value),
                new SqlParameter("@Hint", model.Hint ?? (object)DBNull.Value),
                new SqlParameter("@SortOrder", model.SortOrder),
                new SqlParameter("@Active", model.Active),
                new SqlParameter("@DepartmentId", model.DepartmentId ?? (object)DBNull.Value)
                );
        }

        public static void DeleteDepot(int id)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                @"DELETE FROM [Module].[DepotAmounts_Depot] Where [DepotId]=@id ",
                CommandType.Text,
                new SqlParameter("@id", id));
        }

        public static DepotModel GetDepot(string code)
        {
            return ModulesRepository.ModuleExecuteReadOne<DepotModel>(
                @"SELECT Top 1 * FROM [Module].[DepotAmounts_Depot] Where [Code]=@code",
                CommandType.Text, 
                reader => GetDepotFromReader(reader),
                new SqlParameter("@code", code));
        }

        public static List<DepotProductModel> GetAmounts(int productId)
        {
            return ModulesRepository.ModuleExecuteReadList(
                @"SELECT DepotAmounts_Product.DepotId, IsNull(Amount, 0) As Amount, [Name], [Title] 
                  FROM [Module].[DepotAmounts_Depot] 
                  Left Join [Module].[DepotAmounts_Product] On DepotAmounts_Depot.DepotId=DepotAmounts_Product.DepotId AND ProductID = @productId
                  Where [Active] = 1 " + 
                  "ORDER BY SortOrder",
                CommandType.Text,
                reader => new DepotProductModel
                {
                    //ProductId = ModulesRepository.ConvertTo<int>(reader, "ProductId"),
                    DepotId = ModulesRepository.ConvertTo<int>(reader, "DepotId"),
                    Amount = ModulesRepository.ConvertTo<float>(reader, "Amount"),
                    //ArtNo = ModulesRepository.ConvertTo<string>(reader, "ArtNo"),
                    DepotName = ModulesRepository.ConvertTo<string>(reader, "Name"),
                    DepotTitle = ModulesRepository.ConvertTo<string>(reader, "Title"),
                },
                new SqlParameter("@productId", productId));
        }

        public static void AddUpdateDepotOffer(int productId, DepotProductModel model)
        {
            var c = ModulesRepository.ModuleExecuteScalar<int>(
                "SELECT Count(Amount) FROM [Module].[DepotAmounts_Product] Where [ProductId]=@productId And [DepotId]=@depotId",
                CommandType.Text,
                new SqlParameter("@productId", productId),
                new SqlParameter("@depotId", model.DepotId)
                );
            if (c == 0)
                ModulesRepository.ModuleExecuteNonQuery(
                    @"INSERT INTO [Module].[DepotAmounts_Product] ([ProductId], [DepotId], Amount) Values (@productId, @depotId, @Amount) ",
                    CommandType.Text,
                    new SqlParameter("@productId", productId),
                    new SqlParameter("@depotId", model.DepotId),
                    new SqlParameter("@Amount", model.Amount)
                    );
            else
                ModulesRepository.ModuleExecuteNonQuery(
                    @"UPDATE [Module].[DepotAmounts_Product] Set Amount=@Amount Where [ProductId]=@productId And [DepotId]=@depotId ",
                    CommandType.Text,
                    new SqlParameter("@productId", productId),
                    new SqlParameter("@depotId", model.DepotId),
                    new SqlParameter("@Amount", model.Amount)
                    );
        }

        public static void ClearDepotOffers(int productId)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                @"DELETE FROM [Module].[DepotAmounts_Product] Where [ProductId]=@productId ",
                CommandType.Text,
                new SqlParameter("@productId", productId));
        }

        public static bool UpadateProductAmounts(int productId, List<DepotProductModel> newDepotAmounts)
        {
            var oldDepotAmounts = GetAmounts(productId);
            var oldDA = oldDepotAmounts.OrderBy(x => x.DepotId).Select(x => string.Format("{0}_{1}", x.DepotId, x.Amount)).ToArray();
            var newDA = newDepotAmounts.OrderBy(x => x.DepotId).Select(x => string.Format("{0}_{1}", x.DepotId, x.Amount)).ToArray();
            var changeDepotAmounts = !Enumerable.SequenceEqual(oldDA, newDA);
            if (changeDepotAmounts)
            {
                ClearDepotOffers(productId);
                foreach (var offer in newDepotAmounts)
                {
                    AddUpdateDepotOffer(productId, offer);
                }
            }
            return changeDepotAmounts;
        }

        public static List<ClientDepotProductModel> GetClientAmounts(int productId)
        {
            return ModulesRepository.ModuleExecuteReadList(
                @"SELECT Sum(IsNull(Amount, 0)) As Amount, SortOrder, [Title]--, [Unit] 
                  FROM [Module].[DepotAmounts_Depot] 
                  Left Join [Module].[DepotAmounts_Product] On DepotAmounts_Product.DepotId = DepotAmounts_Depot.DepotId And DepotAmounts_Product.ProductID = @productId
                  Left Join [Catalog].[Product] On DepotAmounts_Product.ProductId = Product.ProductId
                  Where DepotAmounts_Depot.Active = 1 --And DepotAmounts_Product.ProductID = @productId
                  Group By SortOrder, Title--, Unit
                  Order By SortOrder",
                CommandType.Text,
                reader => new ClientDepotProductModel
                {
                    Amount = ModulesRepository.ConvertTo<float>(reader, "Amount"),
                    DepotTitle = ModulesRepository.ConvertTo<string>(reader, "Title"),
                    //Unit = ModulesRepository.ConvertTo<string>(reader, "Unit")
                },
                new SqlParameter("@productId", productId));
        }

        //public static List<AdminDepotOfferModel> GetAmounts(int productId)
        //{
        //    var offers = OfferService.GetProductOffers(productId).Select(x => x.OfferId).ToList();
        //    if (offers.Count == 0)
        //        return new List<AdminDepotOfferModel>();

        //    string offersStr = "";
        //    foreach (var of in offers)
        //    {
        //        offersStr += of + ",";
        //    }

        //    return ModulesRepository.ModuleExecuteReadList(
        //        string.Format(
        //            "SELECT DepotAmounts_Offer.OfferId, DepotAmounts_Offer.DepotId, DepotAmounts_Offer.Amount, ArtNo, Name FROM [Module].[DepotAmounts_Offer] " +
        //            "Left Join [Catalog].[Offer] On Offer.OfferID=DepotAmounts_Offer.OfferId " +
        //            "Left Join [Module].[DepotAmounts_Depot] On DepotAmounts_Depot.DepotId=DepotAmounts_Offer.DepotId " +
        //            "Where DepotAmounts_Offer.OfferId IN ({0})",
        //            offersStr.TrimEnd(',')),
        //        CommandType.Text,
        //        reader => new AdminDepotOfferModel
        //        {
        //            OfferId = ModulesRepository.ConvertTo<int>(reader, "OfferId"),
        //            DepotId = ModulesRepository.ConvertTo<int>(reader, "DepotId"),
        //            Amount = ModulesRepository.ConvertTo<float>(reader, "Amount"),
        //            ArtNo = ModulesRepository.ConvertTo<string>(reader, "ArtNo"),
        //            DepotName = ModulesRepository.ConvertTo<string>(reader, "Name"),
        //        });
        //}

        //public static List<AdminDepotOfferModel> GetAmountsForOffer(int offerId)
        //{
        //    return ModulesRepository.ModuleExecuteReadList(
        //            "SELECT DepotAmounts_Offer.OfferId, DepotAmounts_Offer.DepotId, DepotAmounts_Offer.Amount, ArtNo, Name FROM [Module].[DepotAmounts_Offer] " +
        //            "Left Join [Catalog].[Offer] On Offer.OfferID=DepotAmounts_Offer.OfferId " +
        //            "Left Join [Module].[DepotAmounts_Depot] On DepotAmounts_Depot.DepotId=DepotAmounts_Offer.DepotId " +
        //            "Where DepotAmounts_Offer.OfferId = @offerId",
        //        CommandType.Text,
        //        reader => new AdminDepotOfferModel
        //        {
        //            OfferId = ModulesRepository.ConvertTo<int>(reader, "OfferId"),
        //            DepotId = ModulesRepository.ConvertTo<int>(reader, "DepotId"),
        //            Amount = ModulesRepository.ConvertTo<float>(reader, "Amount"),
        //            ArtNo = ModulesRepository.ConvertTo<string>(reader, "ArtNo"),
        //            DepotName = ModulesRepository.ConvertTo<string>(reader, "Name"),
        //        },
        //        new SqlParameter("@offerId", offerId));
        //}

        //public static AdminDepotOfferModel GetDepotAmount(int offerId, int depotId)
        //{
        //    return ModulesRepository.ModuleExecuteReadOne<AdminDepotOfferModel>(
        //            "SELECT DepotAmounts_Offer.OfferId, DepotAmounts_Offer.DepotId, DepotAmounts_Offer.Amount, ArtNo, Name FROM [Module].[DepotAmounts_Offer] " +
        //            "Left Join [Catalog].[Offer] On Offer.OfferID=DepotAmounts_Offer.OfferId " +
        //            "Left Join [Module].[DepotAmounts_Depot] On DepotAmounts_Depot.DepotId=DepotAmounts_Offer.DepotId " +
        //            "Where DepotAmounts_Offer.OfferId = @offerId And DepotAmounts_Offer.DepotId = @depotId",
        //        CommandType.Text,
        //        reader => new AdminDepotOfferModel
        //        {
        //            OfferId = ModulesRepository.ConvertTo<int>(reader, "OfferId"),
        //            DepotId = ModulesRepository.ConvertTo<int>(reader, "DepotId"),
        //            Amount = ModulesRepository.ConvertTo<float>(reader, "Amount"),
        //            ArtNo = ModulesRepository.ConvertTo<string>(reader, "ArtNo"),
        //            DepotName = ModulesRepository.ConvertTo<string>(reader, "Name"),
        //        },
        //        new SqlParameter("@offerId", offerId),
        //        new SqlParameter("@depotId", depotId)
        //        );
        //}

        //#region ICSVExportImport

        //public static string PrepareCSVField(CSVField field, int productId)
        //{
        //    var code = field.StrName.Replace("amount_", "");
        //    var depot = GetDepot(code);
        //    if (depot == null)
        //        return "";
        //    var product = ProductService.GetProduct(productId);
        //    if (product == null)
        //        return "";
        //    var offer = OfferService.GetMainOffer(product.Offers, true);
        //    if (offer == null)
        //        return "";
        //    var amount = GetAmountsForOffer(offer.OfferId).SingleOrDefault(x => x.DepotId == depot.DepotId);
        //    if (amount == null)
        //        return "";

        //    return amount.Amount.ToString();
        //}

        //public static bool ProcessCSVField(CSVField field, int productId, string value)
        //{
        //    float amount;
        //    if (string.IsNullOrEmpty(value))
        //        amount = 0; 
        //    else if (!float.TryParse(value, out amount))
        //        return true;
        //    var code = field.StrName.Replace("amount_", "");
        //    var depot = GetDepot(code);
        //    if (depot == null)
        //        return true;
        //    var product = ProductService.GetProduct(productId);
        //    if (product == null)
        //        return true;
        //    var offer = OfferService.GetMainOffer(product.Offers, true);
        //    if (offer == null)
        //        return true;

        //    AddUpdateDepotOffer(new AdminDepotOfferModel()
        //        { OfferId = offer.OfferId, DepotId = depot.DepotId, Amount = amount });

        //    return true;
        //}

        //#endregion

    }
}
