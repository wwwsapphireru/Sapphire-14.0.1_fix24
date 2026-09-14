//GlorySoft_015

import '../_common/rating/styles/rating.scss';
import '../_partials/product-view/styles/product-view.scss';
import '../../styles/views/product.scss';
import '../../styles/views/preorder.scss';

import PreorderCtrl from './controllers/preorderController.js';

const moduleName = 'preorder';

angular.module(moduleName, []).controller('PreorderCtrl', PreorderCtrl);

export default moduleName;
