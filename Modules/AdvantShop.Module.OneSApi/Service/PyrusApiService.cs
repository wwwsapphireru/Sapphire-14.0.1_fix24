using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Customers;
using AdvantShop.Module.OneSApi.Models.Client;
using AdvantShop.Orders;
using Pyrus.ApiClient.Requests.Builders;
using PyrusApiClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;

namespace AdvantShop.Module.OneSApi.Service
{
    public class PyrusApiService
    {
        //private PyrusClient _pyrusClient;
        private const string _login = "advanta@sapphire.ru";//"info@glorysoft.ru";
        private const string _apiKey = "LBj52WHzQzhCVku4BQD7wsktEY-10WQDwZTb6cmpIEtGkTtWbiBn~Ett5npr~lBwKQTpmJvRd~Fhvdm1zvtsknbQaOl2QX2l";//"m-gbdygnNy3-91s6jAJXECU~go1uxcn9fGvHDBJaqGrlgLGVDnzbJBw9h5E0478cMw1uY-iu8bYdhp3E0xGwyuBEenBfZwnk";

        private static async Task<KeyValuePair<PyrusClient, string>> Auth()
        {
            var pyrusClient = new PyrusClient();
            var response = await pyrusClient.Auth(_login, _apiKey);
            if (response.Success)
                return new KeyValuePair<PyrusClient, string>(pyrusClient, null);
            else
                return new KeyValuePair<PyrusClient, string>(null, response.Error);
        }

        public static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskFeedback(int formId, FeedbackViewModel model)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var catalogResponse = await RequestBuilder.GetCatalog(253192).Process(pyrusClient);
            var items = catalogResponse.Items;

            Order order = null;
            if (model.OrderNumber.IsNotEmpty())
                order = OrderService.GetOrderByNumber(model.OrderNumber);
            Person person = null;
            if (order?.Manager != null)
            {
                var members = await RequestBuilder.GetMembers().Process(pyrusClient);
                person = members.Members.FirstOrDefault(x => x.Email == order.Manager.Email);
            }

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            //if (person != null)
            //    request = request.AddApproval(person).AddApproval(person, 2);
            var builder = request/*.AddApproval("")*/.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Раздел" || field.Info?.Code == "FeedbackType")
                    builder = builder.Add(SetFormFieldCatalog(field as FormFieldCatalog, (model.MessageType == FeedbackType.Abuse && order != null ? "Претензия по заказу" : model.MessageType.Localize()), items));
                else if (field.Name == "Вопрос" || field.Info?.Code == "Subject")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.MessageType.Localize()));
                else if (field.Name == "Ваше сообщение" || field.Info?.Code == "Description")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Message));
                //else if ((field.Info.Code == "Attachments" || field.Name == "Приложения"))
                //{
                //    //var f = (field as FormFieldFile).DeepCloneJson();
                //    //f.Value = new List<PyrusApiClient.File>();
                //    var att = new List<NewFile>();
                //    foreach (var img in model.Files)
                //    {
                //        img.InputStream.Position = 0;
                //        var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
                //        if (fileResponse.Guid.IsNotEmpty())
                //            att.Add(new NewFile(fileResponse.Guid));
                //        //fileResponse.Guid
                //        //var file = new PyrusApiClient.File
                //        //{
                //        //    Name = img.FileName,
                //        //    Size = img.ContentLength,
                //        //    MD5 = fileResponse.MD5,                         
                //        //};
                //        //using (var reader = new BinaryReader(img.InputStream))
                //        //{
                //        //    var bytes = reader.ReadBytes(img.ContentLength);
                //        //    var md5_1 = BitConverter.ToString(MD5.Create().ComputeHash(bytes))/*.Replace("-", "").ToLower()*/;
                //        //    //return Convert.ToBase64String(myMd5.ComputeHash(byteRepresentation));
                //        //    using (var md5 = new MD5CryptoServiceProvider())
                //        //    {
                //        //        var md5_2 = Convert.ToBase64String(md5.ComputeHash(bytes));
                //        //        //file.MD5 = md5.ComputeHash(s);
                //        //    }
                //        //}
                //        //f.Value.Add(file);
                //    }
                //    request.AddAttachments(att);
                //}
                else if (field.Name == "Ваше имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Name));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "Sender Address")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, model.Email));
                else if (field.Name == "Телефон" || field.Info?.Code == "u_PhoneNumber")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, model.Phone));
                else if (field.Name == "Номер заказа" || field.Info?.Code == "OrderNumber")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.OrderNumber));
                else if ((field.Name == "Ответственный" || field.Info?.Code == "Manager") && person != null)
                    builder = builder.Add(SetFormFieldPerson(field as FormFieldPerson, person));
                //else if ((field.Name == "Есть менеджер заказа" || field.Info?.Code == "HasManager") && person != null)
                //    builder = builder.Add(SetFormFieldCheckmark(field as FormFieldCheckmark, true));
                //if (field.GetType() == typeof(FormFieldMultipleChoice))
                //{
                //    var f = (field as FormFieldMultipleChoice).DeepCloneJson();
                //    f.Value = new MultipleChoice() { };
                //    builder = builder.Add(f);
                //}
                //f.Code
                //builder.Add()
            }
            if (model.Files != null && model.Files.Count > 0)
            {
                var att = new List<NewFile>();
                foreach (var img in model.Files)
                {
                    img.InputStream.Position = 0;
                    var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
                    if (fileResponse.Guid.IsNotEmpty())
                        att.Add(new NewFile(fileResponse.Guid));
                }
                request.AddAttachments(att);
            }
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            //if (person != null)
            //    request = request.AddApproval(new Approval() { Person = person, Step =1, ApprovalChoice = ApprovalChoice.Approved });
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

        public static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskFeedbackSales(int formId, FeedbackViewModel model)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var catalogResponse = await RequestBuilder.GetCatalog(253192).Process(pyrusClient);
            var items = catalogResponse.Items;
            Order order = null;
            if (model.OrderNumber.IsNotEmpty())
                order = OrderService.GetOrderByNumber(model.OrderNumber);
            Person person = null;
            if (order?.Manager != null)
            {
                var members = await RequestBuilder.GetMembers().Process(pyrusClient);
                person = members.Members.FirstOrDefault(x => x.Email == order.Manager.Email);
            }
            var customer = CustomerContext.CurrentCustomer;
            if (customer == null && order != null)
                customer = CustomerService.GetCustomer(order.OrderCustomer.CustomerID);

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            if (model.MessageType == FeedbackType.KP)
                FillBuilderFieldsFeedbackSales_KP(formResponse, builder, model);
            else if (model.MessageType == FeedbackType.Thanks)
                FillBuilderFieldsFeedbackSales_Thanks(formResponse, builder, model);
            else if (model.MessageType == FeedbackType.Question || model.MessageType == FeedbackType.Offer || model.MessageType == FeedbackType.Abuse)
                FillBuilderFieldsFeedbackSales_Feedback(formResponse, builder, model, person, customer);
            if (model.Files != null && model.Files.Count > 0)
            {
                var att = new List<NewFile>();
                foreach (var img in model.Files)
                {
                    img.InputStream.Position = 0;
                    var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
                    if (fileResponse.Guid.IsNotEmpty())
                        att.Add(new NewFile(fileResponse.Guid));
                }
                request.AddAttachments(att);
            }
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

        public static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskFeedbackThanks(int formId, FeedbackViewModel model)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var catalogResponse = await RequestBuilder.GetCatalog(253192).Process(pyrusClient);
            var items = catalogResponse.Items;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "ФИО контактного лица" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Name));
                else if (field.Name == "Эл. почта контактного лица" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, model.Email));
                else if (field.Name == "Телефон контактного лица" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, model.Phone));
                else if ((field.Name == "Тема сообщения" || field.Info?.Code == "Subject") && model.OrderNumber.IsNotEmpty())
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, $"Номер заказа покупки оборудования: {model.OrderNumber}"));
                else if (field.Name == "Сообщение" || field.Info?.Code == "Message")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Message));
                else if (field.Name == "Вид ремонта" || field.Info?.Code == "FeedbackType")
                    if (model.ThanksType == 0)
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 1, "Гарантийный ремонт"));
                    else
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 2, "Платный ремонт"));
                else if (field.Name == "Дата покупки" || field.Info?.Code == "OrderDate")
                    builder = builder.Add(SetFormFieldDate(field as FormFieldDate, model.OrderDate));
                else if (field.Name == "Оборудование принадлежит" || field.Info?.Code == "CustomerType")
                    if (model.CustomerType == (int)CustomerType.LegalEntity)
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 1, "Юридическому лицу или ИП"));
                    else
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 2, "Физическому лицу"));
                else if (field.Name == "ИНН организации или ИП" || field.Info?.Code == "DadataInn")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.INN));
            }
            if (model.Files != null && model.Files.Count > 0)
            {
                var att = new List<NewFile>();
                foreach (var img in model.Files)
                {
                    img.InputStream.Position = 0;
                    var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
                    if (fileResponse.Guid.IsNotEmpty())
                        att.Add(new NewFile(fileResponse.Guid));
                }
                request.AddAttachments(att);
            }
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

        private static void FillBuilderFieldsFeedbackSales_KP(FormResponse formResponse, PyrusApiClient.Builders.FormTaskBuilder.FormFieldsBuilder builder, FeedbackViewModel model)
        {
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Чего касается обращение клиента" || field.Info?.Code == "FeedbackType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 12, "Вопрос из интернет-магазина"/*1, "Запрос счета или КП"*/));
                else if (field.Name == "Проблема" || field.Info?.Code == "Subject")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.MessageType.Localize()));
                else if (field.Name == "Наименование организации" || field.Info?.Code == "CompanyName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyName));
                else if (field.Name == "Адрес (Наименование организации)" || field.Info?.Code == "Dadata Organization Address")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyAddress));
                else if (field.Name == "ИНН (Наименование организации)" || field.Info?.Code == "Dadata Inn")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.INN));
                else if (field.Name == "КПП (Наименование организации)" || field.Info?.Code == "Dadata Kpp")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, (model.CompanyKPP ?? "").Replace("null", "")));
                else if (field.Name == "Перечислите артикулы и количество товаров, которые вас интересуют" || field.Info?.Code == "Items")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Items));
                else if (field.Name == "Текст письма" || field.Info?.Code == "Message")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Message));
                else if (field.Name == "Имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Name));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, model.Email));
                else if (field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, model.Phone));
                //else if (field.Name == "Наименование клиента")
                //    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyName));
                //if (field.GetType() == typeof(FormFieldMultipleChoice))
                //{
                //    var f = (field as FormFieldMultipleChoice).DeepCloneJson();
                //    f.Value = new MultipleChoice() { };
                //    builder = builder.Add(f);
                //}
                //f.Code
                //builder.Add()
            }
        }

        private static void FillBuilderFieldsFeedbackSales_Thanks(FormResponse formResponse, PyrusApiClient.Builders.FormTaskBuilder.FormFieldsBuilder builder, FeedbackViewModel model)
        {
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Чего касается обращение клиента" || field.Info?.Code == "FeedbackType")
                    if (model.ThanksType == 0)
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 11, "Гарантийный ремонт оборудования"));
                    else
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 6, "Платный ремонт оборудования"));
                else if (field.Name == "Текст письма" || field.Info?.Code == "Message")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Message));
                else if (field.Name == "Имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Name));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, model.Email));
                else if (field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, model.Phone));
                else if (field.Name == "Дата покупки" || field.Info?.Code == "OrderDate")
                    builder = builder.Add(SetFormFieldDate(field as FormFieldDate, model.OrderDate));
                else if (field.Name == "Номер документа о покупке ( чек или УПД)" || field.Info?.Code == "OrderNumber")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.OrderNumber));
                else if (field.Name == "Наименование организации" || field.Info?.Code == "CompanyName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyName));
                else if (field.Name == "Адрес (Наименование организации)" || field.Info?.Code == "Dadata Organization Address")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyAddress));
                else if (field.Name == "ИНН (Наименование организации)" || field.Info?.Code == "Dadata Inn")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.INN));
                else if (field.Name == "КПП (Наименование организации)" || field.Info?.Code == "Dadata Kpp")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, (model.CompanyKPP ?? "").Replace("null", "")));
            }
        }

        private static void FillBuilderFieldsFeedbackSales_Feedback(FormResponse formResponse, PyrusApiClient.Builders.FormTaskBuilder.FormFieldsBuilder builder, FeedbackViewModel model, Person person, Customer customer)
        {
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Чего касается обращение клиента" || field.Info?.Code == "FeedbackType")
                {
                    if (model.MessageType == FeedbackType.Question || model.MessageType == FeedbackType.Offer || model.MessageType == FeedbackType.Abuse)
                        builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 12, "Вопрос из интернет-магазина"));
                }
                else if (field.Name == "Проблема" || field.Info?.Code == "Subject")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.MessageType.Localize()));
                else if (field.Name == "Текст письма" || field.Info?.Code == "Message")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Message));
                else if (field.Name == "Имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Name));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, model.Email));
                else if (field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, model.Phone));
                else if ((field.Name == "Дата покупки" || field.Info?.Code == "OrderDate") && model.OrderDate.HasValue)
                    builder = builder.Add(SetFormFieldDate(field as FormFieldDate, model.OrderDate));
                else if (field.Name == "Номер документа о покупке ( чек или УПД)" || field.Info?.Code == "OrderNumber")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.OrderNumber));
                else if (field.Name == "Наименование организации" || field.Info?.Code == "CompanyName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyName));
                else if (field.Name == "Адрес (Наименование организации)" || field.Info?.Code == "Dadata Organization Address")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyAddress));
                else if (field.Name == "ИНН (Наименование организации)" || field.Info?.Code == "Dadata Inn")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.INN));
                else if (field.Name == "КПП (Наименование организации)" || field.Info?.Code == "Dadata Kpp")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, (model.CompanyKPP ?? "").Replace("null", "")));
                else if ((field.Name == "Менеджер" || field.Info?.Code == "Manager") && person != null)
                    builder = builder.Add(SetFormFieldPerson(field as FormFieldPerson, person));
                else if ((field.Name == "Менеджер" || field.Info?.Code == "Manager") && person != null)
                    builder = builder.Add(SetFormFieldPerson(field as FormFieldPerson, person));
                else if ((field.Name == "Ссылка на страницу" || field.Info?.Code == "PageUrl") && customer != null)
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, UrlService.GetAdminUrl("customers/view/" + customer.Id, true)));
            }
        }

        public static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskChangeCommonInfo(int formId, string body, Dictionary<string, string> fields, string email)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Тип запроса" || field.Info?.Code == "RequestType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 1, "Изменение персональных данных"));
                else if (field.Name == "ТЕКУЩИЕ УЧЕТНЫЕ ДАННЫЕ" || field.Info?.Code == "Subject")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, body));
                else if ((field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail") && fields.Keys.Contains("EMail"))
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, fields["EMail"]));
                else if ((field.Name == "Фамилия" || field.Info?.Code == "LastName") && fields.Keys.Contains("LastName"))
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, fields["LastName"]));
                else if ((field.Name == "Имя" || field.Info?.Code == "SenderName") && fields.Keys.Contains("FirstName"))
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, fields["FirstName"]));
                else if ((field.Name == "Отчество" || field.Info?.Code == "Patronymic") && fields.Keys.Contains("Patronymic"))
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, fields["Patronymic"]));
                else if ((field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom") && fields.Keys.Contains("Phone"))
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, fields["Phone"]));
                else if ((field.Name == "Дата рождения" || field.Info?.Code == "BirthDay") && fields.Keys.Contains("BirthDay"))
                    builder = builder.Add(SetFormFieldDate(field as FormFieldDate, fields["BirthDay"], "yyyy-MM-dd"));
                else if (field.Name == "Эл. почта для ответа" || field.Info?.Code == "HiddenEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, fields.Keys.Contains("EMail") ? fields["EMail"] : email));
                else if ((field.Name == "Серия паспорта" || field.Info?.Code == "PassportSerie") && fields.Keys.Contains("Серия паспорта"))
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, fields["Серия паспорта"]));
                else if ((field.Name == "Номер паспорта" || field.Info?.Code == "PassportNumber") && fields.Keys.Contains("Номер паспорта"))
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, fields["Номер паспорта"]));
                else if ((field.Name == "Дата выдачи паспорта" || field.Info?.Code == "PassportDate") && fields.Keys.Contains("Дата выдачи паспорта"))
                    builder = builder.Add(SetFormFieldDate(field as FormFieldDate, fields["Дата выдачи паспорта"], "yyyy-MM-dd"));
                else if ((field.Name == "Кем выдан паспорт" || field.Info?.Code == "PassportKem") && fields.Keys.Contains("Кем выдан паспорт"))
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, fields["Кем выдан паспорт"]));
            }
            //if (model.Files != null && model.Files.Count > 0)
            //{
            //    var att = new List<NewFile>();
            //    foreach (var img in model.Files)
            //    {
            //        img.InputStream.Position = 0;
            //        var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
            //        if (fileResponse.Guid.IsNotEmpty())
            //            att.Add(new NewFile(fileResponse.Guid));
            //    }
            //    request.AddAttachments(att);
            //}
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

        public static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskDeleteAccount(int formId, Customer customer)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Тип запроса" || field.Info?.Code == "RequestType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 2, "Удаление личного кабинета"));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, customer.EMail));
                else if (field.Name == "Фамилия" || field.Info?.Code == "LastName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, customer.LastName));
                else if (field.Name == "Имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, customer.FirstName));
                else if (field.Name == "Отчество" || field.Info?.Code == "Patronymic")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, customer.Patronymic));
                else if (field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, customer.Phone));
                else if (field.Name == "Эл. почта для ответа" || field.Info?.Code == "HiddenEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, customer.EMail));
            }
            //if (model.Files != null && model.Files.Count > 0)
            //{
            //    var att = new List<NewFile>();
            //    foreach (var img in model.Files)
            //    {
            //        img.InputStream.Position = 0;
            //        var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
            //        if (fileResponse.Guid.IsNotEmpty())
            //            att.Add(new NewFile(fileResponse.Guid));
            //    }
            //    request.AddAttachments(att);
            //}
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

        public static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskSendRegCodeError(int formId, Customer customer)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Тип запроса" || field.Info?.Code == "RequestType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 4, "Вопросы по регистрации"));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, customer.EMail));
                else if (field.Name == "Фамилия" || field.Info?.Code == "LastName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, customer.LastName));
                else if (field.Name == "Имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, customer.FirstName));
                else if (field.Name == "Отчество" || field.Info?.Code == "Patronymic")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, customer.Patronymic));
                else if (field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, customer.Phone));
                else if (field.Name == "Эл. почта для ответа" || field.Info?.Code == "HiddenEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, customer.EMail));
                else if (field.Name == "Ссылка" || field.Info?.Code == "View")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, UrlService.GetUrl() + "adminv3/customers/view/" + customer.Id.ToString()));
            }
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

        private static FormFieldText SetFormFieldText(FormFieldText field, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = HttpUtility.HtmlDecode(value);
            return f;
        }

        private static FormFieldEmail SetFormFieldEmail(FormFieldEmail field, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        private static FormFieldPhone SetFormFieldPhone(FormFieldPhone field, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        private static FormFieldDate SetFormFieldDate(FormFieldDate field, DateTime? value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        private static FormFieldDate SetFormFieldDate(FormFieldDate field, string value, string format)
        {
            var f = field.DeepCloneJson();
            f.Value = DateTime.ParseExact(value, format, CultureInfo.InvariantCulture);
            return f;
        }

        private static FormFieldCatalog SetFormFieldCatalog(FormFieldCatalog field, string value, List<CatalogItem> items)
        {
            var f = field.DeepCloneJson();
            var item = items.FirstOrDefault(x => x.Values.Contains(value));
            if (item != null)
            {
                //var val = item.Values.FirstOrDefault(x => x == value);
                f.Value = new PyrusApiClient.Catalog() { ItemIds = new long[] { item.Id } };
            }
            return f;
        }

        private static FormFieldMultipleChoice SetFormFieldMultipleChoice(FormFieldMultipleChoice field, int id, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = new MultipleChoice { ChoiceIds = new int[] { id }, ChoiceNames = new string[] { value } };
            return f;
        }

        private static FormFieldPerson SetFormFieldPerson(FormFieldPerson field, Person value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        private static FormFieldCheckmark SetFormFieldCheckmark(FormFieldCheckmark field, bool value)
        {
            var f = field.DeepCloneJson();
            f.Value = value ? Checkmark.Checked : Checkmark.Unchecked;
            return f;
        }

        private static FormFieldNumber SetFormFieldNumber(FormFieldNumber field, decimal value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        //private async void GetForms()
        //{
        //    var formsResponse = await RequestBuilder.GetForms().Process(_pyrusClient);
        //    var forms = formsResponse.Forms;
        //}

        //private async void GetForm()
        //{
        //    var formResponse = await RequestBuilder.GetForm(_formId).Process(_pyrusClient);
        //    //var forms = formsResponse;
        //}

        private static async Task<KeyValuePair<TaskWithComments, string>> CreateFormTaskFeedbackKP(int formId, FeedbackViewModel model)
        {
            var authResponse = await Auth();
            if (authResponse.Key == null)
                return new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            var catalogResponse = await RequestBuilder.GetCatalog(253192).Process(pyrusClient);
            var items = catalogResponse.Items;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "Чего касается обращение клиента" || field.Info?.Code == "FeedbackType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 1, "Запрос счета или КП"));
                else if (field.Name == "Наименование организации" || field.Info?.Code == "CompanyName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyName));
                else if (field.Name == "Адрес (Наименование организации)" || field.Info?.Code == "Dadata Organization Address")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyAddress));
                else if (field.Name == "ИНН (Наименование организации)" || field.Info?.Code == "Dadata Inn")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.INN));
                else if (field.Name == "КПП (Наименование организации)" || field.Info?.Code == "Dadata Kpp")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, (model.CompanyKPP ?? "").Replace("null", "")));
                else if (field.Name == "Перечислите артикулы и количество товаров, которые вас интересуют" || field.Info?.Code == "Items")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Items));
                else if (field.Name == "Текст письма" || field.Info?.Code == "Message")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Message));
                else if (field.Name == "Имя" || field.Info?.Code == "SenderName")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.Name));
                else if (field.Name == "Эл. почта" || field.Info?.Code == "SenderEmail")
                    builder = builder.Add(SetFormFieldEmail(field as FormFieldEmail, model.Email));
                else if (field.Name == "Телефон" || field.Info?.Code == "PhoneNumberFrom")
                    builder = builder.Add(SetFormFieldPhone(field as FormFieldPhone, model.Phone));
                //else if (field.Name == "Наименование клиента")
                //    builder = builder.Add(SetFormFieldText(field as FormFieldText, model.CompanyName));
                //if (field.GetType() == typeof(FormFieldMultipleChoice))
                //{
                //    var f = (field as FormFieldMultipleChoice).DeepCloneJson();
                //    f.Value = new MultipleChoice() { };
                //    builder = builder.Add(f);
                //}
                //f.Code
                //builder.Add()
            }
            if (model.Files != null && model.Files.Count > 0)
            {
                var att = new List<NewFile>();
                foreach (var img in model.Files)
                {
                    img.InputStream.Position = 0;
                    var fileResponse = await RequestBuilder.UploadFile(img.InputStream, img.FileName).Process(pyrusClient);
                    if (fileResponse.Guid.IsNotEmpty())
                        att.Add(new NewFile(fileResponse.Guid));
                }
                request.AddAttachments(att);
            }
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
                return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            else
                return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
        }

    }
}
