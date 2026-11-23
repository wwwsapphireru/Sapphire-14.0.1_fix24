(function () {
    'use strict';

    // Vadim_Dyl_381073705: модалка изменения личных данных (заявка в Pyrus).
    angular.module('advMyAccount').controller('AdvPersonalModalCtrl', AdvPersonalModalCtrl);

    /* @ngInject */
    function AdvPersonalModalCtrl(advPersonalService, modalService) {
        var ctrl = this;

        ctrl.change = advPersonalService.change;
        // Vadim_Dyl_381073705: ветка юрлица — реквизиты организации из заявки
        ctrl.changeFields = advPersonalService.changeFields;

        ctrl.close = function () {
            modalService.close('modalPersonalChange');
        };

        ctrl.sendRequest = function () {
            advPersonalService.sendRequest(ctrl.customerId);
        };
    }
})();
