;(function(ng) {

    'use strict';

    var RARShowNFCtrl = function($http, $sce, $compile, $scope, toaster, modalService) {
        var ctrl = this;

        ctrl.form = {};
        ctrl.FormRequest = {};
        
        var modalsStorage = {};

        ctrl.$onInit = function () {
            ctrl.currentPage = 'main';
            
            ctrl.FormRequest.ProductOfferId = ctrl.productOfferId;
            ctrl.FormRequest.ProductId = ctrl.productId;
            
            ctrl.FormRequest.SendNotification = false;
            ctrl.showNotificationForm();
            try {
                createMagicElement();
            } catch (e) {

            }
        };

        ctrl.$postLink = function() {
            if (document.documentElement.classList.contains('is-mobile') === false) {
                const button = document.querySelector('#RemindModuleShowNotificationFormBlock');
                if (button != null) {
                    button.style.marginLeft = '10px';
                    button.classList.add('details-payment-cell');

                    let elBlock = document.querySelector('.details-payment .details-payment-block');
                    if (elBlock != null) {
                        elBlock.append(button);
                    }
                }
            }
        };

        ctrl.showNotificationForm = function() {
            $http.get('landingrarclient/getNotificationForm').then(function success(response) {
                ctrl.form = response.data.Form;
                ctrl.form.HeaderForm = $sce.trustAsHtml(ctrl.form.FormHeader);
                ctrl.form.ShowCommentInForm = response.data.Form.ShowCommentInForm;
                ctrl.form.ShowEmailInForm = response.data.Form.ShowEmailInForm;
                ctrl.form.ShowNameInForm = response.data.Form.ShowNameInForm;
                ctrl.form.ShowSurnameInForm = response.data.Form.ShowSurnameInForm;
                ctrl.form.ShowPhoneNumberInForm = response.data.Form.ShowPhoneNumberInForm;
                ctrl.form.AfterFormTextForUser = $sce.trustAsHtml(ctrl.form.AfterFormTextForUser);

                ctrl.FormRequest.Email = ctrl.form.Email;
                ctrl.FormRequest.Name = ctrl.form.FirstName;
                ctrl.FormRequest.Surname = ctrl.form.LastName;
                ctrl.FormRequest.PhoneNumber = ctrl.form.Phone;
                ctrl.form.IsShowUserAgreementText = response.data.Form.IsShowUserAgreementText;
                ctrl.form.UserAgreementText = response.data.Form.UserAgreementText;

                ctrl.FormRequest = response.data.Form.FormRequest;//GlorySoft_012
            });
        };

        ctrl.validate = function() {
            if (ctrl.form.ShowEmailInForm === true &&
                (ctrl.FormRequest.Email === undefined || ctrl.FormRequest.Email === null || ctrl.FormRequest.Email === '')) {
                toaster.pop('error', '', 'Введите Email!');
                return false;
            }
            if (ctrl.form.ShowNameInForm === true &&
                (ctrl.FormRequest.Name === undefined || ctrl.FormRequest.Name === null || ctrl.FormRequest.Name === '')) {
                toaster.pop('error', '', 'Введите имя!');
                return false;
            }
            if (ctrl.form.ShowSurnameInForm === true &&
                (ctrl.FormRequest.Surname === undefined || ctrl.FormRequest.Surname === null || ctrl.FormRequest.Surname === '')) {
                toaster.pop('error', '', 'Введите фамилию!');
                return false;
            }
            if (ctrl.form.ShowPhoneNumberInForm === true &&
                (ctrl.FormRequest.PhoneNumber === undefined || ctrl.FormRequest.PhoneNumber === null || ctrl.FormRequest.PhoneNumber === '')) {
                toaster.pop('error', '', 'Введите номер телефона!');
                return false;
            }
            if (ctrl.form.IsShowUserAgreementText === true &&
                (ctrl.FormRequest.Agreement === undefined || ctrl.FormRequest.Agreement === false)){
                toaster.pop('error', '', 'Необходимо принять пользовательское соглашение!');
                return false;
            }
            return true;
        };

        ctrl.sending = function() {
            if (!ctrl.validate()) {
                return;
            }

            ctrl.FormRequest.SendNotification = false;
            ctrl.FormRequest.ProductOfferId = ctrl.productOfferId;
            ctrl.FormRequest.ProductId = ctrl.productId;

            $http.post('landingrarclient/SendInfo', { FormRequest: ctrl.FormRequest }).then(function success(response) {
                if (response.data === true) {
                    ctrl.currentPage = 'success';
                    toaster.pop('success', '', 'Ваша заявка успешно отправлена');
                } else {
                    toaster.pop('error', '', 'Не удалось отправить заявку');
                }
            });
        };

        ctrl.modalRender = function(parentScope, modalId) {
            modalService.getModal(modalId).then(function(modal) {
                modal.modalScope.open();
            });
        };

        ctrl.modalOpen = function(modalId) {
            if (!modalsStorage[modalId]) {
                ctrl.modalRender(ctrl, modalId);
            } else {
                modalService.open(modalId);
            }
            modalsStorage[modalId] = modalId;
        };

        function DeleteDublicateConvert() {
            var extraBlock = document.getElementsByClassName('remind-about-receipt-modal-header-icon');
            if (extraBlock.length == 0)
                setTimeout(DeleteDublicateConvert, 10);
            for (var i = 0; i < extraBlock.length; i++) {
                if (extraBlock.length > 1) {
                    extraBlock[i + 1].parentNode.removeChild(extraBlock[i + 1]);
                }
            }
        }

        function createMagicElement() {
            var magicEl = document.createElement('div');
            var elContent = document.createTextNode('Должен отображаться');
            magicEl.appendChild(elContent);

            var findElMas = document.getElementsByClassName('first-payment-price');
            var findEl;
            for (var i = 0; i < findElMas.length; i++) {
                if (findElMas.length == 1) {
                    findEl = findElMas[i];
                }
                if (findElMas.length != 0) {
                    findEl = findElMas[0];
                }
            }

            findEl.parentNode.insertBefore(magicEl, findEl);
            magicEl.setAttribute('id', 'magicElement');
            magicEl.style.display = 'none';

            $compile(magicEl)($scope);
        }

        ctrl.modalClose = function(modalId) {
            modalService.close(modalId);
        };
    };

    RARShowNFCtrl.$inject = ['$http', '$sce', '$compile', '$scope', 'toaster', 'modalService'];

    ng.module('RARShowNF', [])
        .controller('RARShowNFCtrl', RARShowNFCtrl)
        .component('rarShowNF', {
            templateUrl: 'modules/RemindAboutReceipt/content/scripts/showNotificationForm/templates/showNotificationForm.html',
            controller: 'RARShowNFCtrl',
            bindings: {
                productOfferId: '<',
                productId: '<',

            },
        });

})(window.angular);