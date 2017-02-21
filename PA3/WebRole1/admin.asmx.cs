using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using ClassLibrary;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.Diagnostics;

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

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string LoadCrawler()
        {
            ic.adminQueue.AddMessage(new CloudQueueMessage("load"));
            return new JavaScriptSerializer().Serialize("load");
        }

        [WebMethod]
        public void StartCrawler()
        {
            ic.adminQueue.AddMessage(new CloudQueueMessage("start"));
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string TestTest()
        {
            CloudQueue tempQueue = ic.adminQueue;
            for (int i = 0; i < 400; i++)
            {
                tempQueue.AddMessage(new CloudQueueMessage("This is a test message " + i));
            }

            string temp = "TEST COMPLETE";
            return new JavaScriptSerializer().Serialize(temp);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetMemUsage()
        {
            float memUsage = memPerformance.NextValue();
            return new JavaScriptSerializer().Serialize(memUsage);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetCPUUsage()
        {
            float cpuUsage = cpuPerformance.NextValue();
            return new JavaScriptSerializer().Serialize(cpuUsage);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetUsage()
        {
            float cpuUsage = cpuPerformance.NextValue();
            float memUsage = memPerformance.NextValue();
            return new JavaScriptSerializer().Serialize(new float[2] { cpuUsage, memUsage });
        }

        [WebMethod]
        public void StopCrawling()
        {

        }

        [WebMethod]
        public void ClearIndex()
        {

        }

        [WebMethod]
        public void GetPageTitle()
        {

        }
    }
}
