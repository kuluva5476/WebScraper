using System;
using com.charlie.ebook;
using com.charlie.ebook.web.article;

namespace com.charlie.main
{
    class Program
    {
        static string _OutputFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output.txt");
        private static void ClearOutput()
        {
            System.IO.File.WriteAllText(_OutputFile, "");
        }

        private static void AddText(string _Text="\r\n")
        {
            System.IO.File.AppendAllText(_OutputFile, _Text + "\r\n");
        }

        static void Main(string[] args)
        {
            ePubBuilder oEpub = new ePubBuilder();
            oEpub.InitFolders();
            oEpub.Generate();

            #region Single Article Tests
            //sNewsSource = "am730";
            //sUrl = "https://www.am730.com.hk/%E6%9C%AC%E5%9C%B0/%E7%96%AB%E6%83%85%E7%A9%A9%E5%AE%9A-%E9%98%B2%E8%AD%B7%E4%B8%AD%E5%BF%83%E7%BA%8C%E6%9F%A5%E7%A7%81%E6%88%BF%E8%8F%9C%E7%BE%A4%E7%B5%84-%E7%B1%B2%E5%B8%82%E6%B0%91%E6%94%BE%E5%BF%83%E6%AF%8B%E9%A0%88%E6%80%A5%E8%81%9A%E9%A4%90/320084";
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

            //sNewsSource = "toy-people";
            //sUrl = "https://www.toy-people.com/?p=71484";
            //oArticle = new Article(sUrl, sNewsSource, 100);
            //oArticle.Save();
            #endregion
        }
    }
}
