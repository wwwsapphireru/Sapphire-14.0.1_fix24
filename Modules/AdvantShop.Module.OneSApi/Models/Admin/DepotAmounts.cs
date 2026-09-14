using System;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public class DepotModel
    {
        public int DepotId { get; set; }

        public string Code { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Hint { get; set; }
        public int? DepartmentId { get; set; }

        public bool Active { get; set; }
        public int SortOrder { get; set; }
    }

    public class DepotsProductModel
    {
        public int ProductId { get; set; }
        public List<DepotProductModel> Amounts { get; set; }
    }

    public class DepotProductModel
    {
        public int DepotId { get; set; }

        //public string ArtNo { get; set; }
        //public string DepotCode { get; set; }
        public string DepotName { get; set; }
        public string DepotTitle { get; set; }

        public float Amount { get; set; }
    }

    public class ProductModel
    {
        public int ProductId { get; set; }
        public string ExternalId { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public bool PriceOnRequest { get; set; }
        public short? PriceNumber { get; set; }
    }

}
