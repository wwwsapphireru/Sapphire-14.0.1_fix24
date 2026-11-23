using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdvantShop.Customers;

namespace AdvantShop.Extensions
{
    // Vadim_Dyl_381073705: проверка реквизитов юрлица (ИНН/КПП/ОГРН/ОКПО)
    // по полям Customers.CustomerField (FieldAssignment). Используется в ЛК:
    // прямое сохранение пустых реквизитов (SaveUserInfo) и заявка на изменение (SendRequestCommonInfo).
    public static class CustomerFieldsValidation
    {
        public static List<string> ValidateLegalEntityFields(IEnumerable<CustomerFieldWithValue> fields)
        {
            var errors = new List<string>();

            if (fields == null)
                return errors;

            var assignments = CustomerFieldService.GetCustomerFieldsWithValue(System.Guid.Empty)
                .ToDictionary(x => x.Id, x => x.FieldAssignment);

            foreach (var field in fields)
            {
                var value = (field.Value ?? string.Empty).Trim();
                if (value.Length == 0)
                    continue;

                CustomerFieldAssignment assignment;
                if (!assignments.TryGetValue(field.Id, out assignment))
                    assignment = field.FieldAssignment;

                switch (assignment)
                {
                    case CustomerFieldAssignment.INN:
                        if (!Regex.IsMatch(value, @"^\d{10}(\d{2})?$"))
                            errors.Add("ИНН должен содержать 10 или 12 цифр");
                        break;
                    case CustomerFieldAssignment.KPP:
                        if (!Regex.IsMatch(value, @"^\d{9}$"))
                            errors.Add("КПП должен содержать 9 цифр");
                        break;
                    case CustomerFieldAssignment.OGRN:
                        if (!Regex.IsMatch(value, @"^\d{13}(\d{2})?$"))
                            errors.Add("ОГРН должен содержать 13 или 15 цифр");
                        break;
                    case CustomerFieldAssignment.OKPO:
                        if (!Regex.IsMatch(value, @"^\d{8}(\d{2})?$"))
                            errors.Add("ОКПО должен содержать 8 или 10 цифр");
                        break;
                }
            }

            return errors;
        }
    }
}
