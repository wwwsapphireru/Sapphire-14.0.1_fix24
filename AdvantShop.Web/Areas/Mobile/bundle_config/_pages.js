import PagesStorage from '../../../node_scripts/pagesStorage.js';
import shippingMethods from '../../../bundle_config/_shippingMethods.js';
import { getDirname } from '../../../node_scripts/shopPath.js';
const obj = new PagesStorage(getDirname(import.meta.url));

obj.assign(shippingMethods);

obj.addItem('brand', 'brand.js');
obj.addItem('brandItem', 'brandItem.js');
obj.addItem('billing', 'billing.js');
obj.addItem('cart', 'cart.js');
obj.addItem('catalog', 'catalog.js');
obj.addItem('checkout', 'checkout.js');
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
obj.addItem('product', 'product.js');
obj.addItem('productList', 'productList.js');
obj.addItem('staticPage', 'staticPage.js');
obj.addItem('wishlist', 'wishlist.js');
obj.addItem('bonusPage', 'bonusPage.js');

//для мобилки
obj.addItem('catalogSearch', 'catalogSearch.js');
obj.addItem('checkoutMobile', 'checkoutMobile.js');
obj.addItem('checkoutModern', 'checkoutModern.js');

//нужен для модулей, которые выводят свои вьюшки, к примеру, модуль "Блог"
obj.addItem('common', 'common.js');
obj.addItem('head', 'head.js');

obj.addItem('uiAce', '../../Admin/bundle_config/uiAce.js');

/*payments*/
obj.addItem('mokka', '../../../scripts/_partials/payment/widgets/mokka/mokkaCtrl.js');

obj.addItem('mobileOverlap', '../../../bundle_config/mobileOverlap.js');

obj.addItem('shippings', 'shippings.js');

obj.addItem('shops', 'shops.js');

obj.addItem('geoMode', 'geoMode.js');
obj.addItem('warehouse', 'warehouse.js');

obj.addItem('productQuickview', 'productQuickview.js');

obj.addItem('user', 'user.js');

export default obj;
