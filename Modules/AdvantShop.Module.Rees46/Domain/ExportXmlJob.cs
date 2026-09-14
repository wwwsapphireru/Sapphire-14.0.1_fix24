using AdvantShop.Core.Modules;
using AdvantShop.Core.Scheduler;
using AdvantShop.Diagnostics;
using AdvantShop.ExportImport;
using AdvantShop.Module.Rees46.Handlers;
using AdvantShop.Statistic;
using Quartz;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Domain
{
    [DisallowConcurrentExecution]
    public class ExportXmlJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            if (context != null)
            {
                if (!Rees46Settings.Shedule)
                {
                    return;
                }
                if (!context.CanStart())
                {
                    Debug.Log.Info("Rees46 ExportXmlJob !CanStart");
                    return;
                }
                context.WriteLastRun();
            }

            Debug.Log.Info("Rees46 ExportXmlJob start");

            var id = Rees46Settings.FeedId;
            var exportFeed = ExportFeedService.GetExportFeed(id);
            if (Check(exportFeed) != string.Empty)
                return;

            var model = new GetExportFeedHandler(id).Execute();

            var filePath = string.Empty;

            CommonStatistic.StartNew(() => {
                filePath = new StartingExportHandler(id).Execute();
                CommonStatistic.FileName = "../" + filePath;
            },
            exportFeed.LastExportFileFullName,
            exportFeed.Name);

            Debug.Log.Info("Rees46 ExportXmlJob done");
        }

        public string Check(ExportFeed exportFeed)
        {
            if (exportFeed == null)
            {
                var err = "Rees46 ExportXmlJob exportFeed == null";
                Debug.Log.Warn(err);
                return err;
            }

            if (CommonStatistic.IsRun)
            {
                var err = "Rees46 ExportXmlJob CommonStatistic.IsRun";
                Debug.Log.Warn(err);
                return err;
            };

            return string.Empty;
        }

    }
}