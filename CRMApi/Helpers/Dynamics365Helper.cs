using CRMApi.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;

namespace CRMApi.Helpers
{
    public class Dynamics365Helper
    {
        private Boolean isConnectionReady = false;
        public bool IsConnectionReady { get => isConnectionReady; set => isConnectionReady = value; }
        private static CrmServiceClient crmSvc;
        public CrmServiceClient CrmSvc { get => crmSvc; set => crmSvc = value; }
        private string connectionError;
        public string ConnectionError { get => connectionError; set => connectionError = value; }
        public enum CRMRegions
        {
            NorthAmerica,
            EMEA,
            APAC,
            SouthAmerica,
            Oceania,
            JPN,
            CAN,
            IND,
            NorthAmerica2,
            UnitedKingdom
        };
        public void ConnectCRM(string crmUsername, string crmPassword, string crmRegion, string crmOrgId, bool isO365)
        {
            try
            {
                CrmSvc = new CrmServiceClient(crmUsername, CrmServiceClient.MakeSecureString(crmPassword), crmRegion, crmOrgId, true, true, null, isO365);
                IsConnectionReady = CrmSvc.IsReady;
            }
            catch (Exception e)
            {
                ConnectionError = e.Message.ToString();
            }
        }
        public static EntityCollection RetrieveXML(string XML)
        {
            EntityCollection queryResult = null;

            if (crmSvc != null && crmSvc.IsReady && XML != null)
            {
                return queryResult = crmSvc.GetEntityDataByFetchSearchEC(XML);
            }
            else
            {
                return queryResult;
            }
        }
        public static Tuple<bool, Guid> CreateChat(BotChat transcript)

        {
            Dictionary<string, CrmDataTypeWrapper> inData = new Dictionary<string, CrmDataTypeWrapper>();
            inData.Add("subject", new CrmDataTypeWrapper("Bot chat: " + DateTime.Now.ToString() + " - " + transcript.channel.ToString(), CrmFieldType.String));
            if (transcript.regardingId != Guid.Empty)
            {
                inData.Add("regardingobjectid", new CrmDataTypeWrapper(transcript.regardingId, CrmFieldType.Lookup, "contact"));
            }
            inData.Add("kbot_transcript", new CrmDataTypeWrapper("From: " + transcript.chatUser + Environment.NewLine +
                                                                    "Message: " + transcript.chatMessage + Environment.NewLine +
                                                                    "Time: " + transcript.timeStamp + Environment.NewLine +
                                                                    "Channel: " + transcript.channel + Environment.NewLine + Environment.NewLine,
                                                                    CrmFieldType.String));

            inData.Add("kbot_skypeconversationid", new CrmDataTypeWrapper(transcript.conversationId, CrmFieldType.String));

            try
            {
                var cId = crmSvc.CreateNewRecord("kbot_botchat", inData);

                return new Tuple<bool, Guid>(true, cId);
            }
            catch (Exception e)
            {
                return new Tuple<bool, Guid>(false, Guid.Empty);
            }
        }
        public static Tuple<bool, Guid> CreateCRMBotChat(BotChat transcript)
        {
            bool success = false;
            Guid chatId = Guid.Empty;

            if (crmSvc != null && crmSvc.IsReady && transcript != null)
            {
                if (transcript.existingChatID == Guid.Empty)
                {
                    Tuple<bool, Guid> res = CreateChat(transcript);
                    success = res.Item1;
                    chatId = res.Item2;
                }
                else
                {
                    Dictionary<string, object> data = crmSvc.GetEntityDataById("kbot_botchat", transcript.existingChatID, new List<string> { "kbot_transcript", "regardingobjectid" , "kbot_skypeconversationid" });
                    if (data != null)
                    {
                        Dictionary<string, CrmDataTypeWrapper> updateData = new Dictionary<string, CrmDataTypeWrapper>();
                        if (!data.ContainsKey("regardingobjectid"))
                        {
                            if (transcript.regardingId != Guid.Empty)
                            {
                                updateData.Add("regardingobjectid", new CrmDataTypeWrapper(transcript.regardingId, CrmFieldType.Lookup, "contact"));
                            }
                        }

                        foreach (var pair in data)
                        {
                            switch (pair.Key)
                            {
                                case "kbot_transcript":
                                    string original = (string)pair.Value;
                                    updateData.Add("kbot_transcript", new CrmDataTypeWrapper(original + "From: " + transcript.chatUser + Environment.NewLine +
                                                                                "Message: " + transcript.chatMessage + Environment.NewLine +
                                                                                "Time: " + transcript.timeStamp + Environment.NewLine +
                                                                                "Channel: " + transcript.channel + Environment.NewLine + Environment.NewLine,
                                                                                CrmFieldType.String));
                                    break;
                                default:
                                    break;
                            }
                        }
                        success = crmSvc.UpdateEntity("kbot_botchat", "activityid", transcript.existingChatID, updateData);
                    }
                    else
                    {
                        Tuple<bool, Guid> res = CreateChat(transcript);
                        success = res.Item1;
                        chatId = res.Item2;
                    }
                }
            }
            return new Tuple<bool, Guid>(success, chatId);
        }
    }
}