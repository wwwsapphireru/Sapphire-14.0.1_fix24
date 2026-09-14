using System;
using System.Collections.Generic;
using System.Linq;
//GlorySoft_001
namespace AdvantShop.Payment
{

    public class PayOnlineFiscalRequestInput
    {
        public string operation { get; set; }
        public string transactionId { get; set; }
        public string paymentSystemType { get; set; }
        public decimal/*float*/ totalAmount { get; set; }
        public List<PayOnlineFiscalRequestInput_good> goods { get; set; }
        public string email { get; set; }
        public string clientPhone { get; set; }
    }
    public class PayOnlineFiscalRequestInput_good
    {
        public string description { get; set; }
        public decimal/*double*/ quantity { get; set; }
        public decimal/*float*/ amount { get; set; }
        public string tax { get; set; }
        public int paymentMethodType { get; set; }
        public int paymentSubjectType { get; set; }
    }

    public class PayOnlineFiscalRequestOutput
    {
        public PayOnlineFiscalRequestIOutput_payload payload { get; set; }
    }
    public class PayOnlineFiscalRequestIOutput_payload
    {
        public string inn { get; set; }
        public string name_document { get; set; }
    }

}
