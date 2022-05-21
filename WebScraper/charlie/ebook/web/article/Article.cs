using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace com.charlie.ebook.web.article
{
    class Article
    {
        private int ArticleNo { get; set; }
        public string ArticleUrl { get; private set; }
        public string MapName { get; private set; }
        public string Title { get; private set; }
        public string Author { get; private set; }
        public string PubDate { get; private set; }
        public string ModDate { get; private set; }
        public string Summary { get; private set; }
        public string Content { get; private set; }
        public string MediaType { get; set; }

        public string Filename { get; private set; }

        public List<Article> SubArticles = new List<Article>();


        public string ArticleId { get; set; }

        private ArticleMapper _Mapper = new ArticleMapper();

        private HtmlDocument _Html = new HtmlDocument();

        /// <summary>
        /// Empty class
        /// </summary>
        /// <param name="_MapName">Mapper</param>
        /// <param name="_ArticleNum">ID</param>
        public Article(string _MapName, int _ArticleNum)
        {
            ArticleNo = _ArticleNum;
            ArticleId = _MapName + _ArticleNum.ToString("000");

            _Mapper.LoadMapper(_MapName);
            PubDate = System.DateTime.Now.ToString("yyyy.MM.dd HH:mm");
            Filename = ArticleId + ".xhtml";
            CreateIndex();
        }

        /// <summary>
        /// Load content from URL
        /// </summary>
        /// <param name="_Url">URL of the article</param>
        /// <param name="_MapName">Mapper</param>
        /// <param name="_ArticleId">ID</param>
        public Article(string _Url, string _MapName, int _ArticleId)
        {
            ArticleNo = _ArticleId;

            ArticleId = _MapName + _ArticleId.ToString("000");
            _Mapper.LoadMapper(_MapName);
            ArticleUrl = _Url;
            Filename = ArticleId + ".xhtml";
            Parse();
        }

        public void LoadHtml()
        {
            _Html.LoadHtml(LoadHtmlAsync(ArticleUrl).Result);
            _Html.Save("Loaded.html");
        }
        private async Task<string> LoadHtmlAsync(string _Url)
        {
            IWebDriver _Driver;
            var options = new ChromeOptions();
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArgument("start-minimized");
            options.AddArgument("--window-position=-32000,-32000");
            options.AddArgument("--log-level=5");
            _Driver = new ChromeDriver(options);

            _Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(100);
            _Driver.Navigate().GoToUrl(_Url);
            //WebDriverWait wait = new WebDriverWait(_Driver, TimeSpan.FromSeconds(100));
            
            string sPageSource = _Driver.PageSource;
            _Driver.Close();
            return sPageSource;
        }

        private void CreateIndex()
        {
            Title = _Mapper.Caption;
            Content = "";

            List<string> sUrls = GetArticleUrls();

            int nIndex = 1;
            SubArticles.Clear();
            string sTable = "";
            sTable += "<table class=\"articleTable\">\r\n";
            foreach (string s in sUrls)
            {
                System.Threading.Tasks.Task.Delay(10000);

                try
                {
                    Article oArticle = new Article(s, _Mapper.MapName, nIndex++);
                    oArticle.Save();
                    SubArticles.Add(oArticle);

                    sTable += "\t<tr><td><a href=\"" + oArticle.Filename + "\" >" + oArticle.Title + "</a></td></tr>\r\n";
                }
                catch (Exception ex) 
                { 
                }
            }
            sTable += "\t<tr /><td /></table>\r\n";

            Content = sTable;
        }

        private List<string> GetArticleUrls()
        {
            List<string> ArticleItems = new List<string>();

            HtmlDocument oHtml = new HtmlDocument();
            oHtml.LoadHtml(LoadHtmlAsync(_Mapper.IndexUrl).Result);

            HtmlNodeCollection xNodeList = oHtml.DocumentNode.SelectNodes(_Mapper.IndexListXPath);

            foreach (HtmlNode oNode in xNodeList)
            {
                HtmlNode oArticleNode = oNode.SelectSingleNode(_Mapper.IndexItemXPath);
                if (oArticleNode != null)
                {
                    string sHref = oArticleNode.Attributes["href"].Value;
                    // Make it absolute url
                    Uri oBase = new Uri(_Mapper.IndexUrl);
                    sHref = (new Uri(oBase, sHref)).AbsoluteUri;
                    string sTitle = oArticleNode.InnerText;
                    bool bContinue = false;

                    // Filter unwanted...
                    foreach (string sFilter in _Mapper.IndexItemFilter)
                    {
                        if (oArticleNode.OuterHtml.Contains(sFilter))
                        //if (sTitle.Contains(sFilter))
                            bContinue = true;
                    }

                    if (bContinue)
                        continue;

                    ArticleItems.Add(sHref);
                }
            }
            return ArticleItems;
        }

        private string DownloadImage(string _Url, string _ImageIndex)
        {
            WebClient oDownloader = new WebClient();
            // Get file extension
            string sExtension = _Url.Substring(_Url.LastIndexOf(".")).Split("?")[0];

            string sReturnFileName = "";
            string sDownloadedName = ArticleId + "_" + _ImageIndex + sExtension;
            try
            {
                oDownloader.DownloadFile(_Url, "output\\OEBPS\\images\\" + sDownloadedName);
                sReturnFileName = sDownloadedName;
            }
            catch
            { }

            return sReturnFileName;
        }

        // Recursively parse div nodes
        private string ParseRecurse(HtmlNode _RootNode)
        {
            string sReturn = "";

            // If script node, do nothing
            if (_RootNode.Name.ToLower() != "script")
            {
                bool bContinue = true;
                foreach (string sFilter in _Mapper.ContentFilters)
                {
                    if (_RootNode.OuterHtml.Contains(sFilter))
                        bContinue = false;
                }
                if (bContinue)
                {
                    // If has childs then recursive
                    if (_RootNode.HasChildNodes)
                    {
                        // assuming these are end nodes
                        if (_RootNode.Name.ToLower() == "p" || _RootNode.Name.ToLower() == "h2" || _RootNode.Name.ToLower() == "h3")
                        {
                            sReturn += "<p>";
                            foreach (HtmlNode oInnerNode in _RootNode.ChildNodes)
                            {
                                if (oInnerNode.Name == "a")
                                    sReturn += "<b>" + oInnerNode.InnerText + "</b> (<u>" + oInnerNode.Attributes["href"].Value + "</u>)";
                                else if (oInnerNode.Name == "#text")
                                    sReturn += oInnerNode.InnerText;
                                else
                                    sReturn += "<b>" + oInnerNode.InnerText + "</b>";
                            }
                            sReturn += "</p>\r\n";
                        }
                        else
                            foreach (HtmlNode oChildNode in _RootNode.ChildNodes)
                            {
                                sReturn += ParseRecurse(oChildNode);
                            }
                    }
                    else
                    {
                        if (_RootNode.InnerText.Trim() != "")
                            sReturn = _RootNode.InnerText.Trim();
                    }
                }
            }
            return sReturn;
        }

        private void TestParse()
        { 
        
        }

        private void Parse()
        {
            LoadHtml();
            Title = _Mapper.Title != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.Title).InnerText.Trim() : "";
            Author = _Mapper.Author != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.Author).InnerText.Trim() : "";
            PubDate = _Mapper.PubDate != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.PubDate).InnerText.Trim() : "";
            Summary = _Mapper.Summary != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.Summary).InnerText.Trim() : "";

            HtmlNode oContentRoot = _Html.DocumentNode.SelectSingleNode(_Mapper.Content);
            HtmlNodeCollection oChildNodes = oContentRoot.ChildNodes;

            int nImageIndex = 1;
            Content = "";
            string sTable = "";

            if (_Mapper.TopImage != "")
            {
                HtmlNode xRootNode = _Html.DocumentNode.SelectSingleNode(_Mapper.TopImage);

                if (xRootNode != null)
                {
                    string[] sImageSource = _Mapper.TopImageSource.Split(" --> ");
                    string[] sImageDescSource = _Mapper.TopImageDesc.Split(" --> ");
                    HtmlNodeCollection xImageNodes = xRootNode.SelectNodes(sImageSource[0]);
                    HtmlNodeCollection xImageDescNodes = xRootNode.SelectNodes(sImageDescSource[0]);
                    if (xImageNodes != null)
                    {
                        for (int i = 0; i < xImageNodes.Count; i++)
                        {
                            HtmlNode xImageNode = xImageNodes[i];
                            string sImageIndex = "top_" + (i + 1).ToString("000");
                            string sImageUrl = xImageNode.Attributes[sImageSource[1]].Value;
                            string sImageDesc = sImageDescSource.Length == 2 ? xImageDescNodes[i].Attributes[sImageDescSource[1]].Value : xImageDescNodes[i].InnerHtml;

                            string sImageFileName = DownloadImage(sImageUrl, sImageIndex);
                            sTable += "<table class=\"imageTable\"><tr><td><img src=\"../images/" + sImageFileName + "\" /></td></tr>" + ((sImageDesc.Trim() != "") ? "<tr><td>" + sImageDesc + "</td></tr>" : "") + "</table>\r\n<p />\r\n";
                        }
                    }
                }
            }
            Content += sTable;

            foreach (HtmlNode oChildNode in oChildNodes)
            {
                // Try select image node and see if exists
                string[] sImageMapperTokens = _Mapper.ContentImageSource.Split(" --> "); // 0: wrapper, 1: xpath, 2: attribute
                string sWrapperName = sImageMapperTokens[0];
                string sImageXPath = sImageMapperTokens[1];
                string sImageAttribute = sImageMapperTokens[2];

                bool bImageDetected = false;
                if (oChildNode.Name == sWrapperName)
                {
                    sTable = "";
                    if (sImageXPath != "")
                    {
                        HtmlNodeCollection xImageNodes = oChildNode.SelectNodes(sImageXPath);
                        // Image detected, this ChildNode is the wrapper
                        if (xImageNodes != null)
                        {
                            string[] sImageDescSource = _Mapper.ContentImageDesc.Split(" --> ");

                            HtmlNodeCollection xImageDescNodes = oChildNode.SelectNodes(sImageDescSource[0]);

                            for (int i = 0; i < xImageNodes.Count; i++)
                            {
                                HtmlNode xImageNode = xImageNodes[i];

                                string sImageUrl = xImageNode.Attributes[sImageAttribute].Value;
                                string sDescXPath = sImageDescSource[1];
                                string sImageDesc = xImageDescNodes[i].Attributes[sDescXPath].Value;

                                string sDownloadedName = ArticleId + "_" + nImageIndex.ToString("000");
                                string sReturnedFileName = DownloadImage(sImageUrl, sDownloadedName);

                                if (sReturnedFileName != "")
                                {
                                    sTable += "<table class=\"imageTable\"><tr><td><img src=\"../images/" + sReturnedFileName + "\" /></td></tr>\r\n" + ((sImageDesc.Trim() != "") ? "<tr><td>" + sImageDesc + "</td></tr>" : "") + "</table>\r\n<p />\r\n";
                                    nImageIndex++;
                                    bImageDetected = true;
                                }
                            }
                            Content += sTable;
                        }
                    }
                    // No wrapper, must be img tag...
                    else if (sImageAttribute != "")
                    {
                        string[] sImageDescSource = _Mapper.ContentImageDesc.Split(" --> ");

                        string sImageUrl = oChildNode.Attributes[sImageAttribute].Value;
                        string sDescXPath = sImageDescSource[1];
                        string sImageDesc = oChildNode.Attributes[sDescXPath]?.Value;

                        string sDownloadedName = ArticleId + "_" + nImageIndex.ToString("000");
                        string sReturnedFileName = DownloadImage(sImageUrl, sDownloadedName);

                        if (sReturnedFileName != "")
                        {
                            sTable += "<table class=\"imageTable\"><tr><td><img src=\"../images/" + sReturnedFileName + "\" /></td></tr>\r\n" + ((sImageDesc.Trim() != "") ? "<tr><td>" + sImageDesc + "</td></tr>" : "") + "</table>\r\n<p />\r\n";
                            nImageIndex++;
                            bImageDetected = true;
                        }
                    }
                }

                if (!bImageDetected)
                {
                    Content += ParseRecurse(oChildNode);
                }
            }
        }

        public void Save()
        {
            string sXhtmlContents = "";

            sXhtmlContents += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
            sXhtmlContents += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\"\r\n";
            sXhtmlContents += "\"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\r\n";
            sXhtmlContents += "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"zh-tw\">\r\n";
            sXhtmlContents += "<head>\r\n";
            sXhtmlContents += "  <link href=\"../styles/stylesheet.css\" rel=\"stylesheet\" type=\"text/css\"/>\r\n";
            sXhtmlContents += "  <title>" + Title + "</title>\r\n";
            sXhtmlContents += "</head>\r\n";
            sXhtmlContents += "<body>\r\n";
            sXhtmlContents += "<table class=\"articleTitle\"><tr><td>" + Title + "</td></tr>\r\n";
            sXhtmlContents += "<tr><td>" + PubDate + "</td></tr></table>\r\n";
            sXhtmlContents += Summary == ""? "<p>" + Summary + "</p>\r\n" : "";
            sXhtmlContents += Content + "\r\n";
            sXhtmlContents += Author == ""? "<p>" + Author + "</p>\r\n" : "";
            sXhtmlContents += "</body></html>";

            string sFilename = "output\\OEBPS\\text\\" + Filename;
            System.IO.File.WriteAllText(sFilename, sXhtmlContents);
        }
    }
}
