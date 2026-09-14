using AdvantShop.Catalog;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.InplaceEditor;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Module.OneSApi.Service;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class InplaceProductHandler
    {
        public bool Execute(int id, string content, ProductInplaceField field)
        {
            Debug.Log.Info("InplaceProductHandler");
            //var product = ProductService.GetProduct(id);
            //if (product == null)
            //    return false;
            var modifiedBy = CustomerContext.CustomerId.ToString();

            switch (field)
            {
                //case ProductInplaceField.ArtNo:
                //    if (ProductService.IsUniqueArtNo(content))
                //    {
                //        product.ArtNo = content;
                //        return false;
                //    }
                //    break;
                case ProductInplaceField.Description:
                    if (content.IsLongerThan(ProductService.MaxDescLength))
                        return false;
                    ProductServiceV8.UpdateProductDescription(id, content, modifiedBy);
                    break;
                //case ProductInplaceField.BriefDescription:
                //    if (content.IsLongerThan(ProductService.MaxDescLength))
                //        return false;
                //    product.BriefDescription = content;
                //    break;
                // case ProductInplaceField.Unit:
                //     product.UnitId = content;
                //     break;
                //case ProductInplaceField.Weight:
                //    product.Weight = content.TryParseFloat();
                //    break;
                default:
                    return false;
            }

            return true;
        }
    }
}