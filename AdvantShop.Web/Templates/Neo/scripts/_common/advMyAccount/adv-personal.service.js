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

        function prepareChange(user) {
            service.change.lastname = user.lastname || '';
            service.change.firstname = user.firstname || '';
            service.change.patronymic = user.patronymic || '';
            service.change.phone = user.phone || '';
            service.change.email = user.email || '';
            service.change.birthday = user.birthday || '';

            service.changeFields.length = 0;

            if (Array.isArray(user.CustomerFields)) {
                user.CustomerFields.filter(function (field) {
                    return [13, 14, 15, 16].indexOf(field.Id) !== -1;
                }).forEach(function (field) {
                    service.changeFields.push({
                        Id: field.Id,
                        Name: field.Name,
                        FieldType: field.FieldType,
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
                            toaster.pop('success', '', 'Запрос на изменение личных данных отправлен');
                            modalService.close('modalPersonalChange');

                            if (data.redirectTo) {
                                $window.location = data.redirectTo;
                            }

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
