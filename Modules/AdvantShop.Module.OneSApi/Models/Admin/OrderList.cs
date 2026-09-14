using AdvantShop.Web.Infrastructure.Admin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public partial class OrderListFilterModel : BaseFilterModel
    {
        public int OrderId { get; set; }
        public int ExportType { get; set; }
    }

    public partial class OrderListModel
    {
        public int OrderId { get; set; }
        public int ExportType { get; set; }
        public string Number { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime Changed { get; set; }
        public string ChangeType { get; set; }
    }
}
