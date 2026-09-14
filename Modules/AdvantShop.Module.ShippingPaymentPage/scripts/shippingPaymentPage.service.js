/* @ngInject */
export default function shippingPaymentPageService($http) {
    const service = this;
    let contact;

    service.getShipping = function (/*GlorySoft_008*/contact, preorderList, typeCalculationVariants, allowChangeSelectedShipping) {
        var params = { rnd: Math.random(), typeCalculationVariants: typeCalculationVariants, allowChangeSelectedShipping: allowChangeSelectedShipping };
        params.contact = contact;//GlorySoft_008
        if (preorderList != null) {
            params.preorderList = preorderList;
        }

        return $http.post('shipping-payment/CheckoutShippingJson'/*GlorySoft_008 '/checkout/CheckoutShippingJson'*/, params).then(function (response) {
            return response.data;
        });
    };

    service.saveShipping = function (shipping, preorderList) {
        var params = { shipping: shipping, rnd: Math.random() };
        if (preorderList != null) {
            params.preorderList = preorderList;
        }

        return $http.post('/checkout/CheckoutShippingPost', params).then(function (response) {
            return response.data;
        });
    };

    service.getPayment = function (preorderList) {
        var params = { rnd: Math.random() };
        if (preorderList != null) {
            params.preorderList = preorderList;
        }

        return $http.post('/checkout/CheckoutPaymentJson', params).then(function (response) {
            return response.data;
        });
    };

    service.savePayment = function (payment, preorderList) {
        var params = { payment: payment, rnd: Math.random() };
        if (preorderList != null) {
            params.preorderList = preorderList;
        }

        return $http.post('/checkout/CheckoutPaymentPost', params).then(function (response) {
            return response.data;
        });
    };

    service.getCheckoutCart = function () {
        return $http.get('/checkout/CheckoutCartJson', { params: { rnd: Math.random() } }).then(function (response) {
            return response.data;
        });
    };

    service.saveContact = function (address, $httpOptions) {
        return $http.post('/checkout/CheckoutContactPost', { address: address, rnd: Math.random() }, $httpOptions).then(function (response) {
            contact = address;
            return response.data;
        });
    };
};