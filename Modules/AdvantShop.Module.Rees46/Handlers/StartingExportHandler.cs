
using AdvantShop.ExportImport;
using AdvantShop.Module.Rees46.Service;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Handlers
{
    public class StartingExportHandler
    {
        private readonly int _exportFeedId;

        public StartingExportHandler(int exportFeedId)
        {
            _exportFeedId = exportFeedId;
        }

        public string Execute()
        {
            var exportFeed = ExportImport.ExportFeedService.GetExportFeed(_exportFeedId);

            return MakeExportFile(exportFeed);
        }

        private static string MakeExportFile(ExportFeed exportFeed)
        {
            var currentExportFeed = Service.ExportFeedService.GetExportFeedInstance("Rees46"/*exportFeed.Type*/, exportFeed.Id, true);
            var fileName = currentExportFeed.Export();

            if (exportFeed.FeedType != EExportFeedType.None)
                Track.TrackService.TrackEvent(Track.ETrackEvent.Shop_ExportFeeds_ExportManual, exportFeed.Type.ToString());

            return fileName;
        }
    }
}
