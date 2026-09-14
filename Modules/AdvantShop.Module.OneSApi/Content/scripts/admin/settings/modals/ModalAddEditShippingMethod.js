; (function (ng) {
    'use strict';

    var ModalAddEditShippingMethodCtrl = function ($uibModalInstance, $http, $filter, toaster, $translate) {
        var ctrl = this;

        ctrl.$onInit = function () {
            var params = ctrl.$resolve;

            ctrl.id = params.id;
            //ctrl.type = ctrl.id !== 0 ? "edit" : "add";
            ctrl.method = {};

            //if (ctrl.id !== 0) {
            //    ctrl.getDepot();
            //} else {
            //    ctrl.depot.SortOrder = 0;
            //    ctrl.depot.Hint = 'В каталоге сайта отображаются остатки по данному складу';
            //}

            ctrl.getShippingMethod().then(function () {
                ctrl.shippingMethodKey = ctrl.shippingMethodKeys.filter(function (x) { return x.value == ctrl.method.ShippingMethodKey; })[0];
            });
        };

        ctrl.close = function () {
            $uibModalInstance.dismiss('cancel');
        };


        ctrl.getShippingMethod = function () {
            return $http.get('../OneSAdmin/getShippingMethod', { params: { key: ctrl.id } }).then(function (response) {
                console.log(response);
                var data = response.data;
                if (data != null) {
                    ctrl.method = data.method;
                    ctrl.shippingMethodKeys = data.shippingMethodKeys;
                }
            });
        }

        ctrl.save = function () {
            ctrl.method.ShippingMethodKey = ctrl.shippingMethodKey.value;

            var url = '../OneSAdmin/addUpdateShippingMethod';

            console.log(ctrl.method);
            $http.post(url, { method: ctrl.method }).then(function (response) {
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

    ModalAddEditShippingMethodCtrl.$inject = ['$uibModalInstance', '$http', '$filter', 'toaster', '$translate'];

    ng.module('uiModal')
        .controller('ModalAddEditShippingMethodCtrl', ModalAddEditShippingMethodCtrl);

})(window.angular);