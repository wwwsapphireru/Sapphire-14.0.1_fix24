; (function (ng) {
    'use strict';

    var moduleSmsConfirmationService = function ($http, modalService) {
        var service = this;

        service.sendSmsCode = function (phone, pageType, captchaCode, captchaSource) {
            return $http.post('smsConfirmationClient/sendCode', { phone: phone, pageType: pageType, captchaCode: captchaCode, captchaSource: captchaSource, rnd: Math.random() }).then(function (response) {
                return response.data;
            });
        };

        service.confirmSmsCode = function (phone, smsCode, pageType) {
            return $http.post('smsConfirmationClient/confirmCode', { phone: phone, smsCode: smsCode, pageType: pageType, rnd: Math.random() }).then(function (response) {
                return response.data;
            });
        };

        service.getFormSettings = function (pageToRedirect,/*GlorySoft_018*/ pageType) {
            return $http.get('smsConfirmationClient/getFormSettings', { params: { pageToRedirect: pageToRedirect,/*GlorySoft_018*/ pageType: pageType}}).then(function (response) {
                return response.data;
            });
        };

        service.dialogRender = function (title, parentScope) {

            var options = {
                //'modalClass': '',
                'isOpen': true,
                'destroyOnClose': true,
                'isShowFooter': false
            };

            modalService.renderModal(
                'modalSmsConfirmation',
                title,
                '<div data-ng-include="\'/modules/smsConfirmation/content/scripts/smsConfirmation/templates/smsConfirmation.html\'"></div>',
                null,
                options,
                { moduleSmsConfirmation: parentScope });
        };

        service.dialogOpen = function () {
            modalService.open('modalSmsConfirmation');
        };

        service.dialogClose = function () {
            modalService.close('modalSmsConfirmation');
        };

        service.setVisibleFooter = function (visible) {
            modalService.setVisibleFooter('modalSmsConfirmation', visible);
        };

        service.getSmsCodeField = function () {
            return fetch('Modules/SmsConfirmation/Content/Scripts/smsConfirmation/templates/smsCodeField.html',
                {
                    method: 'GET',
                    headers: { 'Content-Type': 'text/html; charset=utf-8' }
                }).then(response => response.text())
                .then(function (result) {                   
                    return result;
                });
        };

    };

    ng.module('moduleSmsConfirmation')
        .service('moduleSmsConfirmationService', moduleSmsConfirmationService);

    moduleSmsConfirmationService.$inject = ['$http', 'modalService'];
        
})(angular);