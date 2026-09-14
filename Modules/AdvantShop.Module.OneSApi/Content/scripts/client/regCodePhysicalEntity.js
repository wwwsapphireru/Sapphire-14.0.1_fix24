; (function (ng) {
    'use strict';

    var RegCodePhysicalEntityCtrl = function ($http, toaster) {

        var ctrl = this;

        ctrl.retrySendRegCode = function (customerId, email) {
            $http.post("/ones/retrySendRegCode", { customerId: customerId, email: email }).then(function (response) {
                if (response.data.result === true) {
                    toaster.pop('success', 'Письмо отправлено');
                } else {
                    toaster.pop('error', 'Ошибка при отправке', response.data.errors[0]);
                }
            });
        };

        ctrl.sendFeedback = function (customerId) {
            $http.post("/ones/retrySendRegCode", { customerId: customerId, createTask: true }).then(function (response) {
                if (response.data.result === true) {
                    toaster.pop('success', 'Уведомление отправлено администрации магазина');
                } else {
                    toaster.pop('error', 'Ошибка при отправке', response.data.errors[0]);
                }
            });
        };

    };

    RegCodePhysicalEntityCtrl.$inject = ['$http', 'toaster'];

    ng.module('regCodePhysicalEntity', [])
        .controller('RegCodePhysicalEntityCtrl', RegCodePhysicalEntityCtrl);

})(window.angular);