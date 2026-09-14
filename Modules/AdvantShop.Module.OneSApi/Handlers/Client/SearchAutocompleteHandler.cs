using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Core.Services.FullSearch;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class SearchAutocompleteHandler
    {
        private readonly string _q;
        private readonly ISearchService _searchService;
        private readonly UrlHelper _urlHelper;

        public SearchAutocompleteHandler(string q)
        {
            _q = q;
            _searchService = new SearchService();
            _urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

        }

        public List<object> GetCategories()
        {
            if (!SettingsCatalog.SearchByCategories)
                return new List<object>();

            var tanslitQ = StringHelper.TranslitToRusKeyboard(_q);

            var categoryIds = CategorySeacher.Search(_q).SearchResultItems.Select(x => x.Id).ToList();
            var translitCategoryIds = CategorySeacher.Search(tanslitQ).SearchResultItems.Select(x => x.Id).ToList();

            var cats =
                categoryIds.Union(translitCategoryIds)
                    .Distinct()
                    .Select(CategoryService.GetCategory)
                    .Where(x => x != null && x.Enabled && x.ParentsEnabled)
                    .Take(5)
                    .ToList();

            var categories = new List<object>();
            categories.AddRange(cats.Select(x => new
            {
                Name = x.Name,
                Photo = x.Icon != null ? x.Icon.IconSrc() : "",
                Url = _urlHelper.AbsoluteRouteUrl("Category", new { url = x.UrlPath }),
                Template = "scripts\\_common\\autocompleter\\templates\\categories.html"
            }));

            return categories;
        }

        public List<object> GetProducts()
        {
            var resultIds = _searchService.Find(_q).SearchResultItems.Select(x => x.Id).Take(10)
                .Aggregate("", (current, item) => current + (item + "/"));

            //for preprare discount
            var productModels = ProductServiceV8.GetForAutoCompleteProducts(resultIds);

            // selected offer
            var offer = OfferService.GetOffer(_q);
            ProductModel selectedProduct;
            if (offer != null && (selectedProduct = productModels.Find(x => x.ProductId == offer.ProductId)) != null)
            {
                selectedProduct.BasePrice = offer.BasePrice;
                selectedProduct.Amount = offer.Amount;
                if (offer.ColorID.HasValue && offer.Photo != null && offer.Photo.PhotoName.IsNotEmpty())
                    selectedProduct.Photo = offer.Photo;
            }

            var tempModel = new ProductViewModel(productModels);

            var products = new List<object>();
            products.AddRange(tempModel.Products.Select(x => new
            {
                ArtNo = x.ArtNo,
                Name = x.Name,
                Photo = x.Photo.ImageSrcXSmall(),
                Photo2x = x.Photo.ImageSrcSmall(),
                Photo3x = x.Photo.ImageSrcMiddle(),
                Amount = x.Amount,
                Price = tempModel.HidePrice ? SettingsCatalog.TextInsteadOfPrice : x.PreparedPrice,
                Rating = x.ManualRatio ?? x.Ratio,
                Url = _urlHelper.AbsoluteRouteUrl("Product", new 
                    {
                        url = x.UrlPath, 
                        color = offer != null && offer.ProductId == x.ProductId ? offer.ColorID : null, 
                        size = offer != null && offer.ProductId == x.ProductId ? offer.SizeID : null 
                    }),
                Template = "Templates\\Sapphire\\scripts\\_common\\autocompleter\\templates\\products.html",
                Gifts = x.Gifts
            }));

            return products;
        }
    }
}