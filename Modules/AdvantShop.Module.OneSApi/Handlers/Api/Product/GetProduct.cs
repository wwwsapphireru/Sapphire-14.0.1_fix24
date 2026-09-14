using AdvantShop.Catalog;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Handlers;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Product
{
    public class GetProduct : ICommandHandler<string, ProductExportModel>
    {
        public ProductExportModel Execute(string externalId)
        {
            var productId = ImportService.GetIdByExternalId(externalId, "Product");
            var product = ProductService.GetProduct(productId);

            if (product == null)
                throw new BlException("Товар не найден");

            return ProductExportModel.FromProduct(product);
        }
    }
}