using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Byteopia.Music.GoogleMusicAPI
{
    public class GoogleCookieManager
    {
        public static String URI = "https://play.google.com/music/";
        private CookieContainer cookieContainer;

        [DataMember(Name = "Cookies")]
        public IEnumerable<Cookie> Cookies
        {
            get
            {
                return GetCookiesList();
            }
        }

        public GoogleCookieManager()
        {
            cookieContainer = new CookieContainer();
        }

        public bool HandleResponse(HttpResponseMessage msg)
        {
            bool cookiesChanged = false;
            IEnumerable<String> cookies;
            if (msg.Headers.TryGetValues("Set-Cookie", out cookies))
            {
                foreach (String cookie in cookies)
                {
                    cookieContainer.SetCookies(new Uri(URI), cookie);
                    cookiesChanged = true;
                }
            }

            return cookiesChanged;
        }

        public void SetCookiesFromList(List<Cookie> cookies)
        {
            if (cookies == null) return;
    
            foreach (Cookie c in cookies)
                cookieContainer.Add(new Uri(URI), c);
        }

        public String GetCookies()
        {
            return cookieContainer.GetCookieHeader(new Uri(URI));
        }

        public List<Cookie> GetCookiesList()
        {
            List<Cookie> cookies = new List<Cookie>();
            foreach (Cookie cookie in cookieContainer.GetCookies(new Uri(URI)))
            {
                cookies.Add(cookie);
            }

            return cookies;
        }
    }
}
