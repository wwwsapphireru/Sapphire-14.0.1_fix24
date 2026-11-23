(function () {
    'use strict';

    // Vadim_Dyl_381073705: общее состояние паспортного блока ЛК — карточка и модалка смены.
    angular.module('advMyAccount').factory('advPassportService', advPassportService);

    /* @ngInject */
    function advPassportService($http, $window, toaster, modalService) {
        var PASSPORT_FIELD_IDS = [13, 14, 15, 16];
        var service = {
            changeFields: [],
            isPassportField: isPassportField,
            isStarted: isStarted,
            prepareChange: prepareChange,
            sendRequest: sendRequest,
        };

        return service;

        function isPassportField(field) {
            return Boolean(field) && PASSPORT_FIELD_IDS.indexOf(field.Id) !== -1;
        }

        function getFields(user) {
            var fields = user && user.CustomerFields;

            if (!Array.isArray(fields)) {
                return [];
            }

            return fields.filter(isPassportField).sort(function (a, b) {
                return a.Id - b.Id;
            });
        }

        function isFilled(field) {
            var value = field && field.Value != null ? String(field.Value).trim() : '';
            return value !== '' && value !== '0';
        }

        function isStarted(user) {
            return getFields(user).some(isFilled);
        }

        function prepareChange(user) {
            var fields = getFields(user);

            service.changeFields.length = 0;

            fields.forEach(function (field) {
                service.changeFields.push({
                    Id: field.Id,
                    Name: field.Name,
                    FieldType: field.FieldType,
                    Value: field.Value,
                });
            });
        }

        function sendRequest(user, customerId, birthday) {
            return $http
                .post('user/sendRequestCommonInfo', {
                    customerId: customerId,
                    lastname: user.lastname,
                    firstname: user.firstname,
                    patronymic: user.patronymic,
                    phone: user.phone,
                    email: user.email,
                    birthday: birthday,
                    CustomerFields: service.changeFields,
                })
                .then(
                    function (response) {
                        var data = response.data;

                        if (data && data.result) {
                            toaster.pop('success', '', 'Запрос на изменение личных данных отправлен');
                            modalService.close('modalPassportChange');

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
