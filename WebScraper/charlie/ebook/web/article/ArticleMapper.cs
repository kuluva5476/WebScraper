using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace com.charlie.ebook.web.article
{
    class ArticleMapper
    {
        public string MapName { get; private set; }
        public string IndexUrl { get;  private set; }
        public string IndexListXPath { get; private set; }
        public string IndexItemXPath { get;  private set; }
        public List<string> IndexItemFilter { get;  set; }
        public List<string> ContentFilters { get; set; }
        public string Caption { get; private set; }
        public string Title { get; private set; }
        public string Author { get; private set; }
        public string PubDate { get; private set; }
        public string Summary { get; private set; }
        public string Content { get; private set; }

        public string ContentImageSource { get; private set; }
        public string ContentImageDesc { get; private set; }

        public string TopImage { get; private set; }
        public string TopImageSource { get; private set; }
        public string TopImageDesc { get; private set; }

        public string LastVisit { get; set; }

        public void LoadMapper(string _MapName)
        {
            MapName = _MapName;
            XmlDocument xXmlDoc = new XmlDocument();
            string sMappingTemplate = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
            xXmlDoc.Load(sMappingTemplate);
            XmlNode xNode = xXmlDoc.SelectNodes("/configurations/mappings/map[@name='" + _MapName + "']")[0];

            Caption = xNode.Attributes["caption"].Value;

            IndexUrl = xNode.SelectSingleNode("./index").Attributes["url"].Value;
            IndexListXPath = xNode.SelectSingleNode("./index").Attributes["list_xpath"].Value;
            IndexItemXPath = xNode.SelectSingleNode("./index").Attributes["item_xpath"].Value;
            IndexItemFilter = new List<string>();
            XmlNodeList xFilters = xNode.SelectSingleNode("./index").ChildNodes;
            foreach (XmlNode xFilter in xFilters)
            {
                IndexItemFilter.Add(xFilter.Attributes["value"].Value);
            }

            ContentFilters = new List<string>();
            XmlNodeList xContentFilters = xNode.SelectNodes("./article/filter");
            foreach (XmlNode xFilter in xContentFilters)
            {
                ContentFilters.Add(xFilter.Attributes["value"].Value);
            }
            LastVisit = xNode.SelectSingleNode("./article").Attributes["last_visit"].Value;
            Title = xNode.SelectSingleNode("./article/title").Attributes["xpath"].Value;
            Author = xNode.SelectSingleNode("./article/author").Attributes["xpath"].Value;
            PubDate = xNode.SelectSingleNode("./article/publish_date").Attributes["xpath"].Value;
            Summary = xNode.SelectSingleNode("./article/summary").Attributes["xpath"].Value;
            Content = xNode.SelectSingleNode("./article/content").Attributes["xpath"].Value;
            ContentImageSource = xNode.SelectSingleNode("./article/content").Attributes["image_source"].Value;
            ContentImageDesc = xNode.SelectSingleNode("./article/content").Attributes["image_desc"].Value;
            TopImage = xNode.SelectSingleNode("./article/top_image").Attributes["xpath"].Value;
            TopImageSource = xNode.SelectSingleNode("./article/top_image").Attributes["image_source"].Value;
            TopImageDesc = xNode.SelectSingleNode("./article/top_image").Attributes["image_desc"].Value;
        }

        /// <summary>
        /// Update last_visit 
        /// </summary>
        public void Save()
        {
            XmlDocument xXmlDoc = new XmlDocument();
            string sMappingTemplate = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
            xXmlDoc.Load(sMappingTemplate);
            XmlNode xNode = xXmlDoc.SelectSingleNode("/configurations/mappings/map[@name='" + MapName + "']");

            if (xNode != null)
            {
                xNode.SelectSingleNode("./article").Attributes["last_visit"].Value = LastVisit;
                xXmlDoc.Save(sMappingTemplate);
            }
        }

    }
}
