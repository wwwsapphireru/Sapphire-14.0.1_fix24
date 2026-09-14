import addressListTemplate from '../templates/addressList.html';
import addAddressFormTemplate from '../templates/addressModal.html';
function addressListDirective() {
    return {
        restrict: 'A',
        scope: {
            type: '@', // change, view, viewRo
            initAddressFn: '&',
            changeAddressFn: '&',
            saveAddressFn: '&',
            deleteAddressFn: '&',
            applyAddressFn: '&',
            isShowFullAddress: '<?',
            isShowName: '<?',
            isLockedChanges: '<?',
            // isShowMap: '<?',
            contactId: '<?',
            themeAlt: '<?',
            compactMode: '<?',
            customerId: '<?',
            autocompleteAlt: '<?',
            zoneUpdateOnAdd: '<?',
            requiredValidationEnabled: '<?',
            isGeoMode: '<?',
            isProgress: '<?',
            isRequiredAddress: '<?'//GlorySoft_011
        },
        controller: 'AddressListCtrl',
        controllerAs: 'addressList',
        bindToController: true,
        replace: true,
        templateUrl: addressListTemplate,
        transclude: {
            footer: '?addressListFooter',
        },
        link(scope, element, attr, ctrl, transclude) {},
    };
}

/* @ngInject */
function addressListTransclude() {
    return {
        restrict: 'A',
        require: '^addressList',
        transclude: {
            addressListFooter: '?addressListFooter',
        },
        link(scope, element, attrs, parentCtrl) {
            parentCtrl.transcudeContent(scope, element);
        },
    };
}

function addEditAddressFormDirective() {
    return {
        restrict: 'A',
        transclude: true,
        templateUrl: addAddressFormTemplate,
        controller: 'AddEditAddressFormCtrl',
        controllerAs: 'addEditAddressForm',
        bindToController: true,
        scope: {
            type: '@',
            isShowName: '<?',
            // isShowMap: '<?',
            onAddEditAddress: '&',
            customerId: '<?',
            themeAlt: '<?',
            autocompleteAlt: '<?',
            formData: '<?',
            addressSelected: '<?',
            zoneUpdateOnAdd: '<?',
            contactId: '<?',
            currentCity: '<?',
        },
        link(scope, element, attrs, parentCtrl) {},
    };
}

export { addressListDirective, addressListTransclude, addEditAddressFormDirective };
