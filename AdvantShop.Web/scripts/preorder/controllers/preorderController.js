//GlorySoft_015
/* @ngInject */
function PreorderCtrl(toaster, $translate, $document) {
	var ctrl = this;

	ctrl.isValid = false;

	ctrl.validateInput = function () {
		var result = true;

		if (typeof ctrl.agreement != 'undefined' && !ctrl.agreement) {
			toaster.pop('error', $translate.instant('Js.Subscribe.ErrorAgreement'));
			result = false;
		}

		ctrl.isValid = true;

		return result;
	};

	ctrl.submit = function (formCtrl) {
		$document[0].forms[formCtrl.$name].submit();
	};
}

export default PreorderCtrl;
