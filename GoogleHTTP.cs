using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

using Byteopia.Helpers;

namespace Byteopia.Music.GoogleMusicAPI
{
    /// <summary>
    /// Wraps an HttpClient for use with Google requests
    /// </summary>
    ///
    public class GoogleHTTP
    {
        /// <summary>
        /// The HttpClient
        /// </summary>
        private HttpClient client;

        /// <summary>
        /// This is required for each and every HTTP request
        /// </summary>
        ///
        [DataMember(Name="AuthToken")]
        private String authroizationToken;
        public System.String AuthroizationToken
        {
            get { return authroizationToken; }
            set { authroizationToken = value; }
        }

        [DataMember(Name = "AuthTokenIssueDate")]
        private DateTime authTokenIssueDate;

        public DateTime AuthTokenIssueDate
        {
            get { return authTokenIssueDate; }
            set { authTokenIssueDate = value; }
        }

        /// <summary>
        /// The status code from the last POST\GET request
        /// </summary>
        private HttpStatusCode lastStatusCode;

        public System.Net.HttpStatusCode LastStatusCode
        {
            get { return lastStatusCode; }
            set { lastStatusCode = value; }
        }


        private string rejectedReason;

        public string RejectedReason
        {
            get { return rejectedReason; }
            set { rejectedReason = value; }
        }

        GoogleCookieManager cookieManager;

        public GoogleCookieManager CookieManager
        {
            get { return cookieManager; }
            set { cookieManager = value; }
        }

        public event EventHandler CookiesChanged;

        public GoogleHTTP()
        {
            authroizationToken = String.Empty;

            HttpClientHandler handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false
            };


            client = new HttpClient(handler);
            cookieManager = new GoogleCookieManager();
        }

        /// <summary>
        /// Set the auth token from the login data
        /// </summary>
        /// <param name="loginData"></param>
        public void SetAuthToken(String loginData)
        {
            string CountTemplate = @"Auth=(?<AUTH>(.*?))$";
            Regex CountRegex = new Regex(CountTemplate, RegexOptions.IgnoreCase);
            string auth = CountRegex.Match(loginData).Groups["AUTH"].ToString();
            authroizationToken = auth;

            this.AuthTokenIssueDate = DateTime.Now;
        }

        /// <summary>
        /// Generic POST method that deserializes its result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<T> POST<T>(Uri address, HttpContent content = null)
        {
            return JSON.Deserialize<T>(await POST(address, content));
        }

        /// <summary>
        /// Generic GET method that deserializes its result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<T> GET<T>(Uri address)
        {
            return JSON.Deserialize<T>(await GET(address));
        }

        /// <summary>
        /// POST request
        /// </summary>
        /// <param name="address">end point</param>
        /// <param name="content">content</param>
        /// <returns></returns>
        public async Task<String> POST(Uri address, HttpContent content = null)
        {
            SetAuthHeader();
            //RebuildCookieContainer();

            HttpResponseMessage responseMessage = null;
            HttpRequestMessage requestMessage = null;

            try
            {
                String reqUri = BuildGoogleRequest(address).ToString();
                requestMessage = new HttpRequestMessage(HttpMethod.Post, reqUri);
                requestMessage.Content = content;
                requestMessage.Headers.Add("Cookie", cookieManager.GetCookies());
                responseMessage = await client.SendAsync(requestMessage);
            }
            catch (Exception e)
            {
                throw;
            }

            LastStatusCode = responseMessage.StatusCode;

            if (cookieManager.HandleResponse(responseMessage))
                if (CookiesChanged != null)
                    CookiesChanged(this, new EventArgs());

            //CheckForCookies(responseMessage, address);
            CheckForUpdatedAuth(responseMessage);
            CheckForRejection(responseMessage);

            String retnData = String.Empty;

            try
            {
                retnData = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                throw; // Bubble up
            }

            return retnData;
        }

       
        /// <summary>
        /// GET request
        /// </summary>
        /// <param name="address">endpoint</param>
        /// <returns></returns>
        public async Task<String> GET(Uri address)
        {
            SetAuthHeader();
            //RebuildCookieContainer();

            HttpResponseMessage responseMessage = null;
            HttpRequestMessage requestMessage = null;

            try
            {
                String reqUri = BuildGoogleRequest(address).ToString();
                requestMessage = new HttpRequestMessage(HttpMethod.Get, reqUri);
                requestMessage.Headers.Add("Cookie", cookieManager.GetCookies());
                responseMessage = await client.SendAsync(requestMessage);
            }
            catch (Exception e)
            {
                throw;
            }

            LastStatusCode = responseMessage.StatusCode;

            if (cookieManager.HandleResponse(responseMessage))
                if (CookiesChanged != null)
                    CookiesChanged(this, new EventArgs());

            //CheckForCookies(responseMessage, address);
            CheckForUpdatedAuth(responseMessage);
            CheckForRejection(responseMessage);

            String retnData = String.Empty;

            try
            {
                retnData = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                throw; // Bubble up
            }

            return retnData;
        }

    
        /// <summary>
        /// Sets Google's auth header
        /// </summary>
        private void SetAuthHeader()
        {
            if (!authroizationToken.Equals(String.Empty))
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(String.Format("GoogleLogin auth={0}", authroizationToken));
        }

        public bool CheckForUpdatedAuth(HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                if (header.Key.Equals("Update-Client-Auth"))
                {
                    foreach (var v in header.Value)
                    {
                        authroizationToken = v;
                        return true;
                    }
                }
            }

            return false;
        }

        public void CheckForRejection(HttpResponseMessage responseMessage)
        {
            rejectedReason = String.Empty;

            foreach (var header in responseMessage.Headers)
            {
                if (header.Key.Equals("X-Rejected-Reason"))
                {
                    foreach (var v in header.Value)
                    {
                        rejectedReason = v;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Append xt cookie value to each request
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private Uri BuildGoogleRequest(Uri uri)
        {
            String xt = GetXtCookie();
            if (xt.Equals(String.Empty))
                return uri;

            if (uri.ToString().Contains("songid"))
                return uri;

            if (uri.ToString().StartsWith("https://play.google.com/music/listen"))
                return uri;

            if (uri.ToString().StartsWith("https://www.google.com/accounts/Logout"))
                return uri;

            return new Uri(uri.OriginalString + String.Format("?u=0&xt={0}", xt));
        }

        public String GetXtCookie()
        {
            // Get the last one
            String xt = "";
            foreach (Cookie cook in cookieManager.GetCookiesList())
                if (cook.Name.Equals("xt"))
                    xt = cook.Value;

            return xt;
        }
    }
}