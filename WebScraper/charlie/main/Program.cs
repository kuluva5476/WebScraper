using System;
using com.charlie.ebook;
using com.charlie.ebook.web.article;

namespace com.charlie.main
{
    class Program
    {
        static string _OutputFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output" + System.DateTime.Now.ToString("yyyyMMdd") + ".txt");
        private static void ClearOutput()
        {
            System.IO.File.WriteAllText(_OutputFile, "");
        }

        public static void AddText(string _Text="\r\n")
        {
            System.IO.File.AppendAllText(_OutputFile, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss\t" + _Text + "\r\n"));
        }

        static void Main(string[] args)
        {
            ePubBuilder oEpub = new ePubBuilder();
            oEpub.Generate();

            #region Hard coded tests, to generate single article file

            string sNewsSource = "";
            Article oArticle;
            string sUrl = "";

            //sNewsSource = "am730";
            //sUrl = "https://www.am730.com.hk/%E8%B2%A1%E7%B6%93/%E7%BE%8E%E5%9C%8B%E6%95%B8%E6%93%9A-%E7%BE%8E5%E6%9C%88%E9%9B%B6%E5%94%AE%E6%84%8F%E5%A4%96%E8%B7%8C0.3-%E6%B1%BD%E8%BB%8A%E9%8A%B7%E5%94%AE%E6%B8%9B%E6%8B%96%E7%B4%AF/324175";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "rthk";
            //sUrl = "https://news.rthk.hk/rthk/ch/component/k2/1649304-20220519.htm?spTabChangeable=0";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "ming-pao";
            //sUrl = "https://news.mingpao.com/pns/%e8%a6%81%e8%81%9e/article/20220516/s00001/1652638268562/60%e6%ad%b2%e4%bb%a5%e4%b8%8b%e6%88%90%e4%ba%ba-%e5%b0%88%e5%ae%b6%e5%80%a1%e5%85%8d%e3%80%8c%e7%96%ab%e8%8b%97%e9%80%9a%e3%80%8d-%e4%bb%8d%e7%b1%b2%e9%95%b7%e8%80%85%e6%89%933%e8%87%b34%e9%87%9d-%e4%b8%8d%e8%b4%8a%e6%88%90%e5%90%91%e5%b9%b4%e8%bc%95%e6%88%90%e4%ba%ba%e3%80%8c%e8%84%85%e8%bf%ab%e6%80%a7%e3%80%8d%e8%b0%b7%e9%87%9d";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "cna";
            //sUrl = "https://www.cna.com.tw/news/ahel/202205185009.aspx";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "udn-game";
            //sUrl = "https://game.udn.com/game/story/122089/6392022";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "toy-people";
            //sUrl = "https://www.toy-people.com/?p=72253";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "techbang";
            //sUrl = "https://www.techbang.com/posts/97141-the-strongest-bald-head-in-history-one-punch-man-will-be?from=home_news";
            ////sUrl = "https://www.techbang.com/posts/97193-acer-built-the-predator-esports-pop-up-store";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            //sNewsSource = "weekendhk-go";
            //sUrl = "https://www.weekendhk.com/%e9%a6%99%e6%b8%af%e5%a5%bd%e5%8e%bb%e8%99%95/%e6%a8%82%e5%af%8c%e5%bb%a3%e5%a0%b4-%e5%b8%82%e9%9b%86-%e9%9c%b2%e7%87%9f-%e5%92%96%e5%95%a1-js02-1329832/3/";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();

            #endregion
        }
    }
}
