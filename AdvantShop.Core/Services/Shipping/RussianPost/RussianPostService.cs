using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Orders;
using Pyrus.ApiClient.Requests.Builders;
using PyrusApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//GlorySoft_001
namespace AdvantShop.Shipping.RussianPost
{
    public class RussianPostService
    {
        //private PyrusClient _pyrusClient;
        private const string _pyrusLogin = "advanta@sapphire.ru";//"info@glorysoft.ru";
        private const string _pyrusApiKey = "LBj52WHzQzhCVku4BQD7wsktEY-10WQDwZTb6cmpIEtGkTtWbiBn~Ett5npr~lBwKQTpmJvRd~Fhvdm1zvtsknbQaOl2QX2l";//"m-gbdygnNy3-91s6jAJXECU~go1uxcn9fGvHDBJaqGrlgLGVDnzbJBw9h5E0478cMw1uY-iu8bYdhp3E0xGwyuBEenBfZwnk";

        private static async Task<KeyValuePair<PyrusClient, string>> Auth()
        {
            var pyrusClient = new PyrusClient();
            var response = await pyrusClient.Auth(_pyrusLogin, _pyrusApiKey);
            if (response.Success)
                return new KeyValuePair<PyrusClient, string>(pyrusClient, null);
            else
                return new KeyValuePair<PyrusClient, string>(null, response.Error);
        }

        public static async void /*Task<KeyValuePair<TaskWithComments, string>>*/ CreateFormTaskWrongTrack(int formId, string orderNumber, string trackNumber, int orderId)
        {
            if (OrderService.GetOrderAdditionalData(orderId, RussianPostTemplate.KeyNameWrongTrackFormTaskAdditionalData).IsNotEmpty())
                return;

            var authResponse = await Auth();
            if (authResponse.Key == null)
                return;// new KeyValuePair<TaskWithComments, string>(null, authResponse.Value);
            var pyrusClient = authResponse.Key;

            //var catalogResponse = await RequestBuilder.GetCatalog(253192).Process(pyrusClient);
            //var items = catalogResponse.Items;

            var formResponse = await RequestBuilder.GetForm(formId).Process(pyrusClient);
            var request = RequestBuilder.CreateFormTask(formId);
            var builder = request.Fields;
            foreach (var field in formResponse.Fields)
            {
                if (field.Name == "номер заказа" || field.Info?.Code == "OrderNumber")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, orderNumber));
                else if (field.Name == "метод доставки" || field.Info?.Code == "ShippingType")
                    builder = builder.Add(SetFormFieldMultipleChoice(field as FormFieldMultipleChoice, 1, "Почта России"));
                else if (field.Name == "ошибочный трек-номер" || field.Info?.Code == "WrongTrack")
                    builder = builder.Add(SetFormFieldText(field as FormFieldText, trackNumber));
            }
            //return new KeyValuePair<TaskWithComments, string>(null, "123");////
            var taskResponse = await builder.Process(pyrusClient);
            if (taskResponse.Task != null && taskResponse.Error.IsNullOrEmpty())
            {
                OrderService.AddUpdateOrderAdditionalData(
                    orderId,
                    RussianPostTemplate.KeyNameWrongTrackFormTaskAdditionalData,
                    taskResponse.Task.Id.ToString());
                //return new KeyValuePair<TaskWithComments, string>(taskResponse.Task, null);
            }
            else
            {
                MailService.SendMailNow(Guid.Empty, SettingsMail.EmailForProductDiscuss, "Некорректный трек-номер", $"Некорректный трек-номер по заказу {orderNumber} - <a href='https://www.pochta.ru/tracking?barcode={trackNumber}'>{trackNumber}</a>", true);
                //return new KeyValuePair<TaskWithComments, string>(null, taskResponse.Error);
            }
        }

        private static FormFieldText SetFormFieldText(FormFieldText field, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = value;
            return f;
        }

        private static FormFieldMultipleChoice SetFormFieldMultipleChoice(FormFieldMultipleChoice field, int id, string value)
        {
            var f = field.DeepCloneJson();
            f.Value = new MultipleChoice { ChoiceIds = new int[] { id }, ChoiceNames = new string[] { value } };
            return f;
        }

    }
}
