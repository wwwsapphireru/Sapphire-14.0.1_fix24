import { PubSub } from '../../_common/PubSub/PubSub.js';

/* @ngInject */
function FeedbackCtrl($http, toaster,/*GlorySoft_014*/ Upload, $q, $window, $timeout, $translate) {
    const ctrl = this;
    //const timerProcessCompany;//GlorySoft_014
    ctrl.images = [];//GlorySoft_014
    ctrl.files = [];//GlorySoft_014

    ctrl.switchTheme = function (val) {
        ctrl.curTheme = val;
    };

    ctrl.isSelectedTheme = function (val) {
        return ctrl.curTheme === val;
    };

    ctrl.send = function () {
        const captchaExist = typeof CaptchaSource != 'undefined' && CaptchaSource != null;
        const captchaSource = captchaExist ? CaptchaSource.InstanceId : null;

        const params = {
            messageType: ctrl.curTheme,
            message: ctrl.message,
            orderNumber: ctrl.orderNumber,
            name: ctrl.name,
            email: ctrl.email,
            phone: ctrl.phone,
            agree: ctrl.agreement,
            captchaCode: ctrl.captchaCode,
            captchaSource,
        };

        $http.post('feedback/feedbackForm', params).then((response) => {
            const result = response.data;

            if (result.error != null && result.error.length > 0) {
                toaster.pop('error', result.error);

                if (captchaExist) {
                    CaptchaSource.ReloadImage();
                }
            } else {
                ctrl.view = 'success';
                PubSub.publish('send_feedback');
            }
        });
    };

    ctrl.hideSecret = function () {
        ctrl.secret = null;
    };

    ctrl.selectedImage = function (files) {//GlorySoft_014
        if (files && files.length) {
            for (var i = 0; i < files.length; i++) {
                ctrl.pushFiles(files[i]);
            }
        }
    };

    ctrl.pushFiles = function (file) {//GlorySoft_014
        console.log(file.type.substring(0, 5));
        ctrl.files.push(file || {});
        if (file.type.substring(0, 5) == 'image') {
            console.log('push');
            ctrl.images.push(file || {});
        } else if (file.type.search('pdf') > -1) {
            ctrl.images.push('images/file_extension_pdf.png' || {});
        } else if (file.type.search('word') > -1) {
            ctrl.images.push('images/file_extension_doc.png' || {});
        } else {
            ctrl.images.push('images/nophoto_xsmall.png' || {});
        }
        //console.log(ctrl.images);
    };

    ctrl.deleteImage = function (index) {//GlorySoft_014
        ctrl.files.splice(index, 1);
        ctrl.images.splice(index, 1);
    };

    ctrl.addReview = function (actionUrl, messageType, message, orderNumber, name, email, phone, inn, companyName, companyAddress, companyKpp, items, thanksType, orderDate, customerType, agreement, captchaCode, captchaSource, files) {//GlorySoft_044
        PubSub.publish("add_response");

        return Upload.upload({
            url: actionUrl,
            data: {
                messageType: messageType,
                message: message,
                orderNumber: orderNumber,
                name: name,
                email: email,
                phone: phone,
                inn: inn,
                companyName: companyName,
                companyAddress: companyAddress,
                companyKpp: companyKpp,
                items: items,
                thanksType: thanksType,
                orderDate: orderDate,
                customerType: customerType,
                agree: agreement,
                captchaCode: captchaCode,
                captchaSource: captchaSource,
                files: files
            },
        });
    };

    ctrl.submitFn = function (form, actionUrl) {//GlorySoft_014
        //if (form.form.captchaCode != undefined) {
        console.log(form);
        console.log(actionUrl);
        return ctrl.addReview(actionUrl, form.curTheme, form.message, form.orderNumber, form.name, form.email, form.phone, form.inn, form.companyName, form.companyAddress, form.companyKpp, form.items, form.thanksType, form.orderDate, form.customerType, form.agreement, form.captchaCode, form.captchaSource, form.files).then(function (response) {
            console.log(response);
            if (response.data.status != "succes") {
                toaster.pop('error', response.data.error);
                return $q.reject(response.data.error);
            }
            toaster.success('', $translate.instant('Js.Feedback.MessageSent'));
            $window.location = response.data.redirectTo;
        })
            .then(function () {
                //    if (ctrl.showFormAfterDo === true) {
                //        ctrl.formInStart();
                //    } else {
                ctrl.formReset();
                //        ctrl.reviewIdActive = 0;
                //        ctrl.visibleFormCancelButton = false;
                //        ctrl.formVisible = false;
                //    }
            })
            .catch((err) => console.warn(err))
        //}
    };

    ctrl.submit = function () {//GlorySoft_014
        ctrl.files = ctrl.files.filter(function (image) { return image.name; });

        const defer = $q.defer();
        if (typeof (CaptchaSource) !== "undefined") {
            CaptchaSource.InputId = "CaptchaCode";

            ctrl.captchaCode = CaptchaSource.GetInputElement().value;
            ctrl.captchaSource = CaptchaSource.InstanceId;

            $http.get(CaptchaSource.ValidationUrl + '&i=' + CaptchaSource.GetInputElement().value)
                .then(function (result) {
                    if (result.data === true) {
                        $timeout(function () { CaptchaSource.ReloadImage(); }, 1000);
                        CaptchaSource.GetInputElement().value = '';
                        defer.resolve();
                    } else {
                        toaster.pop('error', $translate.instant('Js.Captcha.Wrong'));
                        return $q.reject($translate.instant('Js.Captcha.Wrong'));
                    }
                })
        }
        else {
            defer.resolve();
        }

        defer.promise.then(() => ctrl.submitFn(ctrl, 'feedback/feedbackForm'));
    };

    ctrl.processCompany = function (item) {//GlorySoft_014
        if (ctrl.timerProcessCompany != null) {
            $timeout.cancel(ctrl.timerProcessCompany);
        }

        return ctrl.timerProcessCompany = $timeout(function () {
            if (item != null && item.CompanyData) {
                console.log(item);
                ctrl.companyName = item.CompanyData.CompanyName;
                ctrl.inn = item.CompanyData.INN;
                ctrl.companyAddress = item.CompanyData.LegalAddress;
                ctrl.companyKpp = item.CompanyData.KPP;
            }
        }, item != null ? 0 : 700);
    };

    ctrl.fillItemsFromBasket = function () {//GlorySoft_014
        $http.post('feedback/FillItemsFromBasket').then(function (response) {
            var result = response.data;
            if (result.result != true) {
                toaster.pop('error', result.errors[0]);
            } else {
                ctrl.items = result.obj;
            }
        });
    };

    ctrl.formReset = function () {//GlorySoft_014
        ctrl.message = '';
        ctrl.orderNumber = '';
        ctrl.items = '';
        ctrl.files = [];
    };

}

export default FeedbackCtrl;
