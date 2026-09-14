using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AdvantShop.Configuration;
using AdvantShop.Core.Caching;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AdvantShop.Shipping.Sdek.Api
{
    // https://api-docs.cdek.ru/29923926.html
    public class SdekApiService20
    {
        private const int MillisecondsBetweenRequests = 0; // время в миллисекундах между запросами
        private static readonly string BaseUrl = LinkService.ShippingMethod.Sdek.Api;

        private static DateTime _lastRequestApi = DateTime.UtcNow.AddHours(-1);
        private static readonly ConcurrentDictionary<string, string> AccessTokens = new ConcurrentDictionary<string, string>();
  
        protected string KeyAccessToken { get; private set; }
        public List<string> LastActionErrors { get; set; }
    
        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }

        private readonly string _login;
        private readonly string _password;

        public SdekApiService20(string login, string password)
        {
            _login = login;
            _password = password;

            KeyAccessToken = $"{_login}#{_password}";

            Initialize();
        }
        
        /// <summary>
        /// Расчет по коду тарифа
        /// </summary>
        public TariffResult CalculatorTariff(TariffParams calculateParams)
        {
            return MakeRequest<TariffResult>("/calculator/tariff", calculateParams);
        }
        
        /// <summary>
        /// Расчет по доступным тарифам
        /// </summary>
        public TariffListResult CalculatorTariffList(TariffListParams calculateParams)
        {
            return MakeRequest<TariffListResult>("/calculator/tarifflist", calculateParams);
        }

        public List<DeliveryPoint> GetDeliveryPoints(DeliveryPointsFilter filter)
        {
            string query = null;
            if (filter != null)
            {
                var getParams = GetObjectParams(filter);

                query = getParams.Count > 0 
                    ? "?" + string.Join("&", getParams)
                    : null;
            }

            return MakeRequest<List<DeliveryPoint>>($"/deliverypoints{query}", method: WebRequestMethods.Http.Get);
        }

        /// <summary>
        /// Регистрация заказа
        /// </summary>
        public NewOrderResult CreateOrder(NewOrder order)
        {
            return MakeRequest<NewOrderResult>("/orders", order);
        }

        /// <summary>
        /// Информация о заказе
        /// </summary>
        public GetOrderResult GetOrder(Guid? uuid, string cdekNumber, string imNumber)
        {
            if (!uuid.HasValue && cdekNumber.IsNullOrEmpty() && imNumber.IsNullOrEmpty())
                throw new ArgumentNullException($"{nameof(uuid)} and {nameof(cdekNumber)} and {nameof(imNumber)}");
            string query = null;
            if (uuid.HasValue)
                query = $"/{uuid}";
            else if (cdekNumber.IsNotEmpty())
                query = $"?cdek_number={cdekNumber}";
            else
                query = $"?im_number={imNumber}";
       
            return MakeRequest<GetOrderResult>($"/orders{query}", method: WebRequestMethods.Http.Get);
        }

        /// <summary>
        /// Информация о заказе
        /// </summary>
        public DeleteOrderResult DeleteOrder(Guid uuid)
        {
            return MakeRequest<DeleteOrderResult>($"/orders/{uuid}", method: "DELETE");
        }

        /// <summary>
        /// Формирование квитанции к заказу
        /// </summary>
        public CreatePrintFormResult CreatePrintForm(CreatePrintForm parameters)
        {
            return MakeRequest<CreatePrintFormResult>("/print/orders", parameters);
        }

        /// <summary>
        /// Получение квитанции к заказу
        /// </summary>
        public GetPrintFormResult GetPrintForm(Guid uuid)
        {
            return MakeRequest<GetPrintFormResult>($"/print/orders/{uuid}", method: WebRequestMethods.Http.Get);
        }

        /// <summary>
        /// Формирование ШК места к заказу
        /// </summary>
        public CreateBarCodeOrderResult CreateBarCodeOrder(CreateBarCodeOrder parameters)
        {
            return MakeRequest<CreateBarCodeOrderResult>("/print/barcodes", parameters);
        }

        /// <summary>
        /// Получение ШК места к заказу
        /// </summary>
        public GetBarCodeOrderResult GetBarCodeOrder(Guid uuid)
        {
            return MakeRequest<GetBarCodeOrderResult>($"/print/barcodes/{uuid}", method: WebRequestMethods.Http.Get);
        }

        public string DownloadFile(string url, string path)
        {
            string filePath = null;

            using (var responseStream = MakeRequestGetStream(url, method: WebRequestMethods.Http.Get))
            {
                if (responseStream != null)
                {
                    var fileName = "SdekFile-" + Path.GetFileName(url);
                    filePath = Path.Combine(path, string.Format("{0}.{1}", 
                        GetValidFileName(Path.GetFileNameWithoutExtension(fileName)),
                        Path.GetExtension(fileName)));

                    using (var filestream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                        responseStream.CopyTo(filestream);
                }
            }

            return filePath;
        }

        /// <summary>
        /// Регистрация заявки на вызов курьера
        /// </summary>
        public IntakeResult Intake(IntakeParams parameters)
        {
            return MakeRequest<IntakeResult>("/intakes", parameters);
        }

        public List<CityInfo> GetCities(CitiesFilter filter)
        {
            string query = null;
            if (filter != null)
            {
                var getParams = GetObjectParams(filter);

                query = getParams.Count > 0 
                    ? "?" + string.Join("&", getParams)
                    : null;
            }

            return MakeRequest<List<CityInfo>>($"/location/cities{query}", method: WebRequestMethods.Http.Get);
        }

        #region PrivateMethods

        private List<string> GetObjectParams(object obj)
        {
            if (obj == null)
                return new List<string>();
            
            if (!(SerializationSettings.ContractResolver is DefaultContractResolver contractResolver))
                throw new Exception("contractResolver");

            var getParams = new List<string>();
            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                var value = propertyInfo.GetValue(obj);
                if (value != null)
                {
                    if (value is bool?) value = ((bool?) value).Value ? "1" : "0";
                    if (value is IEnumerable && !(value is string)) value = string.Join(",", ((IEnumerable) value).Cast<object>());

                    getParams.Add(
                        contractResolver.NamingStrategy.GetPropertyName(propertyInfo.Name, false) +
                        "=" + HttpUtility.UrlEncode(value.ToString()));
                }
            }

            return getParams;
        }

        private void Initialize()
        {
            SerializationSettings = new JsonSerializerSettings
            {
#if DEBUG
                Formatting = Formatting.Indented,
#endif
#if !DEBUG
                Formatting = Formatting.None,
#endif
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver =  new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }

            };
            DeserializationSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver =  new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }

            };
        }
 
        private string GetValidFileName(string filename)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));

            return regex.Replace(filename, "-");
        }
       
        private T MakeRequest<T>(string url, object data = null, string method = WebRequestMethods.Http.Post,
            string contentType = "application/json;charset=UTF-8")
            where T : class, new()
        {
            return Task.Run<T>((Func<Task<T>>) (async () =>
                    await MakeRequestAsync<T>(url, data, method, contentType).ConfigureAwait(false)))
                .Result;
        }
        
        private T MakeRequestWithOutToken<T>(string url, object data = null, string method = WebRequestMethods.Http.Post,
            string contentType = "application/json;charset=UTF-8")
            where T : class, new()
        {
            return Task.Run<T>((Func<Task<T>>) (async () =>
                    await MakeRequestAsync<T>(url, data, method, contentType, withOutToken: true)
                        .ConfigureAwait(false)))
                .Result;
        }

        private async Task<T> MakeRequestAsync<T>(string url, object data = null, string method = WebRequestMethods.Http.Post, 
            string contentType = "application/json;charset=UTF-8", bool withOutToken = false, bool noRecallAtUnauthorized = false)
            where T : class, new()
        {
            LastActionErrors = null;

            try
            {
                var request = await CreateRequestAsync(url, data, method, contentType, withOutToken).ConfigureAwait(false);
                //request.Timeout = 10000;

                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                    using (var stream = response.GetResponseStream())
                        if (stream != null)
                            return await GetResult<T>(data, stream).ConfigureAwait(false);
            }
            catch (WebException ex)
            {
                using (var eResponse = (HttpWebResponse)ex.Response)
                {
                    if (eResponse != null)
                    {
                        using (var eStream = eResponse.GetResponseStream())
                            if (eStream != null)
                            {
                                try
                                {
                                    HttpStatusCode statusCode = eResponse.StatusCode;
                                    
                                    if (!noRecallAtUnauthorized && !withOutToken && statusCode == HttpStatusCode.Unauthorized)
                                    {
                                        if (AccessTokens.ContainsKey(KeyAccessToken))
                                        {
                                            AccessTokens.TryRemove(KeyAccessToken, out _);
                                            return await MakeRequestAsync<T>(url, data, method, contentType, withOutToken, noRecallAtUnauthorized: true).ConfigureAwait(false);
                                        }
                                    }

                                    if (statusCode == HttpStatusCode.Unauthorized
                                        && url.Contains("/oauth/token?parameters", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // кэшируем на час, что авторизоваться под этими данными нельзя
                                        CacheManager.Insert(GetCacheKeyUnauthorized(), true, 60);
                                    }

                                    if (statusCode == HttpStatusCode.BadRequest ||
                                        statusCode == HttpStatusCode.InternalServerError)
                                    {
                                        return await GetResult<T>(data, eStream).ConfigureAwait(false);
                                    }

                                    using (var reader = new StreamReader(eStream))
                                    {
                                        var error = reader.ReadToEnd();
                                        if (data != null)
                                        {
                                            error += " data:" + JsonConvert.SerializeObject(data, SerializationSettings);
                                        }
                                        LastActionErrors = new List<string>()
                                        {
                                            string.IsNullOrEmpty(error) ? ex.Message : error
                                        };

                                        if (string.IsNullOrEmpty(error))
                                            Debug.Log.Warn(ex);
                                        else
                                        {
                                            Debug.Log.Warn(error, ex);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    LastActionErrors = new List<string>() { ex.Message };
                                    Debug.Log.Warn(ex);
                                }
                            }
                            else
                            {
                                LastActionErrors = new List<string>() { ex.Message };
                                Debug.Log.Warn(ex);
                            }
                    }
                    else
                    {
                        LastActionErrors = new List<string>() { ex.Message };
                        Debug.Log.Warn(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LastActionErrors = new List<string>() { ex.Message };
                Debug.Log.Error(ex);
            }

            return null;
        }

        private string GetCacheKeyUnauthorized() 
            => $"SdekApiUnauthorized_{(_login + "&&&" + _password).GetHashCode()}";

        private async Task<T> GetResult<T>(object data, Stream stream) where T : class, new()
        {
            var result = await DeserializeObjectAsync<T>(stream).ConfigureAwait(false);

            if (result != null)
            {
                var errors = GetErrors(result);
                if (errors != null && errors.Count > 0)
                {
                    var dataStr = data != null
                        ? JsonConvert.SerializeObject(data, SerializationSettings)
                        : null;
                    foreach (var error in errors)
                        Debug.Log.Warn($"Sdek ErrorCode: {error.Code}, ErrorMessage: {error.Message}, Data: {dataStr}");
                }
            }

            return result;
        }

        private Stream MakeRequestGetStream(string url, object data = null, string method = WebRequestMethods.Http.Post, 
            string contentType = null, bool withOutToken = false, bool noRecallAtUnauthorized = false)
        {
            LastActionErrors = null;

            try
            {
                var request = CreateRequestAsync(url, data, method, contentType, withOutToken).ConfigureAwait(false).GetAwaiter().GetResult();

                var response = request.GetResponse();

                return response.GetResponseStream();
            }
            catch (WebException ex)
            {
                using (var eResponse = (HttpWebResponse)ex.Response)
                {
                    if (eResponse != null)
                    {
                        using (var eStream = eResponse.GetResponseStream())
                            if (eStream != null)
                            {
                                try
                                {
                                    HttpStatusCode statusCode = eResponse.StatusCode;
                                    
                                    if (!noRecallAtUnauthorized && !withOutToken && statusCode == HttpStatusCode.Unauthorized)
                                    {
                                        if (AccessTokens.ContainsKey(KeyAccessToken))
                                        {
                                            AccessTokens.TryRemove(KeyAccessToken, out _);
                                            return MakeRequestGetStream(url, data, method, contentType, withOutToken, noRecallAtUnauthorized: true);
                                        }
                                    }

                                    //if ((int)statusCode == 400 || (int)statusCode == 401 || (int)statusCode == 500)
                                    //{
                                    //    var result = DeserializeObjectAsync<T>(eStream).ConfigureAwait(false).GetAwaiter().GetResult();

                                    //    if (result != null)
                                    //    {
                                    //        BaseResponse baseResponse = (BaseResponse)result;
                                    //        if (baseResponse.IsSuccess == false)
                                    //            Debug.Log.Warn(string.Format("Hermes ErrorCode: {0}, ErrorMessage: {1}", baseResponse.ErrorCode, baseResponse.ErrorMessage));
                                    //    }

                                    //    return result;
                                    //}

                                    using (var reader = new StreamReader(eStream))
                                    {
                                        var error = reader.ReadToEnd();
                                        if (data != null)
                                        {
                                            error += " data:" + JsonConvert.SerializeObject(data, SerializationSettings);
                                        }
                                        LastActionErrors = new List<string>()
                                        {
                                            string.IsNullOrEmpty(error) ? ex.Message : error
                                        };

                                        if (string.IsNullOrEmpty(error))
                                            Debug.Log.Warn(ex);
                                        else
                                        {
                                            Debug.Log.Warn(error, ex);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    LastActionErrors = new List<string>() { ex.Message };
                                    Debug.Log.Warn(ex);
                                }
                            }
                            else
                            {
                                LastActionErrors = new List<string>() { ex.Message };
                                Debug.Log.Warn(ex);
                            }
                    }
                    else
                    {
                        LastActionErrors = new List<string>() { ex.Message };
                        Debug.Log.Warn(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LastActionErrors = new List<string>() { ex.Message };
                Debug.Log.Error(ex);
            }

            return null;
        }
        private async Task<HttpWebRequest> CreateRequestAsync(string url, object data, string method, 
            string contentType, bool withOutToken)
        {
            string token = null;
            if (!withOutToken)
            {
                token = await GetAccessTokenAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(token))
                    throw new Exception("Sdek AdvException: Не удалось получить токен авторизации.");
            }

            var isFullUrl = url.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                            url.StartsWith("https:", StringComparison.OrdinalIgnoreCase);
            var request = WebRequest.Create(isFullUrl ? url : BaseUrl + url) as HttpWebRequest;
            request.Method = method;
            request.ContentType = contentType;
            if (!withOutToken)
                request.Headers.Add(HttpRequestHeader.Authorization,
                    $"Bearer {token}");

            if (data != null)
                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                    if (contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                        await WriteUrlEncodedData(data.ToString(), requestStream).ConfigureAwait(false);
                    else
                        await SerializeObjectAsync(data, requestStream).ConfigureAwait(false);
            return request;
        }

        private Task SerializeObjectAsync(object data, Stream stream)
        {
#if DEBUG
            string dataPost = JsonConvert.SerializeObject(data, SerializationSettings);

            byte[] bytes = Encoding.UTF8.GetBytes(dataPost);
            //request.ContentLength = bytes.Length;

            return stream.WriteAsync(bytes, 0, bytes.Length);
#endif
#if !DEBUG
            using (StreamWriter writer = new StreamWriter(stream))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
            {
                JsonSerializer serializer = JsonSerializer.Create(SerializationSettings);
                serializer.Serialize(jsonWriter, data);
                return jsonWriter.FlushAsync();
            }
#endif
        }

        private Task WriteUrlEncodedData(string data, Stream stream)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            //request.ContentLength = bytes.Length;

            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task<T> DeserializeObjectAsync<T>(Stream stream)
            where T : class
        {
            using (var reader = new StreamReader(stream))
            {
#if DEBUG
                var responseContent = "";
                responseContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(responseContent, DeserializationSettings);
#endif
#if !DEBUG
                using (JsonReader jsonReader = new JsonTextReader(reader))
                {
                    JsonSerializer serializer = JsonSerializer.Create(DeserializationSettings);

                    // read the json from a stream
                    // json size doesn't matter because only a small piece is read at a time from the HTTP request
                    return serializer.Deserialize<T>(jsonReader);
                }
#endif
            }
        }

        private async Task<OAuthData> OAuthAsync()
        {
            var grantType = HttpUtility.UrlEncode("client_credentials");
            var clientId = HttpUtility.UrlEncode(_login);
            var clientSecret = HttpUtility.UrlEncode(_password);
            
            // по этому логину/паролю 401 ошибка была менее часа назад
            if (CacheManager.Contains(GetCacheKeyUnauthorized()))
                return null;
            
            return
                await MakeRequestAsync<OAuthData>("/oauth/token?parameters",
                    data: $"grant_type={grantType}&client_id={clientId}&client_secret={clientSecret}",
                    contentType:"application/x-www-form-urlencoded;charset=UTF-8",
                    withOutToken: true,
                    noRecallAtUnauthorized: true).ConfigureAwait(false);
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!AccessTokens.ContainsKey(KeyAccessToken))
            {
                var oAuthData = await OAuthAsync().ConfigureAwait(false);
                if (oAuthData == null || string.IsNullOrEmpty(oAuthData.AccessToken))
                    return null;

                AccessTokens.TryAdd(KeyAccessToken, oAuthData.AccessToken);
            }

            return AccessTokens[KeyAccessToken];
        }

        public /*GlorySoft_001 private*/ List<Error> GetErrors(object obj)
        {
            var list = new List<Error>();
            if (obj == null)
                return list;

            var type = obj.GetType();
            if (type.IsPrimitive || type == typeof(string) || !type.IsClass)
                return list;

            var errorType = typeof(Error);
            var iEnumerableType = typeof(IEnumerable);
            if (!type.GetInterfaces().Contains(iEnumerableType))
            {
                var properties = type.GetProperties();

                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.PropertyType == errorType)
                    {
                        var error = (Error)propertyInfo.GetValue(obj);
                        
                        if (error != null)
                            list.Add(error);
                    }
                    else if (!propertyInfo.PropertyType.IsPrimitive && propertyInfo.PropertyType != typeof(string))
                    {
                        if (propertyInfo.PropertyType.GetInterfaces().Contains(iEnumerableType) ||
                            propertyInfo.PropertyType.IsClass)
                        {
                            list.AddRange(GetErrors(propertyInfo.GetValue(obj)));
                        }
                    }
                }
            }
            else
            {
                if (obj is IEnumerable<Error>)
                {
                    list.AddRange((IEnumerable<Error>) obj);
                }
                else
                {
                    var values = (IEnumerable) obj;

                    foreach (var value in values)
                        list.AddRange(GetErrors(value));
                }
            }

            return list;
        }

        #endregion

    }
}