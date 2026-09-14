using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using AdvantShop.Catalog;
using AdvantShop.Configuration;
using AdvantShop.Core.Services.Localization;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Repository.Currencies;

namespace AdvantShop.Module.OneSApi.Handlers.Client
{
    public class FilterPriceHandler
    {
        #region Fields

        private readonly int _categoryId;
        private readonly bool _indepth;
        private readonly float _currentMin;
        private readonly float _currentMax;

        #endregion

        #region Constructor

        public FilterPriceHandler(int categoryId, bool indepth, float currentMin, float currentMax)
        {
            _categoryId = categoryId;
            _indepth = indepth;
            _currentMin = currentMin;
            _currentMax = currentMax;
        }

        #endregion

        private int Get10Pow(float src)
        {
            int pow = 1;
            while (src / (10 * pow) >= 1)
            {
                pow *= 10;
            }
            return pow;
        }

        private void MegaRound(ref float src1, ref float src2)
        {
            int pow = Get10Pow(Math.Max(src1, src2));
            int pow2 = Get10Pow(src1);
            src1 = (float) Math.Floor(src1/pow2*10)*pow2/10;
            src2 = (float) Math.Ceiling(src2/pow*10)*pow/10;
        }
        
        public FilterItemModel Get()
        {
            var prices = CategoryService.GetPriceRange(_categoryId, _indepth, SettingsCatalog.ShowOnlyAvalible);

            var currency = CurrencyService.CurrentCurrency;
            var min = (float)Math.Floor(prices.Key / currency.Rate);
            var max = (float)Math.Ceiling(prices.Value / currency.Rate);

            if (min == max)
                return null;

            MegaRound(ref min, ref max);

            var step = 1f;
            var range = Math.Abs(max - min);
            CalcStep(range, ref step);

            var priceModel = new FilterRangeItemModel
            {
                Min = min,
                Max = max,
                CurrentMin = _currentMin,
                CurrentMax = _currentMax,
                Step = step,
                DecimalPlaces = 3
            };

            if (min == max)
                return null;
            
            if (priceModel.CurrentMin < priceModel.Min || priceModel.CurrentMin >= priceModel.Max)
                priceModel.CurrentMin = priceModel.Min;

            if (priceModel.CurrentMax > priceModel.Max || priceModel.CurrentMax <= priceModel.Min)
                priceModel.CurrentMax = priceModel.Max;

            var model = new FilterItemModel
            {
                Expanded = true,
                Title = LocalizationService.GetResource("Catalog.FilterPrice.PriceFilterTitle"),
                Subtitle = currency.Symbol,
                Control = "range",
                Type = "price",
                Values = new List<object>(){priceModel}
            };

            return model;
        }

        public Task<List<FilterItemModel>> GetAsync()
        {
            var ctx = HttpContext.Current;

            return Task.Run(() =>
            {
                HttpContext.Current = ctx;
                Localization.Culture.InitializeCulture();

                return new List<FilterItemModel> {Get()};
            });
        }

        private void CalcStep(float range, ref float step)
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
        }
    }
}