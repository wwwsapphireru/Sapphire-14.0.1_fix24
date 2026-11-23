(function () {
    'use strict';

    // Vadim_Dyl_381073705: модалка запроса на смену паспортных данных.
    angular.module('advMyAccount').controller('AdvPassportModalCtrl', AdvPassportModalCtrl);

    /* @ngInject */
    function AdvPassportModalCtrl($scope, advPassportService, modalService) {
        var ctrl = this;

        ctrl.fields = advPassportService.changeFields;

        ctrl.close = function () {
            modalService.close('modalPassportChange');
        };

        ctrl.sendRequest = function () {
            var user = $scope.myaccount && $scope.myaccount.user;
            var birthdayInput = document.querySelector('input[name="birthday"]');

            advPassportService.sendRequest(user, ctrl.customerId, birthdayInput ? birthdayInput.value : '');
        };
    }
})();
