; (function (ng) {
    'use strict';
    

    var OneExportOrdersCtrl = function (uiGridCustomConfig, $translate, $q, SweetAlert, OneModuleAdminService) {

        var ctrl = this,
            columnDefs = [
                {
                    name: 'Number',
                    displayName: 'Номер',
                    enableCellEdit: false,
                },
                {
                    name: 'OrderDate',
                    displayName: 'Дата заказа',
                    enableCellEdit: false,
                },
                {
                    name: 'Changed',
                    displayName: 'Дата изменения',
                    enableCellEdit: false,
                },
                {
                    name: 'ChangeType',
                    displayName: 'Изменение',
                    enableCellEdit: false,
                },
                {
                    name: '_serviceColumn',
                    displayName: '',
                    width: 120,
                    cellTemplate:
                        '<div class="ui-grid-cell-contents"><div>' +
                        '<a ng-href="orders/edit/{{row.entity.OrderId}}" class="link-invert ui-grid-custom-service-icon fa fa-pencil-alt" target="_blank"></a>' +
                        '<ui-grid-custom-delete url="../OneSAdmin/deleteOrderFromExport" params="{\'orderId\': row.entity.OrderId, \'exportType\': row.entity.ExportType}"></ui-grid-custom-delete>' +
                        '</div></div>'
                }
            ];

        ctrl.gridOptions = ng.extend({}, uiGridCustomConfig, {
            columnDefs: columnDefs,
            uiGridCustom: {
                selectionOptions: [
                    {
                        text: 'Выгрузить выделенные в файл',
                        url: '../OneSAdmin/exportOrdersToFile',
                        field: 'OrderId',
                        //before: function () {
                        //    ctrl.exportOrdersToFileDate = new Date().toJSON().slice(0, 10);
                        //},
                        after: function () {
                            ctrl.showExportProgress = true;
                            OneModuleAdminService.initProgress(ctrl);
                        }
                    },
                    //{
                    //    text: $translate.instant('Admin.Js.Orders.DeleteSelected'),
                    //    url: '../OneSAdmin/deleteOrdersFromExport',
                    //    field: 'OrderId',
                    //    before: function () {
                    //        return SweetAlert.confirm('Подтвердите удаление заказов из списка выгрузки', { title: 'Удаление' }).then(function (result) {
                    //            return result === true ? $q.resolve('sweetAlertConfirm') : $q.reject('sweetAlertCancel');
                    //        });
                    //    }
                    //},
                ]
            }
        });

        ctrl.gridOnInit = function (grid) {
            ctrl.grid = grid;
        };

        ctrl.$onInit = function () {
        } 

        ctrl.breakProcess = function () {
            OneModuleAdminService.breakProcess();
        }

    };

    OneExportOrdersCtrl.$inject = ['uiGridCustomConfig', '$translate', '$q', 'SweetAlert', 'OneModuleAdminService'];

    ng.module('OneExportOrders', ['urlHelper'])
        .controller('OneExportOrdersCtrl', OneExportOrdersCtrl);

})(window.angular);