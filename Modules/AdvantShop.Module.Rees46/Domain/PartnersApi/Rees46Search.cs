using System.Collections.Generic;
//GlorySoft_010
namespace AdvantShop.Module.Rees46.Domain.PartnersApi
{
    public class Rees46Search
    {
        public int products_total { get; set; }
        public List<Rees46Search_product> products { get; set; }
    }
    public class Rees46Search_product
    {
        public string vendor_code { get; set; }
        public string id { get; set; }
        public string url_handle { get; set; }
    }
}
