using System;
using System.Net;
using System.Threading.Tasks;
using RestSharp;

namespace Ranobes
{
    public class Session
    {
        private readonly string _login;
        private readonly string _pass;
        private CookieCollection _cookies;
        
        public CookieCollection Cookies => _cookies;

        public bool IsAuthorized { get; private set; } 

        public Session(string login, string pass)
        {
            _login = login;
            _pass = pass;
            _cookies = new CookieCollection();

        }

        public async Task Login()
        {
            var client = new RestClient("https://ranobes.com/")
            {
                CookieContainer = new CookieContainer()
            };
            var loginRequest = new RestRequest(Method.POST);
            loginRequest.AddParameter("login_name", _login);
            loginRequest.AddParameter("login_password", _pass);
            loginRequest.AddParameter("login", "submit");
            var response = await client.ExecuteAsync(loginRequest);
            
            if (!response.IsSuccessful || response.StatusCode != HttpStatusCode.OK) return;

            _cookies = client.CookieContainer.GetCookies(new Uri("https://ranobes.com/"));
            IsAuthorized = true;
        }


    }
}