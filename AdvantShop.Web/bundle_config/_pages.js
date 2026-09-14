import PagesStorage from '../node_scripts/pagesStorage.js';
import { getDirname } from '../node_scripts/shopPath.js';
import shippingMethods from './_shippingMethods.js';
const obj = new PagesStorage(getDirname(import.meta.url));

obj.assign(shippingMethods);

obj.addItem('brand', 'brand.js');
obj.addItem('brandItem', 'brandItem.js');
obj.addItem('billing', 'billing.js');
obj.addItem('cart', 'cart.js');
obj.addItem('catalog', 'catalog.js');
obj.addItem('catalogSearch', 'catalogSearch.js');
obj.addItem('checkout', 'checkout.js');
obj.addItem('checkoutModern', 'checkoutModern.js');
obj.addItem('checkoutSuccess', 'checkoutSuccess.js');
obj.addItem('compare', 'compare.js');
obj.addItem('error', 'error.js');
obj.addItem('feedback', 'feedback.js');
obj.addItem('giftcertificate', 'giftcertificate.js');
obj.addItem('giftcertificatePrint', 'giftcertificatePrint.js');
obj.addItem('recoveryPassword', 'recoveryPassword.js');
obj.addItem('home', 'home.js');
obj.addItem('login', 'login.js');
obj.addItem('myaccount', 'myaccount.js');
obj.addItem('news', 'news.js');
obj.addItem('newsItem', 'newsItem.js');
obj.addItem('preorder', 'preorder.js');//GlorySoft_015
obj.addItem('printorder', 'printorder.js');
obj.addItem('product', 'product.js');
obj.addItem('productList', 'productList.js');
obj.addItem('staticPage', 'staticPage.js');
obj.addItem('wishlist', 'wishlist.js');
obj.addItem('bonusPage', 'bonusPage.js');
obj.addItem('closedStore', 'closedStore.js');
obj.addItem('receipt', 'receipt.js');
obj.addItem('receiptSberbank', 'receiptSberbank.js');

//отдельные бандлы, которые подключаются через ocLazyLoad
obj.addItem('inplaceMin', 'inplaceMin.js');
obj.addItem('inplaceMax', 'inplaceMax.js');

obj.addItem('mobileOverlap', 'mobileOverlap.js');
obj.addItem('currency', 'currency.js');
obj.addItem('cookiesPolicy', 'cookiesPolicy.js');
obj.addItem('techDomain', 'techDomain.js');
obj.addItem('demo', 'demo.js');
obj.addItem('builder', 'builder.js');
obj.addItem('logogenerator', 'logogenerator.js');
obj.addItem('telephony', 'telephony.js');

obj.addItem('common', 'common.js');
obj.addItem('head', 'head.js');
obj.addItem('uiAce', '../Areas/Admin/bundle_config/uiAce.js');

/*payments*/
obj.addItem('mokka', '../scripts/_partials/payment/widgets/mokka/mokkaCtrl.js');
/**/

obj.addItem('shippings', 'shippings.js');

obj.addItem('shops', 'shops.js');
obj.addItem('stockList', 'stockList.js');

obj.addItem('geoMode', 'geoMode.js');

obj.addItem('warehouse', 'warehouse.js');

obj.addItem('productQuickview', 'productQuickview.js');

obj.addItem('user', 'user.js')

export default obj;
