using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdvantShop.Module.Rees46.Models
{
    public class SettingsModel : IValidatableObject
    {
        public bool RegisteredShop { get; set; }

        public bool ShowRegistration { get; set; }

        #region MainSettings

        // general settings
        public string ShopKey { get; set; }

        public int Limit { get; set; }

        public bool UseSuggestionsInSearch { get; set; }

        public string SettingsUrl { get; set; }

        // Api
        public string RelatedProductCode { get; set; }

        public string AlternativeProductCode { get; set; }

        public string MainPageCode { get; set; }

        public string CatalogTopCode { get; set; }

        public string CatalogBottomCode { get; set; }

        public string CartCode { get; set; }

        #endregion

        //GlorySoft_021
        public int FeedId { get; set; }
        public bool Shedule { get; set; }

        #region IValidatableObject

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(ShopKey))
                yield return new ValidationResult("Укажите код магазина в Rees46");
        }

        #endregion
    }
}
