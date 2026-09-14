/* @ngInject */
export default function ShippingPaymentPageCtrl($http, zoneService, shippingPaymentPageService) {
    var ctrl = this,
        relationship,
        preorderList;

    relationship = {
        'address': function () {
            return ctrl.fetchShipping()
                .then(ctrl.fetchPayment)
                .then(ctrl.fetchCart);
        },
        'shipping': function () {
            return ctrl.fetchPayment()
                .then(ctrl.fetchCart);
        },
        'payment': function () {
            return ctrl.fetchCart();
        }
    };

    ctrl.contact = {};
    ctrl.Payment = {};
    ctrl.Shipping = {};
    ctrl.Cart = {};
    ctrl.shippingDataReceived = false;
    ctrl.paymentDataReceived = false;
    ctrl.useCart = false;
    ctrl.typeCalculationVariants = 'all';

    ctrl.fetchShipping = function () {
        var preorder = ctrl.useCart === false ? preorderList : null;

        return shippingPaymentPageService.getShipping(/*GlorySoft_008*/ctrl.contact, preorder, ctrl.typeCalculationVariants)
            .then(function (response) {
                return fetchTypeCalculationVariants(preorder, response);
            })
            .then(function (response) {
                ctrl.showZip = false;
                if (response != null) {
                    if (response.typeCalculationVariants != null) {
                        ctrl.typeCalculationVariants = response.typeCalculationVariants;
                    }

                    ctrl.ngSelectShipping = ctrl.getSelectedItem(response.option, response.selectShipping);

                    if (response.option != null) {
                        for (var i = 0, len = response.option.length; i < len; i++) {
                            if (response.option[i].ShippingPoints != null) {
                                response.option[i].SelectedPoint = response.option[i].SelectedPoint || response.option[i].ShippingPoints[0];
                            }
                            if (response.option[i].DisplayIndex != null) {
                                ctrl.showZip = ctrl.showZip || response.option[i].DisplayIndex;
                            }
                        }
                    }

                    //if (response.option == null || !response.option.length) {
                    //    ctrl.showZip = true;
                    //}
                }

                ctrl.shippingDataReceived = true;

                return ctrl.Shipping = response;
            });
    };

    function fetchTypeCalculationVariants(preorder, response) {
        if (response == null || response.typeCalculationVariants == null || response.typeCalculationVariants === 'all') {
            return response;
        }

        var oppositeType = response.typeCalculationVariants === 'courier' ? 'self-delivery' : 'courier';

        return shippingPaymentPageService.getShipping(preorder, oppositeType, false)
            .then(function (oppositeResponse) {
                if (oppositeResponse != null && oppositeResponse.option != null) {
                    response.option = (response.option || []).concat(oppositeResponse.option);
                }
                return response;
            });
    }

    ctrl.fetchPayment = function () {
        return shippingPaymentPageService.getPayment(ctrl.useCart === false ? preorderList : null)
            .then(function (response) {

                ctrl.ngSelectPayment = ctrl.getSelectedItem(response.option, response.selectPayment);

                ctrl.paymentDataReceived = true;

                //return angular.extend(ctrl.Payment, response);
                return ctrl.Payment = response;
            });
    };

    ctrl.fetchCart = function () {
        return shippingPaymentPageService.getCheckoutCart()
            .then(function (response) {

                ctrl.shippingDataReceived = true;
                ctrl.paymentDataReceived = true;

                return angular.extend(ctrl.Cart, response);
            });
    };

    ctrl.getSelectedItem = function (array, selectedItem) {
        var item;

        for (var i = array.length - 1; i >= 0; i--) {
            if (array[i].Id === selectedItem.Id) {
                //selectedItem имеет заполненные поля какие опции выбраны, поэтому объединяем
                array[i] = angular.extend(array[i], selectedItem);
                item = array[i];
                break;
            }
        }

        return item;
    };

    ctrl.changeShipping = function (shipping) {

        ctrl.shippingDataReceived = false;
        ctrl.paymentDataReceived = false;

        if (ctrl.ngSelectShipping !== shipping) {
            ctrl.ngSelectShipping = shipping;
        }

        return shippingPaymentPageService.saveShipping(shipping, ctrl.useCart === false ? preorderList : null)
            .then(function (response) {
                ctrl.shippingDataReceived = true;

                return ctrl.ngSelectShipping = angular.extend(ctrl.ngSelectShipping, response.selectShipping);
            })
            .then(relationship['shipping']);
    };

    ctrl.changePayment = function (payment) {

        if (ctrl.ngSelectPayment !== payment) {
            ctrl.ngSelectPayment = payment;
        }

        return shippingPaymentPageService.savePayment(payment, ctrl.useCart === false ? preorderList : null).then(relationship['payment']);
    };

    ctrl.setZone = function (city, region, zip) {
        //zoneService.setCurrentZone(city, null, null, ctrl.showRegion ? region : null, null, ctrl.showZip ? zip : null);GlorySoft_008 // city, obj, countryId, region, country, zip

        //GlorySoft_008
        var zone = {};
        zone.City = city;
        zone.Region = ctrl.showRegion ? region : null;
        zone.Zip = ctrl.showZip ? zip : null;
        ctrl.saveContact(zone);
    };

    ctrl.reloadData = function () {

        ctrl.shippingDataReceived = false;
        ctrl.paymentDataReceived = false;

        relationship['address']();
    };

    ctrl.saveContact = function (zone) {
        ctrl.contact.Country = zone.CountryName;
        ctrl.contact.City = ctrl.zoneCity = zone.City;
        ctrl.contact.Region = ctrl.zoneRegion = zone.Region;
        ctrl.contact.Zip = ctrl.zoneZip = zone.Zip;

        if (!zone.Region)
            ctrl.showRegion = true;

        //shippingPaymentPageService.saveContact(ctrl.contact)
        //    .then(relationship['address']);GlorySoft_008
        relationship['address']();//GlorySoft_008
    };

    $http.get('shipping-payment/getlistproduct').then(function (response) {

        preorderList = response.data;

        zoneService.addCallback('set', function (data) {
            ctrl.shippingDataReceived = false;
            ctrl.paymentDataReceived = false;

            ctrl.saveContact(data);
        });

        zoneService.getCurrentZone().then(function (data) {
            ctrl.saveContact(data);
        });


    });
};