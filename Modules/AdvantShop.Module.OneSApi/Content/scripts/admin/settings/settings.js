; (function (ng) {
    'use strict';

    var OneSettingsCtrl = function ($http, toaster, SweetAlert, uiGridConstants, uiGridCustomConfig, $translate, productPropertiesService, OneModuleAdminService) {
        var ctrl = this;
        var columnDefsDepots = [
            {
                name: 'Code',
                displayName: 'Код',
                enableSorting: true,
                enableCellEdit: true,
            },
            {
                name: 'Name',
                displayName: 'Наименование',
                enableSorting: true,
                enableCellEdit: true,
            },
            {
                name: 'SortOrder',
                displayName: 'Сортировка',
                enableSorting: false,
                enableCellEdit: true,
                width: 100,
            },
            {
                name: 'Active',
                displayName: 'Активен',
                width: 80,
                enableCellEdit: true,
                type: 'checkbox',
                filter: {
                    placeholder: 'Активен',
                    type: uiGridConstants.filter.SELECT,
                    name: 'Active',
                    selectOptions: [{ label: $translate.instant('Admin.Js.Properties.Yes'), value: true }, { label: $translate.instant('Admin.Js.Properties.No'), value: false }]
                }
            },
            {
                name: '_serviceColumn',
                displayName: '',
                width: 80,
                enableSorting: false,
                cellTemplate:
                    '<div class="ui-grid-cell-contents"><div>' +
                    //'<a href="" class="link-invert ui-grid-custom-service-icon fa fa-pencil-alt" ng-click="grid.appScope.$ctrl.gridExtendCtrl.openModal(row.entity.DepotId)"></a> ' +
                    '<ui-modal-trigger data-controller="\'ModalAddEditDepotCtrl\'" controller-as="ctrl" ' +
                    'template-url="../modules/OneSApi/content/scripts/admin/settings/modals/ModalAddEditDepot.html" ' +
                    'data-resolve="{\'id\': row.entity.DepotId}" ' +
                    'data-on-close="grid.appScope.$ctrl.fetchData()"> ' +
                    '<a href="" class="link-invert ui-grid-custom-service-icon fas fa-pencil-alt"></a> ' +
                    '</ui-modal-trigger>' +
                    '<a href="" ng-click="grid.appScope.$ctrl.gridExtendCtrl.deleteDepot(row.entity.DepotId)" class="ui-grid-custom-service-icon fa fa-times link-invert"></a> ' +
                    '</div></div>'
            }
        ];
        var columnDefsShippingMethods = [
            {
                name: 'ShippingMethodKey',
                displayName: 'Метод доставки',
                enableSorting: true,
                enableCellEdit: false,
            },
            {
                name: 'TrackingUrl',
                displayName: 'Шаблон ссылки на отслеживание',
                enableSorting: true,
                enableCellEdit: true,
            },
            //    {
            //        name: '_serviceColumn',
            //        displayName: '',
            //        width: 80,
            //        enableSorting: false,
            //        cellTemplate:
            //            '<div class="ui-grid-cell-contents"><div>' +
            //            //'<a href="" class="link-invert ui-grid-custom-service-icon fa fa-pencil-alt" ng-click="grid.appScope.$ctrl.gridExtendCtrl.openModal(row.entity.DepotId)"></a> ' +
            //            '<ui-modal-trigger data-controller="\'ModalAddEditDepotCtrl\'" controller-as="ctrl" ' +
            //            'template-url="../modules/OneSApi/content/scripts/admin/settings/modals/ModalAddEditDepot.html" ' +
            //            'data-resolve="{\'id\': row.entity.DepotId}" ' +
            //            'data-on-close="grid.appScope.$ctrl.fetchData()"> ' +
            //            '<a href="" class="link-invert ui-grid-custom-service-icon fas fa-pencil-alt"></a> ' +
            //            '</ui-modal-trigger>' +
            //            '<a href="" ng-click="grid.appScope.$ctrl.gridExtendCtrl.deleteDepot(row.entity.DepotId)" class="ui-grid-custom-service-icon fa fa-times link-invert"></a> ' +
            //            '</div></div>'
            //    }
        ];

        ctrl.$onInit = function () {
            ctrl.selectFileCaption = "Выбрать файл";
            $http.get('../onesadmin/getsettings').then(function success(response) {
                var data = response.data;
                console.log(data);
                ctrl.settings = data.Settings;
                ctrl.apiKey = data.ApiKey;

                ctrl.defaultCategoryList = data.DefaultCategoryList;
                ctrl.defaultCategoryId = ctrl.defaultCategoryList.filter(function (x) { return x.value == ctrl.settings.ImportProduct.DefaultCategoryId.toString(); })[0];

                ctrl.importArtnoTypeList = data.ImportArtnoTypeList;
                ctrl.importArtnoType = ctrl.importArtnoTypeList.filter(function (x) { return x.value == ctrl.settings.ImportProduct.ImportArtnoType.toString(); })[0];

                ctrl.importNameTypeList = data.ImportNameTypeList;
                ctrl.importNameType = ctrl.importNameTypeList.filter(function (x) { return x.value == ctrl.settings.ImportProduct.ImportNameType.toString(); })[0];

                ctrl.useIn1CList = data.UseIn1CList;
                ctrl.useIn1C = ctrl.useIn1CList.filter(function (x) { return x.value == ctrl.settings.UseIn1C.toString(); })[0];

                ctrl.itemsPerPageList = data.ItemsPerPageList;
                ctrl.itemsPerPage = ctrl.itemsPerPageList.filter(function (x) { return x.value == ctrl.settings.ItemsPerPage.toString(); })[0];

                ctrl.customerGroupList = data.CustomerGroupList;
                ctrl.customerGroupId = ctrl.customerGroupList.filter(function (x) { return x.value == ctrl.settings.ImportCustomer.CustomerGroupId.toString(); })[0];

                ctrl.triggerCategoryList = data.TriggerCategoryList;
                ctrl.triggerCategoryId = ctrl.triggerCategoryList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.TriggerCategoryId.toString(); })[0];

                ctrl.managerForDeleteList = data.ManagerForDeleteList;
                ctrl.managerIdForDelete = '0';

                ctrl.sendOrdersFromDate = data.SendOrdersFromDate;

                ctrl.StatusConfirmed = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusConfirmed.toString(); })[0];
                ctrl.StatusBuilding = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusBuilding.toString(); })[0];
                ctrl.StatusShipped = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusShipped.toString(); })[0];
                ctrl.StatusReady = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusReady.toString(); })[0];
                ctrl.StatusReadyAtStore = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusReadyAtStore.toString(); })[0];
                ctrl.StatusDone = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusDone.toString(); })[0];
                ctrl.StatusWaiting = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusWaiting.toString(); })[0];
                ctrl.StatusCancelled = ctrl.settings.ImportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.StatusCancelled.toString(); })[0];

                ctrl.StatusLead = ctrl.settings.ExportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ExportOrder.StatusLead.toString(); })[0];

                ctrl.StatusRefusing = ctrl.settings.ExportOrder.OrderStatusList.filter(function (x) { return x.value == ctrl.settings.ExportOrder.StatusRefusing.toString(); })[0];

                ctrl.ShippingForTK = ctrl.settings.ImportOrder.ShippingList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.ShippingForTK.toString(); })[0];
                ctrl.ShippingUnknown = ctrl.settings.ImportOrder.ShippingList.filter(function (x) { return x.value == ctrl.settings.ImportOrder.ShippingUnknown.toString(); })[0];

                ctrl.propertiesPage = [];
                ctrl.propertiesSize = 200;
                ctrl.propertiesTotalPageCount = 0;
                ctrl.propertiesList = [];
                ctrl.selectedProperty = [];
                ctrl.selectedPropertyId = [];
                ctrl.propertiesQ = [];

                ctrl.additionalProperties = [];
                ctrl.propSaleInd = ctrl.additionalProperties.length;
                ctrl.selectedProperty.push({});
                ctrl.selectedPropertyId.push(ctrl.settings.ImportProduct.PropertySaleId);
                if (ctrl.settings.ImportProduct.PropertySaleId != null) {
                    ctrl.selectedProperty[ctrl.propSaleInd] = {};
                    ctrl.selectedProperty[ctrl.propSaleInd].PropertyId = ctrl.settings.ImportProduct.PropertySaleId;
                    ctrl.selectedProperty[ctrl.propSaleInd].Name = ctrl.settings.ImportProduct.PropertySaleName;
                }
                ctrl.propertiesPage.push(0);
                ctrl.propertiesQ.push(null);
                ctrl.propertiesList.push([]);

                ctrl.oneSettingsForm.modified = false;
                ctrl.oneSettingsForm.$setPristine();
            });

            OneModuleAdminService.getCommonStatistic().then(function (response) {
                if (response.IsRun) {
                    //ctrl.IsRun = true;
                    OneModuleAdminService.initProgress(ctrl);
                }
            });
        };

        ctrl.gridDepotsOptions = ng.extend({}, uiGridCustomConfig, {
            columnDefs: columnDefsDepots,
            uiGridCustom: {
                rowClick: function ($event, row) {
                    ctrl.openModal(row.entity.Id);
                },
                //    selectionOptions: [
                //        {
                //            text: 'Удалить выделенные',
                //            url: '../DepotAmounts/deleteCategories',
                //            field: 'Id',
                //            before: function () {
                //                return SweetAlert.confirm("Вы уверены, что хотите удалить? Категории и товары в них будут удалены из Вконтакте тоже.", { title: "Удаление" }).then(function (result) {
                //                    return result === true ? $q.resolve('sweetAlertConfirm') : $q.reject('sweetAlertCancel');
                //                });
                //            }
                //        },
                //    ]
            }
        });

        ctrl.gridDepotsOnInit = function (grid) {
            ctrl.gridDepots = grid;
        };

        ctrl.gridShippingMethodsOptions = ng.extend({}, uiGridCustomConfig, {
            columnDefs: columnDefsShippingMethods,
            uiGridCustom: {
                rowClick: function ($event, row) {
                    ctrl.openModal(row.entity.Id);
                },
            }
        });

        ctrl.gridShippingMethodsOnInit = function (grid) {
            ctrl.gridShippingMethods = grid;
        };

        ctrl.validate = function (form) {

            return true;
        }

        ctrl.saveChanges = function () {
            //console.log(ctrl);
            ctrl.settings.ImportProduct.DefaultCategoryId = ctrl.defaultCategoryId.value;
            ctrl.settings.ImportProduct.ImportArtnoType = ctrl.importArtnoType.value;
            ctrl.settings.ImportProduct.ImportNameType = ctrl.importNameType.value;
            ctrl.settings.ImportProduct.PropertySaleId = ctrl.selectedPropertyId[ctrl.propSaleInd];
            ctrl.settings.ImportCustomer.CustomerGroupId = ctrl.customerGroupId.value;
            ctrl.settings.UseIn1C = ctrl.useIn1C.value;
            ctrl.settings.ItemsPerPage = ctrl.itemsPerPage.value;
            ctrl.settings.ImportOrder.StatusConfirmed = ctrl.StatusConfirmed.value;
            ctrl.settings.ImportOrder.StatusBuilding = ctrl.StatusBuilding.value;
            ctrl.settings.ImportOrder.StatusShipped = ctrl.StatusShipped.value;
            ctrl.settings.ImportOrder.StatusReady = ctrl.StatusReady.value;
            ctrl.settings.ImportOrder.StatusReadyAtStore = ctrl.StatusReadyAtStore.value;
            ctrl.settings.ImportOrder.StatusDone = ctrl.StatusDone.value;
            ctrl.settings.ImportOrder.StatusWaiting = ctrl.StatusWaiting.value;
            ctrl.settings.ImportOrder.StatusCancelled = ctrl.StatusCancelled.value;
            ctrl.settings.ExportOrder.StatusLead = ctrl.StatusLead.value;
            ctrl.settings.ExportOrder.StatusRefusing = ctrl.StatusRefusing.value;
            ctrl.settings.ImportOrder.ShippingForTK = ctrl.ShippingForTK.value;
            ctrl.settings.ImportOrder.ShippingUnknown = ctrl.ShippingUnknown.value;
            ctrl.settings.ImportOrder.TriggerCategoryId = ctrl.triggerCategoryId.value;

            $http.post('../onesadmin/saveSettings',
                {
                    settings: ctrl.settings
                })
                .then(function success(response) {
                    if (response.data.success) {
                        toaster.pop('success', '', 'Настройки сохранены');

                        ctrl.oneSettingsForm.modified = false;
                        ctrl.oneSettingsForm.$setPristine();
                    } else {
                        toaster.pop('error', '', response.data.msg);
                    }
                })
        };

        ctrl.addCategory_change = function () {
            if (ctrl.settings.ImportProduct.AddCategory != true) {
                ctrl.settings.ImportProduct.UpdateCategory = false;
            }
        }

        ctrl.sendOrders = function () {
            var to = '';
            if (ctrl.sendOrdersToDate != null && ctrl.sendOrdersToDate != '') {
                to = ' по ' + ctrl.sendOrdersToDate;
            }
            SweetAlert.confirm('Подтвердите выгрузку в буфер обмена заказов за период с ' + ctrl.sendOrdersFromDate + to, { title: '' }).then(function (result) {
                if (result.value === true) {
                    return $http.post('../onesadmin/sendOrders', { fromDate: ctrl.sendOrdersFromDate, toDate: ctrl.sendOrdersToDate }).then(function (response) {
                        if (response.data.result == true) {
                            toaster.success('', 'Заказы выгружены успешно');
                        } else {
                            toaster.error('', (response.data.errors || [])[0] || 'Ошибка при выгрузке заказов');
                        }
                    });
                }
            });
        };

        ctrl.selectFile = function (file) {
            console.log(file);
            ctrl.file = file;
            ctrl.selectFileCaption = ctrl.file.name;
        }

        ctrl.importFromFile = function (command) {
            SweetAlert.confirm('Подтвердите загрузку данных из файла').then(function (result) {
                if (result.value === true) {
                    toaster.success('', 'Запущена загрузка данных');
                    var fData = new FormData();
                    fData.append("file", ctrl.file);
                    var req = {
                        method: 'POST',
                        url: '../onesadmin/' + command,
                        headers: {
                            'Content-Type': undefined
                        },
                        data: fData
                    }
                    return $http(req).then(function (response) {
                        if (response.data.result == true) {
                            ctrl.file = null;
                            ctrl.showExportProgress = true;
                            OneModuleAdminService.initProgress(ctrl);
                        } else {
                            toaster.error('', (response.data.errors || [])[0] || 'Ошибка при загрузке данных');
                        }
                    });
                }
            });
        }

        ctrl.importFromFile0 = function (command) {
            SweetAlert.confirm('Подтвердите загрузку данных из файла').then(function (result) {
                if (result.value === true) {
                    toaster.success('', 'Запущена загрузка данных');
                    return $http.post('../onesadmin/' + command).then(function (response) {
                        if (response.data.result == true) {
                            ctrl.file = null;
                            ctrl.showExportProgress = true;
                            OneModuleAdminService.initProgress(ctrl);
                        } else {
                            toaster.error('', (response.data.errors || [])[0] || 'Ошибка при загрузке данных');
                        }
                    });
                }
            });
        }

        ctrl.deleteDepot = function (id) {
            SweetAlert.confirm($translate.instant('Admin.Js.OrderStatuses.AreYouSureDelete'), { title: $translate.instant('Admin.Js.OrderStatuses.Deleting') }).then(function (result) {
                if (result.value === true) {
                    console.log(id);
                    $http.post('../OneSAdmin/deleteDepot', { 'id': id }).then(function (response) {
                        ctrl.gridDepots.fetchData();
                    });
                }
            });
        }

        ctrl.importRedirects = function (filename) {
            SweetAlert.confirm('Подтвердите загрузку списка редиректов из файла').then(function (result) {
                if (result.value === true) {
                    toaster.success('', 'Запущена загрузка');
                    return $http.post('../onesadmin/importRedirects', { filename: filename }).then(function (response) {
                        if (response.data.result == true) {
                            ctrl.file = null;
                            ctrl.showExportProgress = true;
                            OneModuleAdminService.initProgress(ctrl);
                        } else {
                            toaster.error('', (response.data.errors || [])[0] || 'Ошибка при загрузке данных');
                        }
                    });
                }
            });
        }

        ctrl.exportProductsToFile = function () {
            //SweetAlert.confirm('Подтвердите выгрузку в буфер обмена заказов за период с ' + ctrl.sendOrdersFromDate + to, { title: '' }).then(function (result) {
            //    if (result.value === true) {
            return $http.post('../onesadmin/exportProductsToFile').then(function (response) {
                if (response.data.result == true) {
                    //ctrl.fileNameStickers = response.data.obj;
                    ctrl.showExportProgress = true;
                    OneModuleAdminService.initProgress(ctrl);
                } else {
                    toaster.error('', (response.data.errors || [])[0] || 'Ошибка при выгрузке');
                }
            });
            //        }
            //    });
        };

        ctrl.deleteManagerFromOrders = function () {
            SweetAlert.confirm('Выбранный менеджер будет удален из всех заказов!').then(function (result) {
                if (result.value === true) {
                    console.log(ctrl.managerIdForDelete);
                    return $http.post('../onesadmin/deleteManagerFromOrders', { managerId: ctrl.managerIdForDelete.value }).then(function (response) {
                        if (response.data.result == true) {
                            //    ctrl.showExportProgress = true;
                            //    OneModuleAdminService.initProgress(ctrl);
                            toaster.success('', 'Удаление завершено');
                            ctrl.managerIdForDelete = '0';
                        } else {
                            toaster.error('', (response.data.errors || [])[0] || 'Ошибка при удалении менеджера');
                        }
                    });
                }
            });
        }

        ctrl.breakProcess = function () {
            OneModuleAdminService.breakProcess();
        }

        // #region Property

        ctrl.getMoreProperty = function (ind) {

            if (ctrl.propertiesPage[ind] > ctrl.propertiesTotalPageCount || ctrl.loadingProperties === true) {
                return $q.resolve();
            }

            ctrl.propertiesPage[ind] += 1;
            ctrl.loadingProperties = true;

            return productPropertiesService.getAllProperties(ctrl.propertiesPage[ind], ctrl.propertiesSize, ctrl.propertiesQ[ind])
                .then(function (data) {
                    ctrl.propertiesList[ind] = ctrl.propertiesList[ind].concat(data.DataItems);
                    ctrl.propertiesTotalPageCount = data.TotalPageCount;

                    return data;
                })
                .finally(function () {
                    ctrl.loadingProperties = false;
                });
        };

        ctrl.closeSelectProperty = function (ind, isOpen) {
            var propertyInList;

            if (isOpen == false) {

                if (ctrl.propertiesQ[ind] != null && ctrl.propertiesQ[ind].length > 0) {
                    for (var i = 0, len = ctrl.propertiesList[ind].length; i < len; i++) {
                        if (ctrl.propertiesList[ind][i].Name.toLowerCase() === ctrl.propertiesQ[ind].toLowerCase() && (ctrl.selectedProperty[ind] != null ? ctrl.selectedProperty[ind] === ctrl.propertiesList[ind][i] : true)) {
                            propertyInList = ctrl.propertiesList[ind][i];
                            break;
                        }
                    }
                }

                if (ctrl.selectedProperty[ind] == null && ctrl.propertiesQ[ind] != null && propertyInList == null) {
                    ctrl.selectedProperty[ind] = {
                        Name: ctrl.propertiesQ[ind]
                    };
                }

                if (propertyInList != null) {
                    ctrl.selectedProperty[ind] = propertyInList;
                    ctrl.selectedPropertyId[ind] = propertyInList.PropertyId;
                }

                ctrl.propertiesPage[ind] = 0;
                ctrl.propertiesQ[ind] = null;
            }
        };

        ctrl.selectProperty = function (ind, $item, $model) {
            if ($item != null) {

                ctrl.selectedPropertyId[ind] = $item.PropertyId;

                ctrl.propertiesQ[ind] = null;

                if (ctrl.$selectProperty) {
                    ctrl.$selectProperty.search = $model.Name;
                }
            } else {
                ctrl.selectedPropertyId[ind] = null;
            }
        };

        ctrl.firstCallProperties = function (ind) {
            //ctrl.propertiesPage = 0;
            ctrl.propertiesList[ind] = [];
            ctrl.propertiesTotalPageCount = 0;
            ctrl.getMoreProperty(ind);
        };

        ctrl.findProperty = function (ind, q, $select) {
            ctrl.$selectProperty = $select;

            ctrl.propertiesQ[ind] = q;
            ctrl.propertiesPage[ind] = 1;

            productPropertiesService.getAllProperties(ctrl.propertiesPage[ind], ctrl.propertiesSize, q)
                .then(function (data) {

                    var hasItems = data.DataItems.length > 0;
                    var result = hasItems === true ? data.DataItems : [];
                    var qItem = { Name: q };
                    var itemFinded;

                    if (q != null && q.length > 0) {
                        for (var i = 0, len = data.DataItems.length; i < len; i++) {
                            if (q === data.DataItems[i].Name) {
                                itemFinded = data.DataItems[i];
                            }
                        }
                    }

                    if (itemFinded != null) {
                        ctrl.selectedProperty[ind] = itemFinded;
                        ctrl.selectedPropertyId[ind] = itemFinded.PropertyId;
                    } else if (q != null && q.length > 0) {
                        ctrl.selectedProperty[ind] = qItem;
                        ctrl.selectedPropertyId[ind] = null;
                        result.push(qItem);
                    }


                    return ctrl.propertiesList[ind] = result;
                });
        };

        // #endregion

        //// progress bar

        //ctrl.getCommonStatistic = function () {
        //    return $http.post('../onesadmin/GetImportStatistic').then(function (response) {
        //        return response.data;
        //    });
        //};

        //ctrl.stop = 0;
        //ctrl.initProgress = function () {
        //    //console.log('initProgress');
        //    setTimeout(function () {
        //        //console.log('setTimeout');
        //        ctrl.getCommonStatistic().then(function (response) {
        //            console.log(response);
        //            if (response.result) {
        //                if (response.obj.IsRun)
        //                //if (!ctrl.importStartFinish)
        //                {
        //                    //ctrl.IsRun = response.obj.IsRun ? "Запущен" : "Не запущен";
        //                    //ctrl.ProcessName = response.obj.ProcessName;
        //                    ctrl.IsRun = response.obj.IsRun;
        //                    ctrl.ProcessName = response.obj.ProcessName;
        //                    ctrl.Process = response.obj.Process;
        //                    ctrl.ProgressValue = response.obj.ProgressValue;
        //                    ctrl.ProgressTotal = response.obj.ProgressTotal;

        //                    ctrl.initProgress();

        //                }
        //                else {
        //                    //console.log(response);
        //                    //ctrl.IsRun = response.obj.IsRun ? "Запущен" : "Не запущен";
        //                    //ctrl.ProcessName = response.obj.ProcessName;
        //                    ctrl.IsRun = false;
        //                    if (response.obj.Process == 'ImportFromFile') {
        //                        ctrl.selectFileCaption = "Выбрать файл";
        //                    }
        //                    if (response.obj.ErrorMessage == '') {
        //                        toaster.pop('success', '', 'Процесс завершен');
        //                    } else {
        //                        toaster.pop('error', '', 'Процесс завершен с ошибками: ' + response.obj.ErrorMessage);
        //                    }
        //                    //console.log(ctrl.ExistFile);
        //                    //console.log(ctrl.ExistFileStiker);
        //                }

        //                //ctrl.Process = response.obj.Process;

        //            }
        //            return response;
        //        })
        //    }, 1000);
        //}

        //ctrl.breakProcess = function () {
        //    SweetAlert.confirm('Текущий процесс будет прерван!', { title: '' })
        //        .then(function (result) {
        //            if (result === true) {
        //                return $http.post('../wbadmin/breakProcess').then(function (response) {
        //                    console.log(response);
        //                    if (response.data.result) {
        //                        toaster.pop('success', '', 'Текущий процесс прерван');
        //                    } else {
        //                        toaster.pop('error', '', response.data.errors[0]);
        //                    }
        //                    return;
        //                });
        //            }
        //        });
        //}

    };

    OneSettingsCtrl.$inject = ['$http', 'toaster', 'SweetAlert', 'uiGridConstants', 'uiGridCustomConfig', '$translate', 'productPropertiesService', 'OneModuleAdminService'];

    ng.module('oneSettings', [])
        .controller('OneSettingsCtrl', OneSettingsCtrl);

})(window.angular);