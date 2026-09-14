using System;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class ClientDepotsProductModel
    {
        public int ProductId { get; set; }
        public List<ClientDepotProductModel> Amounts { get; set; }
        public DateTime? ExpectedDate { get; set; }
    }

    public class ClientDepotProductModel
    {
        public string DepotTitle { get; set; }

        public float Amount { get; set; }
        public string Unit { get; set; }
    }

}
