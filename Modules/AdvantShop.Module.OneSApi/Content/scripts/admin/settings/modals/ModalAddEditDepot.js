; (function (ng) {
    'use strict';

    var ModalAddEditDepotCtrl = function ($uibModalInstance, $http, $filter, toaster, $translate) {
        var ctrl = this;

        ctrl.$onInit = function () {
            var params = ctrl.$resolve;

            ctrl.id = params.id != null ? params.id : 0;
            ctrl.type = ctrl.id !== 0 ? "edit" : "add";
            ctrl.depot = {};

            //if (ctrl.id !== 0) {
            //    ctrl.getDepot();
            //} else {
            //    ctrl.depot.SortOrder = 0;
            //    ctrl.depot.Hint = 'В каталоге сайта отображаются остатки по данному складу';
            //}

            ctrl.getDepot().then(function () {
                if (ctrl.type == "add") {
                    ctrl.depot.SortOrder = 0;
                    //ctrl.depot.Hint = 'В каталоге сайта отображаются остатки по данному складу';
                }
            });
        };

        ctrl.close = function () {
            $uibModalInstance.dismiss('cancel');
        };


        ctrl.getDepot = function () {
            console.log('getDepot');
            return $http.get('../OneSAdmin/getDepot', { params: { id: ctrl.id } }).then(function (response) {
                console.log(response);
                var data = response.data;
                if (data != null) {
                    ctrl.depot = data.depot;
                    ctrl.departments = data.departments;
                }
            });
        }

        ctrl.save = function () {

            var url = ctrl.type === "add" ? '../OneSAdmin/addDepot' : '../OneSAdmin/updateDepot';

            console.log(ctrl.depot);
            $http.post(url, { depot: ctrl.depot }).then(function (response) {
                var data = response.data;
                if (data.result === true) {
                    $uibModalInstance.close();
                } else {
                    if (data.errors != null) {
                        data.errors.forEach(function(error) {
                            toaster.pop('error', '', error);
                        });
                    } else {
                        toaster.pop('error', '', 'Ошибка при сохранении');
                    }
                }
            });
        }

    };

    ModalAddEditDepotCtrl.$inject = ['$uibModalInstance', '$http', '$filter', 'toaster', '$translate'];

    ng.module('uiModal')
        .controller('ModalAddEditDepotCtrl', ModalAddEditDepotCtrl);

})(window.angular);