import AimControl from '../../../_common/yandexMaps/aimControl';

/* @ngInject */
function AddressListCtrl(
    addressService,
    modalService,
    addressListConfig,
    $transclude,
    SweetAlert,
    $translate,
    apiMapService,
    zoneService,
    $q,
    yandexMapsService,
    shippingService,
    zoneMapService,
    $timeout,
    $scope,
) {
    const addressDetailsZoom = 17;
    const checkAddressList = new Map();
    const isInitPolygonsOnMapDefer = $q.defer();
    const deliveryZonesDefer = $q.defer();
    const coordsByAddressCanceler = $q.defer();
    let ctrl = this,
        timerChange,
        processContactTimer;
    ctrl.cities = [];
    ctrl.currentCity = null;
    ctrl.deliveryZones = [];
    ctrl.addressList = [];
    ctrl.aimControl = null;
    ctrl.isReadyShowMap = false;

    ctrl.$onInit = function () {
        // ctrl.apiMapKey = apiMapService.getMapApiKey();
        ctrl.hasFooterTransclude = $transclude.isSlotFilled('footer');
        ctrl.items = [];
        ctrl.isLoaded = false;
        ctrl.isLocked = false;
        ctrl.themeAlt = ctrl.themeAlt != null ? ctrl.themeAlt : addressListConfig.themeAlt;
        ctrl.compactMode = ctrl.compactMode != null ? ctrl.compactMode : addressListConfig.compactMode;
        ctrl.autocompleteAlt = ctrl.autocompleteAlt != null ? ctrl.autocompleteAlt : addressListConfig.autocompleteAlt;

        modalService.getModal('modalAddress').then((modal) => {
            ctrl.formModal = modal.modalScope._form;
        });

        addressService.getAddresses(ctrl.customerId, null, ctrl.isGeoMode).then((response) => {
            ctrl.items = [];

            if (response != null && response !== '' && response.length > 0) {
                ctrl.items = response;
            }

            ctrl.isLocked = Boolean(ctrl.isLockedChanges) && ctrl.items.length > 0;

            ctrl.addressSelected = addressService.findItemForSelect(ctrl.items, ctrl.contactId);

            ctrl.initAddressFn({
                address: ctrl.addressSelected,
            });

            ctrl.isLoaded = true;
        });

        // $q.all([ctrl.getCityList(), ctrl.getAddressList(ctrl.customerId, ctrl.currentCity), ctrl.getCurrentCity()])
        //     .then(([zone, addressList, currentCityResponse]) => {
        //         const currentCity = currentCityResponse.result ? currentCityResponse.obj : null;
        //
        //         ctrl.cities = zone.CountryCities;
        //         ctrl.currentCity = ctrl.getCurrentCityFromAddressList(currentCity, ctrl.cities);
        //         ctrl.items = [];
        //
        //         if (addressList != null && addressList !== '' && addressList.length > 0) {
        //             ctrl.addressList = addressList;
        //             if (ctrl.currentCity) {
        //                 ctrl.items = ctrl.getAddressListByCity(addressList, ctrl.currentCity);
        //             } else {
        //                 ctrl.items = addressList;
        //             }
        //         }
        //         ctrl.addressSelected = addressService.findItemForSelect(ctrl.items, ctrl.contactId);
        //
        //         ctrl.initAddressFn({
        //             address: ctrl.addressSelected,
        //         });
        //     })
        //     .catch((error) => {
        //         console.error(error);
        //     })
        //     .finally(() => {
        //         if (ctrl.isShowMap) {
        //             ctrl.getDeliveryZones(ctrl.currentCity?.CityId)
        //                 .then((data) => {
        //                     ctrl.deliveryZones = ctrl.getGeoObjectDeliveryZones(data?.obj || []);
        //                 })
        //                 .finally(() => {
        //                     deliveryZonesDefer.resolve(ctrl.deliveryZones);
        //                 });
        //         } else {
        //             deliveryZonesDefer.resolve();
        //         }
        //
        //         $q.all([deliveryZonesDefer.promise]).finally(() => {
        //             ctrl.isLoaded = true;
        //             // $scope.$apply();
        //         });
        //     });
    };

    ctrl.getAddressList = function (customerId, currentCity = null) {
        return addressService.getAddresses(customerId, currentCity);
    };

    ctrl.getAddressListByCity = function (addressList, city) {
        return addressList.filter((it) => it.RegionId === city.RegionId && it.City === city.Name);
    };

    ctrl.isCityValid = function (city) {
        return city && city.RegionId !== 0 && city.CityId !== 0 && city.Name !== null && typeof city.Name !== 'undefined' && city.Name.length > 0;
    };

    ctrl.isModalRendered = function () {
        return modalService.hasModal('modalAddress');
    };

    ctrl.change = function () {
        // тут был setTimout, убрали так как в модалке если
        // быстро переключать адрес и в модалке нажать на применить то не успевает запоминаться адрес
        // geoMode

        // if (ctrl.isShowMap) {
        //     coordsByAddressCanceler.resolve();
        //     coordsByAddressCanceler = $q.defer();
        //     // const addressString = ctrl.getStringifyAddress(ctrl.addressSelected);
        //     // ctrl.getCoordsByAddress(addressString, ctrl.apiMapKey, {
        //     //     timeout: coordsByAddressCanceler.promise,
        //     // }).then((coords) => {
        //     ctrl.setAllowInAddress(ctrl.addressSelected, ctrl.geoObjectPolygonsFromMap, {
        //         timeout: coordsByAddressCanceler.promise,
        //     })
        //         .then((addressData) => {
        //             if (addressData.coords) {
        //                 ctrl.map?.setCenter([addressData.coords.Latitude, addressData.coords.Longitude], addressDetailsZoom);
        //             }
        //             return addressData;
        //         })
        //         .then(({ address }) => {
        //             ctrl.setTextInAimControl(ctrl.getTextForAimControl(address.isAllow, address.polygon));
        //         })
        //
        //         .then(() => {
        //             ctrl.removeAimMap();
        //             ctrl.setAimMap();
        //             ctrl.setTextInAimControl(ctrl.getTextForAimControl(ctrl.addressSelected.isAllow, ctrl.addressSelected.polygon));
        //         });
        //     // });
        // }

        ctrl.changeAddressFn({
            address: ctrl.addressSelected,
        });
    };

    ctrl.onAddEditAddress = (formData, contacts, addressSelected, city) => {
        ctrl.save(formData, contacts, addressSelected, city);
    };

    ctrl.add = function () {
        if (ctrl.isLocked) return;
        if (ctrl.isModalRendered() === false) {
            addressService.dialogRender(null, ctrl);
        } else {
            addressService.dialogOpen();
        }
    };

    ctrl.edit = function (event, item) {
        if (ctrl.isLocked) return;
        event.preventDefault();

        const form = {};

        form.contactId = item.ContactId;
        form.fio = item.Name;
        form.firstName = item.FirstName;
        form.lastName = item.LastName;
        form.patronymic = item.Patronymic;
        form.countryId = item.CountryId;
        form.country = item.Country; //set in ctrl.buildModal()
        form.region = item.Region;
        form.city = item.City;
        form.district = item.District;
        form.zip = item.Zip;
        form.street = item.Street;
        form.house = item.House;
        form.apartment = item.Apartment;
        form.structure = item.Structure;
        form.entrance = item.Entrance;
        form.floor = item.Floor;

        if (ctrl.isModalRendered() === false) {
            addressService.dialogRender('addressList.modalCallbackClose', { ...ctrl, formData: form });
        } else {
            addressService.dialogOpen();
        }
    };

    ctrl.remove = function (contactId, index) {
        if (ctrl.isLocked) return;
        SweetAlert.confirm($translate.instant('Js.Address.AreYouSureDelete'), {
            title: $translate.instant('Js.Address.Deleting'),
        }).then((result) => {
            if (result === true || result.value) {
                addressService.removeAddress(contactId, ctrl.customerId).then((response) => {
                    if (response === true) {
                        const isItemSeletedRemoved = ctrl.addressSelected === ctrl.items[index];

                        const itemRemoved = ctrl.items.splice(index, 1);

                        if (isItemSeletedRemoved && ctrl.items.length > 0) {
                            ctrl.addressSelected = ctrl.items[0];
                        }

                        if (ctrl.deleteAddressFn != null) {
                            ctrl.deleteAddressFn({
                                items: ctrl.items,
                                itemRemoved: itemRemoved[0],
                                addressSelected: ctrl.addressSelected,
                                isItemSeletedRemoved,
                            });
                        }
                    }
                });
            }
        });
    };

    ctrl.save = function (formData, contacts, addressSelected, city) {
        // ctrl.items = contacts;
        if (city && ctrl.isCityValid(city)) {
            ctrl.currentCity = city;
            ctrl.items = ctrl.getAddressListByCity(contacts, city);
        } else {
            ctrl.items = contacts;
        }

        ctrl.addressSelected = addressSelected;

        // addressService.dialogClose();

        ctrl.isLocked = Boolean(ctrl.isLockedChanges) && ctrl.items.length > 0;

        ctrl.saveAddressFn({
            address: ctrl.addressSelected,
        });

        // addressService.getAddresses(ctrl.customerId).then(function (response) {
        //     ctrl.items = response;
        //     ctrl.addressSelected = ctrl.findItemForSelect();
        //
        //     addressService.dialogClose();
        //
        //     ctrl.saveAddressFn({
        //         address: ctrl.addressSelected,
        //     });
        // });
    };

    ctrl.cancelAdding = function (modalId) {
        modalService.close(modalId);
    };

    ctrl.getObjectForUpdate = function (form) {
        const account = {};

        if (form.contactId) {
            account.ContactId = form.contactId;
        }

        if (form.fio) {
            account.Fio = form.fio;
        }

        if (form.firstName) {
            account.FirstName = form.firstName;
        }
        if (form.lastName) {
            account.LastName = form.lastName;
        }
        if (form.patronymic) {
            account.Patronymic = form.patronymic;
        }

        if (form.country) {
            account.CountryId = form.country.CountryId;
            account.Country = form.country.Name;
        }

        if (form.region) {
            account.Region = form.region;
        }

        if (form.district) {
            account.District = form.district;
        }

        if (form.city) {
            account.City = form.city;
        }

        if (form.zip) {
            account.Zip = form.zip;
        }

        account.Street = form.street;
        account.House = form.house;
        account.Apartment = form.apartment;
        account.Structure = form.structure;
        account.Entrance = form.entrance;
        account.Floor = form.floor;

        account.IsShowName = ctrl.isShowName;
        account.IsMain = ctrl.addressSelected != null ? form.contactId === ctrl.addressSelected.ContactId : false;

        account.IsRequiredAddress = ctrl.isRequiredAddress;//GlorySoft_011

        return account;
    };

    ctrl.addressStringify = function (address) {
        addressService.addressStringify(address);
    };

    ctrl.transcudeContent = function (scope, element) {
        const scopeNew = scope.$parent.$new();
        scopeNew.address = scope.item;
        const isFooterContainer = element[0].classList.contains('js-address-list-footer-transclude');

        if ($transclude.isSlotFilled('footer')) {
            if (isFooterContainer) {
                $transclude(scope.$new(), (clone) => element.append(clone), null, 'footer');
            }
        } else if (!isFooterContainer) {
            $transclude(scopeNew, (clone) => element.append(clone), null, null);
        }
    };

    ctrl.onInitMap = function (map) {
        ctrl.map = map;

        if (ctrl.addressSelected && ctrl.addressSelected.City === ctrl.currentCity.Name) {
            deliveryZonesDefer.promise
                .then((deliveryZones) => {
                    if (deliveryZones && deliveryZones.length > 0) {
                        // ждем инициализации geoObj полигонов
                        return isInitPolygonsOnMapDefer.promise;
                    }
                })
                .then((geoObjPolygons) =>
                    // TODO чтоб присылал сразу бэк адрес с координатами, тратиться DaData
                    $q.all([ctrl.getCoordsCityByName(ctrl.currentCity.Name), ctrl.setAllowInAddress(ctrl.addressSelected, geoObjPolygons)]),
                )
                .then(([coordsCity, addressData]) => {
                    ctrl.map.setBounds(coordsCity.bounds);
                    return addressData;
                })
                .then(({ coords }) => {
                    if (coords) {
                        ctrl.map.setCenter([coords.Latitude, coords.Longitude]);
                    }
                })
                .then(() => {
                    ctrl.setAimMap();
                    ctrl.setTextInAimControl(ctrl.getTextForAimControl(ctrl.addressSelected.isAllow, ctrl.addressSelected.polygon));
                })
                .finally(() => {
                    ctrl.isReadyShowMap = true;
                    $scope.$apply();
                });
        } else {
            ctrl.getCoordsCityByName(ctrl.currentCity.Name).then((data) => {
                ctrl.map.setBounds(data.bounds).then(() => {
                    ctrl.map.setCenter(data.coords).then(() => {
                        ctrl.isReadyShowMap = true;
                        $scope.$apply();
                    });
                });
            });
        }
    };

    ctrl.onInitDeliveryZoneFromMap = function (polygons) {
        ctrl.geoObjectPolygonsFromMap = polygons;
        isInitPolygonsOnMapDefer.resolve(polygons);
    };

    ctrl.getCityList = function () {
        return zoneService.getZones();
    };

    ctrl.getCurrentCityFromAddressList = function (currentCity, cities) {
        let city = cities[0];
        if (currentCity) {
            city = cities.find((it) => it.CityId === currentCity.CityId);
        }
        return city || cities[0];
    };

    ctrl.getCoordsCityByName = (cityName) => yandexMapsService.getCoordsCityByName(cityName);

    ctrl.getDeliveryZones = function (cityId) {
        return shippingService.getDeliveryZones(cityId);
    };

    ctrl.onChangeCity = function (city) {
        ctrl.addressSelected = addressService.findItemForSelect(ctrl.items, ctrl.contactId);
        // ctrl.addressSelected = null;
        if (!ctrl.addressSelected) {
            ctrl.removeAimMap();
        }
        ctrl.items = ctrl.getAddressListByCity(ctrl.addressList, city);
        // if (ctrl.isShowMap) {
        //     $q.all([ctrl.getDeliveryZones(city.CityId), ctrl.getCoordsCityByName(ctrl.currentCity.Name)]).then(([zones, coordsCity]) => {
        //         // создаем еще промис чтоб карта нам снова вернула гео объекты
        //         isInitPolygonsOnMapDefer = $q.defer();
        //         ctrl.deliveryZones = [];
        //         $timeout(() => {
        //             ctrl.deliveryZones = ctrl.getGeoObjectDeliveryZones(zones?.obj || []);
        //             if (ctrl.deliveryZones.length === 0) {
        //                 isInitPolygonsOnMapDefer.resolve();
        //             }
        //
        //             isInitPolygonsOnMapDefer.promise.then((geoObjPolygons) => {
        //                 ctrl.map?.setBounds(coordsCity.bounds).then(() => {
        //                     ctrl.map?.setCenter(coordsCity.coords);
        //                 });
        //             });
        //         });
        //     });
        // }
    };

    ctrl.getGeoObjectDeliveryZones = function (deliveryZones) {
        return shippingService.getGeoObjectsDeliveryZones(deliveryZones);
    };

    ctrl.getCurrentCity = function () {
        return zoneService.getCurrentCity();
    };

    ctrl.setAimMap = function () {
        ctrl.aimControl = new AimControl();
        ctrl.map?.controls.add(ctrl.aimControl);
    };

    ctrl.removeAimMap = function () {
        ctrl.map?.controls.remove(ctrl.aimControl);
    };

    ctrl.getStringifyAddress = function (addressObj) {
        return addressService.addressStringify(addressObj);
    };

    ctrl.getCoordsByAddress = function (addressString, apiMapKey, options) {
        return zoneMapService.getCoordsByAddress(addressString, apiMapKey, options).then((response) => response?.data.obj);
    };

    ctrl.checkAddressInDeliveryZones = function (coords, deliveryZones) {
        return yandexMapsService.checkPointInPolygons(coords, deliveryZones);
    };

    ctrl.setAllowInAddress = function (address, geoObjPolygons = [], fetchOptions = null) {
        const defer = $q.defer();
        if (!checkAddressList.has(address.ContactId)) {
            address.isAllow = true;
            if (geoObjPolygons.length > 0) {
                // TODO изменить на address.AggregatedAddress
                ctrl.getCoordsByAddress(ctrl.getStringifyAddress(address), ctrl.apiMapKey, fetchOptions).then((coords) => {
                    if (coords) {
                        const { isContain, currentPolygon } = ctrl.checkAddressInDeliveryZones([coords.Latitude, coords.Longitude], geoObjPolygons);
                        address.isAllow = isContain;
                        address.Latitude = coords.Latitude;
                        address.Longitude = coords.Longitude;
                        address.polygon = currentPolygon;
                    }
                    defer.resolve({
                        address,
                        coords,
                    });
                });
            } else {
                defer.resolve({
                    address,
                });
            }
            checkAddressList.set(address.ContactId, address);
        } else {
            const addressFromCache = checkAddressList.get(address.ContactId);
            defer.resolve({
                address: addressFromCache,
            });
        }
        return defer.promise;
    };

    ctrl.getTextForAimControl = function (isAddressInZone, geoObjectPolygon = null) {
        const polygonProperties = geoObjectPolygon?.properties.getAll();
        const deliveryTime = polygonProperties?.deliveryTime;
        return $translate.instant(
            isAddressInZone ? (deliveryTime?.length > 0 ? deliveryTime : 'Js.Address.ZoneDelivery.Deliver') : 'Js.Address.ZoneDelivery.NotDeliver',
        );
    };

    ctrl.setTextInAimControl = function (text) {
        ctrl.aimControl?.setText(text);
    };
}

export default AddressListCtrl;
