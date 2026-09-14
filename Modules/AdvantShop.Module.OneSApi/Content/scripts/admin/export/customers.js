; (function (ng) {
    'use strict';
    

    var OneExportCustomersCtrl = function (uiGridCustomConfig, $translate, $q, SweetAlert, OneModuleAdminService) {

        var ctrl = this,
            columnDefs = [
                {
                    name: 'FullName',
                    displayName: $translate.instant(
                        'Admin.Js.Customers.Customer',
                    ),
                    cellTemplate:
                        '<span>{{row.entity.Organization != null && row.entity.Organization.length > 0 ? row.entity.Organization : row.entity.FullName }}</span>',
                },
                {
                    name: 'Changed',
                    displayName: 'Дата изменения',
                    enableCellEdit: false,
                },
                {
                    name: '_serviceColumn',
                    displayName: '',
                    width: 120,
                    cellTemplate:
                        '<div class="ui-grid-cell-contents"><div>' +
                        '<a ng-href="customers/view/{{row.entity.CustomerId}}" class="link-invert ui-grid-custom-service-icon fa fa-pencil-alt" target="_blank"></a>' +
                        '<ui-grid-custom-delete url="../OneSAdmin/deleteCustomerFromExport" params="{\'customerId\': row.entity.CustomerId, \'exportType\': row.entity.ExportType}"></ui-grid-custom-delete>' +
                        '</div></div>'
                }
            ];

        ctrl.gridOptions = ng.extend({}, uiGridCustomConfig, {
            columnDefs: columnDefs,
            uiGridCustom: {
                selectionOptions: [
                    {
                        text: 'Выгрузить выделенные в файл',
                        url: '../OneSAdmin/exportCustomersToFile',
                        field: 'CustomerId',
                        //before: function () {
                        //    ctrl.exportCustomersToFileDate = new Date().toJSON().slice(0, 10);
                        //},
                        after: function () {
                            ctrl.showExportProgress = true;
                            OneModuleAdminService.initProgress(ctrl);
                        }
                    },
                    //{
                    //    text: $translate.instant('Admin.Js.Customers.DeleteSelected'),
                    //    url: '../OneSAdmin/deleteCustomersFromExport',
                    //    field: 'CustomerId',
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

    OneExportCustomersCtrl.$inject = ['uiGridCustomConfig', '$translate', '$q', 'SweetAlert', 'OneModuleAdminService'];

    ng.module('OneExportCustomers', ['urlHelper'])
        .controller('OneExportCustomersCtrl', OneExportCustomersCtrl);

})(window.angular);