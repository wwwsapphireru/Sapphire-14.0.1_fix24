using AdvantShop.Web.Infrastructure.Admin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public partial class ProductListFilterModel : BaseFilterModel
    {
        public int OrderId { get; set; }
        public int ExportType { get; set; }
    }

    public partial class ProductListModel
    {
        public int ProductId { get; set; }
        public int ExportType { get; set; }
        public string ArtNo { get; set; }
        public string Name { get; set; }
        public DateTime Changed { get; set; }
        public string ChangeType { get; set; }
        public DateTime? Exported { get; set; }
    }
}
