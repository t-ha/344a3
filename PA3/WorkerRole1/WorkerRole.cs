using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using ClassLibrary;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using WebRole1;
using HtmlAgilityPack;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static InitClass ic = new InitClass();
        private Crawler spider;
        private bool hasStarted = false;
        private int totalUrlsCrawled = 0;
        private PerformanceCounter cpuPerformance = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter memPerformance = new PerformanceCounter("Memory", "Available MBytes");

        public override void Run()
        {
            while (true)
            {
                CloudQueueMessage adminMsg = ic.adminQueue.GetMessage();
                if (adminMsg != null)
                {
                    if (adminMsg.AsString == "load")
                    {
                        spider = new Crawler("cnn.com", "bleacherreport.com", ic.urlQueue);
                        //spider = new Crawler("daaadxxrea.com", "bleacherreport.com", ic.urlQueue);
                    }
                    else if (adminMsg.AsString == "start")
                    {
                        hasStarted = true;
                    }
                    else if (adminMsg.AsString == "stop")
                    {
                        hasStarted = false;
                    }
                    ic.adminQueue.DeleteMessage(adminMsg);
                }

                if (hasStarted)
                {
                    totalUrlsCrawled++;
                    CloudQueueMessage urlMsg = spider.toBeCrawled.GetMessage();
                    if (urlMsg != null)
                    {
                        bool isAllowed = true;
                        string url = urlMsg.AsString;
                        foreach (string disallowedUrl in spider.disallowed)
                        {
                            isAllowed = !url.Contains(disallowedUrl);
                        }
                        
                        if (isAllowed)
                        {
                            try
                            {
                                spider.marked.Add(url);
                                string[] roots = new string[2] { "cnn.com", "bleacherreport.com" };
                                HtmlDocument doc = new HtmlWeb().Load(url);

                                List<string> filtered = new List<string>();
                                // filter the urls. only want root domain
                                string root = "";
                                if (url.Contains(roots[0]))
                                {
                                    root = roots[0];
                                }
                                else
                                {
                                    root = roots[1];
                                }
                                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                                {
                                    string rawUrl = link.Attributes["href"].Value;
                                    if (rawUrl.StartsWith("/") && rawUrl != "/users/undefined")
                                    {
                                        filtered.Add("http://" + root + rawUrl);
                                    }
                                    else if (rawUrl.Contains(root))
                                    {
                                        if (rawUrl.StartsWith("http://"))
                                        {
                                            filtered.Add(rawUrl);
                                        }
                                        else
                                        {
                                            filtered.Add("http://" + rawUrl);
                                        }
                                    }
                                }

                                // filter pt. 2: not disallowed and not already marked
                                foreach (string filteredUrl in filtered)
                                {
                                    if (!spider.marked.Contains(filteredUrl) && !spider.disallowed.Contains(filteredUrl))
                                    {
                                        spider.toBeCrawled.AddMessage(new CloudQueueMessage(filteredUrl));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            
                            
                        }

                        float cpuUsage = cpuPerformance.NextValue();
                        float memUsage = memPerformance.NextValue();
                        ic.urlQueue.FetchAttributes();
                        string statsMessage = cpuUsage + "," + memUsage + "," + totalUrlsCrawled + "," + ic.urlQueue.ApproximateMessageCount + "," + spider.marked.Count;
                        ic.statsQueue.AddMessage(new CloudQueueMessage(statsMessage));
                        spider.toBeCrawled.DeleteMessage(urlMsg);
                    }
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
