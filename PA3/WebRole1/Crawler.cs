using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Xml;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WebRole1
{
    public class Crawler
    {
        public HashSet<string> marked;
        public List<string> disallowed = new List<string>();
        public CloudQueue toBeCrawled;

        public Crawler(string url, string url2, CloudQueue toBeCrawled)
        {
            this.toBeCrawled = toBeCrawled;
            SetRobots(url);
            SetRobots(url2);
        }

        public void SetRobots(string url)
        {
            Queue<string> sitemaps = new Queue<string>();

            try
            {
                using (var client = new WebClient())
                {
                    string page = client.DownloadString("http://www." + url + "/robots.txt");
                    using (StringReader reader = new StringReader(page))
                    {
                        TraverseRobots(reader, sitemaps, url);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }

            GetInitialURLs(sitemaps, url);
        }

        private void GetInitialURLs(Queue<string> sitemaps, string url)
        {
            while (sitemaps.Count != 0)
            {
                XmlDocument doc = new XmlDocument();
                string sitemapUrl = sitemaps.Dequeue();
                doc.Load(sitemapUrl);
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    string nodeUrl = node["loc"].InnerText;
                    if (url == "cnn.com")
                    {
                        ConfigureCNN(sitemaps, node, nodeUrl);
                    }
                    else if (url == "bleacherreport.com" && sitemapUrl.Contains("nba"))
                    {
                        ConfigureBR(nodeUrl);
                    }
                }
            }
        }

        private void ConfigureBR(string nodeUrl)
        {
            toBeCrawled.AddMessage(new CloudQueueMessage(nodeUrl));
        }

        private void ConfigureCNN(Queue<string> sitemaps, XmlNode node, string nodeUrl)
        {
            bool isValidUrl = ValidateURL(node, nodeUrl);

            if (isValidUrl)
            {
                if (nodeUrl.EndsWith(".xml"))
                {
                    sitemaps.Enqueue(nodeUrl);
                }
                else
                {
                    toBeCrawled.AddMessage(new CloudQueueMessage(nodeUrl));
                }
            }
        }

        private void TraverseRobots(StringReader reader, Queue<string> sitemaps, string url)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(' ');
                if (parts[0] == "Sitemap:")
                {
                    sitemaps.Enqueue(parts[1]);
                }
                else if (parts[0] == "Disallow:")
                {
                    disallowed.Add(url + parts[1]);
                }
            }
        }

        private bool CheckDate(DateTime cutoffDate, DateTime nodeDate)
        {
            return cutoffDate.Date <= nodeDate.Date;
        }

        private bool ValidateURL(XmlNode node, string nodeUrl)
        {
            if (node["lastmod"] != null)
            {
                return CheckDate(new DateTime(2016, 12, 1), DateTime.Parse(node["lastmod"].InnerText));
            }
            else if (node["news:news"] != null)
            {
                return CheckDate(new DateTime(2016, 12, 1), DateTime.Parse(node["news:news"]["news:publication_date"].InnerText));
            }
            else
            {
                string[] tt = nodeUrl.Substring(19).Split('/');
                int outInt;
                return tt.Length <= 1 || !int.TryParse(tt[1], out outInt);
            }
        }
    }
}