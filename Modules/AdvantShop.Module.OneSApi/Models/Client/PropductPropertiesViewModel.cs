using System.Collections.Generic;
using AdvantShop.Catalog;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class PropductPropertiesViewModel 
    {
        public string CustomViewPath { get; set; }

        public List<PropertyValue> PropertyValues { get; set; }

        public bool ShowInPlaceEditor { get; set; }

        public bool RenderInplaceAddBlock { get; set; }

        public int ProductId { get; set; }

        public int CountVisible { get; set; }
    }
}