(function () {
    'use strict';

    // Vadim_Dyl_381073705: пустая маска (дата/телефон) не должна делать форму невалидной.
    angular.module('advMyAccount').directive('advMaskEmptyValid', advMaskEmptyValid);

    /* @ngInject */
    function advMaskEmptyValid($timeout) {
        return {
            restrict: 'A',
            require: 'ngModel',
            link: function (scope, element, attrs, ngModelCtrl) {
                $timeout(function () {
                    var validator = ngModelCtrl.$validators.mask;

                    if (!validator || validator.__advMaskEmptyValid) {
                        return;
                    }

                    var relaxed = function (modelValue, viewValue) {
                        var value = viewValue == null ? '' : String(viewValue);

                        if (value.replace(/\D/g, '') === '') {
                            return true;
                        }

                        return validator(modelValue, viewValue);
                    };

                    relaxed.__advMaskEmptyValid = true;
                    ngModelCtrl.$validators.mask = relaxed;
                    ngModelCtrl.$validate();
                }, 0);
            },
        };
    }
})();
