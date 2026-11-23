(function () {
    'use strict';

    // Vadim_Dyl_381073705: карточка личных данных — блокировка заполненных полей.
    angular.module('advMyAccount').controller('AdvPersonalCardCtrl', AdvPersonalCardCtrl);

    /* @ngInject */
    function AdvPersonalCardCtrl($scope, advPersonalService) {
        var ctrl = this;

        ctrl.isLocked = function (name) {
            return advPersonalService.isLocked(name);
        };

        ctrl.init = function (values) {
            advPersonalService.init(values);

            var account = $scope.myaccount;

            if (!account) {
                return;
            }

            account.user = account.user || {};
            account.phoneConfirmed = Boolean(values && values.phoneConfirmed);

            ['lastname', 'firstname', 'patronymic', 'phone', 'email', 'birthday'].forEach(function (key) {
                if (!account.user[key] && values && values[key]) {
                    account.user[key] = values[key];
                }
            });
        };

        ctrl.formatBirthday = function (value) {
            if (!value) {
                return '';
            }

            if (/^\d{2}\.\d{2}\.\d{4}$/.test(value)) {
                return value;
            }

            var match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
            return match ? match[3] + '.' + match[2] + '.' + match[1] : value;
        };

        ctrl.openChange = function () {
            advPersonalService.prepareChange($scope.myaccount && $scope.myaccount.user);
        };

        $scope.$on('myaccount:userSaved', function () {
            var user = $scope.myaccount && $scope.myaccount.user;

            advPersonalService.markSaved(user ? {
                lastname: user.lastname,
                firstname: user.firstname,
                patronymic: user.patronymic,
                phone: user.phone,
                email: user.email,
                birthday: user.birthday,
            } : null);
        });
    }
})();
