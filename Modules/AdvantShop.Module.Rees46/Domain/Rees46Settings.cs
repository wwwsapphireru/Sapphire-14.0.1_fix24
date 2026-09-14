using AdvantShop.Core.Modules;

namespace AdvantShop.Module.Rees46.Domain
{
    public class Rees46Settings
    {
        private const string ModuleStringId = "Rees46";

        public static string ShopKey
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("ShopKey", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("ShopKey", value, ModuleStringId); }
        }

        public static int Limit
        {
            get { return ModuleSettingsProvider.GetSettingValue<int>("Limit", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("Limit", value, ModuleStringId); }
        }

        public static string PathFilePushSW
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("PathFilePushSW", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("PathFilePushSW", value, ModuleStringId); }
        }

        public static string SecretKey
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("SecretKey", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("SecretKey", value, ModuleStringId); }
        }


        /* New api */
        public static string RelatedProductCode
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("RelatedProductCode", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("RelatedProductCode", value, ModuleStringId); }
        }

        public static string AlternativeProductCode
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("AlternativeProductCode", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("AlternativeProductCode", value, ModuleStringId); }
        }
        
        public static string MainPageCode
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("MainPageCode", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("MainPageCode", value, ModuleStringId); }
        }

        public static string CatalogTopCode
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("CatalogTopCode", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("CatalogTopCode", value, ModuleStringId); }
        }

        public static string CatalogBottomCode
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("CatalogBottomCode", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("CatalogBottomCode", value, ModuleStringId); }
        }

        public static string CartCode
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("CartCode", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("CartCode", value, ModuleStringId); }
        }

        public static bool UseSuggestionsInSearch
        {
            get { return ModuleSettingsProvider.GetSettingValue<bool>("UseSuggestionsInSearch", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("UseSuggestionsInSearch", value, ModuleStringId); }
        }
        
        public static string Url
        {
            get { return ModuleSettingsProvider.GetSettingValue<string>("Url", ModuleStringId)?.TrimEnd('/'); }
            set { ModuleSettingsProvider.SetSettingValue("Url", value, ModuleStringId); }
        }

        public static int FeedId//GlorySoft_021
        {
            get { return ModuleSettingsProvider.GetSettingValue<int>("FeedId", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("FeedId", value, ModuleStringId); }
        }

        public static bool Shedule//GlorySoft_021
        {
            get { return ModuleSettingsProvider.GetSettingValue<bool>("Shedule", ModuleStringId); }
            set { ModuleSettingsProvider.SetSettingValue("Shedule", value, ModuleStringId); }
        }

    }
}