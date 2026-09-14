using AdvantShop.ExportImport;
using AdvantShop.Module.Rees46.Models;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Handlers
{
    public class GetExportFeedHandler
    {
        private readonly int? _id;

        public GetExportFeedHandler(int? id)
        {
            _id = id;
        }

        public ExportFeedModel Execute()
        {
            if (!_id.HasValue)
            {
                return null;
            }

            var exportFeed = ExportFeedService.GetExportFeed((int)_id);

            if (exportFeed == null)
            {
                return null;
            }

            var model = new ExportFeedModel
            {
                Id = exportFeed.Id,
                Name = exportFeed.Name,
                Description = exportFeed.Description,
                LastExport = exportFeed.LastExport,
                LastExportFileFullName = exportFeed.LastExportFileFullName,
                Type = exportFeed.FeedType,
                //HaveProducts = ExportXmlService.HaveSelectedProducts(exportFeed.Id)
            };             

            var handler = new GetExportFeedSettings(ExportFeedSettingsProvider.GetSettings(model.Id), exportFeed.FeedType);
            model.ExportFeedSettings = handler.Execute();

            return model;
        }
    }
}
