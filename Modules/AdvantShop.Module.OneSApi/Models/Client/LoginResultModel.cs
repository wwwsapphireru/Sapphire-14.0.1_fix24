using Newtonsoft.Json;

namespace AdvantShop.Module.OneSApi.Models.Client
{
    public class LoginResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("requestCaptcha")]
        public bool RequestCaptcha { get; set; }

        [JsonProperty("redirectTo")]
        public string RedirectTo { get; set; }

        public LoginResult(string status) : this(status, "")
        {
        }

        public LoginResult(string status, string error) : this(status, error, false, null)
        {
        }

        public LoginResult(string status, string error, bool requestCaptcha) : this(status, error, requestCaptcha, null)
        {
        }

        public LoginResult(string status, string error, string redirectTo) : this(status, error, false, redirectTo)
        {
        }

        public LoginResult(string status, string error, bool requestCaptcha, string redirectTo)
        {
            Status = status;
            Error = error;
            RequestCaptcha = requestCaptcha;
            RedirectTo = redirectTo;
        }
    }
}