using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AdvantShop.Web.Infrastructure.Api;

namespace AdvantShop.Module.OneSApi.Models.Api.Orders
{
    public class FilterOrdersModel : EntitiesFilterModel, IValidatableObject
    {
        public FilterOrdersModel()
        {
        }

        public Guid? CustomerId { get; set; }
        public int? StatusId { get; set; }
        public bool? IsPaid { get; set; }
        public float? SumFrom { get; set; }
        public float? SumTo { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }

        private bool loadItems;
        public bool LoadItems
        {
            get
            {
                return loadItems;
            }
            set
            {
                loadItems = value;

                if (loadItems)
                {
                    MaxItemsPerPage = 50;
                    DefaultItemsPerPage = 50;
                }
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DateTime temp;
            if (!string.IsNullOrWhiteSpace(DateFrom) && !DateTime.TryParse(DateFrom, out temp))
                yield return new ValidationResult("Неудалось преобразовать дату параметра DateFrom");

            if (!string.IsNullOrWhiteSpace(DateTo) && !DateTime.TryParse(DateTo, out temp))
                yield return new ValidationResult("Неудалось преобразовать дату параметра DateTo");
        }
    }
}