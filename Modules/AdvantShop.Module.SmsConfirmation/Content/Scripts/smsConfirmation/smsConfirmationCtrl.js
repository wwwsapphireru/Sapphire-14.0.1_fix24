; (function (ng) {

    'use strict';

    var SmsConfirmationCtrl = function ($sce, $window, $timeout, $http, moduleSmsConfirmationService, toaster, $location) {

        var ctrl = this;

        const waitingSeconds = 30/*GlorySoft_004 10*/;

        ctrl.$onInit = function () {
            ctrl.returnToFirstStep();
            ctrl.settings = {};
        };

        ctrl.returnToFirstStep = function () {
            ctrl.currentForm = 'first';
            ctrl.firstSend = false;
            ctrl.enableRetrySend = true;
            ctrl.smsCodeConfirmed = false;
            ctrl.phone = '';
            ctrl.smsCode = '';
            ctrl.smsCodeEnabled = true;
            ctrl.smsCodeModel = [];
            ctrl.phoneText = '';
            ctrl.errorPhone = false;
            ctrl.errorCaptcha = false;
            ctrl.errorCode = false;
            ctrl.errorReset = '';
            ctrl.errorMessage = '';//GlorySoft_004
        };

        ctrl.validate = function (noCheckSmsCode) {
            //if (ctrl.pageType !== 'login') {GlorySoft_018
            //    if (!noCheckSmsCode && (ctrl.smsCode === undefined || ctrl.smsCode === null || ctrl.smsCode === '' || ctrl.smsCode.length < 4)) {
            //        return false;
            //    }
				
            //    return true;
            //}
            /*else*/ if (!noCheckSmsCode && ctrl.currentForm === 'second') {
                if (!ctrl.isMobile) {
                    ctrl.smsCode = ctrl.smsCodeModel.join('');
                }

                if (ctrl.smsCode.length < 4) {
                    return false;
                }
            }

            if (ctrl.currentForm === 'first' && !ctrl.firstSend) {
				
                if (ctrl.phone === undefined || ctrl.phone === null || ctrl.phone === '' || ctrl.phone.length <= 0) {
                    ctrl.errorPhone = true;
                    //toaster.pop('error', '', 'Укажите корректный номер телефона');
                    return false;
                }

                if (ctrl.settings.useCaptcha && (ctrl.captchaCode === '' || ctrl.captchaCode === undefined || ctrl.captchaCode === null)) {
                    ctrl.errorCaptcha = true;
                    return false;
                }
            }

            return true;
        };

        ctrl.sendSmsCode = function () {
            if (!ctrl.validate(true))
                return;

            if (ctrl.pageType !== 'login') {
                var phoneBlockId = ctrl.pageType === 'registration' ? 'Phone' : 'Data_User_Phone';
                var phoneBlock = document.getElementById(phoneBlockId);

                if (ctrl.pageType === 'checkout' && phoneBlock == null) {
                    phoneBlock = document.querySelector('[name="Phone"]'); // Блок с номером тел в моб. версии
                }

                var phone = phoneBlock !== null ? phoneBlock.value : '';
                ctrl.phone = phone;
            }
            else {
                var phoneBlock = document.getElementById('smsConfirmationPhone'); // Номер тел. с модалки
                ctrl.phoneText = phoneBlock !== null ? phoneBlock.value : '';
            }

            var captchaExist = typeof (CaptchaSourceCallback) != "undefined" && CaptchaSourceCallback != null;
            var captchaInstanceId = captchaExist ? CaptchaSourceCallback.InstanceId : null;

            moduleSmsConfirmationService.sendSmsCode(ctrl.phone, ctrl.pageType, ctrl.captchaCode, captchaInstanceId).then(function (data) {
                if (data.result != true) {
                    if (data.errors != null && data.errors.length > 0) {
                        toaster.pop('error', null, data.errors[0]);//GlorySoft_004
                    }

                    if (ctrl.settings.useCaptcha && data.captchaError) {
                        ctrl.errorCaptcha = true;

                        if (captchaExist) {
                            CaptchaSourceCallback.ReloadImage();
                        }
                    }
                }
                else {
                    if (!ctrl.firstSend) {
                        ctrl.firstSend = true;
                        ctrl.currentForm = "second";
                    }

                    //toaster.pop('info', '', 'Код подтверждения выслан на номер ' + ctrl.phone);
                    ctrl.setCountdown();
                }
            });
        };

        ctrl.confirmSmsCode = function () {
            if (!ctrl.validate(false))
                return;

            moduleSmsConfirmationService.confirmSmsCode(ctrl.phone, ctrl.smsCode, ctrl.pageType).then(function (data) {
                if (data.result != true) {
                    if (data.resetToPhone === true) {
                        ctrl.returnToFirstStep(); // код аннулирован, снова просим номер телефона
                        ctrl.errorReset = data.errors != null && data.errors.length > 0 ? data.errors[0] : '';
                        return;
                    }

                    if (data.errors != null && data.errors.length > 0) {
                        //toaster.pop('error', null, data.errors[0]);
                        ctrl.errorCode = true;
                        ctrl.errorMessage = data.errors[0];//GlorySoft_004
                    }
                }
                else {
                    if (ctrl.pageType === 'login') {
                        ctrl.errorCode = false;
                        ctrl.errorMessage = '';//GlorySoft_004
                        $window.location.href = $window.location.origin + '/myaccount';
                    }
                    else {
                        if (ctrl.smsCodeConfirmed) {
                            return;
                        }

                        //toaster.pop('success', '', 'Код подтвержден');
                        ctrl.smsCodeConfirmed = true;

                        if (ctrl.pageType == 'registration' /*GlorySoft_018 && ctrl.registrationButton != null*/) {
                            //ctrl.registrationButton.removeAttribute("disabled");GlorySoft_018

                            //GlorySoft_018
                            smsConfirmationService.dialogClose();
                            ctrl.applyFn();
                        }

                        if (ctrl.pageType == 'checkout') {
                            //ctrl.setOrderAllowedAttribute();GlorySoft_018

                            //GlorySoft_018
                            smsConfirmationService.dialogClose();
                            ctrl.applyFn();
                        }

                        ctrl.smsCodeEnabled = false;
                    }
                }
            });
        };

        ctrl.setCountdown = function () {
            ctrl.enableRetrySend = false;

            ctrl.countdownSeconds = waitingSeconds;

            if (ctrl.pageType === 'login') {
                var counter = setInterval(function () {
                    ctrl.countdownSeconds--;
                    var timerBlock = document.getElementById("smsConfirmationCountdownTimer");

                    if (timerBlock !== null) {
                        timerBlock.innerHTML = 'Отправить код повторно можно через ' + ctrl.countdownSeconds + ' сек.';
                    }

                    if (ctrl.countdownSeconds <= 0) {
                        if (timerBlock !== null) {
                            timerBlock.innerHTML = '';
                        }

                        ctrl.enableRetrySend = true;
                        clearInterval(counter);

                        var form = document.querySelector('.sms-confirmation-modal');
                        if (form !== null) {
                            form.click();
                        }
                    }
                }, 1000);
            }
            else {
                $timeout(function () {
                    ctrl.enableRetrySend = true;
                }, ctrl.countdownSeconds * 1000);
            }
        };

        ctrl.dialogOpen = function () {
            //moduleSmsConfirmationService.setVisibleFooter(false);

            ctrl.currentForm = 'first';
            ctrl.firstSend = false;
            ctrl.enableRetrySend = true;
            ctrl.phone = "";
            ctrl.code = "";

            if (ctrl.pageType !== 'login') {//GlorySoft_018
                var phoneBlockId = ctrl.pageType === 'registration' ? 'Phone' : 'Data_User_Phone';
                var phoneBlock = document.getElementById(phoneBlockId);

                if (ctrl.pageType === 'checkout' && phoneBlock == null) {
                    phoneBlock = document.querySelector('[name="Phone"]'); // Блок с номером тел в моб. версии
                }

                var phone = phoneBlock !== null ? phoneBlock.value : '';
                ctrl.phone = phone;
            }

            moduleSmsConfirmationService.getFormSettings(ctrl.pageToRedirect,/*GlorySoft_018*/ ctrl.pageType).then(function (result) {
                ctrl.settings.FormTitle = $sce.trustAsHtml(result.settings.FormTitle);
                ctrl.settings.FormContent = $sce.trustAsHtml(result.settings.FormContent);
                ctrl.settings.SocialLinks = $sce.trustAsHtml(result.socialLinks);
                ctrl.settings.useCaptcha = result.settings.UseCaptcha;

                if (ctrl.settings.useCaptcha) {
                    ctrl.initCaptcha("moduleSmsConfirmation.captchaCode").then(function (data) {
                        ctrl.captchaHtml = data;
                        ctrl.captchaCode = "";
                    });
                }

                moduleSmsConfirmationService.dialogRender(ctrl.settings.FormTitle, ctrl);
            });
        };

        ctrl.setFocus = function (fromId, toId) {
            $timeout(function () {
                var from = document.getElementById(fromId);
                if (from !== null && from.value !== ' ' && from.value.length > 0) {
                    var to = document.getElementById(toId);
                    if (to !== null) {
                        to.focus();
                        to.selectionStart = 0;
                    }
                }
            }, 100);
        };

        // Наблюдение за кнопкой "Подтвердить заказ" и её Enable/Disable
        ctrl.setCheckoutObserver = function () {
            var submitOrderButton = document.querySelector('.checkout-page form button.checkout__button-summary'); // Кнопка "Подтвердить заказ"
            var Observer = new MutationObserver(function (mutations, observer) {
                if (!mutations[0].target.disabled) {
                    submitOrderButton.setAttribute("disabled", "disabled");
                }
                else
                    if (mutations[0].target.dataset.allowOrder == 'true') {
                        observer.disconnect();
                        submitOrderButton.removeAttribute("disabled");
                    }
            });
            Observer.observe(submitOrderButton, { attributes: true, attributeFilter: ["disabled", "data-allow-order"] });
        };

        ctrl.setOrderAllowedAttribute = function () { // Разрешить заказ
            if (ctrl.smsCodeConfirmed) {
                ctrl.SubmitOrderButton.dataset.allowOrder = 'true';
            }   
        };        
        //

        ctrl.getPhoneBlock = function () {
            var blockSelector = null;
            var parentSelector = '.row.middle-xs';

            if (ctrl.pageType == 'checkout') {
                blockSelector = 'Data_User_Phone';
            }
            if (ctrl.pageType == 'registration') {
                blockSelector = 'Phone';
            }

            var phoneBlock = document.getElementById(blockSelector).parentElement.closest(parentSelector); // Блок с ном. тел. в полной форме оф. заказа
            if (phoneBlock != null) {
                return document.getElementById(blockSelector).parentElement.closest(parentSelector);
            }
            //else { // Блок с ном. тел. в заказе в 1 клик в мобилке, пока убрал
            //    blockSelector = '[name="Phone"]';
            //    parentSelector = '.form-input-value';
            //    return document.querySelector(blockSelector).parentElement.closest(parentSelector);
            //}
            else
                return null;
        }

        ctrl.initCaptcha = function (ngModel) {
            return $http.post('/commonExt/getCaptchaHtml', { ngModel: ngModel, captchaId: 'CaptchaSourceCallback' }).then(function (response) {
                return $sce.trustAsHtml(response.data);
            });
        }
    };

    ng.module('moduleSmsConfirmation', [])
        .controller('moduleSmsConfirmationCtrl', SmsConfirmationCtrl);

    SmsConfirmationCtrl.$inject = ['$sce', '$window', '$timeout', '$http', 'moduleSmsConfirmationService', 'toaster', '$location'];

})(window.angular);