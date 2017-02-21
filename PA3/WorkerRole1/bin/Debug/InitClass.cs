using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;

namespace ClassLibrary
{
    public class InitClass
    {
        public CloudQueue urlQueue { get; set; }
        public CloudQueue adminQueue { get; set; }
        public CloudQueue statsQueue { get; set; }
        public CloudTable titlesTable { get; set; }

        public InitClass()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            urlQueue = queueClient.GetQueueReference("urlqueue");
            urlQueue.CreateIfNotExists();
            adminQueue = queueClient.GetQueueReference("adminqueue");
            adminQueue.CreateIfNotExists();
            statsQueue = queueClient.GetQueueReference("statsqueue");
            statsQueue.CreateIfNotExists();

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            titlesTable = tableClient.GetTableReference("titlestable");
            titlesTable.CreateIfNotExists();
        }
    }
}
