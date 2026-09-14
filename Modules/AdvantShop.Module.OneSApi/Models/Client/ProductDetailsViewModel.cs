using System;
using System.Collections.Generic;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Configuration;
using AdvantShop.Core.Services.Landing.Forms;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class ProductDetailsViewModel 
    {
        public string CustomViewPath { get; set; }

        public ProductDetailsViewModel()
        {
            FinalDiscount = new Discount();
        }

        public Product Product { get; set; }

        public Offer Offer { get; set; }

        public List<BreadCrumbs> BreadCrumbs { get; set; }

        public bool IsAdmin { get; set; }
        
        public string Availble { get; set; }

        public bool IsAvailable { get; set; }
        
        public bool IsAvailableForPurchase { get; set; }
        public bool IsAvailableForPurchaseOnCredit { get; set; }
        public bool IsAvailableForPurchaseOnBuyOneClick { get; set; }

        public string Shippings { get; set; }

        public int FirstPaymentId { get; set; }

        public float FirstPayment { get; set; }

        public string FirstPaymentPrice { get; set; }

        public float FirstPaymentMinPrice { get; set; }
        public float? FirstPaymentMaxPrice { get; set; }

        public float MinimumOrderPrice { get; set; }

        public bool ShowAddButton { get; set; }

        public bool ShowPreOrderButton { get; set; }

        public bool ShowCreditButton { get; set; }

        public bool ShowBuyOneClick { get; set; }
        public bool ShowMarketplaceButton { get; set; }

        public float FinalPrice { get; set; }

        public Discount FinalDiscount { get; set; }

        public string PreparedPrice { get; set; }

        public string BonusPrice { get; set; }
        
        public bool AllowReviews { get; set; }
        public string ReviewsCount { get; set; }
        public int ReviewsCountInt { get; set; }
        public int AllReviewsCountInt { get; set; }
        public bool AllowAddReviews { get; set; }
        public bool UseStandartReviews { get; set; }

        public List<PropertyValue> ProductProperties { get; set; }

        public List<PropertyValue> BriefProperties { get; set; }

        public bool RatingReadOnly { get; set; }

        public SettingsDesign.eShowShippingsInDetails ShowShippingsMethods { get; set; }

        public bool RenderShippings { get; set; }
        
        public bool ShowBriefDescription { get; set; }

        public bool ShowDescription { get; set; }

        public bool HasCustomOptions { get; set; }

        public List<GiftModel> Gifts { get; set; }

        public List<MicrodataOffer> MicrodataOffers { get; set; }

        public MicrodataAggregateOffer MicrodataAggregateOffer { get; set; }

        public int? ColorId { get; set; }

        public int? SizeId { get; set; }

        public string PreOrderButtonText { get; set; }

        public bool AllowBuyOutOfStockProducts { get; set; }

        public bool HidePrice { get; set; }

        public string TextInsteadOfPrice { get; set; }
        public int? LandingId { get; set; }
        public bool HideShipping { get; set; }
        public bool? ShowLeadButton { get; set; }
        public int? BlockId { get; set; }
        public bool? ShowVideo { get; set; }

        public string ReturnUrl { get; set; }
        public bool UseHistoryApiForBack { get; set; }
        public LpButton LpButton { get; set; }

        public bool ShowButton2 { get; set; }
        public LpButton LpButton2 { get; set; }
        public string DescriptionMode { get; set; }
        public bool ProductDetails { get; set; }
        
        public float MinAmount { get; set; }
        [Obsolete]
        public bool ShowAvailableLable { get; set; }
        public string CreditButtonText { get; set; }
        
        public bool LpShowPrice { get; set; }
        public bool ShowAvaliableLabelInProduct { get; set; }
        public bool ShowNotAvaliableLabelInProduct { get; set; }

        public bool AllowProductTabs { get; set; }
        public List<ProductTabsLink> ProductTabsLinks { get; set; }
    }

    public class MicrodataOffer
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public bool Available { get; set; }
        public int? ColorId { get; set; }
        public int? SizeId { get; set; }
        public string Currency { get; set; }
    }

    public class MicrodataAggregateOffer
    {
        public string HighPrice { get; set; }
        public string LowPrice { get; set; }
        public string Currency { get; set; }
    }

    public class ProductTabsLink
    {
        public string Title { get; set; }
        public string TabId { get; set; }
    }
}