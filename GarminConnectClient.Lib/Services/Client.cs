﻿using GarminConnectClient.Lib.Dto;
using GarminConnectClient.Lib.Enum;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GarminConnectClient.Lib.Services
{
    /// <inheritdoc />
    /// <summary>
    /// Client implementation.
    /// Inspired by https://github.com/La0/garmin-uploader
    /// </summary>
    /// <seealso cref="T:GarminConnectClient.Lib.Services.IClient" />
    public class Client : IClient
    {
        private const string LOCALE = "fr_FR";
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:64.0) Gecko/20100101 Firefox/64.0";
        private const string CONNECT_DNS = "connect.garmin.com";
        private const string CONNECT_URL = "https://" + CONNECT_DNS;
        private const string CONNECT_URL_MODERN = CONNECT_URL + "/modern/";
        private const string CONNECT_URL_SIGNIN = CONNECT_URL + "/signin/";
        private const string SSO_DNS = "sso.garmin.com";
        private const string SSO_URL = "https://" + SSO_DNS;
        private const string SSO_URL_SSO = SSO_URL + "/sso";
        private const string SSO_URL_SSO_SIGNIN = SSO_URL_SSO + "/signin";
        private const string CONNECT_URL_PROFILE = "https://connect.garmin.com/modern/currentuser-service/user/info";
        private const string CONNECT_MODERN_HOSTNAME = "https://connect.garmin.com/modern/auth/hostname";
        private const string CSS_URL = CONNECT_URL + "/gauth-custom-v1.2-min.css";
        private const string PRIVACY_STATEMENT_URL = "https://www.garmin.com/en-US/privacy/connect/";
        private const string URL_UPLOAD = CONNECT_URL + "/modern/proxy/upload-service/upload"; 
        private const string URL_ACTIVITY_BASE = CONNECT_URL + "/modern/proxy/activity-service/activity";

        private const string UrlActivityTypes =
            "https://connect.garmin.com/modern/proxy/activity-service/activity/activityTypes";

        private const string UrlEventTypes =
            "https://connect.garmin.com/modern/proxy/activity-service/activity/eventTypes";

        private const string UrlActivitiesBase =
            "https://connect.garmin.com/modern/proxy/activitylist-service/activities/search/activities";

        private const string UrlActivityDownloadFile =
            " https://connect.garmin.com/modern/proxy/download-service/export/{0}/activity/{1}";

        private const string UrlActivityDownloadDefaultFile =
            " https://connect.garmin.com/modern/proxy/download-service/files/activity/{0}";

        private const ActivityFileTypeEnum DefaultFile = ActivityFileTypeEnum.Fit;

        private static CookieContainer cookieContainer;
        private static HttpClientHandler clientHandler;
        private HttpClient httpClient;

        private static readonly Tuple<string, string> BaseHeader = new Tuple<string, string>("NK", "NT");

        private static readonly Dictionary<string, string> QueryParams = new Dictionary<string, string>
        {
            {"clientId", "GarminConnect"},
            {"connectLegalTerms", "true"},
            {"consumeServiceTicket", "false"},
            {"createAccountShown", "true"},
            {"cssUrl", CSS_URL},
            {"displayNameShown", "false"},
            {"embedWidget", "false"},
            // ReSharper disable once StringLiteralTypo
            {"gauthHost", SSO_URL_SSO},
            {"generateExtraServiceTicket", "true"},
            {"generateTwoExtraServiceTickets", "false"},
            {"generateNoServiceTicket", "false"},
            {"globalOptInChecked", "false"},
            {"globalOptInShown", "true"},
            // ReSharper disable once StringLiteralTypo
            {"id", "gauth-widget"},
            {"initialFocus", "true"},
            {"locale", LOCALE},
            {"locationPromptShon", "true"},
            {"mobile", "false"},
            {"openCreateAccount", "false"},
            {"privacyStatementUrl", PRIVACY_STATEMENT_URL},
            {"redirectAfterAccountCreationUrl", CONNECT_URL_MODERN},
            {"redirectAfterAccountLoginUrl", CONNECT_URL_MODERN},
            {"rememberMeChecked", "false"},
            {"rememberMeShown", "true"},
            {"service", CONNECT_URL_MODERN},
            {"showTermsOfUse", "false"},
            {"showPrivacyPolicy", "false"},
            {"showConnectLegalAge", "false"},
            {"showPassword", "true"},
            {"source", CONNECT_URL_SIGNIN},
            // {"usernameShown", "false"},
            {"useCustomHeader", "false"},
            {"webhost", CONNECT_URL_MODERN}
        };

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// The logger
        /// </summary>
        // ReSharper disable once NotAccessedField.Local
        private readonly ILogger logger;

        public string _username { get; }
        public string _password { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public Client(IConfiguration configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public Client(string username, string password, ILogger logger)
        {
            _username = username;
            _password = password;
            this.logger = logger;
        }

        /// <inheritdoc />
        /// <summary>
        /// Authenticates this instance.
        /// </summary>
        /// <returns>
        /// Tuple of Cookies and HTTP handler
        /// </returns>
        /// <exception cref="T:System.Exception">
        /// SSO hostname is missing
        /// or
        /// Could not match service ticket.
        /// </exception>
        public async Task<(CookieContainer, HttpClientHandler)> Authenticate()
        {
            cookieContainer = new CookieContainer();
            clientHandler =
                new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    CookieContainer = cookieContainer
                };

            this.httpClient = new HttpClient(clientHandler);

            this.httpClient.DefaultRequestHeaders.Add("user-agent", USER_AGENT);
            var data = await this.httpClient.GetStringAsync(CONNECT_MODERN_HOSTNAME);

            var ssoHostname = JObject.Parse(data)["host"] == null
                ? throw new Exception("SSO hostname is missing")
                : JObject.Parse(data)["host"].ToString();

            var queryParams = string.Join("&", QueryParams.Select(e => $"{e.Key}={WebUtility.UrlEncode(e.Value)}"));

            var url = $"{SSO_URL_SSO_SIGNIN}?{queryParams}";
            var res = await this.httpClient.GetAsync(url);
            ValidateResponseMessage(res, "No login form.");

            data = await res.Content.ReadAsStringAsync();
            var csrfToken = "";
            try
            {
                GetValueByPattern(data, @"input type=\""hidden\"" name=\""_csrf\"" value=\""(\w+)\"" \/>", 2, 1);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception finding token by pattern: ", e);
                this.logger.LogError($"data:\n{data}");
                throw e;
            }

            this.httpClient.DefaultRequestHeaders.Add("origin", SSO_URL);
            this.httpClient.DefaultRequestHeaders.Add("referer", url);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("embed", "false"),
                new KeyValuePair<string, string>("username", _username),
                new KeyValuePair<string, string>("password", _password),
                new KeyValuePair<string, string>("_csrf", csrfToken)
            });

            res = await this.httpClient.PostAsync(url, formContent);
            data = await res.Content.ReadAsStringAsync();
            ValidateResponseMessage(res, $"Bad response {res.StatusCode}, expected {HttpStatusCode.OK}");
            ValidateCookiePresence(cookieContainer, "GARMIN-SSO-GUID");

            var ticket = GetValueByPattern(data, @"var response_url(\s+)= (\""|\').*?ticket=([\w\-]+)(\""|\')", 5, 3);

            // Second auth step
            // Needs a service ticket from previous response
            this.httpClient.DefaultRequestHeaders.Remove("origin");
            //url = $"{CONNECT_URL_MODERN}?ticket={WebUtility.UrlEncode(ticket)}";
            //res = await this.httpClient.GetAsync(url);

            //ValidateModernTicketUrlResponseMessage(res, $"Second auth step failed to produce success or expected 302: {res.StatusCode}.");

            // Check session cookie
            //ValidateCookiePresence(cookieContainer, "SESSIONID");

            // Check login
            res = await this.httpClient.GetAsync(CONNECT_URL_PROFILE);
            ValidateResponseMessage(res, "Login check failed.");

            return (cookieContainer, clientHandler);
        }

        /// <summary>
        /// Gets the value by pattern.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="expectedCountOfGroups">The expected count of groups.</param>
        /// <param name="groupPosition">The group position.</param>
        /// <returns>Value of particular match group.</returns>
        /// <exception cref="Exception">Could not match expected pattern {pattern}</exception>
        private static string GetValueByPattern(string data, string pattern, int expectedCountOfGroups, int groupPosition)
        {
            var regex = new Regex(pattern);
            var match = regex.Match(data);
            if (!match.Success || match.Groups.Count != expectedCountOfGroups)
            {
                throw new Exception($"Could not match expected pattern {pattern}.");
            }
            return match.Groups[groupPosition].Value;
        }

        /// <summary>
        /// Validates the cookie presence.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="cookieName">Name of the cookie.</param>
        /// <exception cref="Exception">Missing cookie {cookieName}</exception>
        private static void ValidateCookiePresence(CookieContainer container, string cookieName)
        {
            var cookies = container.GetCookies(new Uri(CONNECT_URL_MODERN)).Cast<Cookie>().ToList();
            if (!cookies.Any(e => string.Equals(cookieName, e.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new Exception($"Missing cookie {cookieName}");
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ValidateResponseMessage(HttpResponseMessage responseMessage, string errorMessage)
        {
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception(errorMessage);
            }
        }

        private void ValidateModernTicketUrlResponseMessage(HttpResponseMessage responseMessage, string error)
        {
            if (!responseMessage.IsSuccessStatusCode && !responseMessage.StatusCode.Equals(HttpStatusCode.MovedPermanently))
            {
                throw new Exception(error);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Downloads the activity file.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        /// <param name="fileFormat">The file format.</param>
        /// <returns>
        /// Stream
        /// </returns>
        public async Task<Stream> DownloadActivityFile(long activityId, ActivityFileTypeEnum fileFormat)
        {
            var url = fileFormat == DefaultFile
                ? string.Format(UrlActivityDownloadDefaultFile, activityId)
                : string.Format(UrlActivityDownloadFile, fileFormat.ToString().ToLower(), activityId);

            Stream streamCopy = new MemoryStream();
            using (var res = await this.httpClient.GetAsync(url))
            {
                await (await res.Content.ReadAsStreamAsync()).CopyToAsync(streamCopy);
                return streamCopy;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Uploads the activity.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileFormat">The file format.</param>
        /// <returns>
        /// Tuple of result and activity id
        /// </returns>
        /// <exception cref="T:System.Exception">
        /// Failed to upload {fileName}
        /// or
        /// or
        /// Unknown error: {response.ToString()}
        /// </exception>
        public async Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat)
        {
            var extension = fileFormat.FormatKey;
            var url = $"{URL_UPLOAD}/{extension}";
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

            var form = new MultipartFormDataContent(
                $"------WebKitFormBoundary{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}");

            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                using (var content = new StreamContent(stream))
                {
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "file",
                        FileName = Path.GetFileName(fileName),
                        Size = stream.Length
                    };

                    form.Headers.Add(BaseHeader.Item1, BaseHeader.Item2);
                    form.Add(content, "file", Path.GetFileName(fileName));
                    using (var res = await this.httpClient.PostAsync(url, form))
                    {
                        // HTTP Status can either be OK or Conflict
                        if (!new HashSet<HttpStatusCode>
                                {HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Conflict}
                            .Contains(res.StatusCode))
                        {
                            if (res.StatusCode == HttpStatusCode.PreconditionFailed)
                            {
                                throw new Exception($"Failed to upload {fileName}");
                            }
                        }

                        var responseData = await res.Content.ReadAsStringAsync();
                        var response = JObject.Parse(responseData)["detailedImportResult"];
                        var successes = response["successes"];
                        if (successes.HasValues)
                        {
                            return (true, long.Parse(successes[0]["internalId"].ToString()));
                        }

                        var failures = response["failures"];
                        if (!failures.HasValues)
                        {
                            throw new Exception($"Unknown error: {response}");
                        }

                        var messages = failures[0]["messages"];
                        var code = int.Parse(messages[0]["code"].ToString());
                        if (code == (int)HttpStatusCode.Accepted)
                        {
                            // Activity already exists
                            return (false, long.Parse(messages[0]["internalId"].ToString()));
                        }

                        throw new Exception(messages.ToString());
                    }
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Sets the name of the activity.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        /// <param name="activityName">Name of the activity.</param>
        /// <returns>
        /// The task
        /// </returns>
        public async Task SetActivityName(long activityId, string activityName)
        {
            var url = $"{URL_ACTIVITY_BASE}/{activityId}";
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);
            this.httpClient.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

            var data = new
            {
                activityId,
                activityName
            };

            using (var res = await this.httpClient.PostAsync(url,
                new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")))
            {
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Activity name not set: {await res.Content.ReadAsStringAsync()}");
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Loads the activity types.
        /// </summary>
        /// <returns>
        /// List of activities
        /// </returns>
        public async Task<List<ActivityType>> LoadActivityTypes()
        {
            return await this.ExecuteUrlGetRequest<List<ActivityType>>(UrlActivityTypes,
                "Error while getting activity types");
        }

        /// <summary>
        /// Loads the event types.
        /// </summary>
        /// <returns></returns>
        public async Task<List<ActivityType>> LoadEventTypes()
        {
            return await this.ExecuteUrlGetRequest<List<ActivityType>>(UrlEventTypes,
                "Error while getting event types");
        }

        /// <inheritdoc />
        /// <summary>
        /// Sets the type of the activity.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        /// <param name="activityType">Type of the activity.</param>
        /// <returns>
        /// The task
        /// </returns>
        public async Task SetActivityType(long activityId, ActivityType activityType)
        {
            var url = $"{URL_ACTIVITY_BASE}/{activityId}";
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

            this.httpClient.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

            var data = new
            {
                activityId,
                activityTypeDTO = activityType
            };

            using (var res = await this.httpClient.PostAsync(url,
                new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")))
            {
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Activity type not set: {await res.Content.ReadAsStringAsync()}");
                }
            }
        }

        /// <summary>
        /// Sets the type of the event.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <returns></returns>
        public async Task SetEventType(long activityId, ActivityType eventType)
        {
            var url = $"{URL_ACTIVITY_BASE}/{activityId}";
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

            this.httpClient.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

            var data = new
            {
                activityId,
                eventTypeDTO = eventType
            };

            using (var res = await this.httpClient.PostAsync(url,
                new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")))
            {
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Event type not set: {await res.Content.ReadAsStringAsync()}");
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Sets the activity description.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        /// <param name="description">The description.</param>
        /// <returns>
        /// The task
        /// </returns>
        public async Task SetActivityDescription(long activityId, string description)
        {
            var url = $"{URL_ACTIVITY_BASE}/{activityId}";
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

            this.httpClient.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

            var data = new
            {
                activityId,
                description
            };

            using (var res = await this.httpClient.PostAsync(url,
                new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")))
            {
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Activity description not set: {await res.Content.ReadAsStringAsync()}");
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Loads the activity.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        /// <returns>
        /// Activity
        /// </returns>
        public async Task<Activity> LoadActivity(long activityId)
        {
            var url = $"{URL_ACTIVITY_BASE}/{activityId}";
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

            return await this.ExecuteUrlGetRequest<Activity>(url, "Error while getting activity");
        }

        /// <summary>
        /// Gets the unix timestamp.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        private static int GetUnixTimestamp(DateTime date)
        {
            return (int)date.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Creates the activities URL.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="start">The start.</param>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        private static string CreateActivitiesUrl(int limit, int start, DateTime date)
        {
            return $"{UrlActivitiesBase}?limit={limit}&start={start}&_={GetUnixTimestamp(date)}";
        }

        /// <inheritdoc />
        /// <summary>
        /// Loads the activities.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="start">The start.</param>
        /// <param name="from">From.</param>
        /// <returns>
        /// List of activities
        /// </returns>
        public async Task<List<Activity>> LoadActivities(int limit, int start, DateTime from)
        {
            var url = CreateActivitiesUrl(limit, start, from);
            this.httpClient.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

            return await this.ExecuteUrlGetRequest<List<Activity>>(url, "Error while getting activities");
        }

        private static T DeserializeData<T>(string data) where T : class
        {
            return typeof(T) == typeof(string) ? data as T : JsonConvert.DeserializeObject<T>(data);
        }

        /// <summary>
        /// Executes the URL get request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private async Task<T> ExecuteUrlGetRequest<T>(string url, string errorMessage) where T : class
        {
            var res = await this.httpClient.GetAsync(url);
            var data = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"{errorMessage}: {data}");
            }

            return DeserializeData<T>(data);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Client" /> class.
        /// </summary>
        ~Client()
        {
            if (this.httpClient == null)
            {
                return;
            }

            this.httpClient.Dispose();
            this.httpClient = null;
        }
    }
}
