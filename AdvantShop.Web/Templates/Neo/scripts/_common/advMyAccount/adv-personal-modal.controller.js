(function () {
    'use strict';

    // Vadim_Dyl_381073705: модалка изменения данных (заявка в Pyrus + первичный ввод без согласования).
    angular.module('advMyAccount').controller('AdvPersonalModalCtrl', AdvPersonalModalCtrl);

    /* @ngInject */
    function AdvPersonalModalCtrl($scope, advPersonalService, modalService) {
        var ctrl = this;

        ctrl.change = advPersonalService.change;
        // Vadim_Dyl_381073705: ветка юрлица — реквизиты организации из заявки
        ctrl.changeFields = advPersonalService.changeFields;
        ctrl.isPrimaryPersonal = advPersonalService.isPrimaryPersonal;
        ctrl.submitLabel = advPersonalService.submitLabel;
        ctrl.canSubmit = advPersonalService.canSubmit;

        ctrl.close = function () {
            modalService.close('modalPersonalChange');
        };

        ctrl.sendRequest = function () {
            var user = $scope.myaccount && $scope.myaccount.user;
            advPersonalService.sendRequest(ctrl.customerId, user);
        };
    }
})();
