using System;
using System.Collections.Generic;
using System.Linq;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Common;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Catalog;
using AdvantShop.Customers;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Orders;
using AdvantShop.Repository;
using AdvantShop.Repository.Currencies;
using AdvantShop.Shipping;
using Newtonsoft.Json;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Product
{
    public class GetCheckoutShippings
    {
        private readonly GetCheckoutShippingsRequest _request;
        private readonly OrderData _orderData;
        //private readonly List<PreOrderItemV8> _preorderList;
        private CalculationVariants? _typeCalculationVariants;
        private bool _withStatisctic;

        public GetCheckoutShippings(GetCheckoutShippingsRequest request, bool withStatisctic, CalculationVariants? typeCalculationVariants = null)
        {
            _request = request;
            _orderData = request.OrderData;
            _withStatisctic = withStatisctic;
            _typeCalculationVariants = typeCalculationVariants;
        }
        
        public GetCheckoutShippingsResponse Execute()
        {
            var options = new List<BaseShippingOption>();

            //var current = MyCheckout.Factory(CustomerContext.CustomerId);

            //_typeCalculationVariants = GetCalculationVariants(_typeCalculationVariants, current);

            var cust = new OrderCustomer
            {
                City = _orderData.City,
                Country = _orderData.Country ?? CountryService.GetCountry(SettingsMain.SellerCountryId).Name,
                District = _orderData.District,
                Region = _orderData.Region,
                Zip = _orderData.Zip
            };

            var items = new List<OrderItem>();
            foreach (var pre in _request.PreOrderList)
            {
                var productId = ImportService.GetIdByExternalId(pre.ExternalId, "Product");
                var product = ProductService.GetProduct(productId);
                if (product == null)
                    product = ProductService.GetProduct(pre.ArtNo, true);
                if (product == null)
                    continue;
                var offer = OfferService.GetMainOfferForExport(product.ProductId);
                items.Add(new OrderItem
                {
                    ProductID = product.ProductId,
                    Amount = pre.Amount,
                    Price = pre.Price,
                    Weight = offer.GetWeight(),
                    Length = offer.GetLength(),
                    Width = offer.GetWidth(),
                    Height = offer.GetHeight()
                });
            }

            var settings = ModuleService.GetImportExportSettings();
            var dimensions = items.Select(item => new Measure
            {
                XYZ = new[]
                {
                    item.Length,
                    item.Width,
                    item.Height,
                },
                Amount = item.Amount
            }).ToList();
            var order = new Order
            {
                OrderCustomer = cust,
                OrderCurrency = CurrencyService.GetAllCurrencies(true).FirstOrDefault(x => x.Iso3.Equals(SettingsCatalog.DefaultCurrencyIso3, StringComparison.OrdinalIgnoreCase)),
                TotalWeight = items.Sum(x => x.Weight * x.Amount),
                //TotalLength = dimensions.Sum(x => x.XYZ[0] * x.Amount),
                //TotalWidth = dimensions.Sum(x => x.XYZ[1] * x.Amount),
                //TotalHeight = dimensions.Sum(x => x.XYZ[2] * x.Amount),
                Sum = items.Sum(x => x.Price * x.Amount),
                ShippingMethodId = settings.ImportOrder.ShippingUnknown,

                OrderItems = items
            };

            var totalDimensions = OrderServiceV8.CheckNullDimensions(order);
            if (totalDimensions != null)
            {
                order.TotalLength = totalDimensions[0];
                order.TotalWidth = totalDimensions[1];
                order.TotalHeight = totalDimensions[2];
            }

            options = /*current.*/AvailableShippingOptions(order/*_preorderList, _typeCalculationVariants*/);

            var result = new GetCheckoutShippingsResponse
            {
                option = options,
                //typeCalculationVariants = _typeCalculationVariants?.StrName()
            };

            if (_withStatisctic)
                ModuleStatistic.RowPosition++;

            ModuleService.WriteLog("CalculateShippings", JsonConvert.SerializeObject(_request, Formatting.Indented), JsonConvert.SerializeObject(result, Formatting.Indented));

            return result;
        }

        //private CalculationVariants? GetCalculationVariants(CalculationVariants? typeCalculationVariants, MyCheckout current)
        //{
        //    if (!SettingsCheckout.SplitShippingByType)
        //        return null;

        //    if (typeCalculationVariants == null || typeCalculationVariants == CalculationVariants.All)
        //    {
        //        return 
        //            current.Data?.TypeCalculationVariants != null && current.Data.TypeCalculationVariants != CalculationVariants.All
        //                ? current.Data.TypeCalculationVariants.Value
        //                : CalculationVariants.PickPoint;
        //    }

        //    return typeCalculationVariants;
        //}

        private List<BaseShippingOption> AvailableShippingOptions(Order order)
        {
            //var ignoreCartItems = ModulesExecuter.GetIgnoreShippingCartItems();
            //if (ignoreCartItems.Count > 0)
            //{
            //    foreach (var item in ignoreCartItems)
            //        Cart.Remove(item);
            //}

            var configuratorShippingCalculation =
                ShippingCalculationConfigurator.Configure()
                                               .ByOrder(order);
                                               //.WithCurrency(CurrencyService.CurrentCurrency);
            //if (preorderList != null)
            //    configuratorShippingCalculation
            //       .ByShoppingCart(null)
            //       .WithPreOrderItems(preorderList);

            var shippingManager = new ShippingManager(configuratorShippingCalculation.Build());
            return //typeCalculationVariants.HasValue
                //? shippingManager.GetOptions(typeCalculationVariants.Value)
                /*:*/ shippingManager.GetOptions();
        }

        //private BaseFabricCalculationParameters ByV8(IConfiguratorShippingCalculation myCheckout)
        //{
        //    ClearByObjects();
        //    myCheckout = myCheckout ?? throw new ArgumentNullException(nameof(myCheckout));

        //    WithCountry(myCheckout.Data?.Contact?.Country);
        //    WithRegion(myCheckout.Data?.Contact?.Region);
        //    WithDistrict(myCheckout.Data?.Contact?.District);
        //    WithCity(myCheckout.Data?.Contact?.City);
        //    WithZip(myCheckout.Data?.Contact?.Zip);
        //    WithStreet(myCheckout.Data?.Contact?.Street);
        //    WithHouse(myCheckout.Data?.Contact?.House);
        //    WithStructure(myCheckout.Data?.Contact?.Structure);
        //    WithApartment(myCheckout.Data?.Contact?.Apartment);
        //    WithEntrance(myCheckout.Data?.Contact?.Entrance);
        //    WithFloor(myCheckout.Data?.Contact?.Floor);

        //    WithCustomerType(myCheckout.Data?.User.CustomerType);

        //    WithShippingOption(myCheckout.Data?.SelectShipping);
        //    WithPaymentOption(myCheckout.Data?.SelectPayment);

        //    WithBonusCard(myCheckout.Data?.User?.BonusCardId);
        //    if (myCheckout.Data?.Bonus?.UseIt is true) BonusUse(myCheckout.Data.Bonus.AppliedBonuses);

        //    if (myCheckout.Cart != null) ByShoppingCart(myCheckout.Cart);

        //    return this;
        //}

    }
}