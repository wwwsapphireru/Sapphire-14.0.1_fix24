using AdvantShop.Catalog;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Orders;
using AdvantShop.Shipping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public class ImportExportSettingsModel
    {
        public ImportProduct ImportProduct { get; set; }
        public bool CreateProducts { get; set; }
        public bool CreateProperties { get; set; }
        public ImportArtnoType ImportArtnoType { get; set; }
        public bool ImportHierarhy { get; set; }
        public bool ImportToDefaultCategory { get; set; }
        public int DefaultCategoryId { get; set; }
        public bool UpdateArtNo { get; set; }
        public bool UpdateName { get; set; }
        public bool UpdateBriefDescription { get; set; }
        public bool UpdateDescription { get; set; }
        public bool UpdateUnit { get; set; }
        public bool UpdateBrand { get; set; }
        public bool UpdateTax { get; set; }
        public bool AddCategory { get; set; }
        public bool UpdateCategory { get; set; }
        public bool UpdateProperties { get; set; }
        public bool UpdatePhotos { get; set; }
        public bool UpdateEnabled { get; set; }

        public ExportOrder ExportOrder { get; set; }
        public bool UseIn1C { get; set; }
        public int ItemsPerPage { get; set; }
        public bool ExportChangeStatus { get; set; }
        public bool ExportUpdated { get; set; }
        public bool ExportPayed { get; set; }

        public ImportOrder ImportOrder { get; set; }

        public ImportCustomer ImportCustomer { get; set; }

        public ExportProduct ExportProduct { get; set; }

        public bool JobClearLogs { get { return true; } }
        public int ClearLogsDays { get { return 7; } }
    }
    public class ImportProduct
    {
        public bool CreateProducts { get; set; }
        public bool CreateProperties { get; set; }

        public ImportArtnoType ImportArtnoType { get; set; }
        public ImportNameType ImportNameType { get; set; }
        public bool ImportHierarhy { get; set; }
        public bool ImportToDefaultCategory { get; set; }
        public int DefaultCategoryId { get; set; }

        public bool UpdateArtNo { get; set; }
        public bool UpdateName { get; set; }
        public bool UpdateBriefDescription { get; set; }
        public bool UpdateDescription { get; set; }
        public bool UpdateUnit { get; set; }
        public bool UpdateBrand { get; set; }
        public bool UpdateTax { get; set; }
        public bool UpdateDiscount { get; set; }
        public bool AddCategory { get; set; }
        public bool UpdateCategory { get; set; }
        public bool UpdateProperties { get; set; }
        public bool UpdatePhotos { get; set; }
        public bool UpdateEnabled { get; set; }

        public int? PropertySaleId { get; set; }
        private Property propertySale;
        public string PropertySaleName
        {
            get
            {
                if (!PropertySaleId.HasValue)
                    return null;
                if (propertySale == null)
                    propertySale = PropertyService.GetPropertyById(PropertySaleId.Value);
                if (propertySale != null)
                    return propertySale.Name;
                return null;
            }
        }
    }
    public class ExportOrder
    {
        public bool UseIn1C { get; set; }
        public int ItemsPerPage { get; set; }
        public bool ExportChangeStatus { get; set; }
        public bool ExportUpdated { get; set; }
        public bool ExportPayed { get; set; }

        public int StatusLead { get; set; }
        public int StatusRefusing { get; set; }

        [NonSerialized]
        private IEnumerable<object> orderStatusList;
        public IEnumerable<object> OrderStatusList
        {
            get
            {
                if (orderStatusList != null)
                    return orderStatusList;
                var tmp = OrderStatusService.GetOrderStatuses().OrderBy(x => x.SortOrder).ToList();
                tmp.Insert(0, new OrderStatus { StatusID = 0, StatusName = "не выбран" });
                orderStatusList = tmp.Select(x => new
                {
                    label = x.StatusName,
                    value = x.StatusID.ToString(),
                });
                return orderStatusList;
            }
        }
    }
    public class ImportOrder
    {
        public bool UpdateStatus { get; set; }
        public bool UpdatePayed { get; set; }
        //public bool UpdateCurrency { get; set; }
        public bool UpdateAdminComment { get; set; }
        public bool UpdateTracking { get; set; }
        //public bool UpdateOrderCustomer { get; set; }
        //public bool UpdateCustomer { get; set; }
        public bool UpdateItems { get; set; }

        public int StatusConfirmed { get; set; }
        public int StatusBuilding { get; set; }
        public int StatusShipped { get; set; }
        public int StatusReady { get; set; }
        public int StatusReadyAtStore { get; set; }
        public int StatusDone { get; set; }
        public int StatusWaiting { get; set; }
        public int StatusCancelled { get; set; }

        public int ShippingForTK { get; set; }
        public int ShippingUnknown { get; set; }

        public int TriggerCategoryId { get; set; }

        public int MinLength { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }

        [NonSerialized]
        private IEnumerable<object> orderStatusList;
        public IEnumerable<object> OrderStatusList
        {
            get
            {
                if (orderStatusList != null)
                    return orderStatusList;
                var tmp = OrderStatusService.GetOrderStatuses().OrderBy(x => x.SortOrder).ToList();
                tmp.Insert(0, new OrderStatus { StatusID = 0, StatusName = "не выбран" });
                orderStatusList = tmp.Select(x => new
                {
                    label = x.StatusName,
                    value = x.StatusID.ToString(),
                });
                return orderStatusList;
            }
        }

        [NonSerialized]
        private IEnumerable<object> shippingList;
        public IEnumerable<object> ShippingList
        {
            get
            {
                if (shippingList != null)
                    return shippingList;
                var tmp = ShippingMethodService.GetAllShippingMethods(true).OrderBy(x => x.SortOrder).ToList();
                tmp.Insert(0, new ShippingMethod { ShippingMethodId = 0, Name = "не выбран" });
                shippingList = tmp.Select(x => new
                {
                    label = x.Name,
                    value = x.ShippingMethodId.ToString(),
                });
                return shippingList;
            }
        }

        //[NonSerialized]
        //private IEnumerable<object> triggerCategoryList;
        //public IEnumerable<object> TriggerCategoryList
        //{
        //    get
        //    {
        //        if (triggerCategoryList != null)
        //            return triggerCategoryList;
        //        var tmp = TriggerCategoryService.GetList().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
        //        tmp.Insert(0, new TriggerCategory { Id = 0, Name = "не использовать" });
        //        triggerCategoryList = tmp.Select(x => new
        //        {
        //            label = x.Name,
        //            value = x.Id.ToString(),
        //        });
        //        return triggerCategoryList;
        //    }
        //}
    }
    public class ImportCustomer
    {
        public bool UpdateCustomers { get; set; }
        public bool AddCustomers { get; set; }
        public int CustomerGroupId { get; set; }
        public bool DeleteCustomers { get; set; }
        public string ManagerSignTemplate { get; set; }
    }
    public class ExportProduct
    {
        public bool ExportChangeDescription { get; set; }
    }

    public enum ImportArtnoType
    {
        [Localize("Артикул из 1С")]
        ArtNo = 1,

        [Localize("Код из 1С")]
        Code = 2,
    }

    public enum ImportNameType
    {
        [Localize("Рабочее наименование")]
        Name = 1,

        [Localize("Наименование для печати")]
        FullName = 2,
    }

    public class LogFile
    {
        public string Filename { get; set; }
        public string Folder { get; set; }
        public string Title
        {
            get { return DateTime.ParseExact(Filename, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss"); }
        }
        public long Size { get; set; }
    }

}
