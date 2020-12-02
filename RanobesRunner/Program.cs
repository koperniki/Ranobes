using Ranobes;

var session = new Session("koperniki", "org100h");
await session.Login();
var crawler = new Crawler(session);
await crawler.Process();

