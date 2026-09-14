using AdvantShop.Core.Common.Extensions;
using AdvantShop.Statistic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdvantShop.Module.OneSApi.Domain
{
    public class ModuleStatistic
    {
        private static readonly object SyncObject = new object();
        private static readonly ModuleStatisticData Data = new ModuleStatisticData();

        //public static readonly string VirtualFileLogPath = FoldersHelper.GetPath(FolderType.UserFiles, "StatisticLog.txt", true);
        //public static readonly string FileLog = FoldersHelper.GetPathAbsolut(FolderType.UserFiles, "StatisticLog.txt");


        static public void Init()
        {
            Data.ProcessedPercent = 0;
            Data.Processed = 0;
            Data.Total = 0;
            Data.IsRun = false;
            Data.Update = 0;
            Data.Add = 0;
            Data.Error = 0;
            Data.FileName = string.Empty;
            Data.FileSize = string.Empty;
            Data.ZipFile = false;
            Data.CurrentProcess = string.Empty;
            Data.ErrorMessage = string.Empty;
            //if (!Directory.Exists(FoldersHelper.GetPathAbsolut(FolderType.PriceTemp)))
            //{
            //    Directory.CreateDirectory(FoldersHelper.GetPathAbsolut(FolderType.PriceTemp));
            //}
            //FileHelpers.DeleteFile(FileLog);
        }

        public static StatisticData CurrentData
        {
            get
            {
                Data.ProcessedPercent = Data.Total != 0 ? Convert.ToInt32((float)Data.Processed / ((float)Data.Total / 100)) : 0;
                return Data;
            }
        }

        //public static Thread StartNew(Action action, bool inBackGround = true)
        //{
        //    var temp = inBackGround ? new Thread(() => action()) { IsBackground = true } : Thread.CurrentThread;
        //    temp.SetCulture();
        //    IsRun = true;
        //    if (inBackGround)
        //        temp.Start();
        //    else
        //        action();
        //    return temp;
        //}

        public static Task StartNew(Action action)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.SetCulture();
                action();
            }, TaskCreationOptions.LongRunning);
        }

        public static long TotalRow
        {
            get { lock (SyncObject) { return Data.Total; } }
            set { lock (SyncObject) { Data.Total = value; } }
        }

        public static long RowPosition
        {
            get { lock (SyncObject) { return Data.Processed; } }
            set { lock (SyncObject) { Data.Processed = value; } }
        }

        public static bool IsRun
        {
            get { lock (SyncObject) { return Data.IsRun; } }
            set { lock (SyncObject) { Data.IsRun = value; } }
        }

        public static long TotalUpdateRow
        {
            get { lock (SyncObject) { return Data.Update; } }
            set { lock (SyncObject) { Data.Update = value; } }
        }

        public static long TotalAddRow
        {
            get { lock (SyncObject) { return Data.Add; } }
            set { lock (SyncObject) { Data.Add = value; } }
        }

        public static long TotalErrorRow
        {
            get { lock (SyncObject) { return Data.Error; } }
            set { lock (SyncObject) { Data.Error = value; } }
        }

        public static string FileName
        {
            get { lock (SyncObject) { return Data.FileName; } }
            set { lock (SyncObject) { Data.FileName = value; } }
        }

        public static bool ZipFile
        {
            get { lock (SyncObject) { return Data.ZipFile; } }
            set { lock (SyncObject) { Data.ZipFile = value; } }
        }

        public static string FileSize
        {
            get { lock (SyncObject) { return Data.FileSize; } }
            set { lock (SyncObject) { Data.FileSize = value; } }
        }

        public static string CurrentProcess
        {
            get { lock (SyncObject) { return Data.CurrentProcess; } }
            set { lock (SyncObject) { Data.CurrentProcess = value; } }
        }

        public static string CurrentProcessName
        {
            get { lock (SyncObject) { return Data.CurrentProcessName; } }
            set { lock (SyncObject) { Data.CurrentProcessName = value; } }
        }

        public static string ErrorMessage
        {
            get { lock (SyncObject) { return Data.ErrorMessage; } }
            set { lock (SyncObject) { Data.ErrorMessage = value; } }
        }

        //public static void WriteLog(string message)
        //{
        //    lock (SyncObject)
        //    {
        //        using (var fs = new FileStream(FileLog, FileMode.Append, FileAccess.Write))
        //        using (var sw = new StreamWriter(fs, Encoding.UTF8))
        //            sw.WriteLine(message);
        //    }
        //}

        //public static string ReadLog()
        //{
        //    var content = "";
        //    lock (SyncObject)
        //    {
        //        if (File.Exists(FileLog))
        //        {
        //            using (var streamReader = new StreamReader(FileLog))
        //                content = streamReader.ReadToEnd();
        //        }
        //    }
        //    return content;
        //}
    }

    public class ModuleStatisticData : StatisticData
    {
        public string ErrorMessage;
    }

    public class ImportStatisticModel
    {
        public bool IsRun { get; set; }
        public string ProcessName { get; set; }
        public string Process { get; set; }
        public string ProgressValue { get; set; }
        public string ProgressTotal { get; set; }
        public string ErrorMessage { get; set; }
        public string FileName { get; set; }
    }

}