using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public class OrderViewModel
    {
        public string INN { get; set; }
        public string CompanyName { get; set; }
        public string KeepFreeUntil { get; set; }
        public string ManagerName { get; set; }
        public string ChequeUrl { get; set; }
        public string CardHolder { get; set; }
    }

    public class CustomerViewModel
    {
        public bool Enabled { get; set; }
        public string RegConfirmUrl { get; set; }
        public bool PhoneConfirmed { get; set; }
        public string AccountUrl { get; set; }
    }
}
