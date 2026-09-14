using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdvantShop.Module.OneSApi.Extensions
{
    public static class HttpContextHelper
    {
        public static string TryGetIp(this HttpContext context)
        {
            try
            {
                if (context == null)
                    return null;

                var ip = context.Request.Headers["X-1Gb-Client-IP"]
                         ?? context.Request.Headers["X-Real-IP"]
                         ?? context.Request.Headers["X-Forwarded-For"]
                         ?? context.Request.UserHostAddress;

                return ip;
            }
            catch
            {
                // ignored
            }
            return null;
        }
    }
}
