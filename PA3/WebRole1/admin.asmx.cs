using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.Diagnostics;
using ClassLibrary;

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
        private PerformanceCounter cpuPerformance = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter memPerformance = new PerformanceCounter("Memory", "Available MBytes");
        private static InitClass ic = new InitClass();
        private List<string> stats;

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string LoadCrawler()
        {
            ic.adminQueue.AddMessage(new CloudQueueMessage("load"));
            return new JavaScriptSerializer().Serialize("load");
        }

        [WebMethod]
        public void StartCrawling()
        {
            stats = new List<string>();
            for (int i = 0; i < 5; i++) { stats.Add(" "); }
            ic.adminQueue.AddMessage(new CloudQueueMessage("start"));
        }

        [WebMethod]
        public void StopCrawling()
        {
            ic.adminQueue.AddMessage(new CloudQueueMessage("stop"));
        }

        [WebMethod]
        public void ClearIndex()
        {

        }

        [WebMethod]
        public void GetPageTitle()
        {

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
        public string GetUsage()
        {
            float cpuUsage = cpuPerformance.NextValue();
            float memUsage = memPerformance.NextValue();
            return new JavaScriptSerializer().Serialize(new float[2] { cpuUsage, memUsage });
        }
    }
}
