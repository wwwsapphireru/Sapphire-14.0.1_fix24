//GlorySoft_021
namespace AdvantShop.Module.Rees46.Models
{
    public class ExportCategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ParentCategory { get; set; }
        public string UrlPath { get; set; }
        public bool Enabled { get; set; }
    }

    public class ExportFeedSelectedCategory
    {
        public int ExportFeedId { get; set; }
        public int CategoryId { get; set; }
        public bool Opened { get; set; }
    }
}