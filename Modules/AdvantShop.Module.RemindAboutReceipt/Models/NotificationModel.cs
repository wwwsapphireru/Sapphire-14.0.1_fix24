namespace AdvantShop.Module.RemindAboutReceipt.Models
{
    public class NotificationModel
    {
        public bool ShowCommentInForm { get; set; }

        public bool ShowEmailInForm { get; set; }

        public bool ShowNameInForm { get; set; }

        public bool ShowSurnameInForm { get; set; }

        public bool ShowPhoneNumberInForm { get; set; }

        public string FormHeader { get; set; }

        public string AfterFormTextForUser { get; set; }

        public string TextForUser { get; set; }

        public string ImagePath { get; set; }

        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public bool IsShowUserAgreementText { get; set; }
        public string UserAgreementText { get; set; }

        public NotificationModelRequest FormRequest { get; set; }//GlorySoft_012
    }

    //GlorySoft_012
    public class NotificationModelRequest
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
    }

}
