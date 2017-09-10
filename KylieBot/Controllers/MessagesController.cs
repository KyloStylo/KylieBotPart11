using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using KylieBot.Dialogs;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs.Internals;
using KylieBot.Models;
using Autofac;
using KylieBot.Helpers;
using System.Configuration;

namespace KylieBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            StateClient stateClient = activity.GetStateClient();
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
            bool userCreated = userData.GetProperty<bool>("UserCreated");
            User user = null;

            if (!userCreated)
            {
                user = BotHelper.createUser(activity);
                userData.SetProperty<User>("User", user);
                userData.SetProperty<bool>("UserCreated", true);
                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
            }
            else
            {
                user = userData.GetProperty<User>("User");
            }

            if (activity.Type == ActivityTypes.Message)
            {
                int messageCount = user.MessageCount;
                user.MessageCount = messageCount + 1;
                userData.SetProperty<User>("User", user);
                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                if (messageCount > 0)
                {
                    if (messageCount < 2)
                    {
                        var connectionWait = activity.CreateReply("Working on it...");
                        await connector.Conversations.ReplyToActivityAsync(connectionWait);
                    }

                    await new BotLogger().Log(activity);
                }

                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else
            {
                await HandleSystemMessageAsync(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessageAsync(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData) { }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                List<User> memberList = new List<User>();

                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    var client = scope.Resolve<IConnectorClient>();
                    var activityMembers = await client.Conversations.GetConversationMembersAsync(message.Conversation.Id);

                    foreach (var member in activityMembers)
                    {
                        memberList.Add(new User() { Id = member.Id, Name = member.Name });
                    }

                    if (message.MembersAdded != null && message.MembersAdded.Any(o => o.Id == message.Recipient.Id))
                    {
                        var intro = message.CreateReply("Hello **" + message.From.Name + "**! I am **Kylie Bot (KB)**. \n\n What can I assist you with?");
                        await connector.Conversations.ReplyToActivityAsync(intro);
                    }
                }

                if (message.MembersAdded != null && message.MembersAdded.Any() && memberList.Count > 2)
                {
                    var added = message.CreateReply(message.MembersAdded[0].Name + " joined the conversation");
                    await connector.Conversations.ReplyToActivityAsync(added);
                }

                if (message.MembersRemoved != null && message.MembersRemoved.Any())
                {
                    var removed = message.CreateReply(message.MembersRemoved[0].Name + " left the conversation");
                    await connector.Conversations.ReplyToActivityAsync(removed);
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate) { }
            else if (message.Type == ActivityTypes.Typing) { }
            else if (message.Type == ActivityTypes.Ping)
            {
                Activity reply = message.CreateReply();
                reply.Type = ActivityTypes.Ping;
                return reply;
            }
            return null;
        }
    }
}