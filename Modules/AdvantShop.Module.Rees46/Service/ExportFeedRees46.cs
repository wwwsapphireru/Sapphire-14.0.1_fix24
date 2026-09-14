using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Diagnostics;
using AdvantShop.Module.Rees46.Service;
using AdvantShop.Helpers;
using AdvantShop.Repository.Currencies;
using Newtonsoft.Json;
//GlorySoft_021
namespace AdvantShop.ExportImport
{
    [ExportFeedKey("Rees46")]
    public class ExportFeedRees46 : BaseExportFeed<ExportFeedYandexOptions>
    {
        private readonly ExportProductsService<ExportFeedYandexProduct, ExportFeedYandexOptions> _exportService;
        private readonly ExportCategoriesService _exportCategoriesService;
        private readonly ExportFeedYandexOptions _options;

        private HashSet<int> _offerIds;
        private NumberFormatInfo _nfi;
        private Currency _currency;
        private PriceRule _autoPriceRule;
        private PriceRule _autoPriceRuleForOldPrice;

        public ExportFeedRees46(int exportFeedId) : this(exportFeedId, false) { }

        public ExportFeedRees46(int exportFeedId, bool useCommonStatistic) : base(exportFeedId, useCommonStatistic)
        {
            _exportService = new ExportProductsService<ExportFeedYandexProduct, ExportFeedYandexOptions>(exportFeedId, Settings);
            _exportCategoriesService = new ExportCategoriesService(exportFeedId);
            _options = Settings?.AdvancedSettings;
        }

        protected override void Handle()
        {
            _nfi = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
            _nfi.NumberDecimalSeparator = ".";

            _offerIds = _exportService.GetOfferIds();
            _currency = CurrencyService.GetCurrencyByIso3(_options.Currency);

            if (_options.PriceRuleId != null)
                _autoPriceRule = GetAutoRule(_options.PriceRuleId.Value);

            if (_options.PriceRuleIdForOldPrice != null)
                _autoPriceRuleForOldPrice = GetAutoRule(_options.PriceRuleIdForOldPrice.Value);

            var categoriesCount = _exportCategoriesService.GetCategoriesCount(_options.ExportNotAvailable);
            var productsCount = _exportService.GetProductsCount();

            CsSetTotalRow(categoriesCount + productsCount);

            var products = _exportService.GetProducts(productsCount);
            WriteFile(products);
        }

        protected override bool NeedZip() => _options.NeedZip;

        private void WriteFile(IEnumerable<ExportFeedYandexProduct> products)
        {
            using (var outputFile = new FileStream(Settings.FileFullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var writer = XmlWriter.Create(outputFile, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
                {
                    writer.WriteStartDocument();
                    writer.WriteDocType("yml_catalog", null, "shops.dtd", null);
                    writer.WriteStartElement("yml_catalog");
                    writer.WriteAttributeString("date", DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz"));
                    writer.WriteStartElement("shop");

                    writer.WriteStartElement("name");
                    writer.WriteRaw(_options.ShopName.Replace("#STORE_NAME#", SettingsMain.ShopName).XmlEncode());
                    writer.WriteEndElement();

                    writer.WriteStartElement("company");
                    writer.WriteRaw(_options.CompanyName.Replace("#STORE_NAME#", SettingsMain.ShopName).XmlEncode());
                    writer.WriteEndElement();

                    writer.WriteStartElement("url");
                    writer.WriteRaw(SettingsMain.SiteUrl);
                    writer.WriteEndElement();

                    var platform = LocalizationService.GetResource("Core.ExportImport.Yandex.Platform");
                    if (!string.IsNullOrWhiteSpace(platform))
                    {
                        writer.WriteStartElement("platform");
                        writer.WriteRaw(platform);
                        writer.WriteEndElement();
                    }

                    if (!_options.DontExportCurrency)
                    {
                        var currencies = CurrencyService.GetAllCurrencies()
                            .Where(item => AvailableCurrencies.Contains(item.Iso3)).ToList();

                        writer.WriteStartElement("currencies");
                        ProcessCurrency(currencies, _options.Currency, writer);
                        writer.WriteEndElement();
                    }

                    var categories = _exportCategoriesService.GetCategories(_options.ExportNotAvailable);
                    var categoryIds = new HashSet<int>();
                    writer.WriteStartElement("categories");
                    foreach (var categoryRow in categories)
                    {
                        ProcessCategoryRow(categoryRow, writer);
                        categoryIds.Add(categoryRow.Id);
                        CsNextRow();
                    }

                    writer.WriteEndElement();

                    if (_options.Delivery &&
                        _options.DeliveryCost != ExportFeedYandexDeliveryCost.None &&
                        !string.IsNullOrWhiteSpace(_options.GlobalDeliveryCost))
                    {
                        try
                        {
                            var deliveryOptions = JsonConvert.DeserializeObject<List<ExportFeedYandexDeliveryCostOption>>(
                                _options.GlobalDeliveryCost);
                            if (deliveryOptions.Count != 0)
                            {
                                writer.WriteStartElement("delivery-options");

                                foreach (var deliveryOption in deliveryOptions)
                                {
                                    writer.WriteStartElement("option");
                                    writer.WriteAttributeString("cost", deliveryOption.Cost);
                                    writer.WriteAttributeString("days", deliveryOption.Days);
                                    if (!string.IsNullOrEmpty(deliveryOption.OrderBefore))
                                    {
                                        writer.WriteAttributeString("order-before", deliveryOption.OrderBefore);
                                    }

                                    writer.WriteEndElement();
                                }

                                writer.WriteEndElement();
                            }
                        }
                        catch (Exception ex)
                        {
                            CsRowError(ex.Message);
                            Debug.Log.Error(ex);
                        }
                    }

                    var flashDiscountProducts = new HashSet<int>();
                    var giftProducts = new HashSet<int>();
                    var promos = new List<ExportFeedYandexPromo>();
                    if (!string.IsNullOrEmpty(_options.Promos))
                    {
                        promos = JsonConvert.DeserializeObject<List<ExportFeedYandexPromo>>(_options.Promos);
                        RemoveNotAvailableYandexPromos(promos, categoryIds);
                    }

                    foreach (var promo in promos)
                    {
                        if (promo.Type == YandexPromoType.Flash)
                        {
                            foreach (var productId in promo.ProductIDs)
                            {
                                flashDiscountProducts.Add(productId);
                            }
                        }

                        if (promo.Type == YandexPromoType.Gift)
                        {
                            giftProducts.Add(promo.GiftID);
                        }
                    }

                    writer.WriteStartElement("offers");

                    foreach (var productRow in products)
                    {
                        ProcessProductRow(productRow, writer,
                            productExistInPromos: flashDiscountProducts.Count > 0 && flashDiscountProducts.Contains(productRow.ProductId));

                        CsNextRow();
                    };

                    writer.WriteEndElement();

                    RemoveNotAvailableYandexGifts(giftProducts);
                    if (giftProducts.Count > 0)
                    {
                        writer.WriteStartElement("gifts");
                        foreach (var id in giftProducts)
                        {
                            var offer = OfferService.GetOffer(id);
                            ProcessGiftRow(offer, writer);
                        }

                        writer.WriteEndElement();
                    }

                    if (promos.Count > 0)
                    {
                        writer.WriteStartElement("promos");
                        foreach (var promo in promos)
                        {
                            ProcessYandexPromoRow(promo, writer, categoryIds);
                        }

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();

                    writer.Flush();
                    writer.Close();
                }
            }
        }

        public static List<string> AvailableCurrencies =>
            new List<string>
            {
                "RUR", "RUB", "USD", "EUR", "UAH", "KZT", "BYN", "GBP", "TRY", "ALL", "AFN", "ARS", "AWG", "AUD", "AZN",
                "BBD", "BZD", "BMD", "BOB", "BAM", "BWP", "BGN", "BRL", "BND", "KHR", "CAD", "KYD", "CLP", "CNY", "COP",
                "CRC", "HRK", "CUP", "CZK", "DKK", "DOP", "XCD", "EGP", "SVC", "FKP", "FJD", "GHS", "GIP", "GTQ", "GYD",
                "HNL", "HKD", "HUF", "ISK", "INR", "IDR", "IRR", "ILS", "JMD", "JPY", "KPW", "KRW", "KGS", "LAK", "LBP",
                "LRD", "MKD", "MYR", "MUR", "MXN", "MNT", "MAD", "MZN", "NAD", "NPR", "ANG", "NZD", "NIO", "NGN", "NOK",
                "OMR", "PKR", "PAB", "PYG", "PEN", "PHP", "PLN", "QAR", "RON", "SHP", "SAR", "RSD", "SCR", "SGD", "SBD",
                "SOS", "ZAR", "LKR", "SEK", "CHF", "SRD", "SYP", "TWD", "THB", "TTD", "AED", "UYU", "UZS", "VEF", "VND",
                "YER", "MDL", "TJS", "GEL", "AMD", "BHD", "KWD"
            };

        public static List<string> AvailableEtalonCurrencies => new List<string> { "RUB", "RUR", "BYN", "KZT", "UAH" };

        private void ProcessCurrency(List<Currency> currencies, string currency, XmlWriter writer)
        {
            if (currencies == null)
                return;

            var defaultCurrency =
                currencies.FirstOrDefault(item => item.Iso3 == currency && AvailableEtalonCurrencies.Contains(currency)) ??
                currencies.FirstOrDefault(item => AvailableEtalonCurrencies.Contains(item.Iso3));

            if (defaultCurrency == null)
                return;

            ProcessCurrencyRow(new Currency
            {
                CurrencyId = defaultCurrency.CurrencyId,
                Rate = 1,
                Iso3 = defaultCurrency.Iso3
            }, writer);

            foreach (var curRow in currencies.Where(item => item.Iso3 != defaultCurrency.Iso3))
            {
                curRow.Rate = Convert.ToSingle(curRow.Rate / defaultCurrency.Rate, CultureInfo.InvariantCulture);
                ProcessCurrencyRow(curRow, writer);
            }
        }

        private void ProcessCurrencyRow(Currency currency, XmlWriter writer)
        {
            writer.WriteStartElement("currency");
            writer.WriteAttributeString("id", currency.Iso3);
            writer.WriteAttributeString("rate", Math.Round(currency.Rate, 4).ToString(_nfi));
            writer.WriteEndElement();
        }

        private void ProcessCategoryRow(ExportCategoryModel row, XmlWriter writer)
        {
            writer.WriteStartElement("category");
            writer.WriteAttributeString("id", row.Id.ToString(CultureInfo.InvariantCulture));
            if (row.ParentCategory != 0)
            {
                writer.WriteAttributeString("parentId", row.ParentCategory.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteRaw(row.Name.XmlEncode().RemoveInvalidXmlChars());
            writer.WriteEndElement();
        }

        private void ProcessProductRow(ExportFeedYandexProduct row, XmlWriter writer, bool productExistInPromos = false)
        {
            writer.WriteStartElement("offer");

            switch (_options.OfferIdType)
            {
                case "id":
                    writer.WriteAttributeString("id", row.OfferId.ToString(CultureInfo.InvariantCulture));
                    break;

                case "artno":
                    writer.WriteAttributeString("id", row.OfferArtNo.ToString(CultureInfo.InvariantCulture));
                    break;

                default:
                    writer.WriteAttributeString("id", row.OfferId.ToString(CultureInfo.InvariantCulture));
                    break;
            }

            var amount = row.AmountByWarehouses ?? row.Amount;

            if (amount > 0 || _options.AllowPreOrderProducts)
                writer.WriteAttributeString("available", (amount > 0).ToLowerString());

            if (row.ColorId != 0 || row.SizeId != 0)
            {
                writer.WriteAttributeString("group_id", row.ProductId.ToString(CultureInfo.InvariantCulture));
            }

            var showVendorModel = !_options.TypeExportYandex && !string.IsNullOrEmpty(row.BrandName);
            if (showVendorModel)
            {
                writer.WriteAttributeString("type", "vendor.model");
            }

            if (row.Bid > 0)
            {
                writer.WriteAttributeString("bid", row.Bid.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteStartElement("url");
            writer.WriteRaw(CreateLink(row));
            writer.WriteEndElement();

            var (price, oldPrice) = GetNewAndOldPrices(row, productExistInPromos, _options.ConsiderMultiplicityInPrice);

            writer.WriteStartElement("price");
            writer.WriteRaw(price.ToString(_nfi));
            writer.WriteEndElement();

            if (!productExistInPromos && _options.ProductPriceType == EExportFeedYandexPriceType.Both && oldPrice - price > 1)
            {
                writer.WriteStartElement("oldprice");
                writer.WriteRaw(oldPrice.ToString(_nfi));
                writer.WriteEndElement();
            }

            if (_options.ExportPurchasePrice)
            {
                var purchasePrice = PriceService.RoundPrice(
                    row.SupplyPrice * (_options.ConsiderMultiplicityInPrice ? row.Multiplicity : 1),
                    _currency,
                    row.CurrencyValue);

                writer.WriteStartElement("purchase_price");
                writer.WriteRaw(Convert.ToInt32(purchasePrice).ToString(_nfi));
                writer.WriteEndElement();
            }

            writer.WriteStartElement("currencyId");
            writer.WriteRaw(_options.Currency);
            writer.WriteEndElement();

            if (_options.ExportMarketCategoryId && row.YandexMarketCategoryId != null)
            {
                writer.WriteStartElement("market_category_id");
                writer.WriteRaw(row.YandexMarketCategoryId.Value.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            writer.WriteStartElement("categoryId");
            writer.WriteRaw(row.ParentCategory.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            if (!string.IsNullOrEmpty(row.Photos))
            {
                if (_options.ExportAllPhotos)
                {
                    var temp = row.Photos.Split(',').Take(10);
                    foreach (var item in temp.Where(item => !string.IsNullOrWhiteSpace(item)))
                    {
                        writer.WriteStartElement("picture");
                        writer.WriteRaw(GetImageProductPath(item));
                        writer.WriteEndElement();
                    }
                }
                else
                {
                    writer.WriteStartElement("picture");
                    writer.WriteRaw(GetImageProductPath(row.Photos.Split(',')[0]));
                    writer.WriteEndElement();
                }
            }

            writer.WriteStartElement("store");
            writer.WriteRaw(_options.StoreString);
            writer.WriteEndElement();

            writer.WriteStartElement("pickup");
            writer.WriteRaw(_options.PickupString);
            writer.WriteEndElement();

            if (_options.ExportBarCode
                && !string.IsNullOrEmpty(row.BarCode)
                && long.TryParse(row.BarCode, out _)
                && (row.BarCode.Length == 8
                    || row.BarCode.Length == 12
                    || row.BarCode.Length == 13))
            {
                writer.WriteStartElement("barcode");
                writer.WriteRaw(row.BarCode);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("delivery");
            writer.WriteRaw(_options.DeliveryString);
            writer.WriteEndElement();

            if (showVendorModel)
            {
                if (!string.IsNullOrEmpty(row.YandexTypePrefix))
                {
                    writer.WriteStartElement("typePrefix");
                    writer.WriteRaw(row.YandexTypePrefix);
                    writer.WriteEndElement();
                }

                writer.WriteStartElement("vendor");
                writer.WriteRaw(row.BrandName.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();

                if (!string.IsNullOrEmpty(row.BrandCountryManufacture) || !string.IsNullOrEmpty(row.BrandCountry))
                {
                    var countryOfOrigin = !string.IsNullOrEmpty(row.BrandCountryManufacture) ? row.BrandCountryManufacture : row.BrandCountry;

                    writer.WriteStartElement("country_of_origin");
                    writer.WriteRaw(countryOfOrigin.XmlEncode().RemoveInvalidXmlChars());
                    writer.WriteEndElement();
                }

                writer.WriteStartElement("model");
                writer.WriteRaw(GetProductName(row, true));
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartElement("name");
                writer.WriteRaw(GetProductName(row, false));
                writer.WriteEndElement();

                if (_options.ExportVendorInSimplifiedType && !string.IsNullOrEmpty(row.BrandName))
                {
                    writer.WriteStartElement("vendor");
                    writer.WriteRaw(row.BrandName.XmlEncode().RemoveInvalidXmlChars());
                    writer.WriteEndElement();

                    if (!string.IsNullOrEmpty(row.BrandCountryManufacture) || !string.IsNullOrEmpty(row.BrandCountry))
                    {
                        var countryOfOrigin = !string.IsNullOrEmpty(row.BrandCountryManufacture)
                            ? row.BrandCountryManufacture
                            : row.BrandCountry;

                        writer.WriteStartElement("country_of_origin");
                        writer.WriteRaw(countryOfOrigin.XmlEncode().RemoveInvalidXmlChars());
                        writer.WriteEndElement();
                    }
                }
            }

            if (_options.ExportShopSku)
            {
                writer.WriteStartElement("shop-sku");
                writer.WriteRaw(
                    _options.OfferIdType == "artno"
                        ? row.OfferArtNo.ToString(CultureInfo.InvariantCulture)
                        : row.OfferId.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            if (_options.ExportManufacturer && !string.IsNullOrEmpty(row.BrandName))
            {
                writer.WriteStartElement("manufacturer");
                writer.WriteRaw(row.BrandName.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            if (_options.ExportCount)
            {
                writer.WriteStartElement("count");
                writer.WriteRaw(amount.ToString(_nfi));
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_options.ProductDescriptionType) &&
                !string.Equals(_options.ProductDescriptionType, "none"))
            {
                var desc = _options.ProductDescriptionType == "full"
                    ? row.GetDescriptionFormatted(_options.WarehouseCity)
                    : row.GetBriefDescriptionFormatted(_options.WarehouseCity);
                if (!desc.IsNullOrEmpty())
                {
                    if (desc.Contains("userfiles/"))
                    {
                        desc = desc.Replace("\"userfiles/", "\"" + SettingsMain.SiteUrl + "/userfiles/")
                            .Replace("'userfiles/", "\'" + SettingsMain.SiteUrl + "/userfiles/")
                            .Replace("\"/userfiles/", "\"" + SettingsMain.SiteUrl + "/userfiles/")
                            .Replace("\'/userfiles/", "'" + SettingsMain.SiteUrl + "/userfiles/");
                    }

                    writer.WriteStartElement("description");
                    if (_options.RemoveHtml)
                    {
                        desc = StringHelper.RemoveHTML(desc);

                        writer.WriteRaw(desc.XmlEncode().RemoveInvalidXmlChars());
                    }
                    else
                    {
                        writer.WriteCData(desc.RemoveInvalidXmlChars());
                    }

                    writer.WriteEndElement();
                }
            }

            var vendorCode =
                _options.VendorCodeType == "offerArtno"
                    ? row.OfferArtNo.ToString(CultureInfo.InvariantCulture)
                    : row.ArtNo;

            writer.WriteStartElement("vendorCode");
            writer.WriteRaw(vendorCode.XmlEncode().RemoveInvalidXmlChars());
            writer.WriteEndElement();

            if (!string.IsNullOrWhiteSpace(row.YandexSalesNote))
            {
                writer.WriteStartElement("sales_notes");
                writer.WriteRaw(row.YandexSalesNote.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }
            else if (!string.IsNullOrWhiteSpace(_options.SalesNotes))
            {
                writer.WriteStartElement("sales_notes");
                writer.WriteRaw(_options.SalesNotesEncoded);
                writer.WriteEndElement();
            }

            var localDeliveryOption = _options.LocalDeliveryOptionObject;
            if (_options.Delivery &&
                _options.DeliveryCost == ExportFeedYandexDeliveryCost.LocalDeliveryCost &&
                row.ShippingPrice >= 0 && localDeliveryOption != null)
            {
                writer.WriteStartElement("delivery-options");
                writer.WriteStartElement("option");
                writer.WriteAttributeString("cost", Math.Round(row.ShippingPrice.Value).ToString(_nfi));
                //condition same that in available
                if (amount > 0)
                {
                    writer.WriteAttributeString("days",
                        !string.IsNullOrEmpty(row.YandexDeliveryDays)
                            ? row.YandexDeliveryDays
                            : localDeliveryOption.Days);

                    if (!string.IsNullOrEmpty(localDeliveryOption.OrderBefore))
                    {
                        writer.WriteAttributeString("order-before", localDeliveryOption.OrderBefore);
                    }
                }
                else
                    writer.WriteAttributeString("days", "");

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            if (row.ManufacturerWarranty)
            {
                writer.WriteStartElement("manufacturer_warranty");
                writer.WriteRaw(row.ManufacturerWarranty.ToLowerString());
                writer.WriteEndElement();
            }

            if (row.Adult)
            {
                writer.WriteStartElement("adult");
                writer.WriteRaw(row.Adult.ToLowerString());
                writer.WriteEndElement();
            }

            if (row.Weight > 0)
            {
                writer.WriteStartElement("weight");
                var weight = Math.Round(row.Weight, 3, MidpointRounding.AwayFromZero);
                writer.WriteRaw((weight >= 0.001f ? weight : 0.001f).ToString("F3").Replace(",", ".").ToLower());
                writer.WriteEndElement();
            }

            if (_options.ExportExpiry && !string.IsNullOrEmpty(row.YandexMarketExpiry))
            {
                writer.WriteStartElement("expiry");
                writer.WriteRaw(row.YandexMarketExpiry);
                writer.WriteEndElement();
            }

            if (_options.ExportWarrantyDays && !string.IsNullOrEmpty(row.YandexMarketWarrantyDays))
            {
                writer.WriteStartElement("warranty-days");
                writer.WriteRaw(row.YandexMarketWarrantyDays);
                writer.WriteEndElement();
            }

            if (_options.ExportCommentWarranty && !string.IsNullOrEmpty(row.YandexMarketCommentWarranty))
            {
                writer.WriteStartElement("comment-warranty");
                writer.WriteRaw(row.YandexMarketCommentWarranty);
                writer.WriteEndElement();
            }

            if (_options.ExportPeriodOfValidityDays && !string.IsNullOrEmpty(row.YandexMarketPeriodOfValidityDays))
            {
                writer.WriteStartElement("period-of-validity-days");
                writer.WriteRaw(row.YandexMarketPeriodOfValidityDays);
                writer.WriteEndElement();
            }

            if (_options.ExportServiceLifeDays && !string.IsNullOrEmpty(row.YandexMarketServiceLifeDays))
            {
                writer.WriteStartElement("service-life-days");
                writer.WriteRaw(row.YandexMarketServiceLifeDays);
                writer.WriteEndElement();
            }

            if (_options.ExportMarketTnVedCode && !string.IsNullOrEmpty(row.YandexMarketTnVedCode))
            {
                writer.WriteStartElement("tn-ved-code");
                writer.WriteRaw(row.YandexMarketTnVedCode);
                writer.WriteEndElement();
            }

            if (_options.ExportMarketStepQuantity)
            {
                var stepQuantity = !string.IsNullOrEmpty(row.YandexMarketStepQuantity) ? row.YandexMarketStepQuantity : null;

                if (string.IsNullOrEmpty(stepQuantity) && row.Multiplicity % 1 == 0)
                    stepQuantity = row.Multiplicity.ToString(_nfi);

                if (!string.IsNullOrEmpty(stepQuantity))
                {
                    writer.WriteStartElement("step-quantity");
                    writer.WriteRaw(stepQuantity);
                    writer.WriteEndElement();
                }
            }

            if (_options.ExportMarketMinQuantity && !string.IsNullOrEmpty(row.YandexMarketMinQuantity))
            {
                writer.WriteStartElement("min-quantity");
                writer.WriteRaw(row.YandexMarketMinQuantity);
                writer.WriteEndElement();
            }

            if (_options.ExportMarketCisRequired && row.IsMarkingRequired)
            {
                writer.WriteStartElement("cargo-types");
                writer.WriteRaw("CIS_REQUIRED");
                writer.WriteEndElement();
            }

            if (row.IsDigital)
            {
                writer.WriteStartElement("downloadable");
                writer.WriteRaw("true");
                writer.WriteEndElement();
            }

            if (_options.ExportRelatedProducts)
            {
                var recProducts = ProductService.GetRelatedProducts(row.ProductId, RelatedType.Related, noCache: true)
                    .Where(x => _offerIds.Any(y => y == x.OfferId)).ToList();
                var result = string.Empty;
                for (int index = 0; index < recProducts.Count && index < 30; ++index)
                {
                    result += (index > 0 && result != string.Empty ? "," : string.Empty) +
                              (_options.OfferIdType == "id"
                                  ? recProducts[index].OfferId.ToString(CultureInfo.InvariantCulture)
                                  : recProducts[index].ArtNo.ToString(CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrEmpty(result))
                {
                    writer.WriteStartElement("rec");
                    writer.WriteRaw(result);
                    writer.WriteEndElement();
                }
            }

            if (!string.IsNullOrWhiteSpace(row.ColorName))
            {
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", ColorHeader);
                writer.WriteRaw(row.ColorName.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            if (!string.IsNullOrWhiteSpace(row.SizeName))
            {
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", SizeHeader);
                if (!string.IsNullOrEmpty(row.YandexSizeUnit))
                {
                    writer.WriteAttributeString("unit", row.YandexSizeUnit);
                }

                writer.WriteRaw(row.SizeName.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            if (_options.ExportProductProperties)
            {
                var propertyValues = PropertyService.GetPropertyValuesByProductId(row.ProductId);

                if (_options.JoinPropertyValues)
                {
                    var newPropertyValues = new List<PropertyValue>();

                    foreach (var propertyValue in propertyValues)
                    {
                        var propertyId = propertyValue.PropertyId;

                        if (newPropertyValues.Any(x => x.PropertyId == propertyId))
                            continue;

                        propertyValue.Value = String.Join(", ",
                            propertyValues.Where(x => x.PropertyId == propertyId).Select(x => x.Value));

                        newPropertyValues.Add(propertyValue);
                    }

                    propertyValues = newPropertyValues;
                }

                if (_options.ExportOnlyUseInDetailsProperties)
                {
                    var exceptionPropIds = _options.ExportOnlyUseInDetailsPropertiesExceptionIds ?? new List<int>();
                    propertyValues = propertyValues
                        .Where(x => x.Property.UseInDetails || exceptionPropIds.Contains(x.PropertyId))
                        .ToList();
                }

                foreach (var prop in propertyValues)
                {
                    var propertyName = _options.ExportPropertyDisplayedName
                                       && prop.Property.NameDisplayed.IsNotEmpty()
                        ? prop.Property.NameDisplayed
                        : prop.Property.Name;
                    if (propertyName.IsNotEmpty() && prop.Value.IsNotEmpty())
                    {
                        writer.WriteStartElement("param");

                        writer.WriteAttributeString("name", propertyName.XmlEncode().RemoveInvalidXmlChars());
                        if (!string.IsNullOrEmpty(prop.Property.Unit))
                        {
                            writer.WriteAttributeString("unit", prop.Property.Unit.XmlEncode().RemoveInvalidXmlChars());
                        }

                        writer.WriteRaw(prop.Value.XmlEncode().RemoveInvalidXmlChars());
                        writer.WriteEndElement();
                    }
                }
            }

            if (_options.ExportDimensions &&
                row.Length.HasValue && !row.Length.Value.IsDefault() &&
                row.Height.HasValue && !row.Height.Value.IsDefault() &&
                row.Width.HasValue && !row.Width.Value.IsDefault())
            {
                writer.WriteStartElement("dimensions");
                writer.WriteRaw(Math.Round(row.Length.Value / 10, 3).ToInvariantString() + "/"
                    + Math.Round(row.Width.Value / 10, 3).ToInvariantString() + "/"
                    + Math.Round(row.Height.Value / 10, 3).ToInvariantString());
                writer.WriteEndElement();
            }

            if (row.YandexProductDiscounted && row.YandexProductDiscountCondition != EYandexDiscountCondition.None)
            {
                writer.WriteStartElement("condition");
                writer.WriteAttributeString("type",
                    row.YandexProductDiscountCondition.StrName().XmlEncode().RemoveInvalidXmlChars());
                if (row.YandexProductQuality != EYandexProductQuality.None)
                {
                    writer.WriteStartElement("quality");
                    writer.WriteRaw(row.YandexProductQuality.StrName().XmlEncode().RemoveInvalidXmlChars());
                    writer.WriteEndElement();
                }
                writer.WriteStartElement("reason");
                writer.WriteRaw(row.YandexProductDiscountReason.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private void ProcessYandexPromoRow(ExportFeedYandexPromo promo, XmlWriter writer, HashSet<int> categoryIds)
        {
            var coupon = new Coupon();

            if (promo.Type == YandexPromoType.PromoCode)
            {
                coupon = CouponService.GetCoupon(promo.CouponId);
            }

            writer.WriteStartElement("promo");
            writer.WriteAttributeString("id", BitConverter.ToInt64(promo.PromoID.ToByteArray(), 0).ToString());
            var type = promo.Type == YandexPromoType.PromoCode ? "promo code"
                : promo.Type == YandexPromoType.Flash ? "flash discount"
                : promo.Type == YandexPromoType.Gift ? "gift with purchase"
                : "n plus m";
            writer.WriteAttributeString("type", type);

            #region Dates

            if (promo.Type == YandexPromoType.PromoCode && coupon.ExpirationDate != null)
            {
                writer.WriteStartElement("start-date");
                writer.WriteRaw((coupon.StartDate ?? coupon.AddingDate).ToString("yyyy-MM-dd HH:mm:ss").XmlEncode()
                    .RemoveInvalidXmlChars());
                writer.WriteEndElement();
                writer.WriteStartElement("end-date");
                writer.WriteRaw(((DateTime)coupon.ExpirationDate).ToString("yyyy-MM-dd HH:mm:ss").XmlEncode()
                    .RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            if (promo.Type != YandexPromoType.PromoCode)
            {
                if (promo.StartDate != null && promo.ExpirationDate != null)
                {
                    writer.WriteStartElement("start-date");
                    writer.WriteRaw(((DateTime)promo.StartDate).ToString("yyyy-MM-dd HH:mm:ss").XmlEncode()
                        .RemoveInvalidXmlChars());
                    writer.WriteEndElement();
                    writer.WriteStartElement("end-date");
                    writer.WriteRaw(((DateTime)promo.ExpirationDate).ToString("yyyy-MM-dd HH:mm:ss").XmlEncode()
                        .RemoveInvalidXmlChars());
                    writer.WriteEndElement();
                }
            }

            #endregion Dates

            if (promo.Description != null)
            {
                writer.WriteStartElement("description");
                writer.WriteRaw(promo.Description.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            if (promo.PromoUrl != null)
            {
                writer.WriteStartElement("url");
                writer.WriteRaw(promo.PromoUrl.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            if (promo.Type == YandexPromoType.PromoCode)
            {
                writer.WriteStartElement("promo-code");
                writer.WriteRaw(coupon.Code.XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
                writer.WriteStartElement("discount");
                var couponType = coupon.Type == CouponType.Fixed ? "currency" : "percent";
                writer.WriteAttributeString("unit", couponType);
                if (coupon.Type == CouponType.Fixed)
                    writer.WriteAttributeString("currency", coupon.CurrencyIso3);
                writer.WriteRaw(coupon.Value.ToInvariantString().XmlEncode().RemoveInvalidXmlChars());
                writer.WriteEndElement();
            }

            writer.WriteStartElement("purchase");
            if (promo.Type == YandexPromoType.PromoCode)
            {
                var offersList = new List<Offer>();

                if (coupon.ProductsIds.Count > 0)
                {
                    foreach (var productId in coupon.ProductsIds)
                    {
                        var offers = OfferService.GetProductOffers(productId);

                        foreach (var offer in offers.Where(x => _offerIds.Any(y => y == x.OfferId)))
                            offersList.Add(offer);
                    }
                }

                if (coupon.OfferIds.Count > 0)
                {
                    foreach (var offerId in coupon.OfferIds.Where(x => _offerIds.Contains(x)))
                    {
                        var offer = OfferService.GetOffer(offerId);
                        if (offer != null
                            && offersList.Find(x => x.OfferId == offer.OfferId) == null)
                        {
                            offersList.Add(offer);
                        }
                    }
                }

                if (offersList.Count > 0)
                {
                    foreach (var offer in offersList)
                    {
                        writer.WriteStartElement("product");

                        switch (_options.OfferIdType)
                        {
                            case "id":
                                writer.WriteAttributeString("offer-id",
                                    offer.OfferId.ToString(CultureInfo.InvariantCulture));
                                break;

                            case "artno":
                                writer.WriteAttributeString("offer-id",
                                    offer.ArtNo.ToString(CultureInfo.InvariantCulture));
                                break;

                            default:
                                writer.WriteAttributeString("offer-id",
                                    offer.OfferId.ToString(CultureInfo.InvariantCulture));
                                break;
                        }

                        writer.WriteEndElement();
                    }
                }

                if (coupon.CategoryIds.Count > 0)
                {
                    foreach (var id in coupon.CategoryIds.Intersect(categoryIds))
                    {
                        writer.WriteStartElement("product");
                        writer.WriteAttributeString("category-id", id.ToString());
                        writer.WriteEndElement();
                    }
                }

                if (coupon.ProductsIds.Count <= 0 && coupon.CategoryIds.Count <= 0)
                {
                    foreach (var categoryId in categoryIds)
                    {
                        writer.WriteStartElement("product");
                        writer.WriteAttributeString("category-id", categoryId.ToString());
                        writer.WriteEndElement();
                    }
                }
            }
            else
            {
                if (promo.Type == YandexPromoType.Gift || promo.Type == YandexPromoType.NPlusM)
                {
                    writer.WriteStartElement("required-quantity");
                    writer.WriteRaw(promo.RequiredQuantity.ToString());
                    writer.WriteEndElement();
                }

                if (promo.Type == YandexPromoType.NPlusM)
                {
                    writer.WriteStartElement("free-quantity");
                    writer.WriteRaw(promo.FreeQuantity.ToString());
                    writer.WriteEndElement();
                    foreach (var categoryId in promo.CategoryIDs)
                    {
                        writer.WriteStartElement("product");
                        writer.WriteAttributeString("category-id", categoryId.ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                }

                foreach (var productId in promo.ProductIDs)
                {
                    var product = ProductService.GetProduct(productId);
                    var offers = OfferService.GetProductOffers(productId);
                    foreach (var offer in offers.Where(x => _offerIds.Any(y => y == x.OfferId)))
                    {
                        writer.WriteStartElement("product");
                        switch (_options.OfferIdType)
                        {
                            case "id":
                                writer.WriteAttributeString("offer-id",
                                    offer.OfferId.ToString(CultureInfo.InvariantCulture));
                                break;

                            case "artno":
                                writer.WriteAttributeString("offer-id",
                                    offer.ArtNo.ToString(CultureInfo.InvariantCulture));
                                break;

                            default:
                                writer.WriteAttributeString("offer-id",
                                    offer.OfferId.ToString(CultureInfo.InvariantCulture));
                                break;
                        }

                        if (promo.Type == YandexPromoType.Flash)
                        {
                            writer.WriteStartElement("discount-price");
                            writer.WriteAttributeString("currency", _options.Currency);

                            float discount = 0;
                            if (ProductDiscountModels != null)
                            {
                                var prodDiscount = ProductDiscountModels.Find(d => d.ProductId == offer.ProductId);
                                if (prodDiscount != null)
                                {
                                    discount = prodDiscount.Discount;
                                }
                            }

                            var priceDiscount =
                                discount > 0 && discount > product.Discount.Amount
                                    ? new Discount(discount, 0)
                                    : product.Discount;

                            var markup = offer.BasePrice * Settings.PriceMarginInPercents / 100 + Settings.PriceMarginInNumbers;

                            var newPrice = (decimal)PriceService.GetFinalPrice(offer.BasePrice + markup, priceDiscount, product.Currency.Rate, _currency);
                            writer.WriteRaw(newPrice.ToString(_nfi));
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                }
            }

            writer.WriteEndElement();

            if (promo.Type == YandexPromoType.Gift)
            {
                writer.WriteStartElement("promo-gifts");
                writer.WriteStartElement("promo-gift");
                var offer = OfferService.GetOffer(promo.GiftID);
                if (_offerIds.All(x => x != offer.OfferId))
                {
                    writer.WriteAttributeString("gift-id", offer.OfferId.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    switch (_options.OfferIdType)
                    {
                        case "id":
                            writer.WriteAttributeString("offer-id",
                                offer.OfferId.ToString(CultureInfo.InvariantCulture));
                            break;

                        case "artno":
                            writer.WriteAttributeString("offer-id", offer.ArtNo.ToString(CultureInfo.InvariantCulture));
                            break;

                        default:
                            writer.WriteAttributeString("offer-id",
                                offer.OfferId.ToString(CultureInfo.InvariantCulture));
                            break;
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private void RemoveNotAvailableYandexPromos(List<ExportFeedYandexPromo> promos, HashSet<int> categoryIds)
        {
            var promosToRemove = new List<Guid>();

            foreach (var promo in promos)
            {
                if (promo.Type == YandexPromoType.PromoCode)
                {
                    var coupon = CouponService.GetCoupon(promo.CouponId);
                    if (coupon == null || !coupon.Enabled || coupon.Code == null || coupon.Code.Length > 20 ||
                        coupon.ExpirationDate.HasValue && coupon.ExpirationDate < DateTime.Now ||
                        coupon.StartDate.HasValue && coupon.StartDate > DateTime.Now && !coupon.ExpirationDate.HasValue)
                    {
                        promosToRemove.Add(promo.PromoID);
                        continue;
                    }

                    var removePromo = true;
                    if (coupon.ProductsIds != null && coupon.ProductsIds.Count > 0)
                    {
                        foreach (var productId in coupon.ProductsIds)
                        {
                            var offers = OfferService.GetProductOffers(productId);
                            if (offers.Any(x => _offerIds.Any(y => y == x.OfferId)))
                            {
                                removePromo = false;
                                break;
                            }
                        }
                    }

                    if (coupon.CategoryIds != null && coupon.CategoryIds.Count > 0)
                    {
                        if (coupon.CategoryIds.Any(x => categoryIds.Any(y => y == x)))
                            removePromo = false;
                    }

                    if ((coupon.ProductsIds == null || coupon.ProductsIds.Count == 0) &&
                        (coupon.CategoryIds == null || coupon.CategoryIds.Count == 0))
                    {
                        removePromo = false;
                    }

                    if (removePromo)
                    {
                        promosToRemove.Add(promo.PromoID);
                    }
                }
                else
                {
                    var productsTemp = new List<int>();
                    if (promo.ProductIDs != null)
                    {
                        foreach (var productId in promo.ProductIDs)
                        {
                            var productOfferIds = OfferService.GetProductOffers(productId).Select(x => x.OfferId);
                            if (_offerIds.Any(x => productOfferIds.Any(y => y == x)))
                            {
                                if (promo.Type == YandexPromoType.Flash)
                                {
                                    var product = ProductService.GetProduct(productId);
                                    if (product.Discount.HasValue)
                                        productsTemp.Add(productId);
                                }
                                else
                                {
                                    productsTemp.Add(productId);
                                }
                            }
                        }
                    }

                    var categoriesTemp = new List<int>();
                    if (promo.CategoryIDs != null)
                    {
                        foreach (var categoryID in promo.CategoryIDs)
                        {
                            if (categoryIds.Any(x => x == categoryID))
                                categoriesTemp.Add(categoryID);
                        }
                    }

                    if (promo.Type == YandexPromoType.NPlusM)
                    {
                        if (productsTemp.Count == 0 && categoriesTemp.Count == 0)
                        {
                            promosToRemove.Add(promo.PromoID);
                            continue;
                        }
                    }
                    else if (productsTemp.Count == 0)
                    {
                        promosToRemove.Add(promo.PromoID);
                        continue;
                    }

                    if (promo.Type == YandexPromoType.Gift)
                    {
                        if (OfferService.GetOffer(promo.GiftID) == null)
                        {
                            promosToRemove.Add(promo.PromoID);
                            continue;
                        }
                    }

                    promo.CategoryIDs = categoriesTemp;
                    promo.ProductIDs = productsTemp;
                }
            }

            if (promosToRemove.Count > 0)
            {
                promos.RemoveAll(x => promosToRemove.IndexOf(x.PromoID) > -1);
            }
        }

        private void ProcessGiftRow(Offer offer, XmlWriter writer)
        {
            writer.WriteStartElement("gift");
            writer.WriteAttributeString("id", offer.OfferId.ToString(CultureInfo.InvariantCulture));
            writer.WriteStartElement("name");
            writer.WriteRaw(offer.Product.Name.XmlEncode().RemoveInvalidXmlChars());
            writer.WriteEndElement();
            writer.WriteStartElement("picture");
            writer.WriteRaw(GetImageProductPath(offer.Product.Photo));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void RemoveNotAvailableYandexGifts(HashSet<int> giftIds)
        {
            var idsToRemove = new List<int>();
            foreach (var id in giftIds)
            {
                var offer = OfferService.GetOffer(id);
                if (offer == null || _offerIds.Any(x => x == id))
                {
                    idsToRemove.Add(id);
                }
            }

            if (idsToRemove.Count > 0)
            {
                giftIds.RemoveWhere(x => idsToRemove.Any(y => y == x));
            }
        }

        private (decimal, decimal) GetNewAndOldPrices(ExportFeedYandexProduct product, bool productExistInPromos,
                                                        bool considerMultiplicityInPrice)
        {
            decimal price = 0m;
            decimal oldPrice = 0m;

            float discount = 0;

            if (ProductDiscountModels != null)
            {
                var prodDiscount = ProductDiscountModels.Find(d => d.ProductId == product.ProductId);
                if (prodDiscount != null)
                    discount = prodDiscount.Discount;
            }

            var priceDiscount = discount > 0 && discount > product.Discount
                ? new Discount(discount, 0)
                : new Discount(product.Discount, product.DiscountAmount);

            if (product.PriceByRule != null)
            {
                var markup = GetMarkup(product.PriceByRule.Value, product.CurrencyValue);

                price = (decimal)PriceService.RoundPrice(product.PriceByRule.Value + markup, _currency, product.CurrencyValue);
            }
            else
            {
                var productPrice = product.Price;

                if (_autoPriceRule != null)
                {
                    productPrice =
                        PriceRuleService.CalculatePriceAutoPriceRule(_autoPriceRule, productPrice, product.SupplyPrice);

                    if (!_autoPriceRule.ApplyDiscounts)
                        priceDiscount = new Discount();
                }

                price = GetPriceWithDiscount(productPrice, priceDiscount, _currency, product.CurrencyValue);
            }

            if (product.OldPriceByRule != null)
            {
                oldPrice = (decimal)PriceService.RoundPrice(product.OldPriceByRule.Value, _currency, product.CurrencyValue);
            }
            else
            {
                var productPrice = product.Price;

                if (_autoPriceRuleForOldPrice != null)
                {
                    productPrice =
                        PriceRuleService.CalculatePriceAutoPriceRule(_autoPriceRuleForOldPrice, productPrice, product.SupplyPrice);
                }

                var markupOldPrice = GetMarkup(productPrice, product.CurrencyValue);

                oldPrice = (decimal)PriceService.GetFinalPrice(productPrice + markupOldPrice, new Discount(), product.CurrencyValue, _currency);

                if (productExistInPromos || _options.ProductPriceType == EExportFeedYandexPriceType.WithoutDiscount)
                {
                    price = oldPrice;
                }
            }

            if (considerMultiplicityInPrice && product.Multiplicity != 1)
            {
                price = PriceService.SimpleRoundPrice(price * (decimal)product.Multiplicity, _currency);
                oldPrice = PriceService.SimpleRoundPrice(oldPrice * (decimal)product.Multiplicity, _currency);
            }

            return (price, oldPrice);
        }

        private string CreateLink(ExportFeedYandexProduct row)
        {
            var queryParams = new List<string>();
            if (!row.Main)
            {
                if (row.ColorId != 0)
                    queryParams.Add($"color={row.ColorId}");
                if (row.SizeId != 0)
                    queryParams.Add($"size={row.SizeId}");
            }

            var urlTags = GetAdditionalUrlTags(row);
            if (!string.IsNullOrEmpty(urlTags))
                queryParams.Add(urlTags);

            return StringHelper.HtmlEncode(SettingsMain.SiteUrl + "/" +
                UrlService.GetLink(ParamType.Product, row.UrlPath, row.ProductId, string.Join("&", queryParams)));
        }

        private string GetProductName(ExportFeedYandexProduct row, bool vendorModel)
        {
            var result =
                vendorModel && !row.YandexModel.IsNullOrEmpty()
                    ? row.YandexModel
                    : row.YandexName.IsNotEmpty()
                        ? row.YandexName
                        : row.Name;

            if (_options.ColorSizeToName)
            {
                result +=
                    (!string.IsNullOrWhiteSpace(row.SizeName) ? " " + row.SizeName : string.Empty) +
                    (!string.IsNullOrWhiteSpace(row.ColorName) ? " " + row.ColorName : string.Empty);
            }

            if (_options.ConsiderMultiplicityInPrice && row.Multiplicity != 1)
            {
                result += ", " + row.Multiplicity.ToString(_nfi) + " " +
                          (row.Unit ?? LocalizationService.GetResource("Core.ExportImport.Yandex.DefaultUnitName"));
            }

            return result.XmlEncode().RemoveInvalidXmlChars();
        }

        public override void SetDefaultSettings()
        {
            var fileExtension = GetAvailableFileExtentions()[0];

            ExportFeedSettingsProvider.SetSettings(_exportFeedId,
                new ExportFeedSettings<ExportFeedYandexOptions>
                {
                    FileName = ExportFeedService.GetNewExportFileName(_exportFeedId, "export/yamarket", fileExtension),
                    FileExtention = fileExtension,
                    AdditionalUrlTags = string.Empty,
                    Interval = 1,
                    IntervalType = Core.Scheduler.TimeIntervalType.Days,
                    JobStartTime = new DateTime(2017, 1, 1, 1, 0, 0),
                    Active = false,

                    AdvancedSettings = new ExportFeedYandexOptions
                    {
                        CompanyName = "#STORE_NAME#",
                        ShopName = "#STORE_NAME#",
                        ProductDescriptionType = "short",
                        Currency = AvailableCurrencies[0],
                        GlobalDeliveryCost = "[]",
                        LocalDeliveryOption = "{\"Cost\":null,\"Days\":\"\",\"OrderBefore\":\"\"}",
                        RemoveHtml = true,
                        AllowPreOrderProducts = true,
                        Delivery = true,
                        TypeExportYandex = true,
                        ColorSizeToName = true,
                        ExportAllPhotos = true,
                        OfferIdType = "id",
                        VendorCodeType = "offerArtno"
                    },
                    ExportAdult = true
                });
        }

        public override object GetAdvancedSettings()
        {
            return ExportFeedSettingsProvider.GetAdvancedSettings<ExportFeedYandexOptions>(_exportFeedId);
        }

        public override List<string> GetAvailableVariables()
        {
            return new List<string>
                {"#STORE_NAME#", "#STORE_URL#", "#PRODUCT_NAME#", "#PRODUCT_ID#", "#PRODUCT_ARTNO#"};
        }

        public override List<string> GetAvailableFileExtentions() => new List<string> { "xml", "yml" };

        private string _colorHeader;
        private string ColorHeader => _colorHeader ??
                                      (_colorHeader = SettingsCatalog.ColorsHeader.XmlEncode().RemoveInvalidXmlChars());

        private string _sizeHeader;
        private string SizeHeader => _sizeHeader ??
                                     (_sizeHeader = SettingsCatalog.SizesHeader.XmlEncode().RemoveInvalidXmlChars());

        private PriceRule GetAutoRule(int priceRuleId)
        {
            var priceRule = PriceRuleService.Get(priceRuleId);

            return priceRule == null || !priceRule.CalculatePriceAutomatically
                ? null
                : priceRule;
        }
    }
}
