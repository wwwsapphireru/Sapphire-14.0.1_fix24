(function () {
    'use strict';

    // Vadim_Dyl_381073705: паспорт в ЛК — поля 13–16 заполняются только вместе.
    // Валидатор вешается на поле серии, остальные поля находит в этой же форме.
    angular.module('advMyAccount')
        .directive('advPassportGroup', advPassportGroup);

    var PASSPORT_FIELD_IDS = [13, 14, 15, 16];
    var PASSPORT_FIELD_LENGTHS = { 13: 4, 14: 6 };

    function advPassportGroup() {
        return {
            restrict: 'A',
            require: ['ngModel', '^form'],
            link: function (scope, element, attrs, ctrls) {
                var formCtrl = ctrls[1];
                var formElement = formCtrl.$$element[0];
                var fields = [];
                var initTimer;

                function collectFields() {
                    var nodes = formElement.querySelectorAll('[data-adv-passport-field-id]');

                    fields = [];

                    Array.prototype.forEach.call(nodes, function (node) {
                        var id = parseInt(node.getAttribute('data-adv-passport-field-id'), 10);
                        var ngModel = angular.element(node).controller('ngModel');

                        if (PASSPORT_FIELD_IDS.indexOf(id) === -1 || !ngModel) {
                            return;
                        }

                        fields.push({ id: id, node: node, ngModel: ngModel, errorNode: null });
                    });
                }

                function getValue(ngModel) {
                    var value = ngModel.$viewValue;
                    return value == null ? '' : String(value).trim();
                }

                function isFilled(field) {
                    var value = getValue(field.ngModel);

                    // У даты маска «__.__.____», а из модели может прийти ISO — заполненной считаем только полную дату.
                    if (field.id === 15) {
                        return /^\d{2}\.\d{2}\.\d{4}$/.test(value) || /^\d{4}-\d{2}-\d{2}$/.test(value);
                    }

                    var cleared = value.replace(/_/g, '').trim();
                    return cleared !== '' && cleared !== '0';
                }

                function getError(field, started) {
                    if (!started) {
                        return null;
                    }

                    if (!isFilled(field)) {
                        return 'required';
                    }

                    var length = PASSPORT_FIELD_LENGTHS[field.id];
                    if (length && getValue(field.ngModel).replace(/\D/g, '').length !== length) {
                        return 'format';
                    }

                    return null;
                }

                function getErrorText(field, error) {
                    if (error === 'required') {
                        return 'Поле обязательно для заполнения';
                    }

                    if (field.id === 13) {
                        return 'Серия должна содержать 4 цифры';
                    }

                    if (field.id === 14) {
                        return 'Номер должен содержать 6 цифр';
                    }

                    return 'Неверный формат';
                }

                function renderError(field, error) {
                    var show = Boolean(error) && (error === 'required' || field.ngModel.$dirty);

                    field.ngModel.$setValidity('passportField', !show);

                    if (!show) {
                        angular.element(field.node).removeClass('adv-passport-invalid');

                        if (field.errorNode) {
                            field.errorNode.remove();
                            field.errorNode = null;
                        }

                        return;
                    }

                    angular.element(field.node).addClass('adv-passport-invalid');

                    if (!field.errorNode) {
                        field.errorNode = angular.element('<span class="adv-passport-error-text"></span>');
                        angular.element(field.node.parentNode).append(field.errorNode);
                    }

                    field.errorNode.text(getErrorText(field, error));
                }

                function validate() {
                    var started = fields.some(function (field) {
                        return isFilled(field);
                    });
                    var valid = true;

                    fields.forEach(function (field) {
                        var error = getError(field, started);

                        if (error) {
                            valid = false;
                        }

                        renderError(field, error);
                    });

                    formCtrl.$setValidity('passportGroup', valid);
                }

                var unwatch = scope.$watch(
                    function () {
                        return (
                            fields
                                .map(function (field) {
                                    return getValue(field.ngModel) + ':' + (field.ngModel.$dirty ? 1 : 0);
                                })
                                .join('|') + '|' + (formCtrl.$submitted ? 1 : 0)
                        );
                    },
                    validate,
                );

                // Поля паспорта идут после директивы в DOM — собираем их после линковки.
                initTimer = window.setTimeout(function () {
                    collectFields();
                    validate();
                }, 0);

                scope.$on('$destroy', function () {
                    unwatch();
                    window.clearTimeout(initTimer);
                });
            },
        };
    }
})();
