using System;
using System.Web;
using AdvantShop.Areas.Api.Models.Preorder;
using AdvantShop.Areas.Api.Services;
using AdvantShop.Catalog;
using AdvantShop.CMS;
using AdvantShop.Configuration;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Handlers.PreOrderProducts;
using AdvantShop.Helpers;
using AdvantShop.Models.PreOrder;
using AdvantShop.ViewModel.PreOrder;
using AdvantShop.Web.Infrastructure.Handlers;

namespace AdvantShop.Areas.Api.Handlers.Preorder
{
    public sealed class PreorderApi : AbstractCommandHandler<PreorderResponse>
    {
        private readonly PreorderModel _model;
        private Offer _offer;
        
        public PreorderApi(PreorderModel model)
        {
            _model = model;
        }

        protected override void Validate()
        {
            if (_model.Customer == null
                || _model.Customer.FirstName.IsNullOrEmpty()
                || _model.Customer.Email.IsNullOrEmpty()
                || _model.Customer.Phone.IsNullOrEmpty())
                throw new BlException("Заполните обязательные поля покупателя");

            if (!ValidationHelper.IsValidEmail(_model.Customer.Email))
                throw new BlException("Невалидный email");

            if (_model.IsAgree != SettingsCheckout.IsShowUserAgreementText)
                throw new BlException(T("Js.Subscribe.ErrorAgreement"));

            if (_model.Offer == null)
                throw new BlException("Товар не найден");
            
            _offer = OfferService.GetOffer(_model.Offer.OfferId);
            if (_offer == null)
                throw new BlException("Товар не найден");
            
            if (_model.Offer.Amount <= 0)
                throw new BlException(T("PreOrder.Index.CantBeOrdered"));
        }

        protected override PreorderResponse Handle()
        {
            var minAmount = _offer.Product.GetMinAmount();

            var amount = _model.Offer.Amount < minAmount || Math.Abs(_model.Offer.Amount % minAmount) > 0.1
                ? minAmount
                : _model.Offer.Amount;
            
            if (!_offer.IsAvailableForPreOrder(amount))
                throw new BlException(T("PreOrder.Index.CantBeOrdered"));
            
            var preOrderModel = new PreOrderViewModel
            {
                Email = HttpUtility.HtmlDecode(_model.Customer.Email),
                FirstName = HttpUtility.HtmlDecode(_model.Customer.FirstName),
                LastName = HttpUtility.HtmlDecode(_model.Customer.LastName),
                Phone = HttpUtility.HtmlDecode(_model.Customer.Phone),
                Comment = HttpUtility.HtmlDecode(_model.Customer.Comment),
                
                OfferId = _offer.OfferId,
                ProductId = _offer.ProductId,
                Offer = _offer,
                Amount = amount,
                OptionsHash = CustomOptionsHelper.GetCustomOptionsHash(_offer, _model.Offer.CustomOptions),
            };
            
            var result = new PreOrderHandler().Send(preOrderModel, _offer);
            if (!result)
                throw new BlException(T("PreOrder.Index.CantBeOrdered"));

            return new PreorderResponse()
            {
                SuccessText = StaticBlockService.GetPagePartByKeyWithCache("requestOnProductSuccess").Content
            };
        }
    }
}