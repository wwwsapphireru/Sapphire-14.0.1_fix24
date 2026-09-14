using AdvantShop.Core.Common;
using AdvantShop.Core.Services.Shipping;
using AdvantShop.Diagnostics;
using AdvantShop.Orders;
using AdvantShop.Repository;
using AdvantShop.Shipping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class MeasureHelperV8
    {
        public static float[] GetDimensions(Order order,
                                            float defaultHeight = 0, float defaultWidth = 0, float defaultLength = 0,
                                            float rate = 1)
        {
            var calculationParameters =
                ShippingCalculationConfigurator.Configure()
                                               .WithTotalWeight(order.TotalWeight)
                                               .WithTotalLength(null/*order.TotalLength*/)
                                               .WithTotalWidth(null/*order.TotalWidth*/)
                                               .WithTotalHeight(null/*order.TotalHeight*/)
                                               .WithPreOrderItems(
                                                    order.OrderItems
                                                         .Select(x => new PreOrderItem(x))
                                                         .ToList())
                                               .Build();

            //Debug.Log.Info("CheckNullDimensions: " + JsonConvert.SerializeObject(calculationParameters));
            return GetDimensions(calculationParameters, defaultHeight, defaultWidth, defaultLength, rate);
        }

        public static float[] GetDimensions(ShippingCalculationParameters calculationParameters,
                                        float defaultHeight = 0, float defaultWidth = 0, float defaultLength = 0,
                                        float rate = 1)
        {
            if (calculationParameters.TotalLength != null &&
                calculationParameters.TotalWidth != null &&
                calculationParameters.TotalHeight != null)
            {
                return new float[3] {
                    (calculationParameters.TotalLength.Value == 0 ? defaultLength : calculationParameters.TotalLength.Value) / rate,
                    (calculationParameters.TotalWidth.Value == 0 ? defaultWidth : calculationParameters.TotalWidth.Value) / rate,
                    (calculationParameters.TotalHeight.Value == 0 ? defaultHeight : calculationParameters.TotalHeight.Value) / rate
                };
            }

            calculationParameters.PreOrderItems = calculationParameters.PreOrderItems ?? throw new ArgumentException($"The {nameof(calculationParameters.PreOrderItems)} property of the {nameof(calculationParameters)} parameter is null");

            var dimensions = calculationParameters.PreOrderItems.Select(item => new Measure
            {
                XYZ = new[]
                {
                    (item.Length == 0 ? defaultLength : item.Length) / rate,
                    (item.Width == 0 ? defaultWidth : item.Width) / rate,
                    (item.Height == 0 ? defaultHeight : item.Height) / rate,
                },
                Amount = item.Amount
            }).ToList();
         
            //return calculationParameters.PreOrderItems.Count == 1 && calculationParameters.PreOrderItems[0].Amount == 1
            //    ? dimensions[0].XYZ
            //    : GetDimensions(dimensions);
            return GetDimensions(dimensions);
        }

        private static float[] GetDimensions(List<Measure> dimensions)
        {
            var result = new float[3];

            foreach (var item in dimensions)
            {
                item.XYZ = item.XYZ.OrderByDescending(x => x).ToArray();
            }

            foreach (var dim in dimensions.Where(x => x.XYZ[0] != 0 && x.XYZ[1] != 0 && x.XYZ[2] != 0))
            {
                if (dim.XYZ[0] >= result[0])
                    result[0] = dim.XYZ[0];

                if (dim.XYZ[1] >= result[1])
                    result[1] = dim.XYZ[1];

                result[2] += dim.XYZ[2] * dim.Amount;
            }

            var settings = ModuleService.GetImportExportSettings();
            float[] minXYZ = new float[] { settings.ImportOrder.MinLength, settings.ImportOrder.MinWidth, settings.ImportOrder.MinHeight };
            minXYZ = minXYZ.OrderByDescending(x => x).ToArray();
            if (minXYZ[0] >= result[0])
                result[0] = minXYZ[0];
            if (minXYZ[1] >= result[1])
                result[1] = minXYZ[1];
            result[2] += minXYZ[2] * 1/*dim.Amount*/;

            return result;
        }

    }
}
