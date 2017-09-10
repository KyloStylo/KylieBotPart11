using KylieBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KylieBot.Helpers
{
    public class BotLogger
    {
        public async Task Log(Activity activity)
        {
            Activity x = activity;

            StateClient stateClient = x.GetStateClient();
            BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
            User user = userData.GetProperty<User>("User");

            BotChat chat = new BotChat(x.From.Name == null ? "" : x.From.Name.ToString() + "(" + 
                                        x.From.Id == null ? "" :  x.From.Id.ToString() + ")", 
                                        x.Text == null ? "" : x.Text, 
                                        x.ChannelId == null ? "" : x.ChannelId.ToString(), 
                                        x.Timestamp == null ? DateTime.Now : x.Timestamp.Value,
                                        x.Conversation.Id);

            if (user.existingChatID != Guid.Empty && user.ConversationId != string.Empty && user.ConversationId == activity.Conversation.Id)
            {
                chat.existingChatID = user.existingChatID;
            }
            if (user.CRMContactId != Guid.Empty)
            {
                chat.regardingId = user.CRMContactId;
            }

            HttpClient cons = new HttpClient();
            cons.BaseAddress = new Uri("TO DO");
            cons.DefaultRequestHeaders.Accept.Clear();
            cons.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cons.Timeout = TimeSpan.FromMinutes(1);

            using (cons)
            {
                var content = new StringContent(JsonConvert.SerializeObject(chat), Encoding.UTF8, "application/json");
                HttpResponseMessage res = await cons.PostAsync("CRM/CreateBotChat", content);
                if (res.IsSuccessStatusCode)
                {
                    Tuple<bool, Guid> result = await res.Content.ReadAsAsync<Tuple<bool, Guid>>();
                    if (user.existingChatID == Guid.Empty && result != null && result.Item1 && result.Item2 != Guid.Empty)
                    {
                        user.existingChatID = result.Item2;
                        userData.SetProperty<User>("User", user);
                    }
                }
            }
            cons.Dispose();

            await stateClient.BotState.SetUserDataAsync(x.ChannelId, x.From.Id, userData);
        }
    }
}