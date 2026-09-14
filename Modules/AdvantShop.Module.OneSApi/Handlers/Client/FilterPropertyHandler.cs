using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvantShop.Catalog;
using AdvantShop.Core.Caching;
using System;
using AdvantShop.Configuration;
using AdvantShop.Module.OneSApi.Models.Client;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterPropertyHandler
    {
        #region Fields

        private readonly int _categoryId;
        private readonly bool _indepth;

        private readonly List<int> _selectedPropertyIds;
        private readonly List<int> _availablePropertyIds;
        private readonly Dictionary<int, KeyValuePair<float, float>> _rangePropertyIds;
        private readonly List<int> _productIds;
        private readonly EProductOnMain _productOnMainType;
        private readonly int? _productListId;

        #endregion

        public FilterPropertyHandler(int categoryId, bool indepth, List<int> selectedPropertyIds,
                                    List<int> availablePropertyIds, 
                                    Dictionary<int, KeyValuePair<float, float>> rangePropertyIds,
                                    List<int> productIds = null,
                                    EProductOnMain productOnMainType = EProductOnMain.None,
                                    int? productListId = null)
        {
            _categoryId = categoryId;
            _indepth = indepth;
            _selectedPropertyIds = selectedPropertyIds ?? new List<int>();
            _availablePropertyIds = availablePropertyIds;
            _rangePropertyIds = rangePropertyIds;
            _productIds = productIds;
            _productOnMainType = productOnMainType;
            _productListId = productListId;
        }

        public List<FilterItemModel> Get()
        {
            var propertyValues =
                _productOnMainType == EProductOnMain.None || _productOnMainType == EProductOnMain.Sale

                    ? CacheManager.Get(CacheNames.PropertiesInCategoryCacheName(_categoryId, _indepth, SettingsCatalog.ShowOnlyAvalible, _productIds),
                        () => PropertyService.GetPropertyValuesByCategories(_categoryId, _indepth, _productIds))

                    : CacheManager.Get(CacheNames.PropertiesInCategoryCacheName((int) _productOnMainType, _productListId),
                        () => PropertyService.GetPropertyValuesByCategories(_productOnMainType, _productListId ?? 0));

            if (propertyValues == null)
                return new List<FilterItemModel>();

            var properties = propertyValues.Select(item => new UsedProperty
                {
                    PropertyId = item.PropertyId,
                    Name = string.IsNullOrEmpty(item.Property.NameDisplayed)
                        ? item.Property.Name
                        : item.Property.NameDisplayed,
                    Expanded = item.Property.Expanded,
                    Unit = item.Property.Unit,
                    Type = item.Property.Type,
                    Description = item.Property.Description,
                    SortOrder = item.Property.SortOrder,
                    ValuesList =
                        propertyValues.Where(value => value.PropertyId == item.PropertyId)
                            .OrderBy(pv => pv.SortOrder)
                            .ThenBy(pv => pv.RangeValue)
                            .ThenBy(pv => pv.Value),
                }).Distinct(new PropertyComparer())
                  .OrderBy(p => p.SortOrder)
                  .ThenBy(p => p.Name)
                  .ToList();

            var list = new List<FilterItemModel>();

            foreach (var property in properties)
            {
                var item = new FilterItemModel()
                {
                    Expanded = property.Expanded,
                    Type = "prop",
                    Title = property.Name,
                    Subtitle = !string.IsNullOrEmpty(property.Unit) ? property.Unit : null,
                    Description = !string.IsNullOrEmpty(property.Description) ? property.Description : null,
                };

                GetItemValues(item, property);

                if (item.Values.Count > 0)
                    list.Add(item);

                if (item.Values.Any(itemValue => itemValue is FilterListItemModel model && model.Selected))
                {   
                    foreach (FilterListItemModel model in item.Values)
                    {
                        model.Available = true;
                    }
                }
            }

            return list;
        }

        private void GetItemValues(FilterItemModel item, UsedProperty property)
        {
            var selected = false;
            var expanded = false;

            switch (property.Type)
            {
                case (int)PropertyType.Checkbox:
                    item.Control = "checkbox";

                    foreach (var value in property.ValuesList)
                    {
                        selected = _selectedPropertyIds != null && _selectedPropertyIds.Contains(value.PropertyValueId);
                        if (selected)
                            expanded = true;

                        item.Values.Add(new FilterListItemModel()
                        {
                            Id = value.PropertyValueId.ToString(),
                            Text = value.Value,
                            Selected = selected,
                            Available = selected || _availablePropertyIds == null || _availablePropertyIds.Contains(value.PropertyValueId),
                        });
                    }
                    break;

                case (int)PropertyType.Selectbox:
                    item.Control = "select";

                    foreach (var value in property.ValuesList)
                    {
                        if (_availablePropertyIds == null ||
                            (_availablePropertyIds != null && _availablePropertyIds.Contains(value.PropertyValueId)))
                        {
                            if (!selected)
                                selected = _selectedPropertyIds != null && _selectedPropertyIds.Contains(value.PropertyValueId);

                            if (selected)
                                expanded = true;

                            item.Values.Add(new FilterListItemModel()
                            {
                                Id = value.PropertyValueId.ToString(),
                                Text = value.Value,
                                Selected = selected,
                                Available = true,
                            });
                        }
                    }
                    break;

                case (int)PropertyType.Range:
                    item.Control = "range";

                    var list = property.ValuesList.Select(v => v.RangeValue).Where(v => v != 0).ToList();
                    if (list.Count < 2)
                        break;

                    var min = list.Min();
                    var max = list.Max();

                    if (min == max && min == 0)
                        break;

                    var curmin = min;
                    var curmax = max;

                    var step = 1f;
                    var listDecimalPlaces = list.Select(x => x.ToString().Remove(0, x.ToString().Contains(",") ? x.ToString().IndexOf(",") + 1 : 0).Length).ToList();
                    var range = Math.Abs(max - min);
                    CalcStep(range, min, ref step);

                    selected = _rangePropertyIds != null && _rangePropertyIds.ContainsKey(property.PropertyId);

                    if (selected)
                    {
                        curmin = _rangePropertyIds[property.PropertyId].Key;
                        curmax = _rangePropertyIds[property.PropertyId].Value;
                        expanded = true;
                    }

                    item.Values.Add(new FilterRangeItemModel()
                    {
                        Id = property.PropertyId,
                        CurrentMax = curmax,
                        CurrentMin = curmin,
                        DecimalPlaces = listDecimalPlaces.Max(),
                        Step = step,
                        Min = min,
                        Max = max,
                    });
                    break;
            }

            if (item.Expanded || expanded)
                item.Expanded = true;
        }



        public Task<List<FilterItemModel>> GetAsync()
        {
            return Task.Run(() =>
            {
                Localization.Culture.InitializeCulture();
                return Get();
            });
        }

        private void CalcStep(float range, float min, ref float step)
        {
            if (range == 0)
            {
                step = 0;
            }
            else if (range < 0.001)
            {
                step = 0.00001f;
            }
            else if (range < 0.01)
            {
                step = 0.0001f;
            }
            else if (range < 0.1)
            {
                step = 0.001f;
            }
            else if (range < 1)
            {
                step = 0.01f;
            }
            else if (range < 10)
            {
                step = 0.1f;
            }
            else if (range < 100)
            {
                step = 1f;
            }
            else if (range < 1000)
            {
                step = 10f;
            }
            else if (range < 10000)
            {
                step = 100f;
            }
            else
            {
                step = 1000f;
            }

            var degreeOfMin = GetDegree(min);
            var degreeOfStep = GetDegree(step);

            if (degreeOfMin - degreeOfStep > 3)
            {
                step = (float)Math.Pow(10, degreeOfMin - 3);
            }
        }

        private int GetDegree(double number)
        {
            if (number == 0)
                return 0;

            var degree = 0;

            if (Math.Abs(number) >= 1)
            {
                degree = -1;

                while (number >= 1)
                {
                    degree++;
                    number = Math.Truncate(number / 10);
                }
            }
            else
            {
                while (number < 1)
                {
                    degree--;
                    number = number * 10;
                }
            }
            return degree;
        }
    }
}