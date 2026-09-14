using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class GetOffersModel
    {
        public int StartOfferIdSelected { get; }
        public string Unit { get; }
        public bool ShowStockAvailability { get; }
        public bool AllowPreOrder { get; }
        public List<OfferItemModel> Offers { get; }

        public GetOffersModel(Product product, List<Offer> offers, int startOfferIdSelectedId, Customer customer)
        {
            StartOfferIdSelected = startOfferIdSelectedId;
            Unit = product.Unit?.DisplayName;
            ShowStockAvailability = SettingsCatalog.ShowStockAvailability;
            AllowPreOrder = product.AllowPreOrder;

            var amountMinToBuy = product.GetMinAmount();
            var minimumOrderPrice = CustomerGroupService.GetMinimumOrderPrice();

            Offers = offers.Select(offer => new OfferItemModel(offer, product, amountMinToBuy, customer, minimumOrderPrice)).ToList();
        }
    }

    public class OfferItemModel
    {
        public int OfferId { get; }
        public int ProductId { get; }
        public string ArtNo { get; }
        public Color Color { get; }
        public Size Size { get; }
        public float RoundedPrice { get; }
        public Discount Discount { get; }
        public float Amount { get; }
        public float AmountBuy { get; }
        public bool Main { get; }
        public bool IsAvailable { get; }
        public string Available { get; }
        public bool AllowBuyOutOfStockProducts { get; }
        public bool IsAvailableForPurchase { get; }
        public bool IsAvailableForPurchaseOnBuyOneClick { get; }
        public float Weight { get; }
        public float Width { get; }
        public float Length { get; }
        public float Height { get; }

        public OfferItemModel(Offer offer, Product product, float amountMinToBuy, Customer customer, float minimumOrderPrice)
        {
            var (oldPrice, finalPrice, finalDiscount, preparedPrice) =
                offer.GetOfferPricesWithPriceRule(amountMinToBuy, null, customer, null);
            
            var amountByMultiplicity = offer.GetAmountByMultiplicity(product.Multiplicity);
            var isAvailable = amountByMultiplicity > 0.0f;
            
            OfferId = offer.OfferId;
            ProductId = offer.ProductId;
            ArtNo = offer.ArtNo;
            Color = offer.Color;
            Size = offer.Size;
            RoundedPrice = finalPrice;
            Discount = finalDiscount;
            Amount = amountByMultiplicity > 0 ? offer.Amount : 0;
            AmountBuy = amountMinToBuy;
            Main = offer.Main;

            AllowBuyOutOfStockProducts = product.AllowBuyOutOfStockProducts();
            IsAvailable = isAvailable || AllowBuyOutOfStockProducts;
            
            Available = string.Format("{0}{2}{1}",
                isAvailable || AllowBuyOutOfStockProducts
                    ? LocalizationService.GetResource("Product.Available")
                    : LocalizationService.GetResource("Product.NotAvailable"),
                isAvailable && SettingsCatalog.ShowStockAvailability
                    ? string.Format(
                        " (<div class=\"details-avalable-text\">{0}</div><div class=\"details-avalable-unit\">{1}</div>)",
                        StockLabelService.GetLabel(amountByMultiplicity),
                        (product.Unit?.DisplayName).IsNotEmpty() ? " " + product.Unit?.DisplayName : "")
                    : string.Empty,
                offer.Amount > 0 && SettingsCatalog.ShowAvailableInWarehouseInProduct
                    ? string.Format(LocalizationService.GetResource("Product.AvailableInWarehouse"),
                        WarehouseStocksService.GetOfferStocks(offer.OfferId).Count(_ => _.Quantity > 0f))
                    : string.Empty);

            if (product.AllowPreOrder && Amount > 0/*GlorySoft_063 (RoundedPrice <= 0 || Amount <= 0)*/)
            {
                IsAvailable = true;
                Available = LocalizationService.GetResource("Product.AvailablePreorder");
            }

            IsAvailableForPurchase = 
                offer.IsAvailableForPurchase(Amount, amountMinToBuy, finalPrice, finalDiscount, AllowBuyOutOfStockProducts);
            
            IsAvailableForPurchaseOnBuyOneClick =
                offer.IsAvailableForPurchaseOnBuyOneClick(IsAvailableForPurchase, finalPrice, minimumOrderPrice);

            
            Weight = offer.GetWeight();
            Width = offer.GetWidth();
            Length = offer.GetLength();
            Height = offer.GetHeight();
        }
    }
}