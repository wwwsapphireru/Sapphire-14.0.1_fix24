; (function (ng) {
    'use strict';

    var Rees46SettingsCtrl = function ($http, toaster) {
        var ctrl = this;

        ctrl.$onInit = function () {            
            ctrl.getSettings().then(function (result) {
                if (result) {
                    ctrl.btnLoading = false;
                    ctrl.btnRegister = false;
                }
            });
        };

        ctrl.getSettings = function () {
            return $http.get('../module/Rees46Admin/GetSettings').then(function (response) {
                var data = response.data;
                if (data.result === true) {
                    ctrl.settings = data.obj;

                    var isNotHaveShopKey = !ctrl.settings.ShopKey
                    ctrl.ShowRegisterButtons = isNotHaveShopKey;
                    ctrl.IsHaveAccount = !isNotHaveShopKey;
                  
                } else {
                    if (data.errors) {
                        data.errors.forEach(function (error) {
                            toaster.pop('error', error);
                        });
                    } else {
                        toaster.pop("error", "Не удалось получить настройки");
                    }
                }
                return response.data.result;
            });
        };

        ctrl.saveSettings = function () {
            ctrl.settings.FeedId = ctrl.currentFeed.Id;//GlorySoft_021
            $http.post("../module/Rees46Admin/SaveSettings", ctrl.settings).then(function (response) {
                var data = response.data;
                if (data.result === true) {
                    toaster.pop('success', '', 'Настройки сохранены');
                    ctrl.getSettings().finally(function () {
                        ctrl.btnLoading = false;
                    });
                } else {
                    data.errors.forEach(function (e) {
                        toaster.pop('error', '', e);
                    });
                    ctrl.btnLoading = false;
                }
            });
        };

        ctrl.register = function (form) {
            var toasterWait = toaster.pop('wait', "Идёт регистрация", null, null, 'template');
            $http.post("../module/Rees46Admin/Register", ctrl.registration).then(function (response) {
                var data = response.data;
                toaster.clear(toasterWait);
                if (data.result === true) {
                    toaster.pop('success', '', 'Вы зарегистрированы');
                    ctrl.getSettings();
                    if (form != null) {
                        form.$setPristine();
                    }
                } else {
                    data.errors.forEach(function (e) {
                        toaster.pop('error', '', e);
                    });
                }
                ctrl.btnRegister = false;
            });
        };

        ctrl.createNewAccount = function () {
            ctrl.IsHaveAccount = false;
            ctrl.ShowRegisterButtons = true;
        };
    };

    Rees46SettingsCtrl.$inject = ['$http', 'toaster'];

    ng.module('Rees46Settings', [])
        .controller('Rees46SettingsCtrl', Rees46SettingsCtrl)
        .component('rees46Settings', {
            templateUrl: '../modules/Rees46/scripts/admin/Rees46Settings/Rees46Settings.html',
            controller: 'Rees46SettingsCtrl'
        });

})(window.angular);