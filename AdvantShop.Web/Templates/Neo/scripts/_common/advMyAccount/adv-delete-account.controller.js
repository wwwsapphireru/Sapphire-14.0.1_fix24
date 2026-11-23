(function () {
    'use strict';

    // Vadim_Dyl_381073705: запрос на удаление личного кабинета (нативный Pyrus-механизм 14.0.1).
    angular.module('advMyAccount').controller('AdvDeleteAccountModalCtrl', AdvDeleteAccountModalCtrl);

    /* @ngInject */
    function AdvDeleteAccountModalCtrl($http, $window, toaster, modalService) {
        var ctrl = this;

        ctrl.sendRequest = function () {
            $http
                .post('user/sendRequestDeleteAccount', { customerId: ctrl.customerId })
                .then(
                    function (response) {
                        var data = response.data;

                        if (data && data.result) {
                            toaster.pop('success', '', 'Запрос на удаление учетной записи отправлен');
                            modalService.close('modalDeleteAccount');

                            if (data.redirectTo) {
                                $window.location = data.redirectTo;
                            }

                            return;
                        }

                        var errors = data && Array.isArray(data.errors) ? data.errors : [];
                        var title = data && data.title ? data.title : '';

                        if (errors.length === 0) {
                            toaster.pop('error', title, 'Не удалось отправить запрос');
                            return;
                        }

                        errors.forEach(function (error) {
                            toaster.pop('error', title, error);
                        });
                    },
                    function () {
                        toaster.pop('error', '', 'Не удалось отправить запрос');
                    },
                );
        };

        ctrl.close = function () {
            modalService.close('modalDeleteAccount');
        };
    }
})();
