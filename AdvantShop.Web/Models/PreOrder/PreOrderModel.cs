using AdvantShop.Catalog;
using AdvantShop.CMS;
using System.Collections.Generic;

namespace AdvantShop.Models.PreOrder
{
    public class PreOrderModel
    {
        public Offer Offer { get; set; }

        public bool CanOrderByRequest { get; set; }

        public string OptionsRendered { get; set; }

        public int ProductId { get; set; }

        public int OfferId { get; set; }

        public float Amount { get; set; }

        public string OptionsHash { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Comment { get; set; }

        public string CaptchaCode { get; set; }

        public string CaptchaSource { get; set; }

        public bool Agreement { get; set; }

        public bool IsLanding { get; set; }

        public float ProdMinAmount { get; set; }

        //GlorySoft_009
        public bool IsSuccess { get; set; }
        public string SuccessText { get; set; }
    }
}
