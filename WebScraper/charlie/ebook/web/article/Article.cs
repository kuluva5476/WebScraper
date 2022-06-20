using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace com.charlie.ebook.web.article
{
    class Article
    {
        private string _AppDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        private int _Max = 50;

        private int ArticleNo { get; set; }
        public string ArticleUrl { get; private set; }
        public string MapName { get; private set; }
        public string Title { get; private set; }
        public string Author { get; private set; }
        public string PubDate { get; private set; }
        public string ModDate { get; private set; }
        public string Summary { get; private set; }
        public string Content { get; private set; }
        public string Filename { get; private set; }

        private bool _Index = false;

        private int _ImageIndex = 1;

        private string _Source = "";

        public List<Article> SubArticles = new List<Article>();

        public string ArticleId { get; set; }

        private ArticleMapper _Mapper = new ArticleMapper();

        private HtmlDocument _Html = new HtmlDocument();

        /// <summary>
        /// Index
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
            _Index = true;
        }

        /// <summary>
        /// Load content from URL
        /// </summary>
        /// <param name="_Url">URL of the article</param>
        /// <param name="_MapName">Mapper</param>
        /// <param name="_ArticleNum">ID</param>
        public Article(string _Url, string _MapName, int _ArticleNum)
        {
            ArticleNo = _ArticleNum;

            ArticleId = _MapName + _ArticleNum.ToString("000");
            _Mapper.LoadMapper(_MapName);
            ArticleUrl = _Url;
            Filename = ArticleId + ".xhtml";
            _Source = "(" + _Mapper.Caption + ") ";
            try
            {
                Parse();
            }
            catch (Exception ex)
            {
                
            }
            _Index = false;
        }


        /// <summary>
        /// Parse Child Nodes
        /// </summary>
        /// <param name="_RootNode"></param>
        /// <returns></returns>
        public string RecursiveParse(HtmlNode _RootNode)
        {
            string sReturn = "";
            bool bContinue = true;
            // If script node, do nothing
            if (_RootNode.Name.ToLower() == "script")
                bContinue = false;
            // Check for filters (Ads, ad news)
            foreach (string sFilter in _Mapper.ContentFilters)
            {
                string sAttributes = "";
                foreach (HtmlAttribute oAttribute in _RootNode.Attributes)
                    sAttributes += oAttribute.Value + " ";

                if (sAttributes.Contains(sFilter))
                    bContinue = false;
            }

            if (bContinue)
            {
                string sTable = "";
                string[] sImageMapperTokens = _Mapper.ContentImageSource.Split(" --> "); // 0: wrapper, 1: xpath, 2: attribute, 3: alternative
                string sWrapperName = sImageMapperTokens[0];
                string sImageXPath = sImageMapperTokens[1];
                string sImageAttribute = sImageMapperTokens[2];
                string sAltImageAttribute = sImageMapperTokens[3];

                bool bImageDetected = false;
                // Check for image

                // No wrapper tag, plain img tag
                if (sWrapperName == "" && _RootNode.Name.ToLower() == "img")
                {
                    if (sImageAttribute != "")
                    {
                        string[] sImageDescSource = _Mapper.ContentImageDesc.Split(" --> ");

                        string sImageUrl;
                        sImageUrl = _RootNode.Attributes[sImageAttribute]?.Value;
                        if (sImageUrl == null)
                            sImageUrl = _RootNode.Attributes[sAltImageAttribute]?.Value;

                        string sDescXPath = sImageDescSource[1];
                        string sImageDesc = _RootNode.Attributes[sDescXPath]?.Value;
                        sImageDesc = (sImageDesc == null ? "" : sImageDesc);

                        string sDownloadedName = ArticleId + "_" + _ImageIndex.ToString("000");
                        string sReturnedFileName = DownloadImage(sImageUrl, sDownloadedName);

                        if (sReturnedFileName != "")
                        {
                            sTable += "<table class=\"imageTable\"><tr><td><img src=\"../images/" + sReturnedFileName + "\" /></td></tr>\r\n" + ((sImageDesc.Trim() != "") ? "<tr><td>" + sImageDesc + "</td></tr>" : "") + "</table>\r\n";
                            _ImageIndex++;
                            bImageDetected = true;
                        }
                    }
                }
                else
                {
                    if (_RootNode.Name == sWrapperName)
                    {
                        sTable = "";
                        if (sImageXPath != "")
                        {
                            HtmlNodeCollection xImageNodes = _RootNode.SelectNodes(sImageXPath);
                            // Image detected, this ChildNode is the wrapper
                            if (xImageNodes != null)
                            {
                                string[] sImageDescSource = _Mapper.ContentImageDesc.Split(" --> ");

                                HtmlNodeCollection xImageDescNodes = _RootNode.SelectNodes(sImageDescSource[0]);

                                for (int i = 0; i < xImageNodes.Count; i++)
                                {
                                    HtmlNode xImageNode = xImageNodes[i];

                                    string sImageUrl;
                                    sImageUrl = xImageNode.Attributes[sImageAttribute]?.Value;
                                    if (sImageUrl == null)
                                        sImageUrl = xImageNode.Attributes[sAltImageAttribute]?.Value;
                                    string sDescXPath = sImageDescSource.Length == 2 ? sImageDescSource[1] : "";
                                    string sImageDesc = "";


                                    if (xImageDescNodes[i] != null)
                                        sImageDesc = sImageDescSource.Length == 2 ? xImageDescNodes[i].Attributes[sDescXPath].Value : xImageDescNodes[i].InnerHtml;
                                    //sImageDesc = xImageDescNodes[i].Attributes[sDescXPath]?.Value;

                                    string sDownloadedName = ArticleId + "_" + _ImageIndex.ToString("000");
                                    string sReturnedFileName = DownloadImage(sImageUrl, sDownloadedName);

                                    if (sReturnedFileName != "")
                                    {
                                        sTable += "<table class=\"imageTable\"><tr><td><img src=\"../images/" + sReturnedFileName + "\" /></td></tr>\r\n" + ((sImageDesc.Trim() != "") ? "<tr><td>" + sImageDesc + "</td></tr>" : "") + "</table>\r\n";
                                        _ImageIndex++;
                                        bImageDetected = true;
                                    }
                                }
                                Content += sTable;
                            }
                        }
                    }
                }
                // if image detected
                if (bImageDetected)
                    sReturn += sTable;
                // ok, not image
                else //(!bImageDetected)
                {
                    // If has childs then recursive
                    if (_RootNode.HasChildNodes)
                    {
                        // assuming these are end nodes
                        if (_RootNode.Name.ToLower() == "h2" || _RootNode.Name.ToLower() == "h3")
                        {
                            sReturn += "<p>";
                            foreach (HtmlNode oInnerNode in _RootNode.ChildNodes)
                            {
                                if (oInnerNode.Name == "a")
                                    sReturn += "<b>" + oInnerNode.InnerText + "</b>";
                                else if (oInnerNode.Name == "#text")
                                    sReturn += oInnerNode.InnerText;
                                else
                                    sReturn += "<b>" + oInnerNode.InnerText + "</b>";
                            }
                            sReturn += "</p>\r\n";
                        }
                        // if p tags wrap the child nodes around
                        else if (_RootNode.Name.ToLower() == "p")
                        {
                            sReturn += "<p>";
                            foreach (HtmlNode oChildNode in _RootNode.ChildNodes)
                                sReturn += RecursiveParse(oChildNode);
                            sReturn += "</p>\r\n";
                        }
                        // others just send child nodes to parse
                        else
                            foreach (HtmlNode oChildNode in _RootNode.ChildNodes)
                                sReturn += RecursiveParse(oChildNode);
                    }
                    // No childs, just get the text
                    else
                    {
                        if (_RootNode.InnerText.Trim() != "")
                            sReturn = _RootNode.InnerText.Trim();
                        else
                        {
                            if (_RootNode.Name.ToLower() == "br")
                                sReturn = "<br />";
                        }
                    }
                }
            }
            return sReturn;
        }

        /// <summary>
        /// Call chromium, navigate to the url and wait until loaded, or until time-out
        /// </summary>
        /// <param name="_Url">Url to be loaded</param>
        /// <returns>HTML code of the loaded url</returns>
        private async Task<string> LoadHtmlAsync(string _Url)
        {
            IWebDriver _Driver;
            var options = new ChromeOptions();
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArgument("start-minimized");
            options.AddArgument("--window-position=-32000,-32000");
            options.AddArgument("--log-level=5");
            string sPageSource = "";
            try
            {
                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;

                _Driver = new ChromeDriver(service, options);
                
                _Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(100);
                _Driver.Navigate().GoToUrl(_Url);

                WebDriverWait wait = new WebDriverWait(_Driver, TimeSpan.FromSeconds(10));
                wait.Until(_Driver => _Driver.FindElement(By.TagName("div")));

                sPageSource = _Driver.PageSource;
                _Driver.Quit();
            }
            catch (Exception ex)
            { 
            
            }
            
            return sPageSource;
        }

        private void CreateIndex()
        {
            Title = _Mapper.Caption;
            Content = "";

            List<string> sUrls = GetArticleUrls();
            
            //string sCheckDate = System.DateTime.Now.ToString("yyyyMMdd");

            int nIndex = 1;
            SubArticles.Clear();

            // Do the date filter here...
            foreach (string s in sUrls)
            {
                System.Threading.Tasks.Task.Delay(10000);

                try
                {
                    Article oArticle = new Article(s, _Mapper.MapName, nIndex++);
                    if (oArticle.Content == "")
                        continue;

                    //DateTime oArticleDate;
                    //DateTime.TryParse(oArticle.PubDate.Split(" ")[0].Split("星期")[0], out oArticleDate);
                    //if ((oArticleDate.ToString("yyyyMMdd") != sCheckDate) && _Mapper.CheckArticleDate)
                    //    break;

                    // "Stack" into SubArticle queue
                    SubArticles.Insert(0, oArticle);
                }
                catch (Exception ex) 
                { 
                }
            }

            // Generate index in reverse order
            string sTable = "";
            foreach (Article oArticle in SubArticles)
            {
                sTable += "\t<tr><td><a href=\"" + oArticle.Filename + "\" >" + oArticle.Title + "</a></td></tr>\r\n";
            }

            if (sTable != "")
            {
                sTable = "<table class=\"articleTable\">\r\n" + sTable + "</table>\r\n";
            }

            Content = sTable;
        }

        /// <summary>
        /// Grabs all article urls from main page
        /// until hit last-visited article, or reach maximum number of articles
        /// </summary>
        /// <returns></returns>
        private List<string> GetArticleUrls()
        {
            List<string> ArticleItems = new List<string>();

            HtmlDocument oHtml = new HtmlDocument();
            oHtml.LoadHtml(LoadHtmlAsync(_Mapper.IndexUrl).Result);

            HtmlNodeCollection xNodeList;
            xNodeList = oHtml.DocumentNode.SelectNodes(_Mapper.IndexListXPath);
            if (xNodeList != null)
            {
                foreach (HtmlNode oNode in xNodeList)
                {
                    // Don't want to grab all...
                    if (ArticleItems.Count >= _Max)
                        break;

                    HtmlNode oArticleNode = _Mapper.IndexItemXPath == "" ? oNode : oNode.SelectSingleNode(_Mapper.IndexItemXPath);
                    if (oArticleNode != null)
                    {
                        string sHref = oArticleNode.Attributes["href"].Value;
                        // Make it absolute url
                        Uri oBase = new Uri(_Mapper.IndexUrl);
                        sHref = (new Uri(oBase, sHref)).AbsoluteUri;

                        // Been here before? then stop here
                        if (sHref == _Mapper.LastVisit)
                            break;

                        bool bContinue = false;

                        // Filter unwanted...
                        foreach (string sFilter in _Mapper.IndexItemFilter)
                        {
                            if (oArticleNode.OuterHtml.Contains(sFilter))
                                bContinue = true;
                        }

                        if (bContinue)
                            continue;

                        ArticleItems.Add(sHref);
                    }
                }
            }
            return ArticleItems;
        }

        /// <summary>
        /// Download image to intermidiate output folder
        /// </summary>
        /// <param name="_Url">Url of the image</param>
        /// <param name="_ImageIndex">Counter</param>
        /// <returns></returns>
        private string DownloadImage(string _Url, string _ImageIndex)
        {
            WebClient oDownloader = new WebClient();
            // Get file extension
            string sExtension = _Url.Substring(_Url.LastIndexOf(".")).Split("?")[0].Split("&")[0];

            if (sExtension.ToLower() == ".svg")
                return "";

            string sReturnFileName = "";
            string sDownloadedName = ArticleId + "_" + _ImageIndex + sExtension;
            try
            {
                oDownloader.DownloadFile(_Url, _AppDir + "output\\OEBPS\\images\\" + sDownloadedName);
                sReturnFileName = sDownloadedName;
            }
            catch
            { }

            return sReturnFileName;
        }

        
        /// <summary>
        /// Parse article informations (Title, Author, Publish Date, Title),
        /// then call RecursiveParse to get contents of the article
        /// </summary>
        private void Parse()
        {
            //LoadHtml();
            _Html.LoadHtml(LoadHtmlAsync(ArticleUrl).Result);
            //_Html.Save("loaded.html");
            Content = "";
            Title = "";
            try
            {
                Title = _Mapper.Title != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.Title)?.InnerText.Trim() : "";
                Author = _Mapper.Author != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.Author)?.InnerText.Trim() : "";
                PubDate = _Mapper.PubDate != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.PubDate)?.InnerText.Trim() : "";
                Summary = _Mapper.Summary != "" ? _Html.DocumentNode.SelectSingleNode(_Mapper.Summary)?.InnerText.Trim() : "";
            }
            catch { }
            if (Title == "" || Title == null)
                return;

            HtmlNode oContentRoot = _Html.DocumentNode.SelectSingleNode(_Mapper.Content);
            HtmlNodeCollection oChildNodes = oContentRoot.ChildNodes;

            Content = "";
            string sTable = "";

            // Some article puts first image separately from content block, grab this first
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


                            sTable += "<table class=\"imageTable\"><tr><td><img src=\"../images/" + sImageFileName + "\" /></td></tr>" + ((sImageDesc.Trim() != "") ? "<tr><td>" + sImageDesc + "</td></tr>" : "") + "</table>\r\n";
                        }
                    }
                }
            }
            Content += sTable;

            // Parse the rest thru RecursiveParse function
            foreach (HtmlNode oChildNode in oContentRoot.ChildNodes)
                Content += RecursiveParse(oChildNode);
        }

        /// <summary>
        /// Save the article to intermediate output folder
        /// </summary>
        public void Save()
        {
            string sXhtmlContents = "";

            sXhtmlContents += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
            sXhtmlContents += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\"\r\n";
            sXhtmlContents += "\"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\r\n";
            sXhtmlContents += "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"zh-tw\">\r\n";
            sXhtmlContents += "<head>\r\n";
            sXhtmlContents += "  <link href=\"../styles/stylesheet.css\" rel=\"stylesheet\" type=\"text/css\"/>\r\n";
            sXhtmlContents += "  <title>" + _Source + Title + "</title>\r\n";
            sXhtmlContents += "</head>\r\n";
            sXhtmlContents += "<body>\r\n";
            sXhtmlContents += "<table class=\"articleTitle\"><tr><td>" + _Source + Title + "</td></tr>\r\n";
            sXhtmlContents += "<tr><td>" + PubDate + "</td></tr></table>\r\n";
            sXhtmlContents += (Summary != "" && Summary != null)? "<p>" + Summary + "</p>\r\n" : "";
            sXhtmlContents += Content + "\r\n";
            sXhtmlContents += (Author != "" && Author != null)? "<p>" + Author + "</p>\r\n" : "";
            sXhtmlContents += "</body></html>";

            string sFilename = _AppDir + "output\\OEBPS\\text\\" + Filename;
            System.IO.File.WriteAllText(sFilename, sXhtmlContents);

            // Update last visited url
            if (!_Index)
            {
                _Mapper.LastVisit = ArticleUrl;
                _Mapper.Save();
            }
        }
    }
}
