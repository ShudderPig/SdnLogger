using SDNWatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml;

namespace SDNWatch.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            // lets set the session variables so we can keep hold of changes //
            if(Session["changes"] == null)
            {
                Session["changes"] = new List<SdnEntry>();
            }
            if(Session["sdnList"] == null)
            {
                // if first hit lets go get the sdnList ww will be working with //
                Session["sdnList"] = await getSDNList();
            }
            
            // now lets check for changes //
            var changes = await getChanges((SdnList)Session["sdnList"]);

            // set viewbag to hold original change set //
            ViewBag.Changes = Session["changes"];
            List<SdnEntry> entries = (List<SdnEntry>)ViewBag.Changes;
            entries.ForEach(e => changes.Add(e));
           
            Session["changes"] = changes;

            ViewBag.Changes = changes;
            /** 
             * here i probalby would have liked to make an api call to save or store this info 
             * I know twitter have an api so maybe we could send a new post there
             * either way we can return the view and access our changes from viewbag
             * ofcourse this isn't ideal, because it means on every load we must render all changes AND if ever the session ends then we loose historic changes
             */
            return View();
        }

        public async Task<List<SdnEntry>> getChanges(SdnList knownList)
        {
            // lets get a more updated version of the list //
            SdnList listToDate = await getSDNList();
            // compare the two and extract any changes //
            var changes = compareLists(listToDate, knownList);

            // lets set the more recent list as our seesion variable //
            Session["sdnList"] = listToDate;

            return changes;
        }

        private List<SdnEntry> compareLists(SdnList listToDate, SdnList KnownList)
        {
            List<SdnEntry> changes = new List<SdnEntry>();

            listToDate.sdnEntries.ForEach(e => {
                // if it doesn't exist in our old list then it's been added //
                var existing = KnownList.sdnEntries.FirstOrDefault(x => x.uid == e.uid);
                if (existing == null)
                {
                    // which means it's a changes //
                    e.changeType = "New Entry";
                    e.changetime = DateTime.Now;
                    changes.Add(e);
                }
                // if it does exist then lets compare it with it's cougnterpart to see if they changed naything within //
                else if (EntriesAreDifferent(existing, e))
                {
                    e.changeType = "Ammendment";
                    e.changetime = DateTime.Now;
                    changes.Add(e);
                }
            });

            // lets look for any that existed in our original but no longer do //
            var removals = KnownList.sdnEntries.Where(k => !listToDate.sdnEntries.Any(y => y.uid == k.uid));
            foreach (var del in removals)
            {
                del.changeType = "Entry Deleted";
                del.changetime = DateTime.Now;
                changes.Add(del);
            }
            return changes;
        }

        private bool EntriesAreDifferent(SdnEntry entry1, SdnEntry entry2)
        {
            /** 
             * compare entities
             * this is long winded and i would have liked to use recursion, however i'm not brilliant with reflection and i think it' sneeded here
             */

            // first check scalar //
            if (entry1.uid != entry2.uid) return true;
            if (entry1.lastName != entry2.lastName) return true;
            if (entry1.sdnType != entry2.sdnType) return true;
            if (entry1.addressList.Count() != entry2.addressList.Count()) return true;
            if (entry1.programList.Count() != entry2.programList.Count()) return true;
            if (entry1.akaList.Count() != entry2.akaList.Count()) return true;

            // now check the collections //
            for (int a = 0; a < entry1.addressList.Count(); a++)
            {
                if(entry1.addressList[a].city != entry2.addressList[a].city) return true;
                if (entry1.addressList[a].address1 != entry2.addressList[a].address1) return true;
                if (entry1.addressList[a].country != entry2.addressList[a].country) return true;
                if (entry1.addressList[a].uid != entry2.addressList[a].uid) return true;
                if (entry1.addressList[a].postalCode != entry2.addressList[a].postalCode) return true;
            }

            for (int ak = 0; ak < entry1.akaList.Count(); ak++)
            {
                if (entry1.akaList[ak].uid != entry2.akaList[ak].uid) return true;
                if (entry1.akaList[ak].type != entry2.akaList[ak].type) return true;
                if (entry1.akaList[ak].category != entry2.akaList[ak].category) return true;
            }

            for (int p = 0; p < entry1.programList.Count(); p++)
            {
                if (entry1.programList[p] != entry2.programList[p]) return true;
            }

            // if nothings changed the retturn //
            return false;
        }
        private async Task<SdnList> getSDNList()
        {
            // go and get the sdn list from our url //
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://www.treasury.gov/ofac/downloads/sdn.xml");

            if (!response.IsSuccessStatusCode)
            {
                // if somethings gone wrong then throw an exception //
                throw new System.Exception("Get Attempt unsuccessful");
            }

            string getResult = await response.Content.ReadAsStringAsync();            

            // lets convert this into our class //
            return CreateModel(getResult);
        }
        private SdnList CreateModel(string xml)
        {
            SdnList sdn = new SdnList();

            // convert our xml string to xml //
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            /** 
             * loop through the nodes 
             * Again here i probably would have prefered to use recursion. 
             */
            var nodes = xmlDoc.GetElementsByTagName("sdnEntry");
            foreach(XmlNode node in nodes)
            {
                SdnEntry entry = new SdnEntry();

                foreach(XmlNode child in node.ChildNodes)
                {
                    // find out what node type it is and handle //
                    switch (child.Name)
                    {
                        case "uid":
                            entry.uid = Convert.ToInt32(child.InnerText);
                            break;
                        case "lastName":
                            entry.lastName = child.InnerText;
                            break;
                        case "sdnType":
                            entry.sdnType = child.InnerText;
                            break;
                        case "addressList":
                            entry.addressList = CreateAddressList(child.ChildNodes);
                            break;
                        case "programList":
                            entry.programList = CreateProgramList(child.ChildNodes);
                            break;
                        case "akaList":
                            entry.akaList = CreateAkaList(child.ChildNodes);
                            break;
                        default:
                            break;
                    }
                }
                SdnEntry t = entry;
                sdn.sdnEntries.Add(entry);
            }

            return sdn;
        }

        private List<Aka> CreateAkaList(XmlNodeList nodelist)
        {
            // generate the aka class //
            List<Aka> akaList = new List<Aka>();
            foreach (XmlNode node in nodelist)
            {
                Aka aka = new Aka();
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "uid":
                            aka.uid = Convert.ToInt32(child.InnerText);
                            break;
                        case "type":
                            aka.type = child.InnerText;
                            break;
                        case "category":
                            aka.category = child.InnerText;
                            break;
                        case "lastName":
                            aka.lastName = child.InnerText;
                            break;
                        default:
                            break;
                    }
                }
                akaList.Add(aka);
            }

            return akaList;
        }

        private List<string> CreateProgramList(XmlNodeList nodelist)
        {
            // programs are just single values by the look of things so this is simpler //
            List<string> progList = new List<string>();
            foreach (XmlNode node in nodelist)
            {
                progList.Add(node.InnerText);
            }

            return progList;
        }

        private List<Address> CreateAddressList(XmlNodeList nodelist)
        {
            // create address class //
            List<Address> addList = new List<Address>();
            foreach (XmlNode node in nodelist)
            {
                Address add = new Address();
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "uid":
                            add.uid = Convert.ToInt32(child.InnerText);
                            break;
                        case "address1":
                            add.address1 = child.InnerText;
                            break;
                        case "city":
                            add.city = child.InnerText;
                            break;
                        case "postalCode":
                            add.postalCode = child.InnerText;
                            break;
                        case "country":
                            add.country = child.InnerText;
                            break;
                        default:
                            break;
                    }
                }
                addList.Add(add);
            }

            return addList;
        }
    }
}