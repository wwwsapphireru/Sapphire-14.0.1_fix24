import appDependency from '../../../scripts/appDependency.js';

import '../vendors/flatpickr/flatpickr.custom.scss';

import '../../../Areas/Mobile/styles/_partials/sidebar.scss';
import '../styles/partials/sidebar.scss';
import '../../../Areas/Mobile/styles/_partials/menu.scss';
import mobileMenuModule from  '../../../Areas/Mobile/scripts/_common/mobileMenu/mobileMenu.module.js';
appDependency.addItem(mobileMenuModule);

import '../styles/variables.scss';
import '../styles/general.scss';
import '../styles/snippets.scss';

import '../styles/common/inputs.scss';
import '../styles/common/headers.scss';
import '../styles/common/buttons.scss';
import '../styles/common/custom-input.scss';
import '../styles/common/sidebar.scss';
import '../styles/common/forms.scss';

import '../styles/partials/header.scss';
import '../styles/partials/header.type1.scss';
import '../styles/partials/header.type2.scss';
import '../styles/partials/header.type3.scss';
import '../styles/partials/menu-header.scss';
import '../styles/partials/menu-dropdown.scss';
import '../styles/partials/brands-carousel.scss';
import '../styles/partials/gallery.scss';
import '../styles/partials/categories-specials.scss';
import '../styles/partials/product-reviews.scss';
import '../styles/partials/product-specials.scss';
import '../styles/partials/footer.scss';
import '../styles/partials/toolbar-top.scss';
import '../styles/partials/news-block.scss';
import '../styles/partials/product-categories.scss';
import '../styles/partials/pagenumberer.scss';
import '../styles/partials/menu-general.scss';

import '../styles/views/cart.scss';
import '../styles/views/home.scss';
import '../styles/views/product.scss';
import '../styles/views/compareproducts.scss';
import '../styles/views/wishlist.scss';
import '../styles/views/myaccount.scss';
import '../styles/views/checkout.scss';
import '../styles/views/catalog.scss';
import '../styles/views/news.scss';
import '../styles/views/brands.scss';
import '../styles/views/registration.scss';
import '../styles/custom/index.scss';

import subscribeModule from '../../../scripts/_partials/subscribe/subscribe.module.js';

appDependency.addItem(subscribeModule);


import '../scripts/_common/autocompleter/styles/autocompleter.scss';

import '../scripts/_partials/product-view/styles/product-view.common.scss';
import '../scripts/_partials/product-view/styles/product-view.tile.scss';
import '../scripts/_partials/product-view/styles/product-view.list.scss';
import '../scripts/_partials/product-view/styles/product-view.table.scss';
import '../scripts/_partials/colors-viewer/colorsViewer.module.js';
import '../scripts/_partials/cookies-policy/cookiesPolicy.module.js';
import '../scripts/_partials/sizes-viewer/sizesViewer.module.js';
import '../scripts/_partials/compare/compare.module';
import '../scripts/_partials/wishlist/wishlist.module';
import '../scripts/_partials/catalog-filter/catalogFilter.module.js';
import '../scripts/_partials/photo-view-list/photo-view-list.scss';
import '../scripts/_partials/reviews/reviews.module.js';
import '../scripts/_partials/subscribe/styles/subscribe.scss';
import '../scripts/_partials/zone/styles/zones.scss';
import '../scripts/_partials/address/styles/address.scss';
import '../scripts/_partials/order/order';
import '../scripts/_partials/price-amount-list/priceAmountList.module.js';
import '../scripts/_partials/order-product/orderProduct.module.js';
import '../scripts/_partials/shipping/shipping.module.ts';
import '../scripts/_partials/payment/payment.module.ts';
import '../scripts/_partials/cart/cart.module.js';
import advRegistrationModule from '../scripts/_common/advRegistration/advRegistration.module.js';
appDependency.addItem(advRegistrationModule);
import '../scripts/_partials/buy-one-click/styles/buyOneClick.scss';

import '../scripts/_common/rating/rating.module.js';
import '../scripts/_common/tabs/tabs.module';
import '../scripts/_common/breadCrumbs/breadCrumbs.module.js';
import '../scripts/_common/carousel/styles/carousel.scss';
import '../scripts/_common/spinbox/styles/spinbox.scss';
import '../scripts/_common/modal/modal.module';

import '../scripts/_partials/bonus/bonus.module.js';

import '../features/carousel-as-background';


import '../scripts/_partials/quickview/quickview.module.js';
import '../scripts/_partials/rootMenu/rootMenu.decorator.js';

import '../styles/extend/menu-sidebar.scss';

import '../scripts/extend/cursor'
import filterAsSidebarModule from'../scripts/extend/filter-as-sidebar'

import setCssCustomProps from '../../../scripts/_common/setCssCustomProps/setCssCustomProps.module.js';
appDependency.addItem(setCssCustomProps);
appDependency.addItem(filterAsSidebarModule);

window.advantshopComparePageOptions = {
  align: false
};
