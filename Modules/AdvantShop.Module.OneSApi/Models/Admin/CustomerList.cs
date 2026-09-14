using AdvantShop.Web.Infrastructure.Admin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public partial class CustomerListFilterModel : BaseFilterModel
    {
        public Guid CustomerId { get; set; }
    }

    public partial class CustomerListModel
    {
        public Guid CustomerId { get; set; }
        public string FullName { get; set; }
        public DateTime Changed { get; set; }
    }
}
