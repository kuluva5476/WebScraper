using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO.Compression;
using com.charlie.ebook.web.article;
using System.Drawing;

namespace com.charlie.ebook
{
    class ePubBuilder
    {
        private string Title { get; set; }
        private string OutputPath { get; set; }
        private string _AppDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        List<string> _ImageList = new List<string>();

        public ePubBuilder()
        {
            Title = "查理的新聞匯報 " + System.DateTime.Now.ToString("yyyy-MM-dd"); 
        }

        private void InitFolders()
        {
            Directory.Delete(_AppDir + "output", true); 
            XCopy(_AppDir + "empty", _AppDir + "output");
        }


        /// <summary>
        /// Save article to output folder
        /// Recursive loop for sub-articles
        /// </summary>
        /// <param name="_Article"></param>
        private void RecursiveSave(Article _Article)
        {
            _Article.Save();
            foreach (Article oSubArticle in _Article.SubArticles)
            {
                RecursiveSave(oSubArticle);
            }
        }

        /// <summary>
        /// Generate...
        /// </summary>
        public void Generate()
        {
            // Copy template folder structures to intermidiate output
            InitFolders();

            // Read config file
            XmlDocument xXmlDoc = new XmlDocument();
            string sConfigFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
            xXmlDoc.Load(sConfigFile);

            XmlNode xEpub = xXmlDoc.SelectSingleNode("/configurations/config/epub");
            Title = xEpub.Attributes["title"].Value + System.DateTime.Now.ToString("yyyy-MM-dd");
            OutputPath = xEpub.Attributes["output_path"].Value;

            XmlNodeList xNewsSource = xXmlDoc.SelectNodes("/configurations/config/source");
            List<Article> IndexList = new List<Article>();

            // Loop through every News Source defined in config file
            foreach (XmlNode xNode in xNewsSource)
            {
                string sNewsSource = xNode.Attributes["map_name"].Value;
                
                // Top-level index, will grab all urls from source and populate sub-articles
                Article oIndex = new Article(sNewsSource, 0);
                if (oIndex.SubArticles.Count > 0)
                {
                    IndexList.Add(oIndex);
                }
            }

            // Save them all to intermidiate output folder
            foreach (Article oArticle in IndexList)
            {
                RecursiveSave(oArticle);
            }

            // Generate UID for e-pub
            string uid = Guid.NewGuid().ToString();

            #region Generate toc.ncx (Table of Content)
            string sNavMap = "";
            int nIndentLevel = 0;
            int nPlayOrder = 0;

            foreach (Article oArticle in IndexList)
            {
                sNavMap += NavMap(oArticle, nIndentLevel, nPlayOrder);
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
            sNavHead += "\t\t<navPoint id=\"coverpage\">\r\n";
            sNavHead += "\t\t\t<navLabel>\r\n";
            sNavHead += "\t\t\t\t<text>封面</text>\r\n";
            sNavHead += "\t\t\t</navLabel>\r\n";
            sNavHead += "\t\t\t<content src=\"text/coverpage.xhtml\" />\r\n";
            sNavHead += "\t\t</navPoint>\r\n";
            sNavMap = sNavHead + sNavMap + "\t<navMap>\r\n</ncx>\r\n";
            System.IO.File.WriteAllText(_AppDir + "output\\OEBPS\\toc.ncx", sNavMap);
            #endregion

            #region content.opf (File paths for e-pub index)
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
            sManifest += "\t\t<item href=\"images/cover.png\" id=\"cover-image\" media-type=\"image/png\"/>\r\n";
            sManifest += "\t\t<item href=\"text/coverpage.xhtml\" id=\"coverpage.xhtml\" media-type=\"application/xhtml+xml\"/>\r\n";
            foreach (Article oArticle in IndexList)
            {
                sManifest += Manifest(oArticle);
            }
            sManifest = "\t<manifest>\r\n" + sManifest + "\t</manifest>\r\n";

            // spine
            string sSpine = "";
            sSpine += "\t\t<itemref idref=\"coverpage.xhtml\" />\r\n";
            foreach (Article oArticle in IndexList)
            {
                sSpine += Spine(oArticle);
            }
            sSpine = "\t<spine toc=\"ncx\" >\r\n" + sSpine + "\t</spine>\r\n";

            sContentOpf = sContentHead + sManifest + sSpine + "</package>\r\n";
            System.IO.File.WriteAllText(_AppDir + "output\\OEBPS\\content.opf", sContentOpf);
            #endregion

            // Generate cover image
            CreateCoverImage().Save(_AppDir + "\\output\\OEBPS\\images\\cover.png", System.Drawing.Imaging.ImageFormat.Png);

            #region Zip (e-pubs are actually zip files)
            string sEPub = "Z:\\news\\" + Title + ".epub";
            if (System.IO.File.Exists(sEPub))
                System.IO.File.Delete(sEPub);
            ZipFile.CreateFromDirectory(_AppDir + "output", sEPub);
            #endregion
        }

        
        //private Image CreateImage()
        //{
        //    Bitmap bmp = new Bitmap(600, 800);
        //    using (Graphics g = Graphics.FromImage(bmp))
        //    {
        //        Rectangle oRec = new Rectangle(0, 0, 600, 800);
        //        g.FillRectangle(Brushes.White, oRec);

        //        g.DrawImage(LoadImageCropped(_ImageList[0], 580, 148), 10, 10);
        //        g.DrawRectangle(Pens.Black, 10, 10, 580, 148);

        //        g.DrawImage(LoadImageCropped(_ImageList[1], 180, 148), 10, 168);
        //        g.DrawRectangle(Pens.Black, 10, 168, 180, 148);

        //        g.DrawImage(LoadImageCropped(_ImageList[3], 390, 148), 200, 168);
        //        g.DrawRectangle(Pens.Black, 200, 168, 390, 148);

        //        g.DrawRectangle(Pens.Black, 10, 326, 380, 148);

        //        g.DrawImage(LoadImageCropped(_ImageList[3], 190, 306), 400, 326);
        //        g.DrawRectangle(Pens.Black, 400, 326, 190, 306);

        //        g.DrawImage(LoadImageCropped(_ImageList[4], 380, 148), 10, 484);
        //        g.DrawRectangle(Pens.Black, 10, 484, 380, 148);

        //        g.DrawImage(LoadImageCropped(_ImageList[5], 380, 148), 10, 642);
        //        g.DrawRectangle(Pens.Black, 10, 642, 380, 148);

        //        g.DrawRectangle(Pens.Black, 400, 642, 190, 148);

        //        int nFont = 48;
        //        Font oTitleFont = new Font("Calibri", nFont, FontStyle.Regular, GraphicsUnit.Pixel);
        //        Brush oBrush = new SolidBrush(ColorTranslator.FromHtml("#222222"));
        //        string sTitle = "查理の新聞匯報";
        //        g.DrawString(sTitle, oTitleFont, oBrush, 14, 350);
        //        nFont = 56;
        //        oTitleFont = new Font("Calibri", nFont, FontStyle.Regular, GraphicsUnit.Pixel);
        //        g.DrawString(System.DateTime.Now.ToString("MMM.dd"), oTitleFont, oBrush, 405, 640);
        //        nFont = 50;
        //        oTitleFont = new Font("Calibri", nFont, FontStyle.Regular, GraphicsUnit.Pixel);
        //        g.DrawString(System.DateTime.Now.ToString("yyyy"), oTitleFont, oBrush, 470, 700);
        //    }

        //    return bmp;
        //}

        //public Image LoadImageCropped(string _Filename, int _Width, int _Height)
        //{
        //    using (var oFile = new FileStream(_Filename, FileMode.Open))
        //    {
        //        var oImageFromFile = new Bitmap(oFile);
        //        Image oResult;

        //        // zoom (width)
        //        if (oImageFromFile.Width < _Width)
        //        {
        //            float nFactor = _Width / oImageFromFile.Width;
        //            Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
        //            oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
        //        }
        //        // zoom (height)
        //        if (oImageFromFile.Height < _Height)
        //        {
        //            float nFactor = _Height / oImageFromFile.Height;
        //            Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
        //            //oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
        //        }


        //        Random nRandom = new Random();
        //        // Crop (choose starting location)
        //        int nStartX = 0;
        //        int nStartY = 0;
        //        if (oImageFromFile.Width > _Width)
        //        {
        //            nStartX = nRandom.Next(0, oImageFromFile.Width - _Width);
        //        }

        //        if (oImageFromFile.Height > _Height)
        //        {
        //            nStartY = nRandom.Next(0, oImageFromFile.Height - _Height);
        //        }

        //        // zoom & crop
        //        return oImageFromFile.Clone(new Rectangle(nStartX, nStartY, _Width, _Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        //        //.Clone(_Rectangle, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        //        //Image oCoverImage = new Bitmap(oImageFile, _Rectangle.Width, _Rectangle.Height);
        //        //Image TempImage = (Bitmap)oCoverImage.Clone();
        //        //oImageFile.Dispose();
        //        ////_CoverImage = TempImage;
        //        //return TempImage;
        //    }
        //}
        private void SelectImages()
        {
            string sImageDir = _AppDir + "output\\OEBPS\\images\\";

            string[] FileList = Directory.EnumerateFiles(sImageDir, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToArray<string>();

            _ImageList.Clear();

            Random nRandom = new Random();
            for (int i = 0; i < 6; i++)
            {
                int nIndex = nRandom.Next(0, FileList.Length);

                while (_ImageList.Contains(FileList[nIndex]) || FileList[nIndex].Contains("cover.png"))
                {
                    nIndex = nRandom.Next(0, FileList.Length);
                }
                _ImageList.Add(FileList[nIndex]);
            }
        }

        #region Helper functions

        #region eBook Helpers
        /// <summary>
        /// Generates NavMap section of toc.ncx (Table of Content)
        /// Recursive call
        /// </summary>
        /// <param name="_Article">Article</param>
        /// <param name="_Indent">Indent level</param>
        /// <param name="_PlayOrder">Play order, seems useless</param>
        /// <returns></returns>
        private string NavMap(Article _Article, int _Indent, int _PlayOrder)
        {
            string sNavMap = "";
            string sIndent = repeat("\t", _Indent + 2);
            sNavMap += sIndent + "<navPoint id=\"" + _Article.ArticleId + "\" playOrder=\"" + _PlayOrder.ToString() + "\" >\r\n";
            sNavMap += sIndent + "\t<navLabel>\r\n";
            sNavMap += sIndent + "\t\t<text>" + _Article.Title + (_Indent == 0 ? " (" + _Article.SubArticles.Count.ToString() + ")" : "") + "</text>\r\n";
            sNavMap += sIndent + "\t</navLabel>\r\n";
            sNavMap += sIndent + "\t<content src=\"text/" + _Article.Filename + "\" />\r\n";

            // Recursive call for all sub-articles
            foreach (Article oSubArticle in _Article.SubArticles)
                sNavMap += NavMap(oSubArticle, _Indent + 1, ++_PlayOrder);
            // Close navPoint tag
            sNavMap += sIndent + "</navPoint>\r\n";
            return sNavMap;
        }


        /// <summary>
        /// Generates itemref section of content.opf
        /// Recursive call
        /// </summary>
        /// <param name="_Article">Article</param>
        /// <returns></returns>
        private string Spine(Article _Article)
        {
            string sSpine = "";
            sSpine += "\t\t<itemref idref=\"" + _Article.ArticleId + "\" />\r\n";
            foreach (Article oSubArticle in _Article.SubArticles)
                sSpine += Spine(oSubArticle);
            return sSpine;
        }

        /// <summary>
        /// Generates file index for manifest section of content.opf
        /// </summary>
        /// <param name="_Article">Article</param>
        /// <returns></returns>
        private string Manifest(Article _Article)
        {
            string sManifest = "";
            sManifest += "\t\t<item id=\"" + _Article.ArticleId + "\" href=\"text/" + _Article.Filename + "\" media-type=\"application/xhtml+xml\" />\r\n";
            foreach (Article oSubArticle in _Article.SubArticles)
                sManifest += Manifest(oSubArticle);
            return sManifest;
        }
        #endregion

        #region Image manipulations

        /// <summary>
        /// Generates 800x600 cover image using a template image
        /// randomly select 3 images downloaded from all articles
        /// prints date
        /// </summary>
        /// <returns></returns>
        private Image CreateCoverImage()
        {
            SelectImages();
            Bitmap bmp = new Bitmap(600, 800);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                Brush oBrush = new SolidBrush(ColorTranslator.FromHtml("#a0a0a0"));
                g.FillRectangle(oBrush, 0, 0, 600, 800);

                Image Cover1 = new Bitmap(580, 780);
                Image Cover2 = new Bitmap(470, 130);
                Image Cover3 = new Bitmap(490, 160);

                try
                {
                    Cover1 = LoadImageCropped(_ImageList[0], 580, 780);
                }
                catch (Exception ex)
                { }

                try
                {
                    Cover2 = LoadImageCropped(_ImageList[1], 470, 130);
                }
                catch (Exception ex)
                { }

                try
                {
                    Cover3 = LoadImageCropped(_ImageList[2], 490, 160);
                }
                catch (Exception ex)
                { }

                // Cover 1
                g.DrawImage(Cover1, 10, 10);

                // Cover 2
                g.FillRectangle(Brushes.White, 100, 385, 480, 140);
                g.DrawRectangle(Pens.Black, 100, 385, 480, 140);
                g.DrawRectangle(Pens.Black, 105, 390, 470, 130);
                g.DrawImage(Cover2, 105, 390);

                // Cover 3
                g.FillRectangle(Brushes.White, 20, 580, 500, 170);
                g.DrawRectangle(Pens.Black, 20, 580, 500, 170);
                g.DrawRectangle(Pens.Black, 25, 585, 490, 160);
                g.DrawImage(Cover3, 25, 585);

                // From template file
                Bitmap transparent = new Bitmap(_AppDir + "empty\\OEBPS\\images\\cover.png");
                g.DrawImage(transparent, 0, 0);

                // Date
                g.DrawRectangle(Pens.Black, 10, 10, 300, 300);
                int nFont = 160;
                Font oTitleFont = new Font("Calibri", nFont, FontStyle.Regular, GraphicsUnit.Pixel);

                // Determine day of week
                string[] sColor = { "#bb5555", "#222222", "#222277", "#222222", "#222222", "#222222", "#447755" };

                string[] sWeekday = { "日", "一", "二", "三", "四", "五", "六" };
                int nDayOfWeek = (int)System.DateTime.Now.DayOfWeek;

                oBrush = new SolidBrush(ColorTranslator.FromHtml(sColor[nDayOfWeek]));
                oTitleFont = new Font("Calibri", nFont, FontStyle.Regular, GraphicsUnit.Pixel);
                g.DrawString(System.DateTime.Now.ToString("dd"), oTitleFont, oBrush, 10, 0);
                g.ScaleTransform((float)0.5, (float)1.3);
                oTitleFont = new Font("Microsoft Yi Baiti", nFont, FontStyle.Regular, GraphicsUnit.Pixel);
                g.DrawString(sWeekday[nDayOfWeek], oTitleFont, oBrush, 400, 20);

                g.ResetTransform();
                nFont = 42;
                oTitleFont = new Font("Calibri", nFont, FontStyle.Regular, GraphicsUnit.Pixel);
                g.DrawString(System.DateTime.Now.ToString("yyyy MMM"), oTitleFont, oBrush, 30, 170);

            }
            return bmp;
        }

        /// <summary>
        /// Loads image file, shrink and enlarge to best fit width or height
        /// then random offset crop to input dimensions
        /// </summary>
        /// <param name="_Filename">Image file path</param>
        /// <param name="_Width">Width of output image</param>
        /// <param name="_Height">Height of output image</param>
        /// <returns></returns>
        public Image LoadImageCropped(string _Filename, int _Width, int _Height)
        {
            using (var oFile = new FileStream(_Filename, FileMode.Open))
            {
                var oImageFromFile = new Bitmap(oFile);
                Image oResult;

                // zoom-in (width)
                if (oImageFromFile.Width < _Width)
                {
                    float nFactor = _Width / oImageFromFile.Width;
                    Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
                    oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
                }
                // shrink if wider
                else
                {
                    float nFactor = (float)(_Width + 200) / oImageFromFile.Width;
                    Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
                    oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
                }

                // zoom-in (height)
                if (oImageFromFile.Height < _Height)
                {
                    float nFactor = (float)_Height / oImageFromFile.Height;
                    Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
                    oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
                }
                // shrink if taller
                else
                {
                    float nFactor = (float)(_Height + 300) / oImageFromFile.Height;
                    Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
                    oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
                }

                // Finally make sure width is enough after the adjustment above
                if (oImageFromFile.Width < _Width)
                {
                    float nFactor = (float)_Width / oImageFromFile.Width;
                    Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
                    oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
                }

                // Also make sure height is enough after the adjustment above
                if (oImageFromFile.Height < _Height)
                {
                    float nFactor = (float)_Height / oImageFromFile.Height;
                    Size nNewSize = new Size((int)(oImageFromFile.Width * nFactor), (int)(oImageFromFile.Height * nFactor));
                    oImageFromFile = new Bitmap(oImageFromFile, nNewSize);
                }

                Random nRandom = new Random();
                // Crop (choose starting location)
                int nStartX = 0;
                int nStartY = 0;
                if (oImageFromFile.Width > _Width)
                {
                    nStartX = nRandom.Next(0, oImageFromFile.Width - _Width);
                }

                if (oImageFromFile.Height > _Height)
                {
                    nStartY = nRandom.Next(0, oImageFromFile.Height - _Height);
                }

                // zoom & crop
                oResult = oImageFromFile.Clone(new Rectangle(nStartX, nStartY, _Width, _Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                oImageFromFile.Dispose();
                return oResult;
            }
        }

        #endregion

        #region Misc.
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
        #endregion
    }
}
