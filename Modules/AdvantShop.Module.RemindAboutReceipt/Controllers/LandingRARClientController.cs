using System;
using System.Web.Mvc;
using AdvantShop.Catalog;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using AdvantShop.Module.RemindAboutReceipt.Models;
using System.Web;
using AdvantShop.Configuration;
using AdvantShop.Web.Infrastructure.Extensions;
using AdvantShop.Customers;

namespace AdvantShop.Module.RemindAboutReceipt.Controllers
{
    [Module(Type = "RemindAboutReceipt")]
    public class LandingRARClientController : ModuleController
    {
        string ModuleID = RemindAboutReceipt.ModuleStringId;

        #region receipt
        public ActionResult ShowNotificationForm(Product product, Offer offer)
        {
            if (!Service.ModuleSettings.RarActive)
                return new EmptyResult();

            var isNotProductPage = HttpContext.Request.RawUrl.ToLower().Contains("productquickview");
            return PartialView("~/modules/" + ModuleID + "/Views/Client/_ShowNotificationForm" + (isNotProductPage ? "QV" : string.Empty) + ".cshtml", offer);
        }
        
        public ActionResult ShowNotificationFormProductView(Product product)
        {
            if (!Service.ModuleSettings.RarActive)
                return new EmptyResult();

            return PartialView("~/modules/" + ModuleID + "/Views/Client/_ShowNotificationFormProductView.cshtml", product);
        }

        public JsonResult GetNotificationForm()
        {
            var customer = CustomerContext.CurrentCustomer;
            var form = new NotificationModel
            {
                FormHeader = Service.ModuleSettings.FormHeader,
                AfterFormTextForUser = Service.ModuleSettings.AfterFormTextForUser,
                ShowCommentInForm = Service.ModuleSettings.ShowCommentInForm,
                ShowEmailInForm = Service.ModuleSettings.ShowEmailInForm,
                ShowNameInForm = Service.ModuleSettings.ShowNameInForm,
                ShowSurnameInForm = Service.ModuleSettings.ShowSurnameInForm,
                ShowPhoneNumberInForm = Service.ModuleSettings.ShowPhoneNumberInForm,
                Email = customer.EMail ?? "",
                FirstName = customer.FirstName ?? "",
                LastName = customer.LastName ?? "",
                Phone = customer.Phone ?? "",
                IsShowUserAgreementText = SettingsCheckout.IsShowUserAgreementText,
                UserAgreementText = SettingsCheckout.UserAgreementText,
                FormRequest = new NotificationModelRequest()//GlorySoft_012
            };
            if (Customers.CustomerContext.CurrentCustomer?.RegistredUser == true)//GlorySoft_012
            {
                form.FormRequest.Email = Customers.CustomerContext.CurrentCustomer.EMail;
                form.FormRequest.Name = Customers.CustomerContext.CurrentCustomer.FirstName;
                form.FormRequest.Surname = Customers.CustomerContext.CurrentCustomer.LastName;
                form.FormRequest.PhoneNumber = Customers.CustomerContext.CurrentCustomer.Phone;
            }
            return Json(new { Form = form });
        }

        [HttpPost]
        public JsonResult SendInfo(FormRequestModel formRequest)
        {
            try
            {
                if (SettingsCheckout.IsShowUserAgreementText && !formRequest.Agreement)
                    return Json(false);

                if(string.IsNullOrEmpty(formRequest.Email))
                    return Json(false);

                if (formRequest.ProductId == 0)//GlorySoft_012
                    formRequest.ProductId = OfferService.GetOffer(formRequest.ProductOfferId)?.ProductId ?? 0;

                var client = Service.RarService.GetRarClient(formRequest.Email, formRequest.ProductId, formRequest.ProductOfferId);

                var leadId = 0;
                if (client == null)
                {
                    client = new RarClient
                    {
                        Email = formRequest.Email,
                        ProductId = formRequest.ProductId,
                        ProductOfferId = formRequest.ProductOfferId,
                        SendNotification = false
                    };

                    if (Service.ModuleSettings.CreateLead)
                    {
                        leadId = Service.ModuleService.CreateLead(formRequest, true);
                        client.LeadId = leadId;
                    }

                    client.Id = Service.RarService.AddRarClient(client);
                }
                else
                {
                    if (client.LeadId.HasValue)
                        leadId = client.LeadId.Value;

                    var isLeadExists = leadId > 0 && Service.ModuleService.IsLeadExists(leadId);
                    if (Service.ModuleSettings.CreateLead && !isLeadExists)
                    {
                        leadId = Service.ModuleService.CreateLead(formRequest, true);
                        client.LeadId = leadId;
                    }

                    client.SendNotification = false;
                    Service.RarService.UpdateRarClient(client);
                }
                
                return Json(true);
            }
            catch (Exception ex)
            {
                Diagnostics.Debug.Log.Error(ex);
                return Json(false);
            }
        }

        public ActionResult ProductView(int offerId)//GlorySoft_012
        {
            if (!Service.ModuleSettings.RarActive)
                return new EmptyResult();
            var offer = OfferService.GetOffer(offerId);
            if (offer == null)
                return new EmptyResult();

            return PartialView("~/modules/" + ModuleID + "/Views/Client/_ProductView.cshtml", offer);
        }

        #endregion

        #region discount

        public ActionResult RadShowNotificationForm(Product product, Offer offer)
        {
            if (!Service.ModuleSettings.RadActive)
                return new EmptyResult();

            var isNotProductPage = HttpContext.Request.RawUrl.ToLower().Contains("productquickview");
            return PartialView("~/modules/" + ModuleID + "/Views/Client/_RadShowNotificationForm" + (isNotProductPage ? "QV" : string.Empty) + ".cshtml", offer);
        }

        public JsonResult GetRadNotificationForm()
        {
            var customer = CustomerContext.CurrentCustomer;
            var form = new NotificationModel
            {
                ImagePath = Service.ModuleSettings.RadImagePath,
                FormHeader = Service.ModuleSettings.RadFormHeader,
                TextForUser = Service.ModuleSettings.RadTextForUser,
                AfterFormTextForUser = Service.ModuleSettings.RadAfterFormTextForUser,
                ShowCommentInForm = Service.ModuleSettings.RadShowCommentInForm,
                ShowEmailInForm = Service.ModuleSettings.RadShowEmailInForm,
                ShowNameInForm = Service.ModuleSettings.RadShowNameInForm,
                ShowSurnameInForm = Service.ModuleSettings.RadShowSurnameInForm,
                ShowPhoneNumberInForm = Service.ModuleSettings.RadShowPhoneNumberInForm,
                Email = customer.EMail ?? "",
                FirstName = customer.FirstName ?? "",
                LastName = customer.LastName ?? "",
                Phone = customer.Phone ?? "",
                IsShowUserAgreementText = SettingsCheckout.IsShowUserAgreementText,
                UserAgreementText = SettingsCheckout.UserAgreementText,
            };
            return Json(new { Form = form });
        }

        [HttpPost]
        public JsonResult RadSendInfo(FormRequestModel formRequest)
        {
            try
            {
                if (SettingsCheckout.IsShowUserAgreementText && !formRequest.Agreement)
                    return Json(false);

                var offer = OfferService.GetOffer(formRequest.ProductOfferId);
                if (offer == null)
                    return Json(false);

                var client = Service.RadService.GetRadClient(formRequest.Email, formRequest.ProductId, formRequest.ProductOfferId);
                var leadId = 0;

                if (client == null)
                {
                    client = new RadClient
                    {
                        Email = formRequest.Email,
                        ProductId = formRequest.ProductId,
                        ProductOfferId = formRequest.ProductOfferId,
                        OldPrice = Service.ModuleService.RadGetCurrentPriceForOffer(offer),
                        SendNotification = false
                    };

                    if (Service.ModuleSettings.RadCreateLead)
                    {
                        leadId = Service.ModuleService.CreateLead(formRequest, false);
                        client.LeadId = leadId;
                    }

                    client.Id = Service.RadService.AddRadClient(client);
                }
                else
                {
                    if (client.LeadId.HasValue)
                        leadId = client.LeadId.Value;

                    var isLeadExists = leadId > 0 && Service.ModuleService.IsLeadExists(leadId);
                    if (Service.ModuleSettings.RadCreateLead && !isLeadExists)
                    {
                        leadId = Service.ModuleService.CreateLead(formRequest, false);
                        client.LeadId = leadId;
                    }

                    client.OldPrice = Service.ModuleService.RadGetCurrentPriceForOffer(offer);
                    client.SendNotification = false;
                    Service.RadService.UpdateRadClient(client);
                }

                return Json(true);
            }
            catch (Exception ex)
            {
                Diagnostics.Debug.Log.Error(ex);
                return Json(false);
            }
        }

        #endregion

        public IHtmlString LoadStyles()
        {
            return new HtmlString("<link rel=\"stylesheet\" href=\"modules/remindaboutreceipt/content/styles/client-style.css?" + ModulesExtensions.GetModuleVersion(null, ModuleID) + "\" />");
        }
    }
}
