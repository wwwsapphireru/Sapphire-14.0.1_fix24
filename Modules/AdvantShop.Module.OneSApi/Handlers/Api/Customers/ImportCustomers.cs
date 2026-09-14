using AdvantShop.Core.Common.Extensions;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Repository;
using AdvantShop.Web.Infrastructure.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AdvantShop.Module.OneSApi.Handlers.Api.Customers
{
    public class ImportCustomers : ICommandHandler<CustomersImportModel, CustomersImportResultModel>
    {
        private ImportExportSettingsModel _settings;
        private bool _withStatisctic;
        private const string _modifiedBy = OneSApi.ModuleName;

        public ImportCustomers(bool withStatisctic)
        {
            _withStatisctic = withStatisctic;
        }

        public CustomersImportResultModel Execute(CustomersImportModel catalog)
        {
            var start = DateTime.Now;
            _settings = ModuleService.GetImportExportSettings();

            var result = new CustomersImportResultModel();
            if (catalog.Managers != null)
                result.Managers = new List<ImportResultModel> { };
            if (catalog.Customers != null)
                result.Customers = new List<ImportResultModel> { };

            ExecuteManagers(catalog.Managers, ref result);
            ExecuteCustomers(catalog.Customers, ref result);

            ModuleService.WriteLog("ImportCustomers", JsonConvert.SerializeObject(catalog, Formatting.Indented), JsonConvert.SerializeObject(result, Formatting.Indented), start);

            return result;
        }

        private void ExecuteManagers(List<AddUpdateManagerModel> managers, ref CustomersImportResultModel result)
        {
            if (managers == null)
                return;

            foreach (var item in managers)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.Code.ToString(), Success = true };

                var FIO = item.FIO.Trim();
                while (FIO.Contains("  "))
                    FIO = FIO.Replace("  ", " ");
                var fio = FIO.Split(" ");
                if (fio.Length > 0)
                    item.LastName = fio[0];
                if (fio.Length > 1)
                    item.FirstName = fio[1];
                if (fio.Length > 2)
                    item.Patronymic = fio[2];

                var manager = ImportService.GetManagerByCode(item.Code);
                if (manager == null)
                {
                    //resultItem.Error = "Менеджер не добавляется через обмен - УТОЧНИТЬ РЕАЛИЗАЦИЮ";
                    //result.Managers.Add(resultItem);
                    //continue;
                    if (item.Email.IsNullOrEmpty())
                    {
                        resultItem.Error = "Менеджер не добавляется т.к. не указан Email";
                        result.Managers.Add(resultItem);
                        continue;
                    }
                    var customer = CustomerService.GetCustomerByEmail(item.Email);
                    if (customer != null && customer.Manager != null)
                    {
                        manager = customer.Manager;
                        ImportService.SetManagerCode(manager.ManagerId, item.Code); 
                    }
                    if (manager == null)
                    {
                        if (customer != null && (customer.CustomerRole != Role.Moderator && customer.CustomerRole != Role.Administrator))
                        {
                            resultItem.Error = "Менеджер не добавляется т.к. Email есть в списке покупателей";
                            result.Managers.Add(resultItem);
                            continue;
                        }
                        if (customer == null && !item.Active)
                        {
                            resultItem.Error = "Менеджер не добавляется т.к. Active=False";
                            result.Managers.Add(resultItem);
                            continue;
                        }
                        if (customer == null)
                        {
                            customer = new Customer
                            {
                                EMail = item.Email,
                                CustomerRole = Role.Moderator,
                                FirstName = item.FirstName,
                                LastName = item.LastName,
                                Patronymic = item.Patronymic,
                                Phone = item.Phone,
                                StandardPhone = StringHelper.ConvertToStandardPhone(item.Phone, true, true),
                                Enabled = true
                            };
                            customer.Id = CustomerService.InsertNewCustomer(customer);
                        }
                        manager = new Manager
                        {
                            CustomerId = customer.Id,
                            DepartmentId = ImportService.GetDepartmentIdBySort(item.Department),
                            Sign = _settings.ImportCustomer.ManagerSignTemplate.Replace("#PHONE#", item.Phone).Replace("#EMAIL#", item.Email)
                        };
                        ManagerService.AddOrUpdateManager(manager);
                        ImportService.SetManagerCode(manager.ManagerId, item.Code);
                    }
                }
                if (manager != null)
                {
                    var sign = _settings.ImportCustomer.ManagerSignTemplate.Replace("#PHONE#", item.Phone).Replace("#EMAIL#", item.Email);
                    if (item.Active != manager.Enabled || item.FirstName != manager.FirstName || item.LastName != manager.LastName || item.Patronymic != manager.Customer.Patronymic || item.Phone != manager.Customer.Phone || sign != manager.Sign)
                    {
                        var customer = manager.Customer;
                        customer.Enabled = item.Active;
                        customer.FirstName = item.FirstName;
                        customer.LastName = item.LastName;
                        customer.Patronymic = item.Patronymic;
                        customer.Phone = item.Phone;
                        customer.StandardPhone = StringHelper.ConvertToStandardPhone(item.Phone, true, true);
                        CustomerService.UpdateCustomer(customer);
                        ExportService.ConfirmCustomer(customer.Id.ToString());
                        if (manager.Sign != sign)
                        {
                            manager.Sign = sign;
                            ManagerService.AddOrUpdateManager(manager);
                        }
                    }
                }

                result.Managers.Add(resultItem);
            }
        }

        private void ExecuteCustomers(List<AddUpdateCustomerModel> customers, ref CustomersImportResultModel result)
        {
            if (customers == null)
                return;

            foreach (var item in customers)
            {
                if (_withStatisctic)
                    ModuleStatistic.RowPosition++;

                var resultItem = new ImportResultModel() { ExternalId = item.Id, Success = true };

                var customer = ValidateCustomer(item, ref resultItem);

                if (resultItem.Success)
                {
                    //var trackChanges = false;
                    //var changedBy = new OrderChangedBy(_modifiedBy);

                    var error = AddUpdateCustomer(item, customer);
                    if (error.IsNotEmpty())
                    {
                        resultItem.Success = false;
                        resultItem.Error = error;
                    }
                }

                result.Customers.Add(resultItem);
            }
        }

        private Customer ValidateCustomer(AddUpdateCustomerModel model, ref ImportResultModel resultItem)
        {
            if (!ValidationHelper.IsValidEmail(model.Email))
            {
                resultItem.Success = false;
                resultItem.Error = "Не валидный email";
                return null;
            }

            var customer = CustomerService.GetCustomer(model.Id.TryParseGuid());
            var isEditMode = customer != null/*!model.Id.IsNullOrEmpty()*/;
            if (isEditMode && !_settings.ImportCustomer.UpdateCustomers)
            {
                resultItem.Success = false;
                resultItem.Error = "Не обновлется согласно настройке";
                return customer;
            }
            if (!isEditMode && !_settings.ImportCustomer.AddCustomers)
            {
                resultItem.Success = false;
                resultItem.Error = "Не добавлется согласно настройке";
                return customer;
            }

            if (!isEditMode)
            {
                if (!string.IsNullOrWhiteSpace(model.Email) && CustomerService.IsEmailExist(model.Email))
                {
                    resultItem.Success = false;
                    resultItem.Error = "Пользователь с таким email уже существует";
                    return customer;
                }

                if (model.Phone.IsNotEmpty())
                {
                    var standardPhone = StringHelper.ConvertToStandardPhone(model.Phone.ToString(), true, true);
                    if (!standardPhone.HasValue)
                    {
                        resultItem.Success = false;
                        resultItem.Error = "Неккорректный номер телефона";
                        return customer;
                    }
                    var phone = standardPhone != null ? standardPhone.ToString() : model.Phone.ToString();

                    if (CustomersService.IsPhoneExist(phone, standardPhone))
                    {
                        resultItem.Success = false;
                        resultItem.Error = "Пользователь с таким телефоном уже существует";
                        return customer;
                    }
                }
            }
            else
            {
                //if (customer == null)
                //{
                //    error = "Покупатель не найден";
                //    return null;
                //}

                if (customer.EMail.ToLower() != model.Email.ToLower() && !string.IsNullOrWhiteSpace(model.Email) && CustomerService.IsEmailExist(model.Email))
                {
                    resultItem.Success = false;
                    resultItem.Error = "Пользователь с таким email уже существует";
                    return customer;
                }

                if (model.Phone != null)
                {
                    var standardPhone = StringHelper.ConvertToStandardPhone(model.Phone.ToString(), true, true);
                    var phone = standardPhone != null ? standardPhone.ToString() : model.Phone.ToString();

                    if (customer.StandardPhone != standardPhone && customer.CustomerType != CustomerType.LegalEntity && CustomersService.IsPhoneExist(phone, standardPhone, CustomerType.PhysicalEntity))
                    {
                        resultItem.Success = false;
                        resultItem.Error = "Пользователь с таким телефоном уже существует";
                        return customer;
                    }
                }
            }

            return customer;
        }

        private string AddUpdateCustomer(AddUpdateCustomerModel model, Customer customer)
        {
            var isEditMode = customer != null;
            if (!isEditMode)
            {
                customer = new Customer(CustomerGroupService.DefaultCustomerGroup)
                { 
                    CustomerRole = Role.User, 
                    EMail = model.Email.EncodeOrEmpty(),
                    Password = model.Password
                };
            }

            var trackChanges = false;
            var changedBy = new Core.Services.ChangeHistories.ChangedBy(_modifiedBy);

            if (model.IsDeleted)
            {
                if (!isEditMode)
                {
                    return null;
                }
                else if (isEditMode && _settings.ImportCustomer.DeleteCustomers)
                {
                    CustomerService.DeleteCustomer(Guid.Parse(model.Id), true, changedBy);
                    return null;
                }
            }

            //customer.EMail = model.Email.EncodeOrEmpty();

            var FIO = model.FIO.Trim();
            while (FIO.Contains("  "))
                FIO = FIO.Replace("  ", " ");
            var fio = FIO.Split(" ");
            if (fio.Length > 0)
                model.LastName = fio[0];
            if (fio.Length > 1)
                model.FirstName = fio[1];
            if (fio.Length > 2)
                model.Patronymic = fio[2];

            if (model.FirstName.IsNotEmpty())
            {
                if (!isEditMode || customer.FirstName != model.FirstName.EncodeOrEmpty())
                {
                    customer.FirstName = model.FirstName.EncodeOrEmpty();
                    trackChanges = true;
                }
            }

            if (model.LastName.IsNotEmpty())
            {
                if (!isEditMode || customer.LastName != model.LastName.EncodeOrEmpty())
                {
                    customer.LastName = model.LastName.EncodeOrEmpty();
                    trackChanges = true;
                }
            }

            if (model.Patronymic.IsNotEmpty())
            {
                if (!isEditMode || customer.Patronymic != model.Patronymic.EncodeOrEmpty())
                {
                    customer.Patronymic = model.Patronymic.EncodeOrEmpty();
                    trackChanges = true;
                }
            }

            if (model.Organization.IsNotEmpty())
            {
                if (!isEditMode || customer.Organization != model.Organization.EncodeOrEmpty())
                {
                    customer.Organization = model.Organization.EncodeOrEmpty();
                    trackChanges = true;
                }
            }

            if (model.Phone.IsNotEmpty())
            {
                var standardPhone = StringHelper.ConvertToStandardPhone(model.Phone, true, true);
                //var phone = standardPhone != null ? standardPhone.ToString() : model.Phone;
                if (!isEditMode || customer.StandardPhone != standardPhone)
                {
                    customer.Phone = model.Phone;//standardPhone != null ? standardPhone.ToString() : phone;
                    customer.StandardPhone = standardPhone;
                    trackChanges = true;
                }
            }

            if (model.Email.IsNotEmpty())
            {
                if (!isEditMode || customer.EMail != model.Email)
                {
                    customer.EMail = model.Email;
                    trackChanges = true;
                }
            }

            if (model.SubscribedForNews != null)
            {
                if (!isEditMode || customer.SubscribedForNews != model.SubscribedForNews.Value)
                {
                    customer.SubscribedForNews = model.SubscribedForNews.Value;
                    trackChanges = true;
                }
            }

            if (model.BirthDay != null)
            {
                if (!isEditMode || customer.BirthDay != model.BirthDay)
                {
                    customer.BirthDay = model.BirthDay;
                    trackChanges = true;
                }
            }

            if (model.AdminComment.IsNotEmpty())
            {
                if (!isEditMode || customer.AdminComment != model.AdminComment.EncodeOrEmpty())
                {
                    customer.AdminComment = model.AdminComment.EncodeOrEmpty();
                    trackChanges = true;
                }
            }

            if (model.ManagerCode.HasValue)
            {
                var manager = ImportService.GetManagerByCode(model.ManagerCode.Value);
                if (manager != null && (!isEditMode || customer.ManagerId != manager.ManagerId))
                {
                    customer.ManagerId = manager.ManagerId;
                    trackChanges = true;
                }
            }

            if (model.GroupId != null && CustomerGroupService.GetCustomerGroup(model.GroupId.Value + 10) != null)
            {
                if (!isEditMode || customer.CustomerGroupId != model.GroupId.Value + 10)
                {
                    customer.CustomerGroupId = model.GroupId.Value + 10;
                    trackChanges = true;
                }
            }

            //if (isEditMode && model.Password.IsNotEmpty() && customer.Password != SecurityHelper.GetPasswordHash(model.Password))
            //{
            //    CustomersService.ChangePassword(customer.Id, model.Password, false, true, changedBy);
            //}

            if (!isEditMode)
            {
                customer.Password = model.Password.DefaultOrEmpty();

                var id = CustomerService.InsertNewCustomer(customer, null, trackChanges, changedBy);

                if (id == Guid.Empty)
                {
                    return "Не удалось создать пользователя";
                }

                customer.Id = model.Id.TryParseGuid();
                ImportService.UpdateCustomerId(id, customer.Id, model.RegistrationDateTime);

                //if (model.PartnerId.HasValue && PartnerService.GetPartner(model.PartnerId.Value) != null)
                //    PartnerService.AddBindedCustomer(new BindedCustomer { CustomerId = customer.Id, PartnerId = model.PartnerId.Value });

                //var mail = new RegistrationMailTemplate(customer);
                //MailService.SendMailNow(SettingsMail.EmailForRegReport, mail, replyTo: customer.EMail);
            }
            else if (trackChanges)
            {
                CustomerService.UpdateCustomer(customer, trackChanges, changedBy);
                ExportService.ConfirmCustomer(customer.Id.ToString());
            }

            if (model.Fields != null)
            {
                foreach (var field in model.Fields)
                    CustomerFieldService.AddUpdateMap(customer.Id, field.Id, field.Value ?? "", true);//, trackChanges, changedBy);
            }

            if (model.Contact != null)
            {
                CustomersService.DeleteManyContacts(customer.Id);
                var contact = CustomersService.GetCustomerContact(customer.Id) ??
                              new CustomerContact() { CustomerGuid = customer.Id };

                contact.Name = !string.IsNullOrEmpty(contact.Name.EncodeOrEmpty())
                    ? contact.Name.EncodeOrEmpty()
                    : (customer.FirstName + " " + customer.LastName).Trim();
                contact.City = model.Contact.City.EncodeOrEmpty();
                contact.District = model.Contact.District.EncodeOrEmpty();
                contact.Zip = model.Contact.Zip.EncodeOrEmpty();

                contact.Country = model.Contact.Country.EncodeOrEmpty();
                var country = CountryService.GetCountryByName(contact.Country);
                contact.CountryId = country?.CountryId ?? 0;

                contact.Region = model.Contact.Region.EncodeOrEmpty();
                contact.RegionId = RegionService.GetRegionIdByName(contact.Region);

                //if (!isEditMode)
                //{
                    if (contact.Street.IsNullOrEmpty())
                        contact.Street = model.Contact.Street.EncodeOrEmpty();
                    //contact.House = model.Contact.House.EncodeOrEmpty();
                    //contact.Apartment = model.Contact.Apartment.EncodeOrEmpty();
                    //contact.Structure = model.Contact.Structure.EncodeOrEmpty();
                    //contact.Entrance = model.Contact.Entrance.EncodeOrEmpty();
                    //contact.Floor = model.Contact.Floor.EncodeOrEmpty();
                //}

                if (contact.ContactId == Guid.Empty)
                {
                    CustomerService.AddContact(contact, customer.Id);
                }
                else
                {
                    CustomerService.UpdateContact(contact);
                }
            }

            return null;
        }

    }
}