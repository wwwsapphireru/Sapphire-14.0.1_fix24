using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Repository.Currencies;
using AdvantShop.Web.Infrastructure.Handlers;
using AdvantShop.FilePath;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using AdvantShop.FullSearch;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Mails;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Product
{
    public class ImportProducts : ICommandHandler<CatalogImportModel, CatalogImportResultModel>
    {
        private const string _columnSeparator = "&&";
        private const string _propertySeparator = "~";
        private const string _modifiedBy = OneSApi.ModuleName;
        private ImportExportSettingsModel _settings;
        private bool _withStatisctic;
        private string _currentProcess;
        private string _currentProcessName;
        private List<short> _priceNumbers;

        public ImportProducts(bool withStatisctic, string currentProcess = null, string currentProcessName = null)
        {
            if (withStatisctic && ModuleStatistic.IsRun)
            {
                throw new Exception("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }
            _withStatisctic = withStatisctic;
            _currentProcess = currentProcess;
            _currentProcessName = currentProcessName;
            _priceNumbers = new List<short> { 2, 3, 4, 100 };
        }

        public CatalogImportResultModel Execute(CatalogImportModel catalog)
        {
            if (_withStatisctic && _currentProcess.IsNotEmpty())
            {
                ModuleStatistic.Init();
                ModuleStatistic.IsRun = true;
                ModuleStatistic.CurrentProcess = _currentProcess;
                ModuleStatistic.CurrentProcessName = _currentProcessName;
                if (catalog.Categories != null)
                    ModuleStatistic.TotalRow += catalog.Categories.Count;
                if (catalog.Properties != null)
                    ModuleStatistic.TotalRow += catalog.Properties.Count;
                if (catalog.Products != null)
                    ModuleStatistic.TotalRow += catalog.Products.Count;
                if (catalog.Offers != null)
                    ModuleStatistic.TotalRow += catalog.Offers.Count;
                if (catalog.Products != null)//related
                    ModuleStatistic.TotalRow += catalog.Products.Count;
                if (catalog.Photos != null)
                    ModuleStatistic.TotalRow += catalog.Photos.Count;
                if (catalog.Files != null)
                    ModuleStatistic.TotalRow += catalog.Files.Count;
            }

            var start = DateTime.Now;
            _settings = ModuleService.GetImportExportSettings();

            var result = new CatalogImportResultModel();
            if (catalog.Categories != null)
                result.Categories = new List<ImportResultModel> { };
            if (catalog.Properties != null)
                result.Properties = new List<ImportResultModel> { };
            if (catalog.Products != null)
                result.Products = new List<ImportResultModel> { };
            if (catalog.Offers != null)
                result.Offers = new List<ImportResultModel> { };
            if (catalog.Products != null)
                result.RelatedProducts = new List<ImportResultModel> { };
            if (catalog.Photos != null)
                result.Photos = new List<ImportResultModel> { };
            if (catalog.Files != null)
                result.Files = new List<ImportResultModel> { };

            var catsforrecalc = new List<int>();

            ExecuteCategories(catalog.Categories, ref result, ref catsforrecalc);
            ExecuteProperties(catalog.Properties, ref result);
            ExecuteProducts(catalog.Products, ref result, ref catsforrecalc);
            ExecuteOffers(catalog.Offers, ref result);
            ExecuteRelatedProducts(catalog.Products, ref result);
            ExecutePhotos(catalog.Photos, ref result);
            ExecuteFiles(catalog.Files, ref result);

            var forPreCalc = new List<string>();
            if (catalog.Categories != null)
                forPreCalc.AddRange(catalog.Categories.Select(x => x.ExternalId));
            //if (catalog.Properties != null)
            //    forPreCalc.AddRange(catalog.Properties.Select(x => x.ExternalId));
            if (catalog.Products != null)
                forPreCalc.AddRange(catalog.Products.Select(x => x.ExternalId));
            if (catalog.Offers != null)
                forPreCalc.AddRange(catalog.Offers.Select(x => x.ExternalId));
            if (catalog.Photos != null)
                forPreCalc.AddRange(catalog.Photos.Select(x => x.ExternalId));
            if (catalog.Files != null)
                forPreCalc.AddRange(catalog.Files.Select(x => x.ExternalId));
            ImportService.PreCalcProductsParams(forPreCalc.Distinct());

            if (catalog.Categories != null || catalog.Products != null || catalog.Offers != null)
            {
                try
                {
                    CategoryService.RecalculateProductsCountManual();
                    ImportService.ShowNotEmptyCategories();
                    ImportService.HideEmptyCategories();
                }
                catch (Exception ex)
                {
                    Debug.Log.Error(ex);
                }
                ModuleService.TryAction(() => LuceneSearch.CreateAllIndex());
            }
            //var root = CategoryService.GetChildCategoriesByCategoryId(0, false);
            //foreach (var cat in root)
            //{
            //    var count = SettingsCatalog.ShowOnlyAvalible ? cat.Available_Products_Count : cat.ProductsCount;
            //    if (count <= 0 && !cat.Hidden)
            //    {
            //        cat.Hidden = true;
            //        ImportService.SetCategoryHidden(cat.CategoryId, true);
            //    }
            //    else if (count > 0 && cat.Hidden)
            //    {
            //        cat.Hidden = false;
            //        ImportService.SetCategoryHidden(cat.CategoryId, true);
            //    }
            //}
            foreach (var catid in catsforrecalc.Distinct())
                CategoryService.SetCategoryHierarchicallyEnabled(catid);

            ModuleService.WriteLog("ImportProducts", JsonConvert.SerializeObject(catalog, Formatting.Indented), JsonConvert.SerializeObject(result, Formatting.Indented), start);

            if (_withStatisctic)
                ModuleStatistic.IsRun = false;

            return result;
        }

        private void ExecuteCategories(List<CategoryImportModel> categories, ref CatalogImportResultModel result, ref List<int> catsforrecalc)
        {
            if (categories == null)
                return;

            foreach (var item in categories)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true };

                var category = CategoryService.GetCategoryFromDbByExternalId(item.ExternalId);
                string strCategory = null;
                if (category == null)
                {
                    var categoryId = ImportService.GetCategoryFromImport(item.Category, false, out strCategory, _settings);
                    if (categoryId != 0)
                    {
                        category = CategoryService.GetCategory(categoryId);
                        catsforrecalc.Add(categoryId);
                    }
                }

                var adding = (category == null);
                if (adding)
                {
                    if (!item.Enabled)
                    {
                        resultItem.Error = "Категория с признаком Enabled=false не добавляется";
                        result.Categories.Add(resultItem);
                        continue;
                    }
                    else if (strCategory.IsNullOrEmpty())
                    {
                        resultItem.Error = "Категория без наименования не добавляется";
                        result.Categories.Add(resultItem);
                        continue;
                    }
                }

                var trackChanges = false;
                var isDeleted = false;

                if (adding)
                {
                    var categoryId = ImportService.SubParseAndCreateCategory(strCategory, item.Enabled, true);
                    catsforrecalc.Add(categoryId);
                    category = CategoryService.GetCategory(categoryId);
                    trackChanges = true;
                }

                if (adding || category.Name != item.Name)
                {
                    category.Name = item.Name;
                    trackChanges = true;
                }
                //var itemBriefDescription = ImportService.RemoveHtmlTags(item.BriefDescription);
                //if (adding || (category.BriefDescription != itemBriefDescription))
                //{
                //    category.BriefDescription = itemBriefDescription;
                //    trackChanges = true;
                //}
                //var itemDescription = ImportService.RemoveHtmlTags(item.Description);
                //if (adding || (category.Description != itemDescription))
                //{
                //    category.Description = itemDescription;
                //    trackChanges = true;
                //}
                if (adding || category.Enabled != item.Enabled)
                {
                    category.Enabled = item.Enabled;
                    trackChanges = true;
                    if (!adding && !item.Enabled)
                        isDeleted = true;
                }
                if (adding || category.ParentCategory.ExternalId != item.ParentId)
                {
                    var parent = CategoryService.GetCategoryFromDbByExternalId(item.ParentId);
                    category.ParentCategoryId = parent != null ? parent.CategoryId : 0;
                    trackChanges = true;
                }
                if (item.MiniPicture != null && (adding || category.MiniPicture.OriginName != item.MiniPicture.OriginName))
                {
                    ExecutePhotoCategory(category.CategoryId, item.MiniPicture, PhotoType.CategorySmall/*, ref result*/);
                    trackChanges = true;
                }

                var changedBy = new Core.Services.ChangeHistories.ChangedBy(_modifiedBy);

                try
                {
                    if (trackChanges)
                    {
                        CategoryService.UpdateCategory(category, true, trackChanges, trackChanges ? changedBy : null);
                        if (isDeleted)
                            MailService.SendMailNow(Guid.Empty, SettingsMail.EmailForProductDiscuss, "Удаление категории",
                                $"Удалена категория \"{category.Name}\"<br /><a href='https://www.sapphire.ru/adminv3/category/edit/{category.CategoryId}'>https://www.sapphire.ru/adminv3/category/edit/{category.CategoryId}</a>",
                                true);
                    }
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                    resultItem.Success = false;
                    resultItem.Error = E.Message;
                }

                result.Categories.Add(resultItem);
            }

        }

        private void ExecuteProperties(List<PropertyImportModel> properties, ref CatalogImportResultModel result)
        {
            if (properties == null)
                return;

            foreach (var item in properties)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = string.Format("{0} ({1})", item.Name, item.GroupName), Success = true };

                var group = PropertyGroupService.Get(item.Group);
                if (group == null)
                {
                    group = new PropertyGroup { Name = item.Group, NameDisplayed = item.GroupName };
                    group.PropertyGroupId = PropertyGroupService.Add(group);
                }
                if (group.NameDisplayed != item.GroupName)
                {
                    group.NameDisplayed = item.GroupName;
                    PropertyGroupService.Update(group);
                }

                var property = ImportService.GetPropertyByNameAndGroup(item.Name, group.PropertyGroupId);
                var isNew = property == null;

                if (isNew)
                {
                    if (!_settings.ImportProduct.CreateProperties)
                    {
                        resultItem.Error = "Свойство не добавляется в соответствии с настройкой модуля CreateProperties=false";
                        result.Properties.Add(resultItem);
                        continue;
                    }
                    else if (item.IsDeleted)
                    {
                        resultItem.Error = "Свойство с признаком IsDeleted=true не добавляется";
                        result.Properties.Add(resultItem);
                        continue;
                    }
                    property = new Property
                    {
                        Type = (int)(item.IsRange ? PropertyType.Range : PropertyType.Checkbox),
                        Name = item.Name,
                        GroupId = group.PropertyGroupId,
                        SortOrder = item.SortOrder,
                        UseInDetails = true,
                        UseInFilter = item.UseInFilter,
                        UseInBrief = item.UseInBrief
                    };
                    try
                    {
                        PropertyService.AddProperty(property);
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                        resultItem.Success = false;
                        resultItem.Error = E.Message;
                    }
                }
                else
                {
                    if (!_settings.ImportProduct.UpdateProperties)
                    {
                        resultItem.Error = "Свойство не обновляется в соответствии с настройкой модуля UpdateProperties=false";
                        result.Properties.Add(resultItem);
                        continue;
                    }
                    if (item.IsDeleted == true)
                    {
                        //PropertyService.GetValuesByPropertyId(property.PropertyId);
                        try
                        {
                            PropertyService.DeleteProperty(property.PropertyId);
                        }
                        catch (Exception E)
                        {
                            Debug.Log.Error(E);
                            resultItem.Success = false;
                            resultItem.Error = E.Message;
                        }
                    }
                    else if (property.SortOrder != item.SortOrder || property.UseInFilter != item.UseInFilter || property.UseInBrief != item.UseInBrief
                        || property.Name != item.Name || property.GroupId != group.PropertyGroupId)
                    {
                        property.SortOrder = item.SortOrder;
                        property.UseInFilter = item.UseInFilter;
                        property.UseInBrief = item.UseInBrief;
                        property.Name = item.Name;
                        property.GroupId = group.PropertyGroupId;
                        try
                        {
                            PropertyService.UpdateProperty(property);
                        }
                        catch (Exception E)
                        {
                            Debug.Log.Error(E);
                            resultItem.Success = false;
                            resultItem.Error = E.Message;
                        }
                    }
                }

                //var trackChanges = false;

                //if (adding)
                //{
                //    trackChanges = true;
                //}

                //var changedBy = new Core.Services.ChangeHistories.ChangedBy(_modifiedBy);

                //try
                //{
                //    if (trackChanges)
                //    {
                //        CategoryService.UpdateCategory(category, true, trackChanges, trackChanges ? changedBy : null);
                //    }
                //}
                //catch (Exception E)
                //{
                //    Debug.Log.Error(E);
                //    resultItem.Success = false;
                //    resultItem.Error = E.Message;
                //}

                result.Properties.Add(resultItem);
            }
        }

        private void ExecuteProducts(List<ProductImportModel> products, ref CatalogImportResultModel result, ref List<int> catsforrecalc)
        {
            if (products == null)
                return;

            foreach (var item in products)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true };

                var productId = ImportService.GetIdByExternalId(item.ExternalId, "Product");
                var product = ProductService.GetProduct(productId);
                if (product == null && productId > 0)
                {
                    productId = 0;
                    ImportService.DeleteExternalId(item.ExternalId, "Product");
                }

                string artNo = "";
                if (_settings.ImportProduct.ImportArtnoType == ImportArtnoType.ArtNo)
                    artNo = item.ArtNo;
                else if (_settings.ImportProduct.ImportArtnoType == ImportArtnoType.Code)
                    artNo = item.Code;

                string name;
                if (_settings.ImportProduct.ImportNameType == ImportNameType.FullName)
                    name = item.FullName;
                else
                    name = item.Name;

                if (product == null && artNo.IsNotEmpty())
                    product = ProductService.GetProduct(artNo);

                ProductModel product1c = null;
                var adding = false;
                if (product == null)
                {
                    product = new Catalog.Product()
                    {
                        CurrencyID = CurrencyService.CurrentCurrency.CurrencyId
                    };
                    adding = true;
                }
                else
                {
                    product1c = ImportService.GetProduct(productId);
                }
                if (product1c == null)
                    product1c = new ProductModel();


                if (adding)
                {
                    if (!_settings.ImportProduct.CreateProducts)
                    {
                        resultItem.Error = "Товар не добавляется в соответствии с настройкой модуля CreateProducts=false";
                        result.Products.Add(resultItem);
                        continue;
                    }
                    //else if (!item.Enabled)
                    //{
                    //    resultItem.Error = "Товар с признаком Enabled=false не добавляется";
                    //    result.Products.Add(resultItem);
                    //    continue;
                    //}
                    else if (artNo.IsNullOrEmpty())
                    {
                        resultItem.Error = "Товар с незаполненым артикулом не добавляется";
                        result.Products.Add(resultItem);
                        continue;
                    }
                    else if (!item.CategoryEnabled)
                    {
                        resultItem.Error = "Товар с признаком CategoryEnabled=false не добавляется";
                        result.Products.Add(resultItem);
                        continue;
                    }
                }

                var trackChanges = false;

                if (adding || product.ArtNo.IsNullOrEmpty() || (_settings.ImportProduct.UpdateArtNo && product.ArtNo != artNo))
                {
                    product.ArtNo = artNo;
                    trackChanges = true;
                }
                if (adding || product.Name.IsNullOrEmpty() || (_settings.ImportProduct.UpdateName && product.Name != name))
                {
                    product.Name = name;
                    trackChanges = true;
                }
                if (product.UrlPath.IsNullOrEmpty())
                {
                    product.UrlPath = UrlService.GetAvailableValidUrl(0, ParamType.Product, product.Name);
                    trackChanges = true;
                }
                var itemBriefDescription = ImportService.RemoveHtmlTags(item.BriefDescription);
                if (adding || (_settings.ImportProduct.UpdateBriefDescription && product.BriefDescription != itemBriefDescription))
                {
                    product.BriefDescription = itemBriefDescription;
                    trackChanges = true;
                }
                var itemDescription = ImportService.RemoveHtmlTags(item.Description);
                if (adding || (_settings.ImportProduct.UpdateDescription && product.Description != itemDescription))
                {
                    product.Description = itemDescription;
                    trackChanges = true;
                }
                if (adding || (_settings.ImportProduct.UpdateUnit && ((product.Unit == null || product.Unit.DisplayName != item.Unit) || (product.Offers.Any(x => x.Weight != item.Weight) && item.Weight != 0)) || product.Multiplicity != item.Multiplicity))
                {
                    product.UnitId = Core.Services.Catalog.UnitService.UnitFromString(item.Unit);
                    if (item.Weight != 0)
                        product.Offers.Where(x => x.Weight != item.Weight).ForEach(x => x.Weight = item.Weight);
                    product.Multiplicity = item.Multiplicity;
                    trackChanges = true;
                }
                if ((adding || _settings.ImportProduct.UpdateBrand) && item.Brand.IsNotEmpty())
                {
                    var brandId = ImportService.FindBrand(item.Brand, true);
                    if (product.BrandId != brandId)
                    {
                        product.BrandId = brandId;
                        trackChanges = true;
                    }
                }
                if ((adding || _settings.ImportProduct.UpdateTax) && item.Tax.IsNotEmpty())
                {
                    var taxId = ImportService.FindTax(item.Tax);
                    if (product.TaxId != taxId)
                    {
                        product.TaxId = taxId;
                        trackChanges = true;
                    }
                }
                if ((adding || _settings.ImportProduct.UpdateDiscount) && item.Discount != null)
                {
                    if (product.Discount == null)
                        product.Discount = new Discount();
                    if (product.Discount.Percent != item.Discount.Percent || product.Discount.Amount != item.Discount.Amount || product.Discount.Type != item.Discount.Type)
                    {
                        product.Discount = new Discount(item.Discount.Percent, item.Discount.Amount, (DiscountType)item.Discount.Type);
                        trackChanges = true;
                    }
                }
                if (adding || (product.AllowPreOrder != item.AllowPreOrder))
                {
                    product.AllowPreOrder = item.AllowPreOrder;
                    trackChanges = true;
                }
                if (adding || (product.Recomended != item.Recomended || product.BestSeller != item.Bestseller))
                {
                    product.Recomended = item.Recomended;
                    product.BestSeller = item.Bestseller;
                    trackChanges = true;
                }
                if (adding || (_settings.ImportProduct.UpdateEnabled && product.Enabled != item.Enabled))
                {
                    product.Enabled = item.Enabled;
                    trackChanges = true;
                }
                if (adding || (product.DoNotApplyOtherDiscounts != item.DoNotApplyOtherDiscounts))
                {
                    product.DoNotApplyOtherDiscounts = item.DoNotApplyOtherDiscounts;
                    trackChanges = true;
                }
                if (adding || (product.ExportOptions.Adult != item.Adult))
                {
                    product.ExportOptions.Adult = item.Adult;
                    trackChanges = true;
                }
                if (product.Enabled && !_priceNumbers.Contains(item.PriceNumber)/*!product.AllowPreOrder*/ && product.Offers.Sum(x => x.Amount) <= 0)
                {
                    product.Enabled = false;
                    trackChanges = true;
                }

                if (item.PriceOnRequest != product1c.PriceOnRequest || item.PriceOnRequest != product.IsMarkingRequired || product1c.PriceNumber != item.PriceNumber)
                {
                    if (item.PriceOnRequest != product.IsMarkingRequired)
                    {
                        product.IsMarkingRequired = item.PriceOnRequest;
                        trackChanges = true;
                    }
                    product1c.PriceOnRequest = item.PriceOnRequest;
                    product1c.PriceNumber = item.PriceNumber;
                    ImportService.UpdateProduct(product1c);
                    if (item.PriceOnRequest)
                        product.Offers.Where(x => x.Amount > 0).ForEach(x => ImportService.SetPriceOnRequest(x, ref trackChanges));
                    else
                        product.Offers.Where(x => x.Amount > 0).ForEach(x => ImportService.RestorePriceOnRequest(x, null, ref trackChanges));
                }
                if (item.PriceOnRequest && product.Offers.Any(x => x.BasePrice > 0))
                    product.Offers.Where(x => x.Amount > 0).ForEach(x => ImportService.SetPriceOnRequest(x, ref trackChanges));

                if (_settings.ImportProduct.UpdatePhotos && item.MainPhoto.IsNotEmpty())
                {
                    var photo = PhotoService.GetProductPhoto(productId, item.MainPhoto);
                    if (photo != null && !photo.Main)
                    {
                        PhotoService.SetProductMainPhoto(photo.PhotoId);
                    }
                }

                product.ModifiedBy = _modifiedBy;
                var changedBy = new Core.Services.ChangeHistories.ChangedBy(_modifiedBy);

                try
                {
                    if (adding)
                    {
                        productId = ProductService.AddProduct(product, true, changedBy: changedBy);
                        if (productId > 0)
                            ImportService.InsertExternalId(productId, item.ExternalId, "Product");
                    }
                    else if (trackChanges || productId == 0)
                    {
                        ProductService.UpdateProduct(product, true, trackChanges, trackChanges ? changedBy : null);
                        if (productId == 0)
                        {
                            productId = product.ProductId;
                            ImportService.InsertExternalId(productId, item.ExternalId, "Product");
                        }
                    }
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                    resultItem.Success = false;
                    resultItem.Error = E.Message;
                }

                //Debug.Log.Info(JsonConvert.SerializeObject(new { resultItem, productId }));
                if (resultItem.Success && productId > 0)
                {
                    //Category
                    var catIds = ProductService.GetCategoriesIDsByProductId(productId, false).ToList();
                    if ((adding || _settings.ImportProduct.AddCategory || catIds.Count == 0) /*&& product.Enabled*/ && item.CategoryEnabled)
                    {
                        string strCategory = null;
                        int categoryId = 0;
                        var cat = CategoryService.GetCategoryFromDbByExternalId(item.CategoryId);
                        if (cat != null)
                            categoryId = cat.CategoryId;
                        if (categoryId == 0)
                            categoryId = ImportService.GetCategoryFromImport(item.Category, true, out strCategory, _settings);
                        if (categoryId == 0 && strCategory.IsNotEmpty())
                            categoryId = ImportService.SubParseAndCreateCategory(strCategory, /*product.Enabled &&*/ item.CategoryEnabled, true);
                        if (categoryId > 0)
                        {
                            if (catIds.Count != 1 || (catIds.Count == 1 && catIds[0] != categoryId))
                            {
                                if (!adding && _settings.ImportProduct.UpdateCategory)
                                    ProductService.DeleteAllProductLink(productId);
                                //Debug.Log.Info(JsonConvert.SerializeObject(new { productId, categoryId }));
                                ProductService.AddProductLink(productId, categoryId, 0, true, adding || _settings.ImportProduct.UpdateCategory, true, false, false);
                                catsforrecalc.Add(categoryId);
                            }
                        }
                    }

                    //Properties
                    if ((adding || _settings.ImportProduct.UpdateProperties) /*&& product.Enabled*/ && item.CategoryEnabled)
                    {
                        //var properties = "";
                        //foreach (var prop in item.Properties.Properties)
                        //{
                        //    properties += prop.Name + _propertySeparator + (prop.RangeValue.HasValue ? prop.RangeValue.ToString() : prop.Value) + _columnSeparator;
                        //}
                        ImportService.ProcessProductProperties(productId, item.Properties, _settings.ImportProduct.CreateProperties);
                    }
                    if ((_settings.ImportProduct.PropertySaleId ?? 0) != 0)
                    {
                        var propValues = PropertyService.GetProductPropertyValues(productId, _settings.ImportProduct.PropertySaleId.Value);
                        var isSale = product.Discount.Type == DiscountType.Amount && product.Discount.Amount > 0;
                        if (isSale && !propValues.Any())
                        {
                            var val = PropertyService.GetValuesByPropertyId(_settings.ImportProduct.PropertySaleId.Value).FirstOrDefault();
                            if (val != null)
                                PropertyService.AddProductProperyValue(val.PropertyValueId, productId);
                        }
                        else if (!isSale && propValues.Any())
                        {
                            PropertyService.DeleteProductPropertyValues(productId, _settings.ImportProduct.PropertySaleId.Value);
                        }
                    }

                    result.Products.Add(resultItem);
                }
            }
        }

        private void ExecuteOffers(List<ProductOffersImportModel> offers, ref CatalogImportResultModel result)
        {
            if (offers == null)
                return;

            var warehouse = WarehouseService.GetListIds().First();

            var trackChanges = false;
            var changedBy = new Core.Services.ChangeHistories.ChangedBy(_modifiedBy);

            foreach (var item in offers)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true };

                var productId = ImportService.GetIdByExternalId(item.ExternalId, "Product");
                var product = ProductService.GetProduct(productId);
                if (product == null)
                {
                    //resultItem.Success = false;
                    resultItem.Error = "ExternalId not found";
                    result.Offers.Add(resultItem);
                    continue;
                }

                var product1c = ImportService.GetProduct(productId);
                if (product1c.ExpectedDate != item.ExpectedDate || product1c.PriceNumber != item.PriceNumber)
                {
                    product1c.ExpectedDate = item.ExpectedDate;
                    product1c.PriceNumber = item.PriceNumber;
                    ImportService.UpdateProduct(product1c);
                }

                product.HasMultiOffer = true;

                var oldOffers = new List<Offer>(product.Offers);
                product.Offers.Clear();

                var mainOffer = true;

                foreach (var fields in item.Offers)
                {
                    var artNo = product.ArtNo/* + fields.ArtNo*/;
                    if (fields.Color.IsNotEmpty())
                        artNo += "_" + fields.Color;
                    if (fields.Size.IsNotEmpty())
                        artNo += "_" + fields.Size;

                    var multiOffer = oldOffers.FirstOrDefault(offer => offer.ArtNo == artNo) ?? new Offer();
                    multiOffer.ProductId = product.ProductId;

                    if (multiOffer.Main != mainOffer)
                    {
                        multiOffer.Main = mainOffer;
                        trackChanges = true;
                    }

                    if (multiOffer.ArtNo != artNo)
                    {
                        multiOffer.ArtNo = artNo;
                        trackChanges = true;
                    }

                    int? sizeId = null;
                    if (fields.Size != null)
                    {
                        Size size = SizeService.GetSize(fields.Size);
                        if (size == null)
                        {
                            size = new Size { SizeName = fields.Size };
                            size.SizeId = SizeService.AddSize(size);
                        }

                        sizeId = size.SizeId;
                    }
                    if (multiOffer.SizeID != sizeId)
                    {
                        multiOffer.SizeID = sizeId;
                        trackChanges = true;
                    }

                    int? colorId = null;
                    if (fields.Color != null)
                    {
                        Color color = ColorService.GetColor(fields.Color);
                        if (color == null)
                        {
                            color = new Color { ColorName = fields.Color, ColorCode = "#000000" };
                            color.ColorId = ColorService.AddColor(color);
                        }

                        colorId = color.ColorId;
                    }
                    if (multiOffer.ColorID != colorId)
                    {
                        multiOffer.ColorID = colorId;
                        trackChanges = true;
                    }

                    if (multiOffer.BasePrice != fields.BasePrice)
                    {
                        multiOffer.BasePrice = fields.BasePrice;
                        trackChanges = true;
                    }
                    if (multiOffer.SupplyPrice != fields.SupplyPrice)
                    {
                        multiOffer.SupplyPrice = fields.SupplyPrice;
                        trackChanges = true;
                    }
                    if (multiOffer.Weight != fields.Weight && fields.Weight != 0)
                    {
                        multiOffer.Weight = fields.Weight;
                        trackChanges = true;
                    }
                    if (multiOffer.Length != fields.Length && fields.Length != 0)
                    {
                        multiOffer.Length = fields.Length;
                        trackChanges = true;
                    }
                    if (multiOffer.Width != fields.Width && fields.Width != 0)
                    {
                        multiOffer.Width = fields.Width;
                        trackChanges = true;
                    }
                    if (multiOffer.Height != fields.Height && fields.Height != 0)
                    {
                        multiOffer.Height = fields.Height;
                        trackChanges = true;
                    }

                    if (fields.Amount <= 0)
                    {
                        multiOffer.BasePrice = 0;
                        trackChanges = true;
                    }
                    else if (product1c.PriceOnRequest != item.PriceOnRequest || item.PriceOnRequest != product.IsMarkingRequired || product1c.PriceNumber != item.PriceNumber)
                    {
                        if (item.PriceOnRequest != product.IsMarkingRequired)
                        {
                            product.IsMarkingRequired = item.PriceOnRequest;
                            trackChanges = true;
                        }
                        product1c.PriceOnRequest = item.PriceOnRequest;
                        product1c.PriceNumber = item.PriceNumber;
                        ImportService.UpdateProduct(product1c);
                        if (item.PriceOnRequest)
                            ImportService.SetPriceOnRequest(multiOffer, ref trackChanges);
                        else
                            ImportService.RestorePriceOnRequest(multiOffer, fields.BasePrice, ref trackChanges);
                    }
                    if (item.PriceOnRequest && multiOffer.BasePrice > 0)
                        ImportService.SetPriceOnRequest(multiOffer, ref trackChanges);

                    if (multiOffer.OfferId == 0)
                    {
                        try
                        {
                            multiOffer.OfferId = OfferService.AddOffer(multiOffer, true, changedBy);
                        }
                        catch (Exception E)
                        {
                            Debug.Log.Error(E);
                            //continue;
                        }
                    }
                    else if (trackChanges)
                    {
                        OfferService.UpdateOffer(multiOffer, trackChanges, changedBy);
                    }

                    if (multiOffer.OfferId != 0)
                    {
                        if (multiOffer.Amount != fields.Amount)
                        {
                            //multiOffer.Amount = fields.Amount;
                            WarehouseStocksService.AddUpdateStocks(
                            new WarehouseStock
                            {
                                OfferId = multiOffer.OfferId,
                                Quantity = fields.Amount,
                                WarehouseId = warehouse
                            });
                            trackChanges = true;
                        }
                    }

                    product.Offers.Add(multiOffer);
                    mainOffer = false;
                }

                if (product.Offers.Count != oldOffers.Count)
                    trackChanges = true;

                if (product.Enabled && !_priceNumbers.Contains(item.PriceNumber)/*!product.AllowPreOrder*/ && product.Offers.Sum(x => x.Amount) <= 0)
                {
                    product.Enabled = false;
                    trackChanges = true;
                }
                else if (!product.Enabled && product.Offers.Sum(x => x.Amount) > 0)
                {
                    product.Enabled = true;
                    trackChanges = true;
                }

                if (trackChanges)
                {
                    product.ModifiedBy = _modifiedBy;

                    try
                    {
                        ProductService.UpdateProduct(product, false, trackChanges, trackChanges ? changedBy : null);
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                        resultItem.Success = false;
                        resultItem.Error = E.Message;
                    }
                }

                var newDepotAmounts = new List<DepotProductModel> { };
                if (item.DepotAmounts != null)
                {
                    foreach (var depotAmount in item.DepotAmounts)
                    {
                        var depot = DepotAmountsService.GetDepot(depotAmount.DepotCode);
                        if (depot == null)
                        {
                            DepotAmountsService.AddDepot(new DepotModel
                            {
                                Code = depotAmount.DepotCode,
                                Name = depotAmount.DepotCode
                            });
                            depot = DepotAmountsService.GetDepot(depotAmount.DepotCode);
                        }
                        newDepotAmounts.Add(new DepotProductModel { Amount = depotAmount.Amount, DepotId = depot.DepotId });
                    }
                }
                var changeDepotAmounts = DepotAmountsService.UpadateProductAmounts(productId, newDepotAmounts);

                if (!trackChanges && !changeDepotAmounts)
                    resultItem.Error = "Нет изменений";

                result.Offers.Add(resultItem);
            }
        }

        private void ExecuteRelatedProducts(List<ProductImportModel> products, ref CatalogImportResultModel result)
        {
            if (products == null)
                return;

            foreach (var item in products)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true, Details = new List<ImportDetailResultModel> { } };

                var productId = ImportService.GetIdByExternalId(item.ExternalId, "Product");
                var product = ProductService.GetProduct(productId);
                if (product == null)
                {
                    resultItem.Success = false;
                    resultItem.Error = "ExternalId not found";
                    result.RelatedProducts.Add(resultItem);
                    continue;
                }

                var existing = ImportService.GetRelatedProductIds(product.ProductId, RelatedType.Related);

                foreach (var related in item.RelatedProducts)
                {
                    var resultDetail = new ImportDetailResultModel() { Id = related.ExternalId, Success = true };

                    var relId = ImportService.GetIdByExternalId(related.ExternalId, "Product");
                    var relateprod = ProductService.GetProduct(relId);
                    if (relateprod == null)
                    {
                        resultDetail.Success = false;
                        resultDetail.Error = "ExternalId not found";
                        resultItem.Details.Add(resultDetail);
                        continue;
                    }

                    try
                    {
                        if (existing.Contains(relateprod.ProductId))
                        {
                            var sort = ImportService.GetRelatedProductSort(product.ProductId, relateprod.ProductId, RelatedType.Related);
                            if (sort != related.SortOrder)
                                ImportService.SetRelatedProductSort(product.ProductId, relateprod.ProductId, RelatedType.Related, related.SortOrder);
                            existing.Remove(relateprod.ProductId);
                        }
                        else
                        {
                            ProductService.AddRelatedProduct(product.ProductId, relateprod.ProductId, RelatedType.Related);
                        }
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                        resultDetail.Success = false;
                        resultDetail.Error = E.Message;
                    }

                    resultItem.Details.Add(resultDetail);
                }

                foreach (var id in existing)
                {
                    try
                    {
                        ProductService.DeleteRelatedProduct(product.ProductId, id, RelatedType.Related);
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                    }
                }

                result.RelatedProducts.Add(resultItem);
            }
        }

        private void ExecutePhotos(List<ProductPhotosImportModel> photos, ref CatalogImportResultModel result)
        {
            if (photos == null)
                return;

            foreach (var item in photos)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true, Details = new List<ImportDetailResultModel> { } };

                var productId = ImportService.GetIdByExternalId(item.ExternalId, "Product");
                var product = ProductService.GetProduct(productId);
                if (product == null)
                {
                    resultItem.Success = false;
                    resultItem.Error = "ExternalId not found";
                    result.Photos.Add(resultItem);
                    continue;
                }
                if (product.ProductPhotos.Count > 0 && !_settings.ImportProduct.UpdatePhotos)
                {
                    resultItem.Success = false;
                    resultItem.Error = "Фото для товара уже загружено";
                    result.Photos.Add(resultItem);
                    continue;
                }

                //var isMain = product.ProductPhotos.Count == 0;
                //////
                ////PhotoService.DeleteProductPhotos(productId);
                ////isMain = true;

                foreach (var fields in item.Photos)
                {
                    var resultDetail = new ImportDetailResultModel() { Id = fields.OriginName, Success = true };

                    if (fields.ValueStorage.IsNullOrEmpty())
                    {
                        resultDetail.Success = false;
                        resultDetail.Error = "Двоичные данные пустые";
                        resultItem.Details.Add(resultDetail);
                        continue;
                    }

                    int? colorId = null;
                    if (fields.Color != null)
                    {
                        Color color = ColorService.GetColor(fields.Color);
                        if (color != null)
                            colorId = color.ColorId;
                    }

                    try
                    {
                        var storage = Convert.FromBase64String(fields.ValueStorage);
                        if (!PhotoService.IsProductHaveThisPhotoByName(productId, fields.OriginName))
                        {
                            ImportService.AddProductPhotoByProductId(productId, storage, fields.OriginName, fields.Main, colorId);
                        }
                        else if (_settings.ImportProduct.UpdatePhotos)
                        {
                            ImportService.UpdateProductPhotoByProductId(productId, storage, fields.OriginName, colorId);
                        }
                        //isMain = false;
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                        resultDetail.Success = false;
                        resultDetail.Error = E.Message;
                    }

                    resultItem.Details.Add(resultDetail);
                }

                result.Photos.Add(resultItem);
            }
        }

        private void ExecuteFiles(List<ProductFilesImportModel> files, ref CatalogImportResultModel result)
        {
            if (files == null)
                return;

            foreach (var item in files)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.ExternalId, Success = true, Details = new List<ImportDetailResultModel> { } };

                if (!ModulesRepository.IsExistsModuleTable("Module", "FilesInProduct"))
                {
                    resultItem.Success = false;
                    resultItem.Error = "Модуль \"Файлы в товаре\" не установлен";
                    result.Files.Add(resultItem);
                    continue;
                }

                var productId = ImportService.GetIdByExternalId(item.ExternalId, "Product");
                var product = ProductService.GetProduct(productId);
                if (product == null)
                {
                    resultItem.Success = false;
                    resultItem.Error = "ExternalId not found";
                    result.Files.Add(resultItem);
                    continue;
                }

                foreach (var fields in item.Files)
                {
                    var resultDetail = new ImportDetailResultModel() { Id = fields.FileType, Success = true };

                    if (fields.ValueStorage.IsNullOrEmpty() && fields.Enabled)
                    {
                        resultDetail.Success = false;
                        resultDetail.Error = "Двоичные данные пустые";
                        resultItem.Details.Add(resultDetail);
                        continue;
                    }

                    try
                    {
                        var fileId = ImportService.GetProductFileId(productId, fields.FileType, fields.ExternalId);
                        if (fields.Enabled || fileId != 0)
                            ImportService.InsertUpdateProductFile(fileId, productId, fields);
                        //else if (fileId != 0)
                        //    ImportService.DeleteProductFile(fileId);
                    }
                    catch (Exception E)
                    {
                        Debug.Log.Error(E);
                        resultDetail.Success = false;
                        resultDetail.Error = E.Message;
                    }

                    resultItem.Details.Add(resultDetail);
                }

                result.Files.Add(resultItem);
            }
        }

        private void ExecutePhotoCategory(int categoryId, PhotoImportModel fields, PhotoType type/*, ref ImportResultModel result*/)
        {
            //var resultDetail = new ImportDetailResultModel() { Id = fields.OriginName, Success = true };

            if (fields.ValueStorage.IsNullOrEmpty())
            {
                //resultDetail.Success = false;
                //resultDetail.Error = "Двоичные данные пустые";
                //resultItem.Details.Add(resultDetail);
                return;
            }

            //int? colorId = null;
            //if (fields.Color != null)
            //{
            //    Color color = ColorService.GetColor(fields.Color);
            //    if (color != null)
            //        colorId = color.ColorId;
            //}

            try
            {
                var storage = Convert.FromBase64String(fields.ValueStorage);
                var photo = PhotoService.GetPhotoByObjId(categoryId, type);
                if (photo == null)
                {
                    ImportService.AddPhotoByObjIdAndType(categoryId, type, FoldersHelper.PhotoFoldersPath[FolderType.Category] + "small", storage, fields.OriginName);
                }
                else if (_settings.ImportProduct.UpdatePhotos)
                {
                    ImportService.UpdatePhotoByObjIdAndType(photo, storage);
                }
            }
            catch (Exception E)
            {
                Debug.Log.Error(E);
                //resultDetail.Success = false;
                //resultDetail.Error = E.Message;
            }

            //resultItem.Details.Add(resultDetail);
        }

    }
}
