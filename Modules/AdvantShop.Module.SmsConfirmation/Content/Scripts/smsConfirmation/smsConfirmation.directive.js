; (function (ng) {
    'use strict';

    ng.module('moduleSmsConfirmation')
        .directive('moduleSmsConfirmationStart', ['$compile', function ($compile) {
                return {
                    restrict: 'A',
                    link: function (scope, element, attrs, ctrl) {
                        var elements = document.querySelectorAll('[data-module-sms-confirmation="true"]');
                        $compile(elements)(scope);
                    }
                };
            }])
        .directive('moduleSmsConfirmation', function () {
            return {
                restrict: 'A',
                scope: {
                    pageType: '@',
                    isMobile: '=',
                    pageToRedirect: '@',
                    applyFn: '&'//GlorySoft_018
                },
                controller: 'moduleSmsConfirmationCtrl',
                controllerAs: 'moduleSmsConfirmation',
                bindToController: true,
                link: function (scope, element, attrs, ctrl) {
                    element.on('click', function (event) {
                        ctrl.dialogOpen();
                        scope.$apply();
                    });
                }
            };
        })
        //.directive('smsConfirmationAddField', function () {
        //    return {
        //        restrict: 'EA',
        //        scope: {
        //            pageType: '@',
        //            isMobile: '='
        //        },
        //        controller: 'SmsConfirmationCtrl',
        //        controllerAs: 'smsConfirmation',
        //        bindToController: true,
        //        replace: true,
        //        templateUrl: 'modules/smsConfirmation/content/scripts/smsConfirmation/templates/smsCodeField.html'
        //    };
        //})
        .directive('moduleSmsConfirmationAddField', ['$compile', 'moduleSmsConfirmationService', function ($compile, moduleSmsConfirmationService) {
            return {
                restrict: 'A',
                scope: {
                    pageType: '@',
                    isMobile: '='
                },
                controller: 'moduleSmsConfirmationCtrl',
                controllerAs: 'moduleSmsConfirmation',
                bindToController: true,
                link: function (scope, element, attrs, ctrl) {
                    moduleSmsConfirmationService.getSmsCodeField()
                        .then(function (template) {
                            ctrl.SmsCodeBlock = document.querySelector("[data-module-sms-confirmation-add-field]");

                            if (ctrl.pageType == 'checkout') {
                                ctrl.SubmitOrderButton = document.querySelector('.checkout-page form button.checkout__button-summary'); // Кнопка "Подтвердить заказ"

                                if (ctrl.SmsCodeBlock.childNodes.length > 0) { // Чекбокс "У меня уже есть учётная запись" переключался и сейчас не checked
                                    var phone = document.getElementById('Data_User_Phone'); // Номер тел. в десктопе
                                    if (phone == null)
                                        phone = document.querySelector('[name="Phone"]'); // Номер тел. в моб. версии
                                    ctrl.phone = phone.value;
                                    ctrl.smsCode = document.getElementById("smsConfirmationSmsCode").value;

                                    if (ctrl.SubmitOrderButton != null && ctrl.SubmitOrderButton.dataset.allowOrder == 'true') {
                                        ctrl.smsCodeConfirmed = true;
                                        ctrl.smsCodeEnabled = false;
                                    }
                                }
                            }

                            //ctrl.b.innerHTML = '';
                            ctrl.SmsCodeBlock.childNodes.forEach((i) => ctrl.SmsCodeBlock.removeChild(i));

                            var innerBlock = angular.element(template);
                            $compile(innerBlock)(scope);

                            element.append(innerBlock);
                            scope.$apply();

                            var phoneBlock = ctrl.getPhoneBlock();
                            if (phoneBlock != null)
                                phoneBlock.after(ctrl.SmsCodeBlock);
                        });

                    if (ctrl.pageType == 'registration') {
                        ctrl.registrationButton = document.querySelector(".registration-block-submit-btn-inner [data-button-validation]"); // Кнопка "Зарегистрироваться"
                        if (ctrl.registrationButton != null)
                            ctrl.registrationButton.setAttribute("disabled", "disabled");
                    }
                    if (ctrl.pageType == 'checkout') {
                        var userTypeCheckbox = document.querySelector('.checkout-usertype-label input'); // Чекбокс "У меня уже есть учётная запись"

                        if (userTypeCheckbox != null) {
                            ctrl.setCheckoutObserver(); // Наблюдение за кнопкой "Подтвердить заказ" и её Enable/Disable
                            userTypeCheckbox.addEventListener("click", userTypeHandler, { once: true }); // Обработчик для чекбокса "У меня уже есть учётная запись"
                        }

                        function userTypeHandler(e) {
                            if (e.target.checked) {
                                ctrl.SubmitOrderButton.dataset.allowOrder = 'true';
                                e.target.addEventListener("click", userTypeHandler, { once: true });
                            }
                            else {
                                if (!ctrl.smsCodeConfirmed) {
                                    ctrl.SubmitOrderButton.removeAttribute("data-allow-order");
                                    ctrl.SubmitOrderButton.setAttribute("disabled", "disabled");
                                }

                                setTimeout(function () {
                                    var phoneBlock = ctrl.getPhoneBlock();

                                    phoneBlock.after(ctrl.SmsCodeBlock);
                                    //document.body.after(ctrl.b);
                                    $compile(ctrl.SmsCodeBlock)(scope);
                                }, 1);
                            }
                        }
                    }
                }
            };
        }]);
})(angular);