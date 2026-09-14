using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using AdvantShop.Core.Scheduler;
using AdvantShop.Core.Services.Localization;
using AdvantShop.ExportImport;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Models
{
    public class ExportFeedSettingsModel : IValidatableObject
    {
        public int ExportFeedId { get; set; }

        public EExportFeedType ExportFeedType { get; set; }
        public string FileName { get; set; }
        public string FileExtention { get; set; }
        public float PriceMarginInPercents { get; set; }
        public float PriceMarginInNumbers { get; set; }
        public string AdditionalUrlTags { get; set; }

        public bool NotAvailableJob { get; set; }
        public bool Active { get; set; }
        public TimeIntervalType IntervalType { get; set; }
        public Dictionary<TimeIntervalType, string> IntervalTypeList { get; set; }
        public int Interval { get; set; }
        //public DateTime JobStartTime { get; set; }

        public int JobStartHour { get; set; }
        public int JobStartMinute { get; set; }

        [Obsolete]
        public bool ExportAllProducts { get; set; }

        public EExportFeedCatalogType ExportCatalogType { get; set; }

        public object AdvancedSettings { get; set; }
        public object AdvancedSettingsModel { get; set; }

        public Dictionary<string, string> FileExtentions { get; set; }

        public bool DoNotExportAdult { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                yield return new ValidationResult(LocalizationService.GetResource("Admin.Category.AdminCategoryModel.Error.Name"), new[] { "FileName" });
            }

            if (Interval < 1 && Active)
            {
                yield return new ValidationResult(LocalizationService.GetResource("Admin.Category.AdminCategoryModel.Error.Interval"), new[] { "Interval" });
            }
        }
    }
}
