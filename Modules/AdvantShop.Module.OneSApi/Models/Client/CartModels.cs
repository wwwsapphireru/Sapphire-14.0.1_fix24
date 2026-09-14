using AdvantShop.Catalog;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class CartCoupon
    {
        public string Price { get; set; }
        public string Percent { get; set; }
        public string Code { get; set; }
        public bool NotApplied { get; set; }
    }

    public class CartModel
    {
        public List<CartItemModel> CartProducts { get; set; }
        public string ColorHeader { get; set; }
        public string SizeHeader { get; set; }
        public string Count { get; set; }
        public float TotalItems { get; set; }
        public string BonusPlus { get; set; }
        public string Valid { get; set; }
        public bool CouponInputVisible { get; set; }
        public bool ShowConfirmButtons { get; set; }
        public bool ShowBuyInOneClick { get; set; }
        public string BuyInOneClickText { get; set; }
        public string TotalProductPrice { get; set; }
        public string TotalPrice { get; set; }
        public string DiscountPrice { get; set; }
        public string DiscountOnTotalPrice { get; set; }
        public string DiscountOnTotalPriceAmount { get; set; }
        public string Certificate { get; set; }
        public CartCoupon Coupon { get; set; }
        public bool MobileIsFullCheckout { get; set; }
        public bool EnablePhoneMask { get; set; }
        public bool IsDefaultCustomerGroup { get; set; }
        public string TotalWeight { get; set; }
        public string TaxesNames { get; set; }
        public string TaxesPrice { get; set; }
    }

    public class CartItemModel
    {
        public string Price { get; set; }
        public string PriceWithDiscount { get; set; }
        public Discount Discount { get; set; }
        public string DiscountText { get; set; }
        public float Amount { get; set; }
        public string Sku { get; set; }
        public string PhotoPath { get; set; }
        public string PhotoMiddlePath { get; set; }
        public string PhotoAlt { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Cost { get; set; }
        public int ShoppingCartItemId { get; set; }
        public List<EvaluatedCustomOptions> SelectedOptions { get; set; }
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
        public int OfferId { get; set; }//GlorySoft_018
        public float PriceValue { get; set; }//GlorySoft_018
        public Taxes.OrderTax Tax { get; set; }
    }

}