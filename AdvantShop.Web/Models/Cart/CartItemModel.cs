using System.Collections.Generic;
using AdvantShop.Catalog;
using Newtonsoft.Json;

namespace AdvantShop.Models.Cart
{
    public class CartItemModel
    {
        public int OfferId { get; set; }
        public string Price { get; set; }
        public int? ProductId { get; set; }
        public string PriceWithDiscount { get; set; }
        public Discount Discount { get; set; }
        public string DiscountText { get; set; }
        public float Amount { get; set; }
        public string Sku { get; set; }
        public string PhotoPath { get; set; }
        public string PhotoSmallPath { get; set; }
        public string PhotoMiddlePath { get; set; }
        public string PhotoAlt { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Cost { get; set; }
        public string CostWithoutDiscount { get; set; }
        public int ShoppingCartItemId { get; set; }
        public List<EvaluatedCustomOptions> SelectedOptions { get; set; }
        
        [JsonIgnore]
        public string AttributesXml { get; set; }
        public string ColorName { get; set; }
        public string SizeName { get; set; }
        public string Avalible { get; set; }
        public float AvailableAmount { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public float Multiplicity { get; set; }
        public bool FrozenAmount { get; set; }
        public bool IsGift { get; set; }
        public string Unit { get; set; }
        public string PriceRuleName { get; set; }
        public string BriefDescription { get; set; }
        public bool InWishlist { get; set; }

        //GlorySoft_006
        public float PriceValue { get; set; }
        public Taxes.OrderTax Tax { get; set; }
    }
}