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
        private HtmlDocument htmlDoc = new HtmlDocument();

        public override void Run()
        {
            while (true)
            {
                CloudQueueMessage adminMsg = ic.adminQueue.GetMessage();
                if (adminMsg != null)
                {
                    if (adminMsg.AsString == "load")
                    {
                        spider = new Crawler("bleacherreport.com", "fdsfs.com", ic.urlQueue);
                        Console.WriteLine(spider.disallowed.Count);
                    }
                    else if (adminMsg.AsString == "start")
                    {
                        hasStarted = true;
                    }
                    ic.adminQueue.DeleteMessage(adminMsg);
                }

                if (hasStarted)
                {
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
                            spider.marked.Add(url);
                            htmlDoc.Load(url);
                            //foreach (HtmlNode link in htmlDoc.DocumentElement.SelectNodes("//a[@href"]))
                        }




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
