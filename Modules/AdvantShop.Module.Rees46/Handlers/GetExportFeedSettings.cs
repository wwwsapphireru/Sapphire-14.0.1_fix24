using System;
using System.Collections.Generic;
using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Scheduler;
using AdvantShop.ExportImport;
using AdvantShop.Module.Rees46.Models;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Handlers
{
    public class GetExportFeedSettings
    {
        private ExportFeedSettings _exportFeedSettings;
        private EExportFeedType _exportFeedType;

        public GetExportFeedSettings(ExportFeedSettings exportFeedSettings, EExportFeedType exportFeedType)
        {
            _exportFeedSettings = exportFeedSettings;
            _exportFeedType = exportFeedType;
        }

        public ExportFeedSettingsModel Execute()
        {
            var intervalTypeList = new Dictionary<TimeIntervalType, string>();
            foreach (TimeIntervalType timeIntervalType in Enum.GetValues(typeof(TimeIntervalType)))
            {
                if (timeIntervalType == TimeIntervalType.Minutes || timeIntervalType == TimeIntervalType.None)
                {
                    continue;
                }
                intervalTypeList.Add(timeIntervalType, timeIntervalType.Localize());
            }

            var currentExportFeed = ExportFeedService.GetExportFeedInstance(_exportFeedType.ToString(), _exportFeedSettings.ExportFeedId);

            var fileExtentions = new Dictionary<string, string>();
            foreach (var fileExtention in currentExportFeed.GetAvailableFileExtentions())
            {
                fileExtentions.Add(fileExtention, fileExtention);
            }

            var model = new ExportFeedSettingsModel
            {
                FileName = _exportFeedSettings.FileName,
                FileExtention = _exportFeedSettings.FileExtention,
                PriceMarginInPercents = _exportFeedSettings.PriceMarginInPercents,
                PriceMarginInNumbers = _exportFeedSettings.PriceMarginInNumbers,
                AdditionalUrlTags = _exportFeedSettings.AdditionalUrlTags,

                Active = _exportFeedSettings.Active,
                IntervalType = _exportFeedSettings.IntervalType,
                Interval = _exportFeedSettings.Interval,

                JobStartHour = _exportFeedSettings.JobStartTime.Hour,
                JobStartMinute = _exportFeedSettings.JobStartTime.Minute,

                IntervalTypeList = intervalTypeList,
                FileExtentions = fileExtentions,

                ExportAllProducts = _exportFeedSettings.ExportAllProducts,

                AdvancedSettings = currentExportFeed.GetAdvancedSettings(),

                //JobStartTime = _exportFeedSettings.JobStartTime,
                DoNotExportAdult = !_exportFeedSettings.ExportAdult,
            };

            model.AdvancedSettingsModel = new GetExportFeedAdvancedSettingsModel(_exportFeedSettings.ExportFeedId,
                _exportFeedType, model.AdvancedSettings).Execute();

            //if (SaasDataService.IsSaasEnabled && !SaasDataService.CurrentSaasData.ExportFeedsAutoUpdate &&
            //    (_exportFeedType == EExportFeedType.YandexMarket || _exportFeedType == EExportFeedType.GoogleMerchentCenter || _exportFeedType == EExportFeedType.Avito))
            //{
            //    model.Active = false;
            //    model.NotAvailableJob = true;
            //}


            return model;
        }
    }
}
