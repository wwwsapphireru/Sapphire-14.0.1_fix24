using AdvantShop.Core.Common.Attributes;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdvantShop.ViewModel.Feedback
{
    public enum FeedbackType
    {
        [Localize("Feedback.FeedbackType.Question")]
        Question,
        [Localize("Feedback.FeedbackType.Thanks")]
        Thanks,
        [Localize("Feedback.FeedbackType.Offer")]
        Offer,
        [Localize("Feedback.FeedbackType.Abuse")]
        Abuse,
        [Localize("Feedback.FeedbackType.KP")]
        KP//GlorySoft_014
    }

    public class FeedbackViewModel
    {
        public FeedbackViewModel()//GlorySoft_014
        {
            Suggestions = AttachedModules.GetModules<ISuggestions>().Select(x => (ISuggestions)Activator.CreateInstance(x)).FirstOrDefault();
        }

        public string Message { get; set; }

        public string OrderNumber { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public FeedbackType MessageType { get; set; }

        public string CaptchaCode { get; set; }

        public string CaptchaSource { get; set; }

        public bool Agree { get; set; }

        public string Secret { get; set; }

        //GlorySoft_014
        public List<HttpPostedFileBase> Files { get; set; }
        public string Items { get; set; }
        public string INN { get; set; }
        public string CompanyName { get; set; }
        public ISuggestions Suggestions { get; private set; }
        public string CompanyAddress { get; set; }
        public string CompanyKPP { get; set; }
        public int CustomerType { get; set; }
        public int ThanksType { get; set; }
        public DateTime? OrderDate { get; set; }

    }

    public class FeedbackSuccessModel//GlorySoft_014
    {
        public int TaskId { get; set; }
        public bool IsEmptyLayout { get; set; }
    }

}