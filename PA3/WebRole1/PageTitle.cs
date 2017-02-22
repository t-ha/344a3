using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WebRole1
{
    public class PageTitle : TableEntity
    {
        public string EncodedUrl { get; set; }
        public string Title { get; set; }

        public PageTitle(string encodedUrl, string title)
        {
            this.PartitionKey = encodedUrl;
            this.RowKey = Guid.NewGuid().ToString();

            this.EncodedUrl = encodedUrl;
            this.Title = title;
        }

        public PageTitle() { }
    }
}