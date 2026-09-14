using AdvantShop.Core.Common.Attributes;
using System;

namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public class ImportProductSettingsModel
    {
        //public string ImportProductUrl { get; set; }
        public bool ImportHierarhy { get; set; }
        public bool ImportToDefaultCategory { get; set; }
        public int DefaultCategoryId { get; set; }
        //public string AppSecret { get; set; }
        //public string ApiToken { get; set; }
        //public string ProductIdType { get; set; }
        //public string OfferIdType { get; set; }
        //public string FreightTemplateId { get; set; }
        public bool JobClearLogs { get { return true; } }
        public int ClearLogsDays { get { return 7; } }
    }

    //public enum ImportCategoryType
    //{
    //    [Localize("Загружать из 1С")]
    //    From1C = 1,

    //    [Localize("Загружать в категорию")]
    //    Default = 2,
    //}

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
