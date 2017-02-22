using System.Collections.Generic;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading;
using System;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;

namespace WebRole1
{
    /// <summary>
    /// Summary description for admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 

    [System.Web.Script.Services.ScriptService]
    public class admin : System.Web.Services.WebService
    {
        private static InitClass ic = new InitClass();
        private List<string> stats = new List<string>();
        private static string status = null;
        private static string[] errorMessage = new string[2];

        [WebMethod]
        public void LoadCrawler()
        {
            ic.statusQueue.AddMessage(new CloudQueueMessage("...Loading sitemaps..."));
            ic.adminQueue.AddMessage(new CloudQueueMessage("load"));
        }

        [WebMethod]
        public void StartCrawling()
        {
            ic.statusQueue.AddMessage(new CloudQueueMessage("Crawling"));
            ic.adminQueue.AddMessage(new CloudQueueMessage("start"));
            
        }

        [WebMethod]
        public void StopCrawling()
        {
            ic.statusQueue.AddMessage(new CloudQueueMessage("Idle"));
            ic.adminQueue.AddMessage(new CloudQueueMessage("stop"));
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ClearIndex()
        {
            ic.statusQueue.AddMessage(new CloudQueueMessage("Idle and cleared"));
            ic.adminQueue.AddMessage(new CloudQueueMessage("clear"));
            ic.urlQueue.Clear();
            ic.adminQueue.Clear();
            ic.errorQueue.Clear();
            ic.statusQueue.Clear();
            ic.statsQueue.Clear();
            TableQuery<PageTitle> query = new TableQuery<PageTitle>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "00"));
            foreach (PageTitle entity in ic.titlesTable.ExecuteQuery(query))
            {
                ic.titlesTable.Execute(TableOperation.Delete(entity));
            }
            return new JavaScriptSerializer().Serialize("All queues and tables cleared. Workers stopped.");
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetPageTitle(string url)
        {
            string pageTitle = "";
            string encodedUrl = Encode64(url);
            TableQuery<PageTitle> query = new TableQuery<PageTitle>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, encodedUrl));
            foreach (PageTitle entity in ic.titlesTable.ExecuteQuery(query))
            {
                pageTitle = entity.Title;
            }
            return new JavaScriptSerializer().Serialize(pageTitle);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string CheckStatus()
        {
            CloudQueueMessage msg = ic.statusQueue.GetMessage();
            if (msg != null)
            {
                status = msg.AsString;
                ic.statusQueue.DeleteMessage(msg);
            }
            return new JavaScriptSerializer().Serialize(status);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetStats()
        {
            CloudQueueMessage msg = ic.statsQueue.GetMessage();
            if (msg != null)
            {
                stats = new List<string>();
                string messageString = msg.AsString;
                foreach (string stat in messageString.Split(','))
                {
                    stats.Add(stat);
                }
                ic.statsQueue.DeleteMessage(msg);
            }
            return new JavaScriptSerializer().Serialize(stats);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetErrors()
        {
            CloudQueueMessage msg = ic.errorQueue.GetMessage();
            if (msg != null)
            {
                errorMessage = msg.AsString.Split('|');
                ic.errorQueue.DeleteMessage(msg);
            }
            return new JavaScriptSerializer().Serialize(errorMessage);
        }


        private string Encode64(string plaintText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintText));
        }
    }
}