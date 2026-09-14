//--------------------------------------------------
// Project: AdvantShop.NET
// Web site: http:\\www.advantshop.net
//--------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using AdvantShop.Configuration;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.UrlRewriter;
using AdvantShop.Diagnostics;
using AdvantShop.Saas;

namespace AdvantShop.Helpers
{
    public class CommonHelper
    {
        public static void DisableBrowserCache()
        {
            if (HttpContext.Current == null) return;
            HttpContext.Current.Response.Cache.SetExpires(new DateTime(1995, 5, 6, 12, 0, 0, DateTimeKind.Utc));
            HttpContext.Current.Response.Cache.SetNoStore();
            HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            HttpContext.Current.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            HttpContext.Current.Response.Cache.AppendCacheExtension("post-check=0,pre-check=0");
        }

        /// <summary>
        /// set to responce any file
        /// </summary>
        /// <param name="filePath">virtual parh</param>
        /// <param name="fileName">file name</param>
        public static void WriteResponseFile(string filePath, string fileName)
        {
            if (String.IsNullOrEmpty(filePath)) return;
            var file = new FileInfo(filePath);
            var response = HttpContext.Current.Response;
            response.Clear();
            response.Charset = "utf-8";
            response.ContentType = "application/octet-stream";
            response.AddHeader("Connection", "Keep-Alive");
            response.AddHeader("Content-Length", file.Length.ToString());
            response.AddHeader("content-disposition", string.Format("attachment; filename={0}", file.Name));
            response.WriteFile(filePath);
            response.Flush();
            response.End();
        }

        /// <summary>
        /// Write XML to response
        /// </summary>
        /// <param name="xml">XML</param>
        /// <param name="fileName">Filename</param>
        public static void WriteResponseXml(string xml, string fileName)
        {
            if (String.IsNullOrEmpty(xml)) return;

            var document = new XmlDocument();
            document.LoadXml(xml);
            var decl = document.FirstChild as XmlDeclaration;
            if (decl != null)
            {
                decl.Encoding = "utf-8";
            }
            var response = HttpContext.Current.Response;
            response.Clear();
            response.Charset = "utf-8";
            response.ContentType = "text/xml";
            response.AddHeader("content-disposition", string.Format("attachment; filename={0}", fileName));
            response.BinaryWrite(Encoding.UTF8.GetBytes(document.InnerXml));
            response.Flush();
            //response.End();
        }

        /// <summary>
        /// Write text to response
        /// </summary>
        /// <param name="txt">text</param>
        /// <param name="fileName">Filename</param>
        public static void WriteResponseTxt(string txt, string fileName)
        {
            if (String.IsNullOrEmpty(txt)) return;
            if (HttpContext.Current == null) return;
            var response = HttpContext.Current.Response;
            response.Clear();
            response.Charset = "utf-8";
            response.ContentType = "text/plain";
            response.AddHeader("content-disposition", string.Format("attachment; filename={0}", fileName));
            response.BinaryWrite(Encoding.UTF8.GetBytes(txt));
            response.Flush();
            //response.End();
        }

        /// <summary>
        /// Write XLS file to response
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="targetFileName">Target file name</param>
        public static void WriteResponseXlsx(string filePath, string targetFileName)
        {
            if (String.IsNullOrEmpty(filePath)) return;
            if (HttpContext.Current == null) return;
            var response = HttpContext.Current.Response;
            response.Clear();
            response.Charset = "utf-8";
            response.ContentType = "text/xlsx";
            response.AddHeader("content-disposition", string.Format("attachment; filename={0}", targetFileName));
            response.BinaryWrite(File.ReadAllBytes(filePath));
            response.Flush();
            //response.End();
        }

        /// <summary>
        /// Write PDF file to response
        /// </summary>
        /// <param name="filePath">File napathme</param>
        /// <param name="targetFileName">Target file name</param>
        /// <remarks>For BeatyStore project</remarks>
        public static void WriteResponsePdf(string filePath, string targetFileName)
        {
            if (String.IsNullOrEmpty(filePath)) return;
            if (HttpContext.Current == null) return;
            var response = HttpContext.Current.Response;
            response.Clear();
            response.Charset = "utf-8";
            response.ContentType = "text/pdf";
            response.AddHeader("content-disposition", string.Format("attachment; filename={0}", targetFileName));
            response.BinaryWrite(File.ReadAllBytes(filePath));
            response.Flush();
            //response.End();
        }

        public static HttpCookie GetCookie(string cookieName)
        {
            return HttpContext.Current == null ? null : HttpContext.Current.Request.Cookies[cookieName];
        }

        public static void SetCookie(string cookieName, string cookieValue, bool httpOnly = false, bool crossSubDomains = true, bool isSession = false, bool? setOnMainDomain = false)
        {
            SetCookie(cookieName, cookieValue, new TimeSpan(7, 0, 0, 0), httpOnly, crossSubDomains, isSession, setOnMainDomain);
        }

        public static void SetCookie(string cookieName, 
                                     string cookieValue, 
                                     TimeSpan ts, 
                                     bool httpOnly, 
                                     bool crossSubDomains = true, 
                                     bool isSession = false,
                                     bool? setOnMainDomain = false)
        {
            try
            {
                
                var cookie = new HttpCookie(cookieName)
                {
                    Value = HttpUtility.UrlEncode(cookieValue),
                    Expires = !isSession ? DateTime.Now.Add(ts) : DateTime.MinValue,
                    HttpOnly = httpOnly,
                };
                if (HttpContext.Current.Request.IsSecureConnection)
                {
                    cookie.SameSite = SameSiteMode.None;
                    cookie.Secure = true;
                }

                var domainSet = false;
                if (SettingsMain.IsTechDomainsReady 
                    && HttpContext.Current.Request.IsAdvantshopAdminProxy()
                    && !string.IsNullOrWhiteSpace(HttpContext.Current.Request.Headers["X-Forwarded-Host"]))
                {
                    var forwardedHostHeader = HttpContext.Current.Request.Headers["X-Forwarded-Host"].TrimEnd('/');
                    var forwardedHostPathHeader =
                        HttpContext.Current.Request.Headers["X-Forwarded-Host-Path"].TrimStart('/');

                    if (!string.IsNullOrWhiteSpace(forwardedHostHeader))
                    {
                        cookie.Domain = "." + forwardedHostHeader;
                        domainSet = true;
                    }

                    if (!string.IsNullOrWhiteSpace(forwardedHostPathHeader))
                        cookie.Path = "/" + forwardedHostPathHeader;

                    if (UrlService.IsSecureConnection(HttpContext.Current.Request))
                    {
                        cookie.SameSite = SameSiteMode.None;
                        cookie.Secure = true;
                    }
                }

                if (!domainSet && !IsLocalUrl() && crossSubDomains)
                {
                    cookie.Domain = "." +
                                    (setOnMainDomain != null && setOnMainDomain.Value && SettingsMain.SetCookieOnMainDomain
                                        ? SettingsMain.SiteUrlPlain
                                        : GetParentDomain());
                }

                if (!domainSet && HttpContext.Current.Request.ApplicationPath != null)
                {
                    cookie.Path =  "/" + HttpContext.Current.Request.ApplicationPath.TrimStart('/');
                }

                if (HttpContext.Current.Response.Cookies[cookieName] != null)
                {
                    HttpContext.Current.Response.Cookies.Remove(cookieName);
                }

                HttpContext.Current.Response.Cookies.Add(cookie);
                
                Core.Common.DataModificationFlag.ResetLastModified();
            }
            catch (Exception exc)
            {
                Debug.Log.Error(exc);
            }
        }

        /// <summary>
        /// Gets cookie string
        /// </summary>
        /// <param name="cookieName">Cookie name</param>
        /// <returns>Cookie string</returns>
        public static string GetCookieString(string cookieName)
        {
            var cookie = HttpContext.Current.Request.Cookies[cookieName];
            return cookie != null ? cookie.Value : string.Empty;
        }

        public static void DeleteCookie(string cookieName, bool crossSubDomains = true)
        {
            HttpCookie myCookie;

            try
            {
                if (HttpContext.Current?.Request?.Cookies?[cookieName] == null)
                    return;
                
                myCookie = new HttpCookie(cookieName) { Expires = DateTime.Now.AddDays(-1D) };
            }
            catch (HttpException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.Log.Error($"DeleteCookie('{cookieName}') failed", exception);
                return;
            }

            var domainSet = false;
            if (SettingsMain.IsTechDomainsReady
                && HttpContext.Current.Request.IsAdvantshopAdminProxy()
                && !string.IsNullOrWhiteSpace(HttpContext.Current.Request.Headers["X-Forwarded-Host"]))
            {
                var forwardedHostHeader = HttpContext.Current.Request.Headers["X-Forwarded-Host"].TrimEnd('/');
                var forwardedHostPathHeader =
                    HttpContext.Current.Request.Headers["X-Forwarded-Host-Path"].TrimStart('/');

                if (!string.IsNullOrWhiteSpace(forwardedHostHeader))
                {
                    myCookie.Domain = "." + forwardedHostHeader;
                    domainSet = true;
                }

                if (!string.IsNullOrWhiteSpace(forwardedHostPathHeader))
                    myCookie.Path = "/" + forwardedHostPathHeader;
            }

            if (UrlService.IsSecureConnection(HttpContext.Current.Request))
            {
                myCookie.SameSite = SameSiteMode.None;
                myCookie.Secure = true;
            }

            if (!domainSet && !IsLocalUrl() && crossSubDomains)
            {
                myCookie.Domain = "." + GetParentDomain();
            }

            if (!domainSet && HttpContext.Current.Request.ApplicationPath != null)
            {
                myCookie.Path =  "/" + HttpContext.Current.Request.ApplicationPath.TrimStart('/');
            }
                
            HttpContext.Current.Response.Cookies.Add(myCookie);

            Core.Common.DataModificationFlag.ResetLastModified();
        }

        public static string GetParentDomain()
        {
            return
                GetParentDomain(HttpContext.Current != null
                    ? HttpContext.Current.Request.Url.Host.ToLower()
                    : SettingsGeneral.AbsoluteUrlPath);
        }

        public static string GetParentDomain(string baseUrl)
        {
            var subStrings = new[] { "http://", "https://", "www.", "m.", "fb.", "vk." };

            foreach (var s in subStrings)
            {
                baseUrl = baseUrl.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) == 0
                    ? baseUrl.Remove(0, s.Length)
                    : baseUrl;
            }

            return baseUrl;
        }

        public static bool IsLocalUrl()
        {
            var isLocal = HttpContext.Current != null && 
                          (HttpContext.Current.Request.Url.ToString().Contains("localhost")
                           || HttpContext.Current.Request.Url.ToString().Contains("http://server")
                           || IPAddress.TryParse(HttpContext.Current.Request.Url.Host, out _));

            return isLocal;
        }

        private const string SaasWarningCookieTemplate = "saasWarningMessageHidden_{0}";

        public static bool IsSaasWarningHidden() =>
            SaasDataService.CurrentSaasData.AllServicesRemainsFormattedHash.HasValue &&
            GetCookieString(string.Format(SaasWarningCookieTemplate,
                    SaasDataService.CurrentSaasData.AllServicesRemainsFormattedHash))
                .TryParseBool();

        public static void HideSaasWarning()
        {
            var hash = SaasDataService.CurrentSaasData.AllServicesRemainsFormattedHash;
            if (hash.HasValue)
                SetCookie(string.Format(SaasWarningCookieTemplate, hash.Value), "true", 
                    new TimeSpan(7, 0, 0, 0), true);
        }

        public static string GenerateRandomString(int length)//GlorySoft_033
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

    }
}