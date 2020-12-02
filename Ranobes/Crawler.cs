using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Ranobes.Models;
using RestSharp;

namespace Ranobes
{
    public class Crawler
    {
        private readonly Session _session;

        public Crawler(Session session)
        {
            _session = session;
        }

        public async Task Process()
        {
            var favorites = GetFavorites();
            await foreach (var favorite in favorites)
            {
                ;
            }
        }

        private async UpdateInfo()
        {

        }

        private async IAsyncEnumerable<Favorite> GetFavorites()
        {
            if (!_session.IsAuthorized) yield break;

            var client = new RestClient("https://ranobes.com/")
            {
                CookieContainer = new CookieContainer()
            };
            client.CookieContainer.Add(_session.Cookies);

            var request = new RestRequest("/favorites/list/5");
            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful) yield break;

            var parser = new HtmlParser();
            var page = parser.ParseDocument(response.Content);
            var contents = page.GetElementById("dle-content");
            var getStoryLine = contents.GetElementsByClassName("cat_block cat_line story_line");
            foreach (var storyLine in getStoryLine)
            {
                var href = storyLine.GetElementsByTagName("a").FirstOrDefault();
                var title = href!.GetAttribute("title");
                var url = href!.GetAttribute("href");
                yield return new Favorite
                {
                    Url = url,
                    Title = title
                };
            }

        }



    }
}