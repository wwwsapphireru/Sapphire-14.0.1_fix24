; (function (ng) {
    'use strict';
    

    var OneExportProductsCtrl = function (uiGridCustomConfig, $translate, $q, SweetAlert, OneModuleAdminService) {

        var ctrl = this,
            columnDefs = [
                {
                    name: 'ArtNo',
                    displayName: 'Артикул',
                    enableCellEdit: false,
                },
                {
                    name: 'Name',
                    displayName: 'Наименование',
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
                        '<a ng-href="product/edit/{{row.entity.ProductId}}" class="link-invert ui-grid-custom-service-icon fa fa-pencil-alt" target="_blank"></a>' +
                        '<ui-grid-custom-delete url="../OneSAdmin/deleteProductFromExport" params="{\'productId\': row.entity.ProductId, \'exportType\': row.entity.ExportType}"></ui-grid-custom-delete>' +
                        '</div></div>'
                }
            ];

        ctrl.gridOptions = ng.extend({}, uiGridCustomConfig, {
            columnDefs: columnDefs,
            uiGridCustom: {
                selectionOptions: [
                    {
                        text: 'Выгрузить выделенные в файл',
                        url: '../OneSAdmin/exportProductsToFile',
                        field: 'ProductId',
                        //before: function () {
                        //    ctrl.exportProductsToFileDate = new Date().toJSON().slice(0, 10);
                        //},
                        after: function () {
                            ctrl.showExportProgress = true;
                            OneModuleAdminService.initProgress(ctrl);
                        }
                    },
                    //{
                    //    text: $translate.instant('Admin.Js.Products.DeleteSelected'),
                    //    url: '../OneSAdmin/deleteProductsFromExport',
                    //    field: 'ProductId',
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

    OneExportProductsCtrl.$inject = ['uiGridCustomConfig', '$translate', '$q', 'SweetAlert', 'OneModuleAdminService'];

    ng.module('OneExportProducts', ['urlHelper'])
        .controller('OneExportProductsCtrl', OneExportProductsCtrl);

})(window.angular);