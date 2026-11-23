(function () {
    'use strict';

    // Vadim_Dyl_381073705: цифровые паспортные поля (серия/номер).
    // Длина задаётся атрибутом data-adv-passport-length, чтобы не хардкодить Id полей.
    angular.module('advMyAccount')
        .directive('advPassportInput', advPassportInput);

    function advPassportInput() {
        return {
            restrict: 'A',
            require: 'ngModel',
            link: function (scope, element, attrs, ngModelCtrl) {
                var length = parseInt(attrs.advPassportLength, 10);

                if (!length) {
                    return;
                }

                element.attr('inputmode', 'numeric');
                element.attr('placeholder', length === 4 ? '____' : new Array(length + 1).join('_'));

                ngModelCtrl.$validators.passportLength = function (modelValue, viewValue) {
                    var digits = String(viewValue == null ? '' : viewValue).replace(/\D/g, '');
                    return digits.length === 0 || digits.length === length;
                };

                ngModelCtrl.$parsers.push(function (value) {
                    return String(value == null ? '' : value).replace(/\D/g, '').slice(0, length);
                });
            }
        };
    }
})();
