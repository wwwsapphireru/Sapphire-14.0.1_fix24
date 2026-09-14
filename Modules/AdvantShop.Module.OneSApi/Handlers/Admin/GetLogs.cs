using AdvantShop.Core.Services.Localization;
using AdvantShop.FilePath;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Web.Infrastructure.Admin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Handlers.Admin
{
    public class GetLogs
    {
        private readonly BaseFilterModel _filterModel;
        private string[] _folders;

        public GetLogs(string folders, BaseFilterModel filterModel)
        {
            _folders = folders.Split(',');
            _filterModel = filterModel;
        }

        public FilterResult<LogFile> Execute()
        {
            var model = new FilterResult<LogFile>();
            List<KeyValuePair<string, string[]>> files = new List<KeyValuePair<string, string[]>> { };

            var logPath = FoldersHelper.GetPathAbsolut(FolderType.UserFiles, "modules\\" + OneSApi.ModuleStringId + "\\");
            foreach (var folder in _folders)
            {
                if (Directory.Exists(logPath + folder))
                    files.AddRange(Directory.GetFiles(logPath + folder, "*.json").
                        Select(x => new KeyValuePair<string, string[]>(x, new string[] { new FileInfo(x).Name, folder })));
            }
            files = files.OrderByDescending(x => x.Value[0]).ToList();

            model.TotalItemsCount = files.Count;
            model.TotalPageCount = (int)(Math.Ceiling((double)model.TotalItemsCount / _filterModel.ItemsPerPage));
            model.TotalString = LocalizationService.GetResourceFormat("Admin.Grid.FildTotal", model.TotalItemsCount);

            if (model.TotalPageCount < _filterModel.Page && _filterModel.Page > 1)
            {
                return model;
            }

            var count = _filterModel.ItemsPerPage;
            var page = _filterModel.Page - 1;
            if (_filterModel.ItemsPerPage * page + _filterModel.ItemsPerPage > files.Count)
                count = files.Count - _filterModel.ItemsPerPage * page;
            var range = files.GetRange(_filterModel.ItemsPerPage * page, count);

            model.DataItems = range.Select(x => new LogFile
            {
                Filename = x.Value[0].Replace(".json", ""),
                Folder = x.Value[1],
                Size = new FileInfo(x.Key).Length
            }).ToList();

            return model;
        }
    }
}