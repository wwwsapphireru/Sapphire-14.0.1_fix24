import { PubSub } from '../../_common/PubSub/PubSub.js';

/*@ngInject*/
const ProductCtrl = function (
    $q,
    $scope,
    $sce,
    $timeout,
    productService,
    modalService,
    toaster,
    $translate,
    $window,
    urlHelper,
    customOptionsService,
) {
    const ctrl = this;
    const callbackListColorsAndSizes = [];
    const deferPostLoad = $q.defer();
    let lastValueChangesSizeColor;

    ctrl.$onInit = function () {
        ctrl.productView = 'photo';

        ctrl.Price = {};

        ctrl.picture = {};

        ctrl.dirty = false;

        ctrl.offerSelected = {};

        ctrl.carouselHidden = true;

        productService.addToStorage(ctrl);

        ctrl.isOpenPreviewModal = false;
    };

    ctrl.$onDestroy = function () {
        modalService.destroy('modalProductVideo');
    };

    ctrl.productTabsModeInMobile = function (isAccordion, isMobile) {
        if (isAccordion && isMobile) {
            document.querySelectorAll('.accordion-css__state')[0].checked = true;
        }
    };

    ctrl.getPrice = function () {
        return productService
            .getPrice(ctrl.offerSelected.OfferId, ctrl.customOptions?.xml, ctrl.lpBlockId, ctrl.offerSelected.AmountBuy)
            .then((price) => {
                ctrl.Price = price;
                ctrl.Price.PriceString = $sce.trustAsHtml(ctrl.Price.PriceString);
                ctrl.Price.Bonuses = $sce.trustAsHtml(ctrl.Price.Bonuses);

                ctrl.offerSelected.AllowBuyOutOfStockProducts = price.AllowBuyOutOfStockProducts;
                ctrl.offerSelected.IsAvailableForPurchase = price.IsAvailableForPurchase;
                ctrl.offerSelected.IsAvailableForPurchaseOnBuyOneClick = price.IsAvailableForPurchaseOnBuyOneClick;

                return ctrl.Price;
            });
    };

    ctrl.getCreditPayment = function (price, discount, discountAmount) {
        if (!price) {
            ctrl.showCreditButton = false;
            ctrl.visibilityFirstPaymentButton = false;
            return null;
        }
        return productService.getCreditPayment(price, discount || 0, discountAmount || 0).then((data) => {
            ctrl.FirstPaymentPrice = $sce.trustAsHtml(data.firstPaymentPrice);
            ctrl.visibilityFirstPaymentButton = data.firstPaymentPrice?.length > 0;
            ctrl.firstPaymentMaxPrice = data.firstPaymentMaxPrice;
            ctrl.firstPaymentMinPrice = data.firstPaymentMinPrice;
            ctrl.firstPaymentId = data.firstPaymentId;
            ctrl.creditButtonText = data.creditButtonText;
            ctrl.showCreditButton = data.showCreditButton;
        });
    };

    ctrl.refreshPrice = function () {
        const defer = $q.defer();
        return $q.when(ctrl.offerSelected.AmountBuy).then((needUpdate) =>
            !needUpdate
                ? null
                : ctrl
                      .getPrice()
                      .then((price) => ctrl.getCreditPayment(price.AmountPrice, ctrl.discount, ctrl.discountAmount))
                      .then(() => {
                          if (ctrl.priceAmountList) {
                              ctrl.priceAmountList.update().then((data) => {
                                  defer.resolve(data);
                                  return data;
                              });
                          } else {
                              defer.resolve();
                          }
                      })
                      .then(() => {
                          if (ctrl.shippingVariants) {
                              ctrl.shippingVariants.update().then((data) => {
                                  defer.resolve(data);
                                  return data;
                              });
                          } else {
                              defer.resolve();
                          }
                          productService.processCallback('refreshPrice');
                      }),
        );
    };

    ctrl.prepareOffers = function (data) {
        for (let i = 0, len = data.Offers.length; i < len; i++) {
            if (data.Offers[i].Available && angular.isString(data.Offers[i].Available) === true) {
                data.Offers[i].Available = $sce.trustAsHtml(data.Offers[i].Available);
            }
        }

        return data;
    };

    ctrl.loadData = function (productId, colorId, sizeId, hiddenPrice, filterPhotosEnable, preventChangeLocation) {
        ctrl.productId = productId;
        ctrl.hiddenPrice = hiddenPrice;
        ctrl.filterPhotosEnable = filterPhotosEnable ? filterPhotosEnable : true;
        ctrl.preventChangeLocation = preventChangeLocation === true;

        return productService.getOffers(productId, colorId, sizeId).then((data) => {
            if (!data) {
                if (!colorId) {
                    ctrl.carouselHidden = false;
                }
                return null;
            }

            ctrl.allowPreOrder = data.AllowPreOrder;//GlorySoft_022

            ctrl.data = ctrl.prepareOffers(data);

            ctrl.offerSelected = productService.findOfferSelected(data.Offers, data.StartOfferIdSelected);

            ctrl.dirty = true;

            ctrl.getColorsViewer()
                .then(() => {
                    if (ctrl.colorsViewer && ctrl.offerSelected?.Color) {
                        ctrl.setColorSelected(ctrl.colorsViewer, ctrl.offerSelected.Color?.ColorId);
                    }

                    return ctrl.data;
                })
                .then(ctrl.getSizesViewer)
                .then(() => {
                    if (ctrl.sizesViewer && ctrl.offerSelected?.Size) {
                        ctrl.setSizeSelected(ctrl.sizesViewer, ctrl.offerSelected.Size.SizeId);
                    }
                    return ctrl.data;
                })
                .then(ctrl.getCarousel)
                .then(() => {
                    if (ctrl.filterPhotosEnable === true && ctrl.carousel) {
                        ctrl.filterPhotos(ctrl.offerSelected.Color?.ColorId, ctrl.carousel);
                    }
                    ctrl.carouselHidden = false;
                })
                .finally(() => {
                    ctrl.isPostLoad = true;
                    deferPostLoad.resolve();
                });

            return ctrl.data;
        });
    };

    ctrl.refreshSelectedOffer = function () {
        if (!ctrl.offerSelected) {
            return false;
        }

        return productService
            .getOffers(ctrl.offerSelected.ProductId, ctrl.offerSelected.Color?.ColorId, ctrl.offerSelected.Size?.SizeId)
            .then((data) => {
                if (!data || !data.Offers) {
                    return null;
                }

                ctrl.data = ctrl.prepareOffers(data);
                ctrl.offerSelected = productService.findOfferSelected(ctrl.data.Offers, ctrl.offerSelected.OfferId);

                return ctrl.data;
            });
    };

    ctrl.validate = function () {
        if (!ctrl.customOptions) {
            return true;
        }

        const { invalidOptions, isValidOptions } = customOptionsService.isValidOptions(ctrl.customOptions.items);
        const form = ctrl.customOptions.customOptionsForm;

        if (form.$invalid || !isValidOptions) {
            form.$setSubmitted();
            form.$setDirty();

            if (invalidOptions.size > 0) {
                invalidOptions.forEach((option) => {
                    let errorText;
                    const { MinQuantity, MaxQuantity, InputType, Title } = option;

                    if (MinQuantity && MaxQuantity && MaxQuantity === MinQuantity) {
                        errorText = `: Выберите ${MaxQuantity} варианта`;
                    } else {
                        const minText = MinQuantity ? `значение должно быть от ${MinQuantity}` : '';
                        const maxText = MaxQuantity && MaxQuantity !== MinQuantity ? ` до ${MaxQuantity}` : '';
                        errorText = `${minText}${minText && maxText ? ' до ' : ''}${maxText}`;
                    }

                    const errorMsg = InputType === 6 ? `Не выбрано поле ${Title} ${errorText}` : `Неверно заполнено поле ${Title} ${errorText}`;

                    toaster.pop('error', errorMsg);
                });
            } else {
                toaster.pop('error', $translate.instant('Js.Product.InvalidCustomOptions'));
            }

            return false;
        }

        return true;
    };

    //#region compare and wishlist

    ctrl.compareInit = function (compare) {
        ctrl.compare = compare;
    };

    ctrl.wishlistControlInit = function (wishlistControl) {
        ctrl.wishlistControl = wishlistControl;
    };

    //#endregion

    //#region customOptions

    ctrl.customOptionsInitFn = function (customOptions) {
        ctrl.customOptions = customOptions;
    };

    ctrl.customOptionsChange = function () {
        if (!ctrl.hiddenPrice) {
            ctrl.refreshPrice().then(() => {
                ctrl.disabledBuyButton = false;
            });
        }
        PubSub.publish('product.customOptions.change', {
            productId: ctrl.productId,
            offerId: ctrl.offerSelected.OfferId,
            items: ctrl.customOptions.items,
        });
    };

    ctrl.beforeCustomOptionsChange = function () {
        if (!ctrl.hiddenPrice) {
            ctrl.disabledBuyButton = true;
        }
    };

    //#endregion

    //#region colors

    ctrl.initColors = function (colorsViewer) {
        ctrl.colorsViewer = colorsViewer;

        if (ctrl.colorsViewerDefer) {
            ctrl.colorsViewerDefer.resolve();
            delete ctrl.colorsViewerDefer;
        }
    };

    ctrl.getColorsViewer = function () {
        const defer = $q.defer();

        if (ctrl.colorsExist === true && !ctrl.colorsViewer) {
            ctrl.colorsViewerDefer = defer;
        } else {
            defer.resolve(ctrl.colorsViewer);
        }

        return defer.promise;
    };

    ctrl.changeColor = function (color) {
        ctrl.colorSelected = color;

        if (ctrl.sizesViewer) {
            ctrl.sizeSelected = ctrl.getSizeAvalable(ctrl.data.Offers, ctrl.colorSelected.ColorId, ctrl.sizesViewer.sizes, ctrl.data.AllowPreOrder);
            if (ctrl.preventChangeLocation !== true) {
                urlHelper.setLocationQueryParams('size', ctrl.sizeSelected?.SizeId, true);
            }
        }

        ctrl.offerSelected = productService.getOffer(
            ctrl.data.Offers,
            ctrl.colorSelected.ColorId,
            ctrl.sizeSelected && ctrl.sizeSelected.isDisabled === false ? ctrl.sizeSelected.SizeId : null,
            ctrl.data.AllowPreOrder,
        );

        if (!ctrl.hiddenPrice) {
            ctrl.refreshPrice();
        }

        if (ctrl.compare) {
            ctrl.compare.checkStatus(ctrl.offerSelected.OfferId);
        }

        if (ctrl.wishlistControl) {
            ctrl.wishlistControl.checkStatus(ctrl.offerSelected.OfferId);
        }

        ctrl.setPreviewByColorId(ctrl.colorSelected.ColorId, ctrl.filterPhotosEnable, ctrl.carousel);

        if (ctrl.preventChangeLocation !== true) {
            urlHelper.setLocationQueryParams('color', ctrl.colorSelected.ColorId, true);
        }

        ctrl.processChangeSizeAndColorCallback(ctrl.colorSelected, 'color', { offer: ctrl.offerSelected });

        if (ctrl.sizesViewer) {
            ctrl.processChangeSizeAndColorCallback(ctrl.sizeSelected, 'size', { offer: ctrl.offerSelected });
        }
    };

    ctrl.setColorSelected = function (colorsViewer, colorId) {
        for (let i = colorsViewer.colors.length - 1; i >= 0; i--) {
            if (colorsViewer.colors[i].ColorId === colorId) {
                ctrl.colorSelected = colorsViewer.colors[i];
                break;
            }
        }

        ctrl.processChangeSizeAndColorCallback(ctrl.colorSelected, 'color', { offer: ctrl.offerSelected });
    };

    //#endregion

    //#region sizes

    ctrl.initSizes = function (sizesViewer) {
        ctrl.sizesViewer = sizesViewer;

        if (ctrl.sizesViewerDefer) {
            ctrl.sizesViewer.sizes = JSON.parse(JSON.stringify(ctrl.sizesViewer.sizes));
            ctrl.sizesViewerDefer.resolve();
            delete ctrl.sizesViewerDefer;
        }
    };

    ctrl.getSizesViewer = function () {
        const defer = $q.defer();

        if (ctrl.sizesExist === true && !ctrl.sizesViewer) {
            ctrl.sizesViewerDefer = defer;
        } else {
            defer.resolve(ctrl.sizesViewer);
        }

        return defer.promise;
    };

    ctrl.changeSize = function (size) {
        ctrl.sizeSelected = size;

        ctrl.offerSelected = productService.getOffer(
            ctrl.data.Offers,
            ctrl.colorSelected ? ctrl.colorSelected.ColorId : 0,
            ctrl.sizeSelected.isDisabled ? null : ctrl.sizeSelected.SizeId,
            ctrl.data.AllowPreOrder,
        );

        if (!ctrl.hiddenPrice) {
            ctrl.refreshPrice();
        }

        if (ctrl.compare) {
            ctrl.compare.checkStatus(ctrl.offerSelected.OfferId);
        }

        if (ctrl.wishlistControl) {
            ctrl.wishlistControl.checkStatus(ctrl.offerSelected.OfferId);
        }
        if (ctrl.preventChangeLocation !== true) {
            urlHelper.setLocationQueryParams('size', ctrl.sizeSelected.SizeId, true);
        }

        ctrl.processChangeSizeAndColorCallback(ctrl.sizeSelected, 'size', { offer: ctrl.offerSelected });
    };

    ctrl.setSizeSelected = function (sizesViewer, sizeId) {
        for (let i = sizesViewer.sizes.length - 1; i >= 0; i--) {
            if (sizesViewer.sizes[i].SizeId === sizeId) {
                ctrl.sizeSelected = sizesViewer.sizes[i];
                break;
            }
        }

        ctrl.sizeSelected = ctrl.getSizeAvalable(
            ctrl.data.Offers,
            ctrl.colorSelected?.ColorId ?? 0,
            ctrl.sizesViewer.sizes,
            ctrl.data.AllowPreOrder,
            true,
        );

        ctrl.processChangeSizeAndColorCallback(ctrl.sizeSelected, 'size', { offer: ctrl.offerSelected });
    };

    ctrl.getSizeAvalable = function (offers, colorId, sizes, allowPreorder, notChangeSelectdSize) {
        let offerItem, sizeSelected, loopCheckStart;

        sizes.forEach((item) => {
            item.isDisabled = true;
        });

        for (let i = offers.length - 1; i >= 0; i--) {
            offerItem = offers[i];

            if (!colorId || !offerItem.Color) {
                loopCheckStart = true;
            } else {
                loopCheckStart = offerItem.Color?.ColorId === colorId;
            }
            if (loopCheckStart === true) {
                for (let sizeItem = sizes.length - 1; sizeItem >= 0; sizeItem--) {
                    if (
                        offerItem.Size != null &&
                        offerItem.Size.SizeId === sizes[sizeItem].SizeId &&
                        (allowPreorder === true || offerItem.Amount > 0)
                    ) {
                        sizes[sizeItem].isDisabled = false;
                        break;
                    }
                }
            }
        }

        if (!notChangeSelectdSize && (!ctrl.sizeSelected || ctrl.sizeSelected.isDisabled === true)) {
            for (const sizesItem of sizes) {
                if (!sizesItem.isDisabled || sizesItem.isDisabled === false) {
                    sizeSelected = sizesItem;
                    break;
                }
            }
        } else {
            sizeSelected = ctrl.sizeSelected;
        }

        return sizeSelected;
    };

    //#endregion

    //#region carousels

    ctrl.addCarousel = function (carousel) {
        ctrl.carousel = carousel;

        if (ctrl.carouselDefer) {
            if (ctrl.carousel.options.asNavFor) {
                ctrl.carousel.whenAsNavForReady(ctrl.carousel.options.asNavFor, () => {
                    ctrl.carouselDefer?.resolve();
                    delete ctrl.carouselDefer;
                });
            } else {
                ctrl.carouselDefer?.resolve();
                delete ctrl.carouselDefer;
            }
        }
    };

    ctrl.getCarousel = function () {
        const defer = $q.defer();

        if (ctrl.carouselExist === true && !ctrl.carousel) {
            ctrl.carouselDefer = defer;
        } else {
            defer.resolve();
        }

        return defer.promise;
    };

    ctrl.carouselItemSelect = function (carousel, item, index) {
        ctrl.previewMediaType = item.parameters?.videoId ? 'video' : 'image';

        ctrl.setPreview(item.parameters);

        ctrl.updateModalPreview(item.parameters.originalPath);

        if (carousel && ctrl.carousel && carousel !== ctrl.carousel) {
            ctrl.carousel.setItemSelect(index);
        } else if (ctrl.carouselPreview && carousel !== ctrl.carouselPreview) {
            ctrl.carouselPreview.setItemSelect(index);
        }
    };

    //#endregion

    //#region modal preview

    ctrl.carouselPreviewNext = function () {
        const items = ctrl.carouselPreview.getItems();
        let itemSelectedNew, newIndex;

        const itemSelected = ctrl.carouselPreview.getSelectedItem() || (items ? items[0] : null);

        if (ctrl.carouselPreview.getSelectedItem() === items[items.length - 1]) {
            ctrl.carouselPreview.goto(0, false);
            newIndex = 0;
        } else {
            ctrl.carouselPreview.next();
            newIndex = itemSelected.carouselItemData.index + 1;
        }

        if (itemSelected) {
            itemSelectedNew = items[newIndex];

            if (itemSelectedNew) {
                ctrl.carouselPreview.setItemSelect(itemSelectedNew);
                ctrl.setPreview(itemSelectedNew.carouselItemData.parameters);
                ctrl.updateModalPreview(itemSelectedNew.carouselItemData.parameters.originalPath);
            }
        }
    };

    ctrl.carouselPreviewPrev = function () {
        const items = ctrl.carouselPreview.getItems();
        let itemSelectedNew, newIndex;

        const itemSelected = ctrl.carouselPreview.getSelectedItem() || (items ? items[0] : null);

        if (ctrl.carouselPreview.getSelectedItem() === items[0]) {
            ctrl.carouselPreview.goto(items.length - 1, false);
            newIndex = items.length - 1;
        } else {
            ctrl.carouselPreview.prev();
            newIndex = itemSelected.carouselItemData.index - 1;
        }

        if (itemSelected) {
            itemSelectedNew = items[newIndex];

            if (itemSelectedNew) {
                ctrl.carouselPreview.setItemSelect(itemSelectedNew);
                ctrl.setPreview(itemSelectedNew.carouselItemData.parameters);
                ctrl.updateModalPreview(itemSelectedNew.carouselItemData.parameters.originalPath);
            }
        }
    };

    ctrl.addModalPictureCarousel = function (carouselPreview) {
        ctrl.carouselPreview = carouselPreview;
        ctrl.carouselPreviewUpdate();
    };

    ctrl.carouselPreviewUpdate = function () {
        if (ctrl.carouselPreview) {
            ctrl.getDialog().then((modal) => {
                if (modal.modalScope.isOpen === true) {
                    ctrl.filterPreviewCarouselItems();
                    ctrl.carouselPreview.update();
                }
            });
        }
    };

    ctrl.updateModalPreview = function (imgSrc) {
        productService.getPhoto(imgSrc).then((img) => {
            $timeout(() => {
                ctrl.maxHeightModalPreview = ctrl.getMaxHeightModalPreview();
                ctrl.modalPreviewHeight = img.naturalHeight > ctrl.maxHeightModalPreview ? ctrl.maxHeightModalPreview : img.naturalHeight;
            }, 0);
        });
    };

    ctrl.modalPreviewCallbackOpen = function () {
        ctrl.setPreviewByColorId(ctrl.offerSelected.Color?.ColorId, ctrl.filterPhotosEnable, ctrl.carouselPreview);

        $timeout(() => {
            ctrl.carouselPreviewUpdate();
            ctrl.isOpenPreviewModal = true;
        }, 100);
    };

    ctrl.modalPreviewCallbackClose = function () {
        if (!('ontouchstart' in window)) {
            $window.removeEventListener(`keydown`, ctrl.onKeydownBackForward);
        }
        ctrl.isOpenPreviewModal = false;
    };

    ctrl.modalPreviewOpen = function (event, picture) {
        const elementAsAnchor = event.target.closest('a:not(.js-product-modal-open)');
        if (elementAsAnchor) {
            const href = elementAsAnchor.getAttribute('href');
            // eslint-disable-next-line no-script-url
            if (href?.length > 0 && !href.startsWith('javascript:')) {
                return;
            }
        }

        event.preventDefault();
        event.stopPropagation();

        if (ctrl.isPostLoad !== true) {
            return;
        }

        ctrl.modalPreviewState = 'load';

        ctrl.dialogOpen().then((modal) => {
            const [modalElement] = modal.modalElement;
            modalElement.classList.add('product-preview-modal-wrap');
            const htmlMain = document.querySelector('html');
            htmlMain.classList.add('overflow-hidden');
            modal.modalScope.callbackClose = function () {
                htmlMain.classList.remove('overflow-hidden');
            };
            productService.getPhoto(!picture ? ctrl.picture.originalPath : picture.originalPath).then((img) => {
                $timeout(() => {
                    ctrl.maxHeightModalPreview = ctrl.getMaxHeightModalPreview();
                    ctrl.modalPreviewHeight = img.naturalHeight > ctrl.maxHeightModalPreview ? ctrl.maxHeightModalPreview : img.naturalHeight;
                    //ctrl.carouselPreviewUpdate();
                    ctrl.modalPreviewState = 'complete';
                    if (ctrl.carouselPreview) {
                        ctrl.filterPreviewCarouselItems();
                    }
                }, 0);
            });
        });
    };

    ctrl.getMaxHeightModalPreview = function () {
        let result = 0,
            height,
            modalElement;
        const modaPreview = document.getElementById(`modalPreview_${ctrl.productId}`);

        if (modaPreview) {
            modalElement = modaPreview.querySelector('.modal-content');
        }
        if (modalElement) {
            height = parseFloat(getComputedStyle(modalElement).height);
            result = isNaN(height) === false ? height : 0;
        }

        return result;
    };

    ctrl.dialogOpen = function () {
        return deferPostLoad.promise
            .then(() => ctrl.getDialog())
            .then((modal) => {
                if (!('ontouchstart' in window)) {
                    $window.addEventListener(`keydown`, ctrl.onKeydownBackForward);
                }
                modal.modalScope.open();
                return modal;
            });
    };

    ctrl.getDialog = function () {
        return modalService.getModal(`modalPreview_${ctrl.productId}`);
    };

    ctrl.resizeModalPreview = function () {
        $scope.$apply(() => {
            ctrl.updateModalPreview(ctrl.picture.originalPath);
            ctrl.carouselPreviewUpdate();
        });
    };

    //#endregion

    //#region productViewChange

    ctrl.showVideo = function (visible) {
        ctrl.visibleVideo = visible;

        if (visible === false) {
            ctrl.videosInModalReceived = false;
            ctrl.carouselVideosInModalInit = false;
        }
    };

    ctrl.onReceiveVideosInModal = function () {
        ctrl.videosInModalReceived = true;
    };

    ctrl.onInitCarouselVideosInModal = function () {
        ctrl.carouselVideosInModalInit = true;
    };

    ctrl.showRotate = function (visible) {
        ctrl.visibleRotate = visible;
    };

    //#endregion

    //#region shippingVariants
    ctrl.addShippingVariants = function (shippingVariants) {
        ctrl.shippingVariants = shippingVariants;
    };
    //#endregion

    //#region priceAmountList
    ctrl.addPriceAmountList = function (priceAmountList) {
        ctrl.priceAmountList = priceAmountList;
    };
    //#endregion

    //#region spinbox amount
    ctrl.updateAmount = function () {
        ctrl.refreshPrice();
    };
    //#endregion

    ctrl.filterPhotosFunction = function (item) {
        return (
            item &&
            (!item.carouselItemData.parameters?.colorId ||
                !ctrl.offerSelected.Color ||
                item.carouselItemData.parameters?.colorId === ctrl.offerSelected.Color.ColorId)
        );
    };

    ctrl.setPreviewByColorId = function (colorId, filterEnabled, carousel) {
        let findArray;

        if (ctrl.carousel) {
            if (filterEnabled === true) {
                ctrl.filterPhotos(colorId, carousel, ctrl.picture.PhotoId);
            } else {
                findArray = ctrl.carousel.items.filter(ctrl.filterPhotosFunction);

                if (findArray?.length > 0) {
                    ctrl.setPreview(findArray[0].carouselItemData.parameters);
                }
            }
        }
    };

    ctrl.filterPhotos = function (colorId, carousel, photoId) {
        let selectedItem, items, oldItem;

        if (carousel) {
            oldItem = carousel.getActiveItem();
            items = carousel.filterItems(ctrl.filterPhotosFunction, colorId);

            if (!items || items.length === 0) {
                carousel.addItem(oldItem);
            }

            selectedItem = items.find((slide) => slide.carouselItemData.PhotoId === photoId) ?? carousel.getActiveItem();

            if (selectedItem) {
                carousel.setItemSelect(selectedItem);
                ctrl.setPreview(selectedItem.carouselItemData.parameters);
            }
        }
    };

    ctrl.setView = function (viewName) {
        ctrl.productView = viewName;

        ctrl.stopVideo();
    };

    ctrl.setPreview = function (itemParameters) {
        if (itemParameters?.videoId) {
            ctrl.previewMediaType = 'video';
            ctrl.video = itemParameters;
        } else {
            ctrl.previewMediaType = 'image';
            ctrl.picture = itemParameters;
        }
    };

    ctrl.getUrl = function (url) {
        let result = url;
        const params = [];

        if (ctrl.colorsViewer?.colorSelected) {
            params.push(`color=${ctrl.colorsViewer.colorSelected.ColorId}`);
        }

        if (ctrl.sizesViewer?.sizeSelected) {
            params.push(`size=${ctrl.sizesViewer.sizeSelected.SizeId}`);
        }

        if (params.length > 0) {
            result = `${result}?${params.join('&')}`;
        }

        return result;
    };

    ctrl.getCommentsCount = function () {
        productService.getReviewsCount(ctrl.productId).then((result) => {
            if (result) {
                ctrl.reviewsCount = result.reviewsCount;
            }
        });
    };

    ctrl.addChangeSizeAndColorCallback = function (callback) {
        if (!callback) {
            throw new Error('Parameter "callback is required"');
        }

        callbackListColorsAndSizes.push(callback);

        if (lastValueChangesSizeColor !== undefined) {
            callback(lastValueChangesSizeColor);
        }
    };

    ctrl.processChangeSizeAndColorCallback = function (value, type, additionalData) {
        lastValueChangesSizeColor = value;
        if (callbackListColorsAndSizes.length > 0) {
            callbackListColorsAndSizes.forEach((fn) => fn(value, type, additionalData));
        }
    };

    ctrl.onKeydownBackForward = (event) => {
        if (event.code === `ArrowRight`) {
            ctrl.carouselPreviewNext();
        }
        if (event.code === `ArrowLeft`) {
            ctrl.carouselPreviewPrev();
        }
    };

    ctrl.handleChangeInplaceArtNo = function (value) {
        ctrl.offerSelected.ArtNo = value;
    };

    ctrl.filterPreviewCarouselItems = function () {
        if (ctrl.carouselPreview) {
            if (ctrl.filterPhotosEnable === true && ctrl.picture) {
                ctrl.filterPhotos(ctrl.offerSelected.Color?.ColorId, ctrl.carouselPreview, ctrl.picture.PhotoId);
            }
            const items = ctrl.carouselPreview.getItems();
            for (const itemsItem of items) {
                if (itemsItem.carouselItemData.parameters.PhotoId === ctrl.picture.PhotoId) {
                    ctrl.carouselPreview.setItemSelect(itemsItem);
                    ctrl.setPreview(itemsItem.carouselItemData.parameters);
                    ctrl.updateModalPreview(itemsItem.carouselItemData.parameters.originalPath);
                    break;
                }
            }
        }
    };

    //модальное окно с магазинами
    const modalId = 'mapShops';
    ctrl.openShopsMap = function (offerId, isMobile, yaMapsKey) {
        modalService.renderModal(
            modalId,
            $translate.instant('Js.Product.ShopsMap.Header'),
            `<div>
                <product-availability-map data-offer-id="${offerId}" data-mobile-mode="${isMobile}" data-api-key-map="${yaMapsKey}"></product-availability-map>
            </div>`,
            null,
            { destroyOnClose: true, modalClass: 'warehouses-list-modal' },
        );
        modalService.getModal(modalId).then((modal) => {
            modal.modalScope.open(true);
        });
    };
};

export default ProductCtrl;
