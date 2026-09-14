using AdvantShop.Core.Services.Localization;
using AdvantShop.Core.Services.Webhook.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Extensions
{
    public static class OrderExtensions
    {
        public static string GetCustomerAddress(this OrderCustomerModel customer)
        {
            if (customer == null)
                return "";

            var result = customer.Zip ?? "";

            if (!string.IsNullOrEmpty(customer.Region))
                result += ", " + customer.Region;

            if (!string.IsNullOrEmpty(customer.District))
                result += ", " + customer.District;

            if (!string.IsNullOrEmpty(customer.City))
                result += ", " + customer.City;

            if (!string.IsNullOrEmpty(customer.Street))
                result += ", " + " " + customer.Street;

            if (!string.IsNullOrEmpty(customer.House))
                result += " " + LocalizationService.GetResource("Core.Orders.OrderContact.House") + " " + customer.House;

            if (!string.IsNullOrEmpty(customer.Structure))
                result += ", " + LocalizationService.GetResource("Core.Orders.OrderContact.Structure") + " " + customer.Structure;

            if (!string.IsNullOrEmpty(customer.Apartment))
                result += ", " + LocalizationService.GetResource("Core.Orders.OrderContact.Apartment") + " " + customer.Apartment;

            if (!string.IsNullOrEmpty(customer.Entrance))
                result += ", " + LocalizationService.GetResource("Core.Orders.OrderContact.Entrance") + " " + customer.Entrance;

            if (!string.IsNullOrEmpty(customer.Floor))
                result += ", " + LocalizationService.GetResource("Core.Orders.OrderContact.Floor") + " " + customer.Floor;


            return result;
        }
    }
}
