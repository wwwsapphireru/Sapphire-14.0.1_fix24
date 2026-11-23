(function () {
    'use strict';

    // Vadim_Dyl_381073705: подтверждение телефона в ЛК по SMS (модуль SmsConfirmation).
    angular.module('advMyAccount').controller('AdvConfirmPhoneModalCtrl', AdvConfirmPhoneModalCtrl);

    /* @ngInject */
    function AdvConfirmPhoneModalCtrl($scope, $http, $timeout, toaster, modalService) {
        var ctrl = this;
        var timer;

        ctrl.phone = '';
        ctrl.smsCode = '';
        ctrl.codeSent = false;
        ctrl.codeConfirmed = false;
        ctrl.countdown = 0;
        ctrl.error = '';

        ctrl.$onInit = function () {
            $timeout(function () {
                ctrl.phone = getPhone();
            }, 0);
        };

        ctrl.sendCode = function () {
            if (!ctrl.phone || ctrl.phone.indexOf('_') !== -1) {
                ctrl.error = 'Введите корректный номер телефона';
                return;
            }

            $http
                .post('smsConfirmationClient/sendsmscode', { phone: ctrl.phone, pageType: 'checkout' })
                .then(
                    function (response) {
                        var data = response.data;

                        if (data && data.result) {
                            ctrl.codeSent = true;
                            ctrl.error = '';
                            startCountdown();
                            toaster.pop('success', '', 'Код отправлен');
                            return;
                        }

                        ctrl.error = getError(data, 'Не удалось отправить код');
                    },
                    function () {
                        ctrl.error = 'Не удалось отправить код';
                    },
                );
        };

        ctrl.confirmCode = function () {
            if (!ctrl.smsCode || ctrl.smsCode.length !== 4) {
                return;
            }

            $http
                .post('smsConfirmationClient/confirmsmscode', {
                    phone: ctrl.phone,
                    smsCode: ctrl.smsCode,
                    pageType: 'checkout',
                })
                .then(
                    function (response) {
                        var data = response.data;

                        if (data && data.result) {
                            markConfirmed();
                            return;
                        }

                        ctrl.error = getError(data, 'Неверный код подтверждения');
                    },
                    function () {
                        ctrl.error = 'Не удалось проверить код';
                    },
                );
        };

        ctrl.close = function () {
            modalService.close('modalConfirmPhone');
        };

        ctrl.$onDestroy = function () {
            if (timer) {
                $timeout.cancel(timer);
            }
        };

        function markConfirmed() {
            $http
                .post('user/sendRequestConfirmPhone', { customerId: ctrl.customerId, phone: ctrl.phone })
                .then(
                    function (response) {
                        var data = response.data;

                        if (!data || !data.result) {
                            ctrl.error = getError(data, 'Не удалось подтвердить телефон');
                            return;
                        }

                        ctrl.codeConfirmed = true;

                        if ($scope.myaccount) {
                            $scope.myaccount.phoneConfirmed = true;
                            $scope.myaccount.user = $scope.myaccount.user || {};
                            $scope.myaccount.user.phone = ctrl.phone;
                            $scope.$root.$broadcast('myaccount:userSaved');
                        }

                        toaster.pop('success', '', 'Телефон подтвержден');
                        modalService.close('modalConfirmPhone');
                    },
                    function () {
                        ctrl.error = 'Не удалось подтвердить телефон';
                    },
                );
        }

        function startCountdown() {
            ctrl.countdown = 60;

            if (timer) {
                $timeout.cancel(timer);
            }

            timer = $timeout(tick, 1000);
        }

        function tick() {
            if (ctrl.countdown <= 0) {
                return;
            }

            ctrl.countdown--;
            timer = $timeout(tick, 1000);
        }

        function getPhone() {
            return ($scope.myaccount && $scope.myaccount.user && $scope.myaccount.user.phone) || '';
        }

        function getError(data, fallback) {
            if (data && data.msg) {
                return data.msg;
            }

            if (data && Array.isArray(data.errors) && data.errors.length > 0) {
                return data.errors[0];
            }

            return fallback;
        }
    }
})();
