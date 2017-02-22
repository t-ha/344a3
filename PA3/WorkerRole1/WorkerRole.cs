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
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using WebRole1;
using HtmlAgilityPack;
using System.Text;

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
        private int index = 0;
        private PerformanceCounter cpuPerformance = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter memPerformance = new PerformanceCounter("Memory", "Available MBytes");

        public override void Run()
        {
            while (true)
            {
                Thread.Sleep(50);
                CloudQueueMessage adminMsg = ic.adminQueue.GetMessage();
                if (adminMsg != null)
                {
                    if (adminMsg.AsString == "load")
                    {
                        spider = new Crawler("cnn.com", "bleacherreport.com", ic.urlQueue);
                        ic.statusQueue.AddMessage(new CloudQueueMessage("Loading complete"));
                    }
                    else if (adminMsg.AsString == "start")
                    {
                        hasStarted = true;
                    }
                    else if (adminMsg.AsString == "stop")
                    {
                        hasStarted = false;
                    }
                    else if (adminMsg.AsString == "clear")
                    {
                        hasStarted = false;
                        totalUrlsCrawled = 0;
                        index = 0;
                    }
                    ic.adminQueue.DeleteMessage(adminMsg);
                }

                if (hasStarted)
                {
                    CloudQueueMessage urlMsg = spider.toBeCrawled.GetMessage();
                    if (urlMsg != null)
                    {
                        string url = urlMsg.AsString;
                        try
                        {
                            totalUrlsCrawled++;
                            HtmlDocument doc = new HtmlWeb().Load(url); // check valid url


                            index++;
                            string title = doc.DocumentNode.SelectSingleNode("//title").InnerText;
                            string encodedUrl = Encode64(url);
                            PageTitle pageTitle = new PageTitle(encodedUrl, title);
                            TableOperation insertOperation = TableOperation.Insert(pageTitle);
                            ic.titlesTable.Execute(insertOperation);


                            string[] roots = new string[2] { "cnn.com", "bleacherreport.com" };
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
                                    spider.marked.Add(filteredUrl);
                                    spider.toBeCrawled.AddMessage(new CloudQueueMessage(filteredUrl));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            ic.errorQueue.AddMessage(new CloudQueueMessage(url + "|" + e.Message));
                        }
                        

                        float cpuUsage = cpuPerformance.NextValue();
                        float memUsage = memPerformance.NextValue();
                        ic.urlQueue.FetchAttributes();
                        string statsMessage = cpuUsage + "," + memUsage + "," + totalUrlsCrawled + "," + ic.urlQueue.ApproximateMessageCount + "," + index;
                        ic.statsQueue.AddMessage(new CloudQueueMessage(statsMessage));
                        spider.toBeCrawled.DeleteMessage(urlMsg);
                    }
                }
            }
        }

        private string Encode64(string plaintText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintText));
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