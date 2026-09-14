using System.Collections.Generic;
using AdvantShop.Catalog;
using AdvantShop.Models.Cart;

namespace AdvantShop.Models.Checkout
{
    public class CheckoutCartItem
    {
        public string Name { get; set; }
        public float Amount { get; set; }
        public string Price { get; set; }
        public string Link { get; set; }
        public string Cost { get; set; }
        public string CostWithoutDiscount { get; set; }
        public Discount Discount { get; set; }
        public List<EvaluatedCustomOptions> SelectedOptions { get; set; }
        public string ColorName { get; set; }
        public string SizeName { get; set; }
        public string PriceRuleName { get; set; }
        public string PhotoPath { get; set; }
        public string PhotoSmallPath { get; set; }
        public string PhotoMiddlePath { get; set; }
        public string PhotoAlt { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public string Avalible { get; set; }
        public float AvailableAmount { get; set; }
        public float Multiplicity { get; set; }
        public bool FrozenAmount { get; set; }
        public int ShoppingCartItemId { get; set; }
        public bool IsGift { get; set; }
        public string Unit { get; set; }
        public string BriefDescription { get; set; }
        
        public int OfferId { get; set; }
        
        public int ProductId { get; set; }
    }

    public class CheckoutCartParam
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class CheckoutCartModel
    {
        public List<CheckoutCartItem> Items { get; set; }

        public string Cost { get; set; }

        public string Delivery { get; set; }

        public CheckoutCartParam Payment { get; set; }

        public CheckoutCartParam Discount { get; set; }

        public string Certificate { get; set; }

        public CartCoupon Coupon { get; set; }

        public List<CheckoutCartParam> Taxes { get; set; }

        public string Result { get; set; }

        public bool BuyOneClickEnabled { get; set; }

        public bool ShowInCart { get; set; }

        public string Bonuses { get; set; }

        public string BonusPlus { get; set; }

        public string ColorHeader { get; set; }

        public string SizeHeader { get; set; }
        public string Valid { get; set; }
        public float TotalItems { get; set; }

        public CheckoutCartParam DiscountOnTotalPrice { get; set; }//GlorySoft_026
    }
}