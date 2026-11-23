(function () {
    'use strict';

    // Vadim_Dyl_381073705: личные данные ЛК — блокировка заполненных полей и заявка в Pyrus.
    angular.module('advMyAccount').factory('advPersonalService', advPersonalService);

    /* @ngInject */
    function advPersonalService($http, $window, toaster, modalService) {
        var service = {
            initial: {},
            change: {
                lastname: '',
                firstname: '',
                patronymic: '',
                phone: '',
                email: '',
                birthday: '',
            },
            changeFields: [],
            saved: false,
            init: init,
            isLocked: isLocked,
            markSaved: markSaved,
            prepareChange: prepareChange,
            sendRequest: sendRequest,
        };

        return service;

        function init(values) {
            service.initial = values || {};
            service.saved = false;
        }

        function isLocked(name) {
            return Boolean(service.initial && service.initial[name]);
        }

        function markSaved(values) {
            if (values) {
                service.initial = values;
            }
        }

        function prepareChange(user, customerType) {
            var isLegalEntity = customerType === 'LegalEntity';
            var passportFieldIds = [13, 14, 15, 16];

            service.change.lastname = user.lastname || '';
            service.change.firstname = user.firstname || '';
            service.change.patronymic = user.patronymic || '';
            service.change.phone = user.phone || '';
            service.change.email = user.email || '';
            service.change.birthday = user.birthday || '';

            service.changeFields.length = 0;

            if (Array.isArray(user.CustomerFields)) {
                // Vadim_Dyl_381073705: юрлицо — все реквизиты организации (кроме паспортных 13–16),
                // физлицо — только паспортные поля (прежнее поведение).
                user.CustomerFields.filter(function (field) {
                    var isPassportField = passportFieldIds.indexOf(field.Id) !== -1;
                    return isLegalEntity ? !isPassportField : isPassportField;
                }).forEach(function (field) {
                    service.changeFields.push({
                        Id: field.Id,
                        Name: field.Name,
                        FieldType: field.FieldType,
                        FieldAssignment: field.FieldAssignment,
                        Required: field.Required,
                        Value: field.Value,
                    });
                });
            }
        }

        function sendRequest(customerId) {
            return $http
                .post('user/sendRequestCommonInfo', {
                    customerId: customerId,
                    lastname: service.change.lastname,
                    firstname: service.change.firstname,
                    patronymic: service.change.patronymic,
                    phone: service.change.phone,
                    email: service.change.email,
                    birthday: formatBirthday(service.change.birthday),
                    CustomerFields: service.changeFields,
                })
                .then(
                    function (response) {
                        var data = response.data;

                        if (data && data.result) {
                            // Vadim_Dyl_381073705: если изменений нет, Pyrus-заявка не создаётся —
                            // не закрываем модалку и не показываем ложный успех.
                            if (!data.redirectTo) {
                                toaster.pop('info', '', 'Вы не изменили данные');
                                return false;
                            }

                            toaster.pop('success', '', 'Запрос на изменение данных отправлен');
                            modalService.close('modalPersonalChange');

                            $window.location = data.redirectTo;

                            return true;
                        }

                        showErrors(data);
                        return false;
                    },
                    function () {
                        toaster.pop('error', '', 'Не удалось отправить запрос');
                        return false;
                    },
                );
        }

        function formatBirthday(value) {
            if (!value) {
                return '';
            }

            if (/^\d{2}\.\d{2}\.\d{4}$/.test(value)) {
                return value;
            }

            var match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
            return match ? match[3] + '.' + match[2] + '.' + match[1] : value;
        }

        function showErrors(data) {
            var errors = data && Array.isArray(data.errors) ? data.errors : [];
            var title = data && data.title ? data.title : '';

            if (errors.length === 0) {
                toaster.pop('error', title, 'Не удалось отправить запрос');
                return;
            }

            errors.forEach(function (error) {
                toaster.pop('error', title, error);
            });
        }
    }
})();
