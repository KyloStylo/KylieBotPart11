using CRMApi.Helpers;
using CRMApi.Models;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace CRMApi.Controllers
{
    public class CRMController : ApiController
    {
        List<CRMContact> contacts = new List<CRMContact>();
        Dynamics365Helper CRMConnection = new Dynamics365Helper();

        private void connectCRM()
        {
            CRMConnection.ConnectCRM(ConfigurationManager.AppSettings["D365.Username"], ConfigurationManager.AppSettings["D365.Password"], Dynamics365Helper.CRMRegions.NorthAmerica.ToString(), ConfigurationManager.AppSettings["D365.Uri"], true);
        }

        [Route("CRM/GetContact/{email}/")]
        [HttpGet]
        public CRMContact GetContact(string email)
        {
            if (CRMConnection != null)
            {
                connectCRM();
            }

            string fetchXML =
            @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' returntotalrecordcount='true' >
                            <entity name='contact'>
                                <attribute name='contactid' />
                                <attribute name='fullname' />
                                <attribute name='emailaddress1' />
                                <filter type = 'and'>
                                    <condition attribute = 'emailaddress1' value = " + email + @" operator = 'eq' />
                                </filter>
                            </entity>
                        </fetch>";

            EntityCollection collection = Dynamics365Helper.RetrieveXML(fetchXML);

            CRMContact contact = null;
            if (collection.Entities != null)
            {
                var entity = collection.Entities.FirstOrDefault();
                contact = new CRMContact();
                contact.Email = entity.Attributes["emailaddress1"].ToString();
                contact.FullName = entity.Attributes["fullname"].ToString();
                contact.ContactId = new Guid(entity.Attributes["contactid"].ToString());
            }

            return contact;
        }

        [Route("CRM/CreateBotChat")]
        [HttpPost]
        public Tuple<bool, Guid> CreateBotChatAsync(JObject transcript)
        {
            BotChat chat;
            Tuple<bool, Guid> result = null;

            if (transcript != null)
            {
                chat = JsonConvert.DeserializeObject<BotChat>(transcript.ToString());

                if (CRMConnection != null)
                {
                    connectCRM();
                }

                if (CRMConnection.IsConnectionReady)
                {
                    try
                    {
                        result = Dynamics365Helper.CreateCRMBotChat(chat);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
            return result;
        }

        [Route("CRM/SearchKB/{keyword}/")]
        [HttpGet]
        public List<CRMKnowledgeBaseArticle> SearchKB(string keyword)
        {
            if (CRMConnection != null)
            {
                connectCRM();
            }

            string fetchXML =
            @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' returntotalrecordcount='true' >
                            <entity name='knowledgearticle'>
                                <attribute name='knowledgearticleid' />
                                <attribute name='modifiedon' />
                                <attribute name='title' />
                                <attribute name='description'/>
                                <attribute name='articlepublicnumber'/>
                                <filter type = 'and'>
                                    <condition attribute = 'statuscode' value = '7' operator = 'eq' />
                                    <condition attribute = 'keywords' value = '%" + keyword + @"%' operator = 'like' />
                                </filter>
                            </entity>
                        </fetch>";

            EntityCollection collection = Dynamics365Helper.RetrieveXML(fetchXML);

            List<CRMKnowledgeBaseArticle> kbaList = new List<CRMKnowledgeBaseArticle>();

            if (collection != null && collection.Entities != null)
            {
                if (collection.Entities.Count > 0)
                {
                    foreach (Entity e in collection.Entities)
                    {
                        CRMKnowledgeBaseArticle kba = null;
                        if (collection.Entities != null)
                        {
                            kba = new CRMKnowledgeBaseArticle();
                            kba.title = e.Attributes["title"].ToString();
                            kba.description = e.Attributes["description"].ToString();
                            kba.articleNumber = e.Attributes["articlepublicnumber"].ToString();
                            kba.publishedDate = DateTime.Parse(e.Attributes["modifiedon"].ToString());
                        }
                        kbaList.Add(kba);
                    }
                }
            }
            return kbaList;
        }
    }
}
