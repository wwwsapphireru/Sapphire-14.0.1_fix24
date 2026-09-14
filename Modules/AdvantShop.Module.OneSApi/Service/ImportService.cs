using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.SEO;
using AdvantShop.Shipping;
using AdvantShop.Taxes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AdvantShop.Module.OneSApi.Service
{
    public class ImportService
    {
        public static int GetIdByExternalId(string externalId, string table)
        {
            return ModulesRepository.ModuleExecuteScalar<int>(
                string.Format("Select {0}Id From Module.OneSApi_{0} Where ExternalId = @externalId ", table),
                CommandType.Text,
                new SqlParameter("@externalId", externalId ?? ""));
        }

        public static string GetExternalIdById(int id, string table)
        {
            return ModulesRepository.ModuleExecuteScalar<string>(
                string.Format("Select ExternalId From Module.OneSApi_{0} Where {0}Id = @id ", table),
                CommandType.Text,
                new SqlParameter("@id", id));
        }

        public static void InsertExternalId(int id, string externalId, string table)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Delete From Module.OneSApi_{0} Where {0}Id = @id Or ExternalId = @externalId ", table),
                CommandType.Text,
                new SqlParameter("@id", id), new SqlParameter("@externalId", externalId));
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Insert Into Module.OneSApi_{0} ({0}Id, ExternalId) Values (@id, @externalId) ", table),
                CommandType.Text,
                new SqlParameter("@id", id), new SqlParameter("@externalId", externalId));
        }

        public static void DeleteExternalId(string externalId, string table)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("Delete From Module.OneSApi_{0} Where ExternalId = @externalId ", table),
                CommandType.Text,
                new SqlParameter("@externalId", externalId));
        }

        public static int FindBrand(string brandName, bool create)
        {
            var brandId = BrandService.GetBrandIdByName(brandName);
            if (brandId > 0)
                return brandId;

            if (!create)
                return 0;

            var brand = new Brand()
            {
                Name = brandName,
                UrlPath = UrlService.GetAvailableValidUrl(0, ParamType.Brand, brandName)
            };
            return BrandService.AddBrand(brand);
        }

        public static int? FindTax(float taxRate)
        {
            var tax = TaxService.GetTaxes().OrderByDescending(x => x.Enabled).FirstOrDefault(x => x.Rate == taxRate && x.Enabled);
            return tax != null ? tax.TaxId : (int?)null;
        }

        public static int? FindTax(string taxRate)
        {
            return FindTax(taxRate.TryParseFloat());
        }

        public static int GetCategoryFromImport(List<ProductCategoryModel> categories, bool create, out string strCategories, ImportExportSettingsModel settings = null)
        {
            if (settings == null)
                settings = ModuleService.GetImportExportSettings();

            var parentId = 0;
            strCategories = "";
            //if (category == null && !settings.ImportHierarhy)
            //    return null;

            if (settings.ImportProduct.ImportToDefaultCategory && settings.ImportProduct.DefaultCategoryId > 0)
            {
                var defaultCat = CategoryService.GetCategory(settings.ImportProduct.DefaultCategoryId);
                if (defaultCat != null)
                    strCategories = defaultCat.Name;
                if (settings.ImportProduct.ImportHierarhy)
                {
                    for (int i = categories.Count - 1; i >= 0; i--)
                    {
                        var cat = categories[i];
                        if (strCategories.IsNotEmpty())
                            strCategories += " >> ";
                        strCategories += cat.Name;
                    }
                }
            }
            else
            {
                if (settings.ImportProduct.ImportHierarhy)
                {
                    for (int i = categories.Count - 1; i >= 0; i--)
                    {
                        var cat = categories[i];
                        var category = CategoryService.GetCategoryFromDbByExternalId(cat.ExternalId);
                        if (category == null)
                        {
                            var c = CategoryService.GetChildCategoryIdByName(parentId, cat.Name);
                            if (c.HasValue)
                            {
                                ModulesRepository.ModuleExecuteNonQuery(
                                    "Update Catalog.Category set ExternalId=@ExternalId where CategoryID = @CategoryID",
                                    CommandType.Text,
                                    new SqlParameter("@ExternalId", cat.ExternalId),
                                    new SqlParameter("@CategoryID", c.Value));
                                category = CategoryService.GetCategory(c.Value);
                                parentId = c.Value;
                            }
                        }
                        else
                            parentId = category.CategoryId;
                        if (category == null)
                        {
                            if (create)
                                parentId = CategoryService.AddCategory(new Category
                                {
                                    ExternalId = cat.ExternalId,
                                    Name = cat.Name,
                                    ParentCategoryId = parentId,
                                    SortOrder = 0,
                                    Enabled = true,
                                    DisplayChildProducts = false,
                                    UrlPath = UrlService.GetAvailableValidUrl(0, ParamType.Category, cat.Name),
                                    DisplayStyle = ECategoryDisplayStyle.Tile,
                                    ModifiedBy = "",
                                    Sorting = ESortOrder.AscByName
                                }, true, false, null);
                            else
                                return 0;
                        }
                        else if (category.Name != cat.Name)
                        {
                            ModulesRepository.ModuleExecuteNonQuery(
                                "Update Catalog.Category set Name=@Name where CategoryID = @CategoryID",
                                CommandType.Text,
                                new SqlParameter("@Name", cat.Name),
                                new SqlParameter("@CategoryID", category.CategoryId));
                        }
                    }
                }
            }

            return parentId;
        }

        public static void ProcessProductProperties(int productId, ProductPropertiesModel properties, bool create)
        {
            var existValuesAll = GetProductPropertyValues(productId, null);
            var actualValueIdsAll = new List<int>();

            var group = PropertyGroupService.Get(properties.Group);
            //if (group == null && properties.Properties.Count > 0)
            //{
            //    group = new PropertyGroup { Name = properties.Group/*OneSApi.ModuleName*/ };
            //    group.PropertyGroupId = PropertyGroupService.Add(group);
            //}

            foreach (var name in properties.Properties.GroupBy(x => x.Name))
            {
                var property = GetPropertyByNameAndGroup(name.Key, group != null ? group.PropertyGroupId : (int?)null);
                var isNew = property == null;
                if (isNew)
                {
                    //if (create && item.IsDeleted != true)
                    //{
                    //    property = new Property
                    //    {
                    //        Type = (int)(item.RangeValue.HasValue ? PropertyType.Range : PropertyType.Checkbox),
                    //        Name = item.Name,
                    //        //GroupId = group.PropertyGroupId,
                    //        SortOrder = item.SortOrder,
                    //        UseInDetails = true,
                    //        UseInFilter = item.UseInFilter,
                    //        UseInBrief = item.UseInBrief
                    //    };
                    //    PropertyService.AddProperty(property);
                    //}
                    //else
                    continue;
                }
                //else
                //{
                //    if (item.IsDeleted == true)
                //    {
                //        //PropertyService.GetValuesByPropertyId(property.PropertyId);
                //        PropertyService.DeleteProperty(property.PropertyId);
                //    }
                //    else if (property.SortOrder != item.SortOrder || property.UseInFilter != item.UseInFilter || property.UseInBrief != item.UseInBrief || property.Name != item.Name)
                //    {
                //        property.SortOrder = item.SortOrder;
                //        property.UseInFilter = item.UseInFilter;
                //        property.UseInBrief = item.UseInBrief;
                //        property.Name = item.Name;
                //        PropertyService.UpdateProperty(property);
                //    }
                //}

                //if (item.Value.IsNullOrEmpty() && !item.RangeValue.HasValue)
                //{
                //    if (!isNew)
                //        PropertyService.DeleteProductPropertyValues(productId, property.PropertyId);
                //    continue;
                //}

                //var olditems = product.ProductPropertyValues;
                var existValues = !isNew ? GetProductPropertyValues(productId, property.PropertyId) : new List<PropertyValue>();
                var actualValueIds = new List<int>();

                foreach (var item in properties.Properties.Where(x => x.Name == name.Key))
                {
                    var value = item.RangeValue.HasValue ? item.RangeValue.ToString() : item.Value;

                    //foreach (var value in values)
                    //{
                    if (value.IsNullOrEmpty())
                        continue;

                    var existValue = existValues.FirstOrDefault(x => x.Value == value);
                    if (existValue != null)
                    {
                        actualValueIds.Add(existValue.PropertyValueId);
                        actualValueIdsAll.Add(existValue.PropertyValueId);
                        if (item.SortOrder != existValue.SortOrder)
                        {
                            existValue.SortOrder = item.SortOrder;
                            PropertyService.UpdatePropertyValue(existValue);
                        }
                        continue;
                    }

                    var propertyValue = (!isNew ? PropertyService.GetPropertyValueByName(property.PropertyId, value) : null) ??
                        new PropertyValue
                        {
                            PropertyId = property.PropertyId,
                            Value = value,
                            RangeValue = value.RemoveChars(),
                            SortOrder = item.SortOrder
                        };

                    if (propertyValue.PropertyValueId != 0 && (existValue = existValues.FirstOrDefault(x => x.PropertyValueId == propertyValue.PropertyValueId)) != null)
                    {
                        actualValueIds.Add(existValue.PropertyValueId);
                        actualValueIdsAll.Add(existValue.PropertyValueId);
                        continue;
                    }

                    if (propertyValue.PropertyValueId == 0)
                        PropertyService.AddPropertyValue(propertyValue);

                    if (!actualValueIds.Contains(propertyValue.PropertyValueId))
                    {
                        PropertyService.AddProductProperyValue(propertyValue.PropertyValueId, productId);
                        actualValueIds.Add(propertyValue.PropertyValueId);
                        actualValueIdsAll.Add(propertyValue.PropertyValueId);
                    }
                    //}
                    //actualValueIdsAll.AddRange(actualValueIds);

                    // удаление неактуальных значений
                    foreach (var propValueId in existValues.Select(x => x.PropertyValueId).Where(x => !actualValueIds.Contains(x)))
                    {
                        PropertyService.DeleteProductPropertyValue(productId, propValueId);
                    }
                }
            }

            // удаление неактуальных значений
            foreach (var propValueId in existValuesAll.Select(x => x.PropertyValueId).Where(x => !actualValueIdsAll.Contains(x)))
            {
                PropertyService.DeleteProductPropertyValue(productId, propValueId);
            }
        }

        private static List<PropertyValue> GetProductPropertyValues(int productId, int? propertyId)
        {
            return ModulesRepository.ModuleExecuteReadList<PropertyValue>(
                "SELECT PropertyValue.* From Catalog.PropertyValue " +
                "INNER JOIN Catalog.ProductPropertyValue ON ProductPropertyValue.PropertyValueID = PropertyValue.PropertyValueID " +
                "WHERE ProductPropertyValue.ProductID = @productId" + (propertyId.HasValue ? " AND PropertyValue.PropertyID = @PropertyID" : ""),
                CommandType.Text, reader => new PropertyValue
                {
                    PropertyId = SQLDataHelper.GetInt(reader, "PropertyID"),
                    PropertyValueId = SQLDataHelper.GetInt(reader, "PropertyValueID"),
                    Value = SQLDataHelper.GetString(reader, "Value"),
                },
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@PropertyID", propertyId ?? (object)DBNull.Value));
        }

        public static Property GetPropertyByNameAndGroup(string name, int? groupId)
        {
            return ModulesRepository.ModuleExecuteReadOne(
                "Select TOP 1 * From Catalog.Property Where Name=@name And GroupId = @groupId",
                CommandType.Text, GetPropertyFromReader,
                new SqlParameter("@name", name), new SqlParameter("@groupId", groupId ?? (object)DBNull.Value));
        }

        private static Property GetPropertyFromReader(SqlDataReader reader)
        {
            return new Property
            {
                PropertyId = SQLDataHelper.GetInt(reader, "PropertyId"),
                Name = SQLDataHelper.GetString(reader, "Name"),
                SortOrder = SQLDataHelper.GetInt(reader, "SortOrder"),
                UseInFilter = SQLDataHelper.GetBoolean(reader, "UseInFilter"),
                UseInDetails = SQLDataHelper.GetBoolean(reader, "UseInDetails"),
                UseInBrief = SQLDataHelper.GetBoolean(reader, "UseInBrief"),
                Type = SQLDataHelper.GetInt(reader, "Type"),
                GroupId = SQLDataHelper.GetNullableInt(reader, "GroupId"),
            };
        }

        public static void AddProductPhotoByProductId(int productId, byte[] storage, string fullfilename, bool isMain, int? colorId, bool skipOriginal = false)
        {
            var tempName = PhotoService.AddPhoto(new Photo(0, productId, PhotoType.Product)
            {
                Description = "",
                OriginName = fullfilename,
                PhotoSortOrder = 0,
                ColorID = colorId
            });

            if (string.IsNullOrWhiteSpace(tempName)) return;

            using (var ms = new MemoryStream(storage, 0, storage.Length))
            {
                using (var image = System.Drawing.Image.FromStream(ms))
                {
                    FileHelpers.SaveProductImageUseCompress(tempName, image, skipOriginal);
                }
            }
        }

        public static void AddPhotoByObjIdAndType(int objId, PhotoType type, string folder, byte[] storage, string fullfilename, bool skipOriginal = false)
        {
            var tempName = PhotoService.AddPhoto(new Photo(0, objId, type)
            {
                Description = "",
                OriginName = fullfilename,
                PhotoSortOrder = 0,
                //ColorID = colorId
            });

            if (string.IsNullOrWhiteSpace(tempName)) return;

            var resultPath = SettingsGeneral.AbsolutePath + folder;
            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);
            File.WriteAllBytes(resultPath + "/" + tempName, storage);
            //using (var ms = new MemoryStream(storage, 0, storage.Length))
            //{
            //    using (var image = System.Drawing.Image.FromStream(ms))
            //    {
            //        FileHelpers.SaveProductImageUseCompress(tempName, image, skipOriginal);
            //    }
            //}
        }

        public static void UpdateProductPhotoByProductId(int productId, byte[] storage, string fullfilename, int? colorId, bool skipOriginal = false)
        {
            //Если битая фотка - то не будет добавляться запись в базу, упадет на открытии файла
            //try
            //{
            var productPhoto = PhotoService.GetProductPhoto(productId, fullfilename);
            if (productPhoto == null || !productPhoto.PhotoName.IsNotEmpty())
                return;

            productPhoto.ColorID = colorId;

            //var binary = Decompress(storage);

            using (var ms = new MemoryStream(storage, 0, storage.Length))
            {
                using (var image = System.Drawing.Image.FromStream(ms))
                {
                    PhotoService.UpdatePhoto(productPhoto);
                    FileHelpers.SaveProductImageUseCompress(productPhoto.PhotoName, image, skipOriginal);
                }
            }

            //}
            //catch { }
        }

        public static void UpdatePhotoByObjIdAndType(Photo photo, byte[] storage, bool skipOriginal = false)
        {
            using (var ms = new MemoryStream(storage, 0, storage.Length))
            {
                using (var image = System.Drawing.Image.FromStream(ms))
                {
                    PhotoService.UpdatePhoto(photo);
                    FileHelpers.SaveProductImageUseCompress(photo.PhotoName, image, skipOriginal);
                }
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            byte[] decompressedArray = null;
            //try
            //{
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (MemoryStream compressStream = new MemoryStream(data))
                {
                    using (DeflateStream deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(decompressedStream);
                    }
                }
                decompressedArray = decompressedStream.ToArray();
            }
            //}
            //catch (Exception E)
            //{
            //    // do something !
            //}

            return decompressedArray;
        }

        public static int GetProductFileId(int productId, string fileType, string externalId)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Delete From Module.FilesInProduct Where ProductId = @productId And FileType = @fileType And IsNull(ExternalId, '') = '' ",
                CommandType.Text,
                new SqlParameter("@productId", productId), new SqlParameter("@fileType", fileType));
            return ModulesRepository.ModuleExecuteScalar<int>(
                "Select Id from Module.FilesInProduct where ProductId = @productId And ExternalId = @externalId ",
                CommandType.Text,
                new SqlParameter("@productId", productId), new SqlParameter("@fileType", fileType), new SqlParameter("@externalId", externalId));
        }

        public static void InsertUpdateProductFile(int fileId, int productId, FileImportModel model)
        {
            if (!SaveFileInProduct(model))
                return;

            var iconId = ModulesRepository.ModuleExecuteScalar<int>(
                "Select Top 1 IconId from Module.FilesInProductIcon where FileType = @fileType ",
                CommandType.Text,
                new SqlParameter("@fileType", model.FileType));
            if (iconId == 0)
                iconId = ModulesRepository.ModuleExecuteScalar<int>(
                    "Select Top 1 IconId from Module.FilesInProductIcon where DefaultIcon = 1 ",
                    CommandType.Text);

            var groupId = ModulesRepository.ModuleExecuteScalar<int>(
                "Select Top 1 Id from Module.FilesGroup where GroupName = @GroupName ",
                CommandType.Text,
                new SqlParameter("@GroupName", model.GroupName));
            if (groupId == 0)
                groupId = ModulesRepository.ModuleExecuteScalar<int>(
                    "Select Top 1 Id from Module.FilesGroup Order By SortOrder ",
                    CommandType.Text);

            if (fileId == 0)
            {
                if (model.Enabled)
                    ModulesRepository.ModuleExecuteNonQuery(
                    "Insert Into Module.FilesInProduct (ProductId, FileName, TextLink, IconId, GroupId, SortOrder, OpenNewTab, Enabled, FileType, ExternalId) " +
                    "Values (@ProductId, @FileName, @TextLink, @IconId, @GroupId, @SortOrder, 1, @Enabled, @FileType, @ExternalId) ",
                    CommandType.Text,
                    new SqlParameter("@ProductId", productId),
                    new SqlParameter("@FileName", model.FileName),
                    new SqlParameter("@TextLink", model.TextLink + (model.Name.IsNotEmpty() ? " " + model.Name : "")),
                    new SqlParameter("@IconId", iconId),
                    new SqlParameter("@GroupId", groupId),
                    new SqlParameter("@SortOrder", model.SortOrder),
                    new SqlParameter("@Enabled", model.Enabled),
                    new SqlParameter("@FileType", model.FileType),
                    new SqlParameter("@ExternalId", model.ExternalId)
                    );
            }
            else
                ModulesRepository.ModuleExecuteNonQuery(
                    "Update Module.FilesInProduct Set FileName = @FileName, TextLink = @TextLink, IconId = @IconId, GroupId = @GroupId, Enabled = @Enabled Where Id = @Id ",//, SortOrder = @SortOrder
                    CommandType.Text,
                    new SqlParameter("@Id", fileId),
                    new SqlParameter("@FileName", model.FileName),
                    new SqlParameter("@TextLink", model.TextLink + (model.Name.IsNotEmpty() ? " " + model.Name : "")),
                    new SqlParameter("@IconId", iconId),
                    new SqlParameter("@GroupId", groupId),
                    //new SqlParameter("@SortOrder", model.SortOrder),
                    new SqlParameter("@Enabled", model.Enabled)
                    );
        }

        public static void DeleteProductFile(int fileId)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Delete From Module.FilesInProduct Where Id = @fileId ",
                CommandType.Text,
                new SqlParameter("@fileId", fileId));
        }

        public static bool SaveFileInProduct(FileImportModel model)
        {
            if (!model.Enabled)
                return true;

            var path = System.Web.Hosting.HostingEnvironment.MapPath("~/userfiles/modules/FilesInProduct/Files/");
            if (!Directory.Exists(path))
                return false;

            try
            {
                var storage = Convert.FromBase64String(model.ValueStorage);
                File.WriteAllBytes(path + "\\" + model.FileName, storage);
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return false;
            }

            return true;
        }

        public static void UpdateCustomerId(Guid oldId, Guid newId, DateTime registrationDateTime)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Customers].[Customer] Set CustomerID = @newId, RegistrationDateTime = @registrationDateTime Where CustomerID = @oldId ",
                CommandType.Text,
                new SqlParameter("@oldId", oldId), new SqlParameter("@newId", newId), new SqlParameter("@registrationDateTime", registrationDateTime));
        }

        public static void PreCalcProductsParams(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                var productId = GetIdByExternalId(id, "Product");
                if (productId > 0)
                    ProductService.PreCalcProductParams(productId);
            }
        }

        public static ProductModel GetProduct(int productId)
        {
            return ModulesRepository.ModuleExecuteReadOne<ProductModel>(
                "Select * From Module.OneSApi_Product Where ProductId = @productId ",
                CommandType.Text,
                reader => new ProductModel
                {
                    ProductId = SQLDataHelper.GetInt(reader, "ProductId"),
                    ExternalId = SQLDataHelper.GetString(reader, "ExternalId"),
                    ExpectedDate = SQLDataHelper.GetNullableDateTime(reader, "ExpectedDate"),
                    PriceOnRequest = SQLDataHelper.GetNullableBoolean(reader, "PriceOnRequest") ?? false,
                    ////PriceNumber = (short?)SQLDataHelper.GetNullableInt(reader, "PriceNumber")
                },
                new SqlParameter("@productId", productId));
        }

        public static void UpdateProduct(ProductModel product)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                ////"Update Module.OneSApi_Product Set ExpectedDate = @ExpectedDate, PriceOnRequest = @PriceOnRequest, PriceNumber = @PriceNumber Where ProductId = @ProductId ",
                "Update Module.OneSApi_Product Set ExpectedDate = @ExpectedDate, PriceOnRequest = @PriceOnRequest Where ProductId = @ProductId ",
                CommandType.Text, new SqlParameter("@ProductId", product.ProductId),
                new SqlParameter("@ExpectedDate", product.ExpectedDate ?? (object)DBNull.Value),
                new SqlParameter("@PriceOnRequest", product.PriceOnRequest),
                new SqlParameter("@PriceNumber", product.PriceNumber ?? (object)DBNull.Value)
                );
        }

        public static void SetPriceOnRequest(Offer offer, ref bool changed)
        {
            if (offer.BasePrice == 0)
                return;
            ModulesRepository.ModuleExecuteNonQuery(
                "IF((SELECT COUNT(OfferId) FROM Module.OneSApi_Offer WHERE OfferId = @OfferId) > 0) " +
                "Update Module.OneSApi_Offer Set PriceOnRequest = @BasePrice Where OfferId = @OfferId " +
                "ELSE Insert Into Module.OneSApi_Offer (OfferId, PriceOnRequest) VALUES (@OfferId, @BasePrice)",
                CommandType.Text, new SqlParameter("@OfferId", offer.OfferId),
                new SqlParameter("@BasePrice", offer.BasePrice));
            offer.BasePrice = 0;
            OfferService.UpdateOffer(offer);
            changed = true;
        }

        public static void RestorePriceOnRequest(Offer offer, float? basePrice, ref bool changed)
        {
            if (offer.BasePrice != 0)
                return;
            var priceOnRequest = basePrice;
            if (!priceOnRequest.HasValue)
            {
                priceOnRequest = ModulesRepository.ModuleExecuteScalar<float>(
                    "Select PriceOnRequest From Module.OneSApi_Offer Where OfferId = @OfferId ",
                    CommandType.Text,
                    new SqlParameter("@OfferId", offer.OfferId));
            }
            if (priceOnRequest == 0)
                return;
            ModulesRepository.ModuleExecuteNonQuery(
                "Update Module.OneSApi_Offer Set PriceOnRequest = NULL Where OfferId = @OfferId ",
                CommandType.Text, new SqlParameter("@OfferId", offer.OfferId));
            offer.BasePrice = priceOnRequest.Value;
            OfferService.UpdateOffer(offer);
            changed = true;
        }

        public static string RemoveHtmlTags(string source)
        {
            return Regex.Replace(source, "<.*?>", String.Empty);

            const string HTML_TAG_PATTERN = @"(?'tag_start'</?)(?'tag'\w+)((\s+(?'attr'(?'attr_name'\w+)(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+)))?)+\s*|\s*)(?'tag_end'/?>)";
            Dictionary<string, List<string>> ValidHtmlTags = new Dictionary<string, List<string>>
            {
                //{ "p", new List<string>() },
                //{ "br", new List<string>() },
                //{ "strong", new List<string>() },
                //{ "ul", new List<string>() },
                //{ "li", new List<string>() },
                //{ "a", new List<string> { "href", "target" } }
            };
            Regex htmlTagExpression = new Regex(HTML_TAG_PATTERN, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var result = htmlTagExpression.Replace(HttpUtility.HtmlDecode(source), m =>
            {
                if (!ValidHtmlTags.ContainsKey(m.Groups["tag"].Value))
                    return String.Empty;

                try
                {
                    StringBuilder generatedTag = new StringBuilder(m.Length);

                    Group tagStart = m.Groups["tag_start"];
                    Group tagEnd = m.Groups["tag_end"];
                    Group tag = m.Groups["tag"];
                    Group tagAttributes = m.Groups["attr"];

                    generatedTag.Append(tagStart.Success ? tagStart.Value : "<");
                    generatedTag.Append(tag.Value);

                    foreach (Capture attr in tagAttributes.Captures)
                    {
                        int indexOfEquals = attr.Value.IndexOf('=');

                        if (indexOfEquals < 1)
                            continue;

                        string attrName = attr.Value.Substring(0, indexOfEquals);

                        if (ValidHtmlTags[tag.Value].Contains(attrName))
                        {
                            generatedTag.Append(' ');
                            generatedTag.Append(attr.Value);
                        }
                    }

                    generatedTag.Append(tagEnd.Success ? tagEnd.Value : ">");

                    return generatedTag.ToString();
                }
                catch (Exception E)
                {
                    return string.Empty;
                }
            });

            return result;
        }

        public static Manager GetManagerByCode(int code)
        {
            var id = ModulesRepository.ModuleExecuteScalar<int>(
                "Select ManagerId From Customers.Managers Where Code = @code ",
                CommandType.Text,
                new SqlParameter("@code", code));
            return ManagerService.GetManager(id);
        }

        public static int GetDepartmentIdBySort(int sort)
        {
            return ModulesRepository.ModuleExecuteScalar<int>(
                "Select DepartmentId From Customers.Departments Where Sort = @sort ",
                CommandType.Text,
                new SqlParameter("@sort", sort));
        }

        public static void SetManagerCode(int managerId, int code)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update Customers.Managers Set Code = @code Where ManagerId = @managerId ",
                CommandType.Text, new SqlParameter("@managerId", managerId), new SqlParameter("@code", code));
        }

        public static int SubParseAndCreateCategory(string strCategory, bool create, bool updateCache = false)
        {
            int categoryId = -1;
            //
            // strCategory "[Техника >> Игровые приставки >> PlayStation]-10;[....]" (10 - порядок сортировки товара в категории(* необязательно))
            //
            foreach (string strT in strCategory.Split(new[] { ';' }))
            {
                var st = strT;
                st = st.Replace("[", "");
                st = st.Replace("]", "");
                int parentId = 0;
                string[] temp = st.Split(new[] { ">>" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i <= temp.Length - 1; i++)
                {
                    string name = temp[i].SupperTrim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        var cat = CategoryService.GetChildCategoryIdByName(parentId, name);
                        if (cat.HasValue)
                        {
                            parentId = cat.Value;
                            categoryId = cat.Value;
                        }
                        else if (create)
                        {
                            parentId = CategoryService.AddCategory(
                                new Category
                                {
                                    Name = name,
                                    ParentCategoryId = parentId,
                                    SortOrder = 0,
                                    Enabled = true,
                                    DisplayChildProducts = false,
                                    UrlPath = UrlService.GetAvailableValidUrl(0, ParamType.Category, name.Reduce(140)),
                                    DisplayStyle = ECategoryDisplayStyle.Tile,
                                    Meta = MetaInfoService.GetDefaultMetaInfo(MetaType.Category, name),
                                    Sorting = ESortOrder.AscByName
                                },
                                updateCache, false, null);
                        }
                        else
                            break;
                    }
                    if (i == temp.Length - 1)
                    {
                        categoryId = parentId;
                    }
                }
            }
            return categoryId;
        }

        public static void UpdateTrackNumber(int orderId, string trackNumber)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Order].[Order] Set TrackNumber = @trackNumber Where OrderID = @orderId ",
                CommandType.Text,
                new SqlParameter("@orderId", orderId), new SqlParameter("@trackNumber", trackNumber ?? (object)DBNull.Value));
        }

        public static void SetCategoryHidden(int categoryId, bool hidden)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                 "Update [Catalog].[Category] Set Hidden = @hidden Where CategoryID = @categoryId",
                 CommandType.Text,
                 new SqlParameter("@categoryId", categoryId),
                 new SqlParameter("@hidden", hidden));
        }

        public static List<int> GetRelatedProductIds(int productId, RelatedType relatedType)
        {
            return ModulesRepository.ModuleExecuteReadColumn<int>(
                "Select LinkedProductID " +
                "From [Catalog].[RelatedProducts] " +
                "Where [ProductID] = @productId and RelatedType = @relatedType " +
                "Order by SortOrder",
                CommandType.Text, "LinkedProductID",
                new SqlParameter("@productId", productId), new SqlParameter("@relatedType", (int)relatedType));
        }

        public static int GetRelatedProductSort(int productId, int linkedId, RelatedType relatedType)
        {
            return ModulesRepository.ModuleExecuteScalar<int>(
                "Select SortOrder From [Catalog].[RelatedProducts] Where [ProductID] = @productId and RelatedType = @relatedType and LinkedProductID = @linkedId ",
                CommandType.Text,
                new SqlParameter("@productId", productId), new SqlParameter("@relatedType", (int)relatedType), new SqlParameter("@linkedId", linkedId));
        }

        public static void SetRelatedProductSort(int productId, int linkedId, RelatedType relatedType, int sortOrder)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                 "Update [Catalog].[RelatedProducts] Set SortOrder = @sortOrder " +
                 "Where [ProductID] = @productId and RelatedType = @relatedType and LinkedProductID = @linkedId",
                 CommandType.Text,
                new SqlParameter("@productId", productId), new SqlParameter("@relatedType", (int)relatedType), new SqlParameter("@linkedId", linkedId), new SqlParameter("@sortOrder", sortOrder));
        }

        public static bool SaveOrderBill(int orderId, string bill)
        {
            var path = System.Web.Hosting.HostingEnvironment.MapPath("~/content/bill/");

            try
            {
                var storage = Convert.FromBase64String(bill);
                File.WriteAllBytes(path + "\\" + orderId + ".pdf", storage);
                //using (var wr = new StreamWriter(path + "\\" + orderId + ".pdf", false))
                //{
                //    wr.Write(storage);
                //}
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                return false;
            }

            return true;
        }

        public static void UpdateOrderShipping(int orderId, int? shippingId, decimal shippingCost, decimal productsCost, ShippingMethod shippingMethod, string shippingMethodName = null)
        {
            //var oldcost = ModulesRepository.ModuleExecuteScalar<float>(
            //    "Select ShippingCost From [Order].[Order] Where OrderID = @orderId ",
            //    CommandType.Text,
            //    new SqlParameter("@orderId", orderId));
            if (shippingMethod == null)
                shippingMethod = ShippingMethodService.GetShippingMethod(shippingId ?? 0) ?? new ShippingMethod();
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Order].[Order] Set ShippingMethodId = @shippingId, ShippingCost = @shippingCost, [Sum] = @productsCost+@shippingCost, [ShippingMethodName] = @shippingMethodName " +
                "Where OrderID = @orderId ",
                CommandType.Text,
                new SqlParameter("@orderId", orderId),
                new SqlParameter("@shippingId", shippingId ?? (object)DBNull.Value),
                new SqlParameter("@shippingMethodName", shippingMethodName ?? (shippingMethod.Name ?? (object)DBNull.Value)),
                new SqlParameter("@shippingCost", shippingCost),
                new SqlParameter("@productsCost", productsCost));
        }

        public static void UpdateOrderShippingName(int orderId, string shippingMethodName = null)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Order].[Order] Set [ShippingMethodName] = @shippingMethodName " +
                "Where OrderID = @orderId ",
                CommandType.Text,
                new SqlParameter("@orderId", orderId),
                new SqlParameter("@shippingMethodName", shippingMethodName ?? (object)DBNull.Value));
        }

        public static void UpdateOrderSum(int orderId, decimal sum)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Order].[Order] Set [Sum] = @sum Where OrderID = @orderId ",
                CommandType.Text,
                new SqlParameter("@orderId", orderId), new SqlParameter("@sum", sum));
        }

        public static void ShowNotEmptyCategories()
        {
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("UPDATE Catalog.Category SET [Hidden] = 0 WHERE [Hidden] = 1 AND {0} > 0 AND CategoryID <> 0", SettingsCatalog.ShowOnlyAvalible ? "Available_Products_Count" : "Products_Count"),
                CommandType.Text);
        }

        public static void HideEmptyCategories()
        {
            ModulesRepository.ModuleExecuteNonQuery(
                string.Format("UPDATE Catalog.Category SET [Hidden] = 1 WHERE [Hidden] = 0 AND {0} <= 0 AND CategoryID <> 0", SettingsCatalog.ShowOnlyAvalible ? "Available_Products_Count" : "Products_Count"),
                CommandType.Text);
        }

        public static int GetPaymentIdByTypeAndShipping(string paymentType, int shippingId)
        {
            return ModulesRepository.ModuleExecuteScalar<int>(
                "SELECT TOP 1 [PaymentMethodID] FROM [Order].[PaymentMethod] Where PaymentType = @paymentType " +
                "And [PaymentMethodID] Not In (SELECT [PaymentMethodID] FROM [Order].[ShippingPayments] Where ShippingMethodID = @shippingId)",
                CommandType.Text,
                new SqlParameter("@paymentType", paymentType), new SqlParameter("@shippingId", shippingId));
        }

        public static void UpdateOrderPayment(int orderId, Payment.PaymentMethod payment)
        {
            ModulesRepository.ModuleExecuteNonQuery(
                "Update [Order].[Order] Set PaymentMethodId = @shippingId, [PaymentMethodName] = @paymentMethodName " +
                "Where OrderID = @orderId ",
                CommandType.Text,
                new SqlParameter("@orderId", orderId),
                new SqlParameter("@shippingId", payment != null ? payment.PaymentMethodId : (object)DBNull.Value),
                new SqlParameter("@paymentMethodName", payment != null ? payment.Name : (object)DBNull.Value));
        }

    }
}
