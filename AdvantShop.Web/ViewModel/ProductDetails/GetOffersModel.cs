using System.Collections.Generic;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.Catalog.Warehouses;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Customers;
using AdvantShop.Orders;

namespace AdvantShop.ViewModel.ProductDetails
{
    public class GetOffersModel
    {
        public int StartOfferIdSelected { get; }
        public string Unit { get; }
        public bool ShowStockAvailability { get; }
        public bool AllowPreOrder { get; }
        public List<OfferItemModel> Offers { get; }

        public GetOffersModel(Product product, List<Offer> offers, int startOfferIdSelectedId, Customer customer, List<int> warehouseIds)
        {
            StartOfferIdSelected = startOfferIdSelectedId;
            Unit = product.Unit?.DisplayName;
            ShowStockAvailability = SettingsCatalog.ShowStockAvailability;
            AllowPreOrder = product.AllowPreOrder;

            var amountMinToBuy = product.GetMinAmount();
            var minimumOrderPrice = CustomerGroupService.GetMinimumOrderPrice(ShoppingCartService.CurrentShoppingCart);

            Offers = offers.Select(offer => new OfferItemModel(offer, product, amountMinToBuy, customer, minimumOrderPrice, warehouseIds)).ToList();
        }
    }

    public class OfferItemModel
    {
        public int OfferId { get; }
        public int ProductId { get; }
        public string ArtNo { get; }
        public Color Color { get; }
        public List<ProductPhoto> Photos { get; }
        public Size Size { get; }
        public string PreparedPrice { get; }
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
        public string WeightFormatted => Weight.ToString("0.###");
        public string WeightUnits { get; }
        public float Width { get; }
        public float Length { get; }
        public float Height { get; }

        public OfferItemModel(Offer offer, Product product, float amountMinToBuy, Customer customer,
            float minimumOrderPrice, List<int> warehouseIds)
        {
            var warehouseId = warehouseIds?.FirstOrDefault();

            var (oldPrice, finalPrice, finalDiscount, preparedPrice) =
                offer.GetOfferPricesWithPriceRule(amountMinToBuy, null, customer, null);
            
            var amountByMultiplicity = offer.GetAmountByMultiplicity(product.Multiplicity);
            var isAvailable = amountByMultiplicity > 0.0f;
            
            OfferId = offer.OfferId;
            ProductId = offer.ProductId;
            ArtNo = offer.ArtNo;
            Color = offer.Color;
            Photos = product.ProductPhotos.Where(x=> x.ColorID == offer.ColorID).ToList();
            Size = offer.Size;

            PreparedPrice = preparedPrice;
            RoundedPrice = finalPrice;
            Discount = finalDiscount;
            Amount = amountByMultiplicity > 0 ? offer.Amount : 0;
            AmountBuy = amountMinToBuy;
            Main = offer.Main;

            AllowBuyOutOfStockProducts = product.AllowBuyOutOfStockProducts();
            IsAvailable = isAvailable || AllowBuyOutOfStockProducts;

            var showCountWarehousesWithAvailable = 
                isAvailable 
                && SettingsCatalog.ShowAvailableInWarehouseInProduct 
                && (warehouseIds is null || warehouseIds.Count > 1);
            var warehouseAmount =
                showCountWarehousesWithAvailable
                    ? WarehouseStocksService.GetOfferStocks(offer.OfferId)
                                            .Where(x => warehouseIds is null || warehouseIds.Contains(x.WarehouseId))
                                            .Count(x => x.Quantity > 0f)
                    : 0;

            var availableInWarehouseText = warehouseAmount == 1 
                ? LocalizationService.GetResource("Product.OneAvailableInWarehouse") 
                : LocalizationService.GetResource("Product.AvailableInWarehouse");


            Available = string.Format("{0}{2}{1}",
                isAvailable || AllowBuyOutOfStockProducts
                    ? LocalizationService.GetResource("Product.Available")
                    : LocalizationService.GetResource("Product.NotAvailable"),
                isAvailable && SettingsCatalog.ShowStockAvailability
                    ? string.Format(
                        " (<div class=\"details-avalable-text\">{0}</div><div class=\"details-avalable-unit\">{1}</div>)",
                        StockLabelService.GetLabel(amountByMultiplicity),
                        (product.Unit?.DisplayName).IsNotEmpty() ? "&nbsp" + product.Unit?.DisplayName : "")
                    : string.Empty,
                showCountWarehousesWithAvailable
                    ? string.Format(availableInWarehouseText, warehouseAmount)
                    : string.Empty);

            if (product.AllowPreOrder && Amount > 0/*GlorySoft_022 (RoundedPrice <= 0 || Amount <= 0)*/)
            {
                IsAvailable = true;
                Available = LocalizationService.GetResource("Product.AvailablePreorder");
            }

            IsAvailableForPurchase = 
                offer.IsAvailableForPurchase(Amount, amountMinToBuy, finalPrice, finalDiscount, AllowBuyOutOfStockProducts);
            
            IsAvailableForPurchaseOnBuyOneClick =
                offer.IsAvailableForPurchaseOnBuyOneClick(IsAvailableForPurchase, finalPrice, minimumOrderPrice);

            (Weight, WeightUnits) = offer.GetWeightAndUnits();
            Width = offer.GetWidth();
            Length = offer.GetLength();
            Height = offer.GetHeight();
        }
    }
}