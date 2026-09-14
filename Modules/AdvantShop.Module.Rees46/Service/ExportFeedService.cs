using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.ExportImport;
using System;
//GlorySoft_021
namespace AdvantShop.Module.Rees46.Service
{
    public class ExportFeedService
    {
        public static IExportFeed GetExportFeedInstance(string feedType, int exportFeedId, bool useCommonStatistic = false)
        {
            var type = ReflectionExt.GetTypeByAttributeValue<ExportFeedKeyAttribute>(typeof(IExportFeed), atr => atr.Value, feedType);
            return (IExportFeed)Activator.CreateInstance(type, exportFeedId, useCommonStatistic);
        }
    }
}