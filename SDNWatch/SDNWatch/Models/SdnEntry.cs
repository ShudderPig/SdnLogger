using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SDNWatch.Models
{

    public class Aka
    {
        public int? uid { get; set; }
        public string type { get; set; }
        public string category { get; set; }
        public string lastName { get; set; }
    }

    public class Address
    {
        public int? uid { get; set; }
        public string address1 { get; set; }
        public string city { get; set; }
        public string postalCode { get; set; }
        public string country { get; set; }
    }
    public class SdnEntry
    {
        public SdnEntry()
        {
            this.uid = 0;
            this.lastName = "";
            this.sdnType = "";
            this.programList = new List<string>();
            this.akaList = new List<Aka>();
            this.addressList = new List<Address>();
            this.changeType = "";
            this.changetime = new DateTime();
        }

        public int? uid { get; set; }
        public string lastName { get; set; }
        public string sdnType { get; set; }
        public List<string> programList { get;set;}
        public List<Aka> akaList { get; set; }
        public List<Address> addressList { get; set; }
        public string changeType { get; set; }
        public DateTime changetime { get; set; }

    }
    public class PublishInformation
    {
        public DateTime Publish_Date { get; set; }
        public int? Record_Count { get; set; }
    }
    public class SdnList
    {
        public SdnList()
        {
            this.sdnEntries = new List<SdnEntry>();
        }

        public List<SdnEntry> sdnEntries { get; set; }
    }
}