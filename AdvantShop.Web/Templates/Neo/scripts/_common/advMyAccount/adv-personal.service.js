(function () {
    'use strict';

    // Vadim_Dyl_381073705: личные данные ЛК — блокировка заполненных полей,
    // первичный ввод (пустое -> значение) сохраняется сразу, изменение заполненных уходит заявкой в Pyrus.
    angular.module('advMyAccount').factory('advPersonalService', advPersonalService);

    /* @ngInject */
    function advPersonalService($http, $window, $rootScope, toaster, modalService) {
        var PRIMARY_PERSONAL_KEYS = ['lastname', 'firstname', 'patronymic', 'phone', 'birthday'];
        var REQUEST_PERSONAL_KEYS = ['lastname', 'firstname', 'patronymic', 'phone', 'email', 'birthday'];

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
            hasPrimaryValues: hasPrimaryValues,
            hasRequestedChanges: hasRequestedChanges,
            hasClearedValues: hasClearedValues,
            canSubmit: canSubmit,
            submitLabel: submitLabel,
            isPrimaryPersonal: isPrimaryPersonal,
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
                    var value = field.Value != null ? field.Value : '';
                    service.changeFields.push({
                        Id: field.Id,
                        Name: field.Name,
                        FieldType: field.FieldType,
                        FieldAssignment: field.FieldAssignment,
                        Required: field.Required,
                        Value: value,
                        // снимок на момент открытия модалки: было ли поле заполнено
                        InitialValue: value,
                        WasFilled: isFilled(value),
                    });
                });
            }
        }

        function sendRequest(customerId, user) {
            if (hasClearedValues()) {
                toaster.pop('error', '', 'Заполненные поля нельзя очищать — изменение только через заявку');
                return false;
            }

            if (!canSubmit()) {
                toaster.pop('info', '', 'Вы не изменили данные');
                return false;
            }

            // отправляем только непустые значения: сервер сам решает,
            // что сохранить сразу (первичный ввод), а что отправить заявкой
            var payloadFields = service.changeFields
                .filter(function (field) {
                    return isFilled(field.Value);
                })
                .map(function (field) {
                    return {
                        Id: field.Id,
                        Name: field.Name,
                        FieldType: field.FieldType,
                        FieldAssignment: field.FieldAssignment,
                        Required: field.Required,
                        Value: field.Value,
                    };
                });

            return $http
                .post('user/sendRequestCommonInfo', {
                    customerId: customerId,
                    lastname: service.change.lastname,
                    firstname: service.change.firstname,
                    patronymic: service.change.patronymic,
                    phone: service.change.phone,
                    email: service.change.email,
                    birthday: formatBirthday(service.change.birthday),
                    CustomerFields: payloadFields,
                })
                .then(
                    function (response) {
                        var data = response.data;

                        if (data && data.result) {
                            if (data.requested && data.redirectTo) {
                                toaster.pop(
                                    'success',
                                    '',
                                    data.saved
                                        ? 'Данные сохранены, запрос на изменение отправлен'
                                        : 'Запрос на изменение данных отправлен',
                                );
                                modalService.close('modalPersonalChange');
                                $window.location = data.redirectTo;
                                return true;
                            }

                            if (data.saved) {
                                // первичный ввод принят без согласования — обновляем карточки на месте
                                applyPrimary(user);
                                $rootScope.$broadcast('myaccount:userSaved');
                                toaster.pop('success', '', 'Данные сохранены. Первичный ввод не требует согласования менеджера.');
                                modalService.close('modalPersonalChange');
                                return true;
                            }

                            toaster.pop('info', '', 'Вы не изменили данные');
                            return false;
                        }

                        // первичный ввод мог сохраниться, даже если заявку отправить не удалось
                        if (data && data.saved) {
                            applyPrimary(user);
                            $rootScope.$broadcast('myaccount:userSaved');
                            toaster.pop('success', '', 'Данные сохранены. Первичный ввод не требует согласования менеджера.');
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

        function getPrimaryFields() {
            return service.changeFields.filter(function (field) {
                return !field.WasFilled && isFilled(field.Value);
            });
        }

        function getChangedFields() {
            return service.changeFields.filter(function (field) {
                return field.WasFilled && isFilled(field.Value) && normalize(field.Value) !== normalize(field.InitialValue);
            });
        }

        function getClearedFields() {
            return service.changeFields.filter(function (field) {
                return field.WasFilled && !isFilled(field.Value);
            });
        }

        function getPrimaryPersonal() {
            var result = {};

            PRIMARY_PERSONAL_KEYS.forEach(function (key) {
                var initialValue = comparePersonalValue(key, service.initial[key]);
                var newValue = comparePersonalValue(key, service.change[key]);

                if (!initialValue && newValue) {
                    result[key] = service.change[key];
                }
            });

            return result;
        }

        function getRequestedPersonal() {
            var result = {};

            REQUEST_PERSONAL_KEYS.forEach(function (key) {
                var initialValue = comparePersonalValue(key, service.initial[key]);
                var newValue = comparePersonalValue(key, service.change[key]);

                if (!newValue || newValue === initialValue) {
                    return;
                }

                // email не сохраняем напрямую (регистрация/подтверждение по почте) — только заявкой
                if (key === 'email' || initialValue) {
                    result[key] = service.change[key];
                }
            });

            return result;
        }

        // дату сравниваем в едином формате: flatpickr может отдать ISO, initial — dd.MM.yyyy
        function comparePersonalValue(key, value) {
            return key === 'birthday' ? formatBirthday(value) : normalize(value);
        }

        function hasPrimaryValues() {
            return Object.keys(getPrimaryPersonal()).length > 0 || getPrimaryFields().length > 0;
        }

        function hasRequestedChanges() {
            return Object.keys(getRequestedPersonal()).length > 0 || getChangedFields().length > 0;
        }

        function hasClearedValues() {
            var clearedPersonal = REQUEST_PERSONAL_KEYS.some(function (key) {
                return Boolean(normalize(service.initial[key])) && !normalize(service.change[key]);
            });

            return clearedPersonal || getClearedFields().length > 0;
        }

        function canSubmit() {
            return hasPrimaryValues() || hasRequestedChanges() || hasClearedValues();
        }

        function isPrimaryPersonal(name) {
            return !isLocked(name);
        }

        function submitLabel() {
            var hasPrimary = hasPrimaryValues();
            var hasRequested = hasRequestedChanges();

            if (hasPrimary && hasRequested) {
                return 'Сохранить и отправить';
            }

            if (hasRequested) {
                return 'Отправить запрос';
            }

            return 'Сохранить';
        }

        function applyPrimary(user) {
            if (!user) {
                return;
            }

            var primaryPersonal = getPrimaryPersonal();
            Object.keys(primaryPersonal).forEach(function (key) {
                user[key] = service.change[key];
            });

            getPrimaryFields().forEach(function (field) {
                var target = (user.CustomerFields || []).filter(function (item) {
                    return item.Id === field.Id;
                })[0];

                if (target) {
                    target.Value = field.Value;
                }
            });
        }

        function isFilled(value) {
            var text = value != null ? String(value).trim() : '';
            return text !== '' && text !== '0';
        }

        function normalize(value) {
            return value != null ? String(value).trim() : '';
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
