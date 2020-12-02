using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
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
                await UpdateInfo(favorite);
                await GetChapters(favorite);
                await MakeHtml(favorite);
            }
        }

        private async Task MakeHtml(Favorite favorite)
        {
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(r =>
                r.Content($"<p><h1>{favorite.Title}</h1></p><h2>{favorite.Description}</h2><div id=\"story\"></div>"));
            var node = document.GetElementById("story");
            var elements = new List<INode>();
            favorite.Chapters.Reverse();
            foreach (var favoriteChapter in favorite.Chapters)
            {
                var title = document.CreateElement("div");
                title.InnerHtml = $"<p><b>{favoriteChapter.Title}</b></p>";
                elements.Add(title);
                var element = document.CreateElement("div");
                element.InnerHtml = favoriteChapter.Body;
                elements.Add(element);
               
            }
            node.Append(elements.ToArray());
            var test = document.ToHtml();
            await File.WriteAllTextAsync($"{favorite.Title}.html", test);
        }

        private async Task GetChapters(Favorite favorite)
        {
            favorite.Chapters = new List<Chapter>();
            var client = new RestClient("https://ranobes.com/")
            {
                CookieContainer = new CookieContainer()
            };
            client.CookieContainer.Add(_session.Cookies);

            var response = await client.ExecuteAsync(new RestRequest(favorite.ChaptersUrl));
            if (!response.IsSuccessful) return;

            var parser = new HtmlParser();
            var navPage = await parser.ParseDocumentAsync(response.Content);

            var navPageCount = navPage.GetElementsByClassName("pages").First().GetElementsByTagName("a").Last()
                .TextContent;
            var pageCount = int.Parse(navPageCount);

            for (int i = 1; i <= pageCount; i++)
            {
                var pageRequest = new RestRequest($"{favorite.ChaptersUrl}/page/{i}/");
                var pageResp = await client.ExecuteAsync(pageRequest);
                var page = await parser.ParseDocumentAsync(pageResp.Content);
                var chaptersUrl = page.GetElementsByClassName("cat_block cat_line")
                    .Select(t => t.GetElementsByTagName("a").First());
                foreach (var chapterUrl in chaptersUrl)
                {
                    var title = chapterUrl.GetAttribute("title");
                    var url = chapterUrl.GetAttribute("href");
                    var textResp = await client.ExecuteAsync(new RestRequest(url));
                    var textPage = await parser.ParseDocumentAsync(textResp.Content);
                    var text = textPage.GetElementById("arrticle");
                    foreach (var scriptElement in text.QuerySelectorAll("script").ToList())
                    {
                        scriptElement.Remove();
                    }
                    foreach (var scriptElement in text.QuerySelectorAll("div").ToList())
                    {
                        scriptElement.Remove();
                    }
                    favorite.Chapters.Add(new Chapter
                    {
                        Title = title,
                        Body = text.InnerHtml
                    });
                }
            }

            ;

        }

        private async Task UpdateInfo(Favorite favorite)
        {
            var client = new RestClient("https://ranobes.com/")
            {
                CookieContainer = new CookieContainer()
            };
            client.CookieContainer.Add(_session.Cookies);

            var request = new RestRequest(favorite.Url);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) return;

            var parser = new HtmlParser();
            var page = await parser.ParseDocumentAsync(response.Content);
            var specElement = page.GetElementsByClassName("r-fullstory-spec").First();
            var descElement = page.GetElementsByClassName("r-desription showcont").First();
            var chapElement = page.GetElementsByClassName("r-fullstory-chapters-foot").First();

            favorite.ChaptersUrl = chapElement.GetElementsByTagName("a").Take(2).Last().GetAttribute("href");
            favorite.Description = descElement.GetElementsByClassName("cont-text showcont-h").First().InnerHtml;
            favorite.Status = specElement.GetElementsByTagName("li").Select(t => t.TextContent).ToList();
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
            var page = await parser.ParseDocumentAsync(response.Content);
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