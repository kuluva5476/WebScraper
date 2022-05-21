using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO.Compression;
using com.charlie.ebook.web.article;

namespace com.charlie.ebook
{
    class ePubBuilder
    {
        private string Title { get; set; }

        public ePubBuilder()
        {
            Title = "查理的新聞匯報-" + System.DateTime.Now.ToString("yyyy-MM-dd"); 
        }
        public void InitFolders()
        {
            try
            {
                Directory.Delete("output", true); 
            }
            catch { }
            XCopy("empty", "output");
        }

        public void Generate()
        {
            XmlDocument xXmlDoc = new XmlDocument();
            string sConfigFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
            xXmlDoc.Load(sConfigFile);

            XmlNodeList xNewsSource = xXmlDoc.SelectNodes("/configurations/config/source");

            List<Article> ArticleList = new List<Article>();

            foreach (XmlNode xNode in xNewsSource)
            {
                string sNewsSource = xNode.Attributes["map_name"].Value;
                Article oIndex = new Article(sNewsSource, 0);
                ArticleList.Add(oIndex);
                oIndex.Save();
            }

            string uid = Guid.NewGuid().ToString();

            #region toc.ncx
            string sNavMap = "";
            int nLevel = 0;
            int nPlayOrder = 0;

            foreach (Article oArticle in ArticleList)
            {
                sNavMap += NavMap(oArticle, nLevel, nPlayOrder);
            }


            string sNavHead = "";
            sNavHead += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
            sNavHead += "<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\">";
            sNavHead += "\t<head>\r\n";
            sNavHead += "\t\t<meta content=\"" + uid + "\" name=\"dtb:uid\"/>\r\n";
            sNavHead += "\t\t<meta content=\"2\" name=\"dtb:depth\"/>\r\n";
            sNavHead += "\t\t<meta content=\"0\" name=\"dtb:totalPageCount\"/>\r\n";
            sNavHead += "\t\t<meta content=\"0\" name=\"dtb:maxPageNumber\"/>\r\n";
            sNavHead += "\t</head>\r\n";
            sNavHead += "\t<docTitle>\r\n";
            sNavHead += "\t\t<text>" + Title + "</text>\r\n";
            sNavHead += "\t</docTitle>\r\n";
            sNavHead += "\t<navMap>\r\n";

            sNavMap = sNavHead + sNavMap + "\t<navMap>\r\n</ncx>\r\n";
            System.IO.File.WriteAllText("output\\OEBPS\\toc.ncx", sNavMap);
            #endregion

            #region content.opf
            string sContentOpf = "";

            // header
            string sContentHead = "";
            sContentHead += "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n";
            sContentHead += "<package unique-identifier=\"BookId\" version=\"2.0\" xmlns=\"http://www.idpf.org/2007/opf\">\r\n";
            sContentHead += "\t<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf=\"http://www.idpf.org/2007/opf\">\r\n";
            sContentHead += "\t\t<dc:identifier id=\"BookId\" opf:scheme=\"UUID\">" + uid + "</dc:identifier>\r\n";
            sContentHead += "\t\t<meta name=\"cover\" content=\"cover-image\" xmlns=\"http://www.idpf.org/2007/opf\"/>\r\n";
            sContentHead += "\t\t<dc:title>" + Title + "</dc:title>\r\n";
            sContentHead += "\t\t<dc:creator>查理</dc:creator>\r\n";
            sContentHead += "\t\t<dc:language>zh-tw</dc:language>\r\n";
            sContentHead += "\t</metadata>\r\n";

            // manifest
            string sManifest = "";

            sManifest += "\t\t<item href=\"toc.ncx\" id=\"ncx\" media-type=\"application/x-dtbncx+xml\"/>\r\n";
            sManifest += "\t\t<item href=\"styles/stylesheet.css\" id=\"main-css\" media-type=\"text/css\"/>\r\n";
            sManifest += "\t\t<item href=\"images/cover.jpg\" id=\"cover-image\" media-type=\"image/jpeg\"/>\r\n";
            sManifest += "\t\t<item href=\"text/coverpage.xhtml\" id=\"coverpage.xhtml\" media-type=\"application/xhtml+xml\"/>\r\n";
            foreach (Article oArticle in ArticleList)
            {
                sManifest += Manifest(oArticle);
            }
            sManifest = "\t<manifest>\r\n" + sManifest + "\t</manifest>\r\n";

            // spine
            string sSpine = "";
            sSpine += "\t\t<itemref idref=\"coverpage.xhtml\" />\r\n";
            foreach (Article oArticle in ArticleList)
            {
                sSpine += Spine(oArticle);
            }
            sSpine = "\t<spine toc=\"ncx\" >\r\n" + sSpine + "\t</spine>\r\n";

            sContentOpf = sContentHead + sManifest + sSpine + "</package>\r\n";
            System.IO.File.WriteAllText("output\\OEBPS\\content.opf", sContentOpf);
            #endregion

            #region Zip
            string sEPub = Title + ".epub";
            if (System.IO.File.Exists(sEPub))
                System.IO.File.Delete(sEPub);
            ZipFile.CreateFromDirectory("output", sEPub);
            #endregion


        }

        #region Helper functions
        private string NavMap(Article _Article, int _Level, int _PlayOrder)
        {
            string sNavMap = "";
            string sIndent = repeat("\t", _Level + 2);
            sNavMap += sIndent + "<navPoint id=\"" + _Article.ArticleId + "\" playOrder=\"" + _PlayOrder.ToString() + "\" >\r\n";
            sNavMap += sIndent + "\t<navLabel>\r\n";
            sNavMap += sIndent + "\t\t<text>" + _Article.Title + "</text>\r\n";
            sNavMap += sIndent + "\t</navLabel>\r\n";
            sNavMap += sIndent + "\t<content src=\"text/" + _Article.Filename + "\" />\r\n";
            foreach (Article oSubArticle in _Article.SubArticles)
            {
                sNavMap += NavMap(oSubArticle, _Level + 1, ++_PlayOrder);
            }
            sNavMap += sIndent + "</navPoint>\r\n";
            return sNavMap;
        }

        private string Spine(Article _Article)
        {
            string sSpine = "";
            sSpine += "\t\t<itemref idref=\"" + _Article.ArticleId + "\" />\r\n";
            foreach (Article oSubArticle in _Article.SubArticles)
            {
                sSpine += Spine(oSubArticle);
            }
            return sSpine;
        }

        private string Manifest(Article _Article)
        {
            string sManifest = "";
            sManifest += "\t\t<item id=\"" + _Article.ArticleId + "\" href=\"text/" + _Article.Filename + "\" media-type=\"application/xhtml+xml\" />\r\n";
            foreach (Article oSubArticle in _Article.SubArticles)
            {
                sManifest += Manifest(oSubArticle);
            }
            return sManifest;
        }

        public bool XCopy(string SourcePath, string DestinationPath)
        {
            SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
            DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";

            try
            {
                if (Directory.Exists(SourcePath))
                {
                    if (Directory.Exists(DestinationPath) == false)
                    {
                        Directory.CreateDirectory(DestinationPath);
                    }

                    foreach (string files in Directory.GetFiles(SourcePath))
                    {
                        FileInfo fileInfo = new FileInfo(files);
                        fileInfo.CopyTo(string.Format(@"{0}\{1}", DestinationPath, fileInfo.Name), true);
                    }

                    foreach (string drs in Directory.GetDirectories(SourcePath))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(drs);
                        if (XCopy(drs, DestinationPath + directoryInfo.Name) == false)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch // (Exception ex)
            {
                return false;
            }
        }

        public string repeat(string _value, int _times)
        {
            string s = "";
            for (int i = 0; i < _times; i++)
            {
                s += _value;
            }
            return s;
        }
        #endregion
    }
}
