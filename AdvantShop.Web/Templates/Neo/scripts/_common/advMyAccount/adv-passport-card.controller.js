(function () {
    'use strict';

    // Vadim_Dyl_381073705: карточка паспорта в ЛК — блокировка после первого сохранения.
    angular.module('advMyAccount').controller('AdvPassportCardCtrl', AdvPassportCardCtrl);

    /* @ngInject */
    function AdvPassportCardCtrl($scope, $timeout, advPassportService) {
        var ctrl = this;

        ctrl.locked = false;

        ctrl.openChange = function () {
            advPassportService.prepareChange($scope.myaccount && $scope.myaccount.user);
        };

        ctrl.$onInit = function () {
            // Поля инициализируются ng-init внутри формы — ждём первый digest.
            $timeout(function () {
                ctrl.locked = advPassportService.isStarted($scope.myaccount && $scope.myaccount.user);
            }, 0);
        };

        $scope.$on('myaccount:userSaved', function () {
            ctrl.locked = advPassportService.isStarted($scope.myaccount && $scope.myaccount.user);
        });
    }
})();
