; (function (ng) {
    'use strict';

    var OneModuleAdminService = function ($http, SweetAlert, toaster) {
        var service = this;

        // progress bar

        service.getCommonStatistic = function () {
            return $http.post('../onesadmin/GetImportStatistic').then(function (response) {
                return response.data;
            });
        };

        service.stop = 0;
        service.initProgress = function (ctrl) {
            //console.log('initProgress');
            setTimeout(function () {
                //console.log('setTimeout');
                service.getCommonStatistic().then(function (response) {
                    console.log(response);
                    if (response.result) {
                        if (response.obj.IsRun)
                        //if (!service.importStartFinish)
                        {
                            //ctrl.IsRun = response.obj.IsRun ? "Запущен" : "Не запущен";
                            //ctrl.ProcessName = response.obj.ProcessName;
                            ctrl.IsRun = response.obj.IsRun;
                            ctrl.ProcessName = response.obj.ProcessName;
                            ctrl.Process = response.obj.Process;
                            ctrl.ProgressValue = response.obj.ProgressValue;
                            ctrl.ProgressTotal = response.obj.ProgressTotal;

                            service.initProgress(ctrl);

                        }
                        else {
                            //console.log(response);
                            //ctrl.IsRun = response.obj.IsRun ? "Запущен" : "Не запущен";
                            //ctrl.ProcessName = response.obj.ProcessName;
                            ctrl.IsRun = false;
                            if (response.obj.Process == 'ImportFromFile') {
                                ctrl.selectFileCaption = "Выбрать файл";
                            } else if (response.obj.Process == 'ExportOrdersToFile') {
                                ctrl.ExportFileName = response.obj.FileName;
                            } else if (response.obj.Process == 'ExportProductsToFile') {
                                ctrl.ExportFileName = response.obj.FileName;
                            }
                            if (response.obj.ErrorMessage == '') {
                                toaster.pop('success', '', 'Процесс завершен');
                            } else {
                                toaster.pop('error', '', 'Процесс завершен с ошибками: ' + response.obj.ErrorMessage);
                            }
                            //console.log(ctrl.ExistFile);
                            //console.log(ctrl.ExistFileStiker);
                        }

                        //ctrl.Process = response.obj.Process;

                    }
                    return response;
                })
            }, 1000);
        }

        service.breakProcess = function () {
            SweetAlert.confirm('Текущий процесс будет прерван!', { title: '' })
                .then(function (result) {
                    if (result.value === true) {
                        return $http.post('../wbadmin/breakProcess').then(function (response) {
                            console.log(response);
                            if (response.data.result) {
                                toaster.pop('success', '', 'Текущий процесс прерван');
                            } else {
                                toaster.pop('error', '', response.data.errors[0]);
                            }
                            return;
                        });
                    }
                });
        }

    }

    OneModuleAdminService.$inject = ['$http', 'SweetAlert', 'toaster'];

    ng.module('module').service('OneModuleAdminService', OneModuleAdminService);

})(window.angular)