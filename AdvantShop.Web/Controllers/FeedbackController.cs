using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Customers;
using AdvantShop.Helpers;
using AdvantShop.Mails;
using AdvantShop.ViewModel.Feedback;
using AdvantShop.Web.Infrastructure.Controllers;
using System.Web.Mvc;
using AdvantShop.Core.Services.Crm;
using AdvantShop.Core.Services.Orders;
using AdvantShop.Orders;
using AdvantShop.Saas;
using BotDetect.Web.Mvc;
using AdvantShop.Core.Services.Mails;
using AdvantShop.Repository;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Configuration;
using AdvantShop.Models.User;
using AdvantShop.Web.Infrastructure.Filters;
using System.Collections.Generic;
using System;
using AdvantShop.Extensions;
using System.Threading.Tasks;
using System.Linq;

namespace AdvantShop.Controllers
{
    public class FeedbackController : BaseClientController
    {
        [AccessByChannel(EProviderSetting.StoreActive)]
        public ActionResult Index(FeedbackType messageType = FeedbackType.Question,/*GlorySoft_029*/ string orderNumber = null)
        {
            var model = new FeedbackViewModel() { MessageType = messageType, Secret = "secret"};

            var customer = CustomerContext.CurrentCustomer;
            if (customer.RegistredUser)
            {
                model.Email = customer.EMail;
                model.Phone = customer.Phone;
                model.Name = customer.GetShortName();
                model.Agree = customer.IsAgreeForPromotionalNewsletter;

                //GlorySoft_029
                model.OrderNumber = orderNumber;
                if (customer.CustomerType == CustomerType.LegalEntity)
                {
                    var customerFields = CustomerFieldService.GetCustomerFieldsWithValue(customer.Id)
                    .Where(x => (x.ShowInRegistration || x.ShowInCheckout || x.Enabled) &&
                                (x.CustomerType == customer.CustomerType || x.CustomerType == CustomerType.All))
                    .Where(x => (x.ValueDateFormat ?? x.Value).IsNotEmpty()).ToList();
                    if (customerFields != null)
                    {
                        CustomerFieldWithValue inn = null, org = null, kpp = null;
                        inn = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.INN);
                        org = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.CompanyName);
                        kpp = customerFields.FirstOrDefault(x => x.FieldAssignment == CustomerFieldAssignment.KPP);
                        model.INN = inn != null ? inn.Value : "";
                        model.CompanyName = org != null ? org.Value : "";
                        model.CompanyKPP = kpp != null ? kpp.Value : "";
                    }
                }
            }

            SetNgController(NgControllers.NgControllersTypes.FeedbackCtrl);
            SetMetaInformation(T("Feedback.Index.FeedbackHeader"));

            return View(model);
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public async Task<JsonResult>/*GorySoft_014 JsonResult*/ FeedbackForm(FeedbackViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Message) ||
                string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                (!string.IsNullOrWhiteSpace(model.Email) && !ValidationHelper.IsValidEmail(model.Email)) ||
                string.IsNullOrWhiteSpace(model.Phone))
            {
                return Json(new LoginResult("error", T("Feedback.Index.WrongData")));
            }

            if (!string.IsNullOrEmpty(model.Secret))
            {
                return Json(new LoginResult("error", T("Feedback.Index.WrongData")));
            }

            if (SettingsMain.EnableCaptchaInFeedback &&/*GlorySoft_012*/ (CustomerContext.CurrentCustomer == null || CustomerContext.CurrentCustomer.CustomerRole == Role.Guest) &&
                !MvcCaptcha.Validate("CaptchaSource", model.CaptchaCode, model.CaptchaSource))
            {
                return Json(new LoginResult("error", T("Js.Captcha.Wrong")));
            }

            //if (SettingsCheckout.IsShowUserAgreementText && !model.Agree)GorySoft_033
            //{
            //    return Json(new LoginResult("error", T("User.Registration.ErrorAgreement")));
            //}

            if (model.MessageType == FeedbackType.KP)//GlorySoft_014
            {
                var pyrusResponse = await PyrusApiService.CreateFormTaskFeedbackSales(1535344/*1493585*/, model);
                if (pyrusResponse.Key != null)
                    return Json(new LoginResult("succes", null, "feedback/success?task=" + pyrusResponse.Key.Id.ToString()));
                else
                    return Json(new LoginResult("error", pyrusResponse.Value));
            }

            model.Name = StringHelper.HtmlEncode(model.Name);
            model.Email = StringHelper.HtmlEncode(model.Email);
            model.Phone = StringHelper.HtmlEncode(model.Phone);
            model.Message = StringHelper.HtmlEncode(model.Message);
            model.OrderNumber = StringHelper.HtmlEncode(model.OrderNumber);

            var allowMessage = ModulesExecuter.CheckInfo(System.Web.HttpContext.Current, Core.Modules.Interfaces.ECheckType.Feedback, model.Email, model.Name, message: model.Message);
            if (!allowMessage)
            {
                return Json(new LoginResult("error", T("Common.SpamCheckFailed")));
            }

            var crmEnabled = !SaasDataService.IsSaasEnabled ||
                             (SaasDataService.IsSaasEnabled && SaasDataService.CurrentSaasData.HaveCrm);
            if (SettingsFeedback.FeedbackAction == EnFeedbackAction.SendEmail
                || (SettingsFeedback.FeedbackAction == EnFeedbackAction.CreateLead && !crmEnabled))
            {
                if (model.Files != null)//GlorySoft_033
                {
                    var files = model.Files.Where(x => FileHelpers.CheckFileExtensionByType(x.FileName, EFileType.Image)).ToList();
                    if (files.Count > 0)
                        model.Message += "<br /><stong>Ссылки на прикрепленные изображения:</stong>";
                    foreach (var fPhoto in files)
                    {
                        var fe = fPhoto.FileName.Split('.');
                        using (var image = System.Drawing.Image.FromStream(fPhoto.InputStream))
                        {
                            var isRotated = FileHelpers.RotateImageIfNeed(image);
                            var filename = string.Format("feedback_attach_{0}.{1}", DateTime.Now.ToString("yyyyMMddHHmmssfff"), fe.Length > 1 ? fe[fe.Length - 1] : "jpg");
                            FileHelpers.SaveResizePhotoFile(SettingsGeneral.AbsolutePath + "content/upload_images/feedback_attach/" + filename,
                                image.Width, image.Height, image, isRotated: isRotated);
                            //mail.Body += string.Format("div><img src='{3}/{0}' alt='{0}' width='{1}' height='{2}'></div>",
                            //    "/content/attachments/feedback/" + filename, image.Width, image.Height, SettingsMain.SiteUrl.TrimEnd('/'));
                            model.Message += string.Format("<br /><a href='{2}/{0}/{1}' target='_blank'>{2}/{0}/{1}</a>",
                                "content/upload_images/feedback_attach", filename, SettingsMain.SiteUrl.TrimEnd('/'));
                        }
                    }
                }

                //var mail = new FeedbackMailTemplate(SettingsMain.SiteUrl, SettingsMain.ShopName,
                //    model.Name, model.Email, model.Phone,
                //    T("Feedback.Index.FeedbackForm") + ": " + model.MessageType.Localize(),
                //    model.Message, model.OrderNumber);GlorySoft_033

                //MailService.SendMailNow(CustomerContext.CustomerId, SettingsMail.EmailForFeedback, mail,
                //    replyTo: model.Email);GlorySoft_033

                //GlorySoft_033
                KeyValuePair<PyrusApiClient.TaskWithComments, string> pyrusResponse;
                if (model.MessageType == FeedbackType.Thanks)
                {
                    //pyrusResponse = await PyrusApiService.CreateFormTaskFeedbackSales(1535344, model);
                    pyrusResponse = await PyrusApiService.CreateFormTaskFeedbackThanks(2329434, model);
                }
                else if (model.MessageType == FeedbackType.Question || model.MessageType == FeedbackType.Offer || model.MessageType == FeedbackType.Abuse)
                {
                    if (model.OrderNumber.IsNotEmpty())
                    {
                        var order = OrderService.GetOrderByNumber(model.OrderNumber);
                        if (order != null)
                            model.OrderDate = order.OrderDate;
                    }
                    pyrusResponse = await PyrusApiService.CreateFormTaskFeedbackSales(1535344, model);
                }
                else
                    pyrusResponse = await PyrusApiService.CreateFormTaskFeedback(1514828, model);
                if (pyrusResponse.Key == null)
                    return Json(new LoginResult("error", pyrusResponse.Value));
                else
                    return Json(new LoginResult("succes", null, "feedback/success?task=" + pyrusResponse.Key.Id.ToString()));
            }
            else if (SettingsFeedback.FeedbackAction == EnFeedbackAction.CreateLead && crmEnabled)
            {
                CreateLead(model);
            }

            return Json(new LoginResult("succes"));
        }

        private void CreateLead(FeedbackViewModel model)
        {
            var message =
                string.Format("{0} со страницы \"{1}\"{2}: \n{3}",
                model.MessageType.Localize(),
                T("Feedback.Index.FeedbackHeader"),
                !string.IsNullOrWhiteSpace(model.OrderNumber) ? " к заказу " + model.OrderNumber : "",
                model.Message.Replace("\n", "<br>"));

            var source = OrderSourceService.GetOrderSource(OrderType.Feedback);

            var lead = new Lead()
            {
                Email = model.Email,
                FirstName = model.Name,
                Phone = model.Phone,
                CustomerId = CustomerContext.CurrentCustomer.RegistredUser ? CustomerContext.CustomerId : default,

                Customer = new Customer(CustomerGroupService.DefaultCustomerGroup)
                {
                    FirstName = model.Name,
                    EMail = model.Email,
                    Phone = model.Phone,
                    StandardPhone = model.Phone != null ? StringHelper.ConvertToStandardPhone(model.Phone) : default,
                    CustomerRole = Role.User
                },

                Comment = message,
                OrderSourceId = source?.Id ?? 0,
                
                Country = IpZoneContext.CurrentZone.CountryName,
                Region = IpZoneContext.CurrentZone.Region,
                District = IpZoneContext.CurrentZone.District,
                City = IpZoneContext.CurrentZone.City,
                Zip = IpZoneContext.CurrentZone.Zip
            };

            LeadService.AddLead(lead);

            Track.TrackService.TrackEvent(Track.ETrackEvent.Core_Leads_LeadCreated_Desktop);
        }

        public ActionResult Success(int task)//GlorySoft_014
        {
            var model = new FeedbackSuccessModel { TaskId = task };

            SetMetaInformation(T("Feedback.Success.Success"));

            return View(model);
        }

        [HttpPost]
        public JsonResult FillItemsFromBasket()//GlorySoft_014
        {
            var result = "";

            try
            {
                var shpCart = ShoppingCartService.CurrentShoppingCart;
                var list = new List<string>();
                foreach (var item in shpCart)
                    list.Add(string.Format("{0} - {1}", item.ArtNo, item.Amount));
                result = list.AggregateString('\n');
            }
            catch (Exception e)
            {
                return JsonError(e.Message);
            }

            return JsonOk(result);
        }

    }
}