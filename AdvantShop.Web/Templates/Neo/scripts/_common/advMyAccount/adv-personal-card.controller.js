(function () {
    'use strict';

    // Vadim_Dyl_381073705: карточка личных данных — блокировка заполненных полей.
    angular.module('advMyAccount').controller('AdvPersonalCardCtrl', AdvPersonalCardCtrl);

    /* @ngInject */
    function AdvPersonalCardCtrl($scope, $timeout, advPersonalService) {
        var ctrl = this;
        var LEGAL_ENTITY = 'LegalEntity';

        ctrl.isLegalEntity = false;
        ctrl.orgLocked = {};

        ctrl.isLocked = function (name) {
            return advPersonalService.isLocked(name);
        };

        ctrl.init = function (values) {
            advPersonalService.init(values);

            var account = $scope.myaccount;

            ctrl.isLegalEntity = Boolean(values && values.customerType === LEGAL_ENTITY);

            if (!account) {
                return;
            }

            account.user = account.user || {};
            account.phoneConfirmed = Boolean(values && values.phoneConfirmed);

            ['lastname', 'firstname', 'patronymic', 'phone', 'email', 'birthday'].forEach(function (key) {
                if (!account.user[key] && values && values[key]) {
                    account.user[key] = values[key];
                }
            });

            // Поля CustomerFields заполняет ng-init внутри формы — ждём первый digest.
            $timeout(ctrl.refreshOrgLocks, 0);
        };

        // Vadim_Dyl_381073705: реквизиты юрлица — заполненные блокируются, пустые доступны до сохранения.
        ctrl.refreshOrgLocks = function () {
            var fields = ($scope.myaccount && $scope.myaccount.user && $scope.myaccount.user.CustomerFields) || [];
            var locked = {};

            fields.forEach(function (field, index) {
                if (!field || field.Id > 12) {
                    return;
                }

                locked[index] = isFilled(field.Value);
            });

            ctrl.orgLocked = locked;
        };

        ctrl.isOrgFieldLocked = function (index) {
            return Boolean(ctrl.orgLocked[index]);
        };

        ctrl.hasUnlockedOrgFields = function () {
            return Object.keys(ctrl.orgLocked).some(function (index) {
                return !ctrl.orgLocked[index];
            });
        };

        // Vadim_Dyl_381073705: кнопки карточек юрлица — у «Контактного лица» и «Организации» свои.
        ctrl.contactCanSave = function () {
            return !ctrl.isLocked('lastname') || !ctrl.isLocked('firstname') || !ctrl.isLocked('patronymic');
        };

        ctrl.contactCanEdit = function () {
            return ctrl.isLocked('lastname') && ctrl.isLocked('firstname');
        };

        ctrl.orgCanSave = function () {
            return ctrl.hasUnlockedOrgFields();
        };

        ctrl.orgCanEdit = function () {
            return !ctrl.hasUnlockedOrgFields();
        };

        ctrl.canSave = function () {
            if (ctrl.isLegalEntity) {
                return (
                    !ctrl.isLocked('lastname') ||
                    !ctrl.isLocked('firstname') ||
                    !ctrl.isLocked('patronymic') ||
                    ctrl.hasUnlockedOrgFields()
                );
            }

            return (
                !ctrl.isLocked('lastname') ||
                !ctrl.isLocked('firstname') ||
                !ctrl.isLocked('patronymic') ||
                !ctrl.isLocked('birthday')
            );
        };

        ctrl.canEdit = function () {
            if (ctrl.isLegalEntity) {
                return ctrl.isLocked('lastname') && ctrl.isLocked('firstname') && !ctrl.hasUnlockedOrgFields();
            }

            return ctrl.isLocked('lastname') && ctrl.isLocked('firstname');
        };

        ctrl.formatBirthday = function (value) {
            if (!value) {
                return '';
            }

            if (/^\d{2}\.\d{2}\.\d{4}$/.test(value)) {
                return value;
            }

            var match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
            return match ? match[3] + '.' + match[2] + '.' + match[1] : value;
        };

        ctrl.openChange = function () {
            advPersonalService.prepareChange(
                $scope.myaccount && $scope.myaccount.user,
                ctrl.isLegalEntity ? LEGAL_ENTITY : 'PhysicalEntity',
            );
        };

        $scope.$on('myaccount:userSaved', function () {
            var user = $scope.myaccount && $scope.myaccount.user;

            advPersonalService.markSaved(user ? {
                lastname: user.lastname,
                firstname: user.firstname,
                patronymic: user.patronymic,
                phone: user.phone,
                email: user.email,
                birthday: user.birthday,
            } : null);

            ctrl.refreshOrgLocks();
        });

        function isFilled(value) {
            var text = value != null ? String(value).trim() : '';
            return text !== '' && text !== '0';
        }
    }
})();
