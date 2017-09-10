using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading;
using KylieBot.Models;
using System.Net.Http;
using CRMApi.Models;
using System.Collections.Generic;
using KylieBot.Helpers;

namespace KylieBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public int index;
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity lastActivity = await result as Activity;
            var userData = context.UserData;

            User retrieveUser = userData.GetValue<User>("User");

            if (string.IsNullOrEmpty(await context.GetAccessToken(AuthSettings.Scopes)))
            {
                retrieveUser.searchTerm = lastActivity.Text;
                userData.SetValue<User>("User", retrieveUser);

                await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterAuth, lastActivity, CancellationToken.None);
            }
            else
            {
                //handle extra messages
                await context.PostAsync("break");
                context.Wait(MessageReceivedAsync);
            }

            if (!string.IsNullOrEmpty(retrieveUser.Token) && lastActivity.Text == "logout")
            {
                await context.Logout();
            }
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var userData = context.UserData;

            var message = await result;
            AuthResult lResult = result as AuthResult;
            User retrieveUser = userData.GetValue<User>("User");
            retrieveUser.Token = await context.GetAccessToken(AuthSettings.Scopes);
            userData.SetValue<User>("User", retrieveUser);
            
            await context.PostAsync(message);

            await getCRMContact(context, retrieveUser);

            #region CRM Knowledge Search
            List<Attachment> kbaList = await SearchKB(context);
            if (kbaList.Count > 0)
            {
                var reply = context.MakeMessage();

                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                reply.Attachments = kbaList;

                await context.PostAsync("I found some Knowledge Articles: ");
                await context.PostAsync(reply);

                context.Wait(this.MessageReceivedAsync);
            }
            else
            {
                await context.PostAsync("I couldn't find anything :(");
                context.Wait(this.MessageReceivedAsync);
            }
            #endregion
        }

        public async Task getCRMContact(IDialogContext context, User retrieveUser)
        {
            User user = retrieveUser;

            if (user != null)
            {
                HttpClient cons = new HttpClient();
                cons.BaseAddress = new Uri("TO DO");
                cons.DefaultRequestHeaders.Accept.Clear();
                cons.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                cons.Timeout = TimeSpan.FromMinutes(1);

                using (cons)
                {
                    HttpResponseMessage res = await cons.GetAsync("CRM/GetContact/'" + user.AADEmail.ToString() + "'/");
                    if (res.IsSuccessStatusCode)
                    {
                        CRMContact contact = await res.Content.ReadAsAsync<CRMContact>();
                        user.CRMContactId = contact.ContactId;
                        context.UserData.SetValue<User>("User", user);
                    }
                }
                cons.Dispose();
            }
        }

        public async Task<List<Attachment>> SearchKB(IDialogContext context)
        {
            List<Attachment> llist = new List<Attachment>();
            User retrieveUser = context.UserData.GetValue<User>("User");

            if (retrieveUser != null)
            {
                HttpClient cons = new HttpClient();
                cons.BaseAddress = new Uri("TO DO");
                cons.DefaultRequestHeaders.Accept.Clear();
                cons.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                cons.Timeout = TimeSpan.FromMinutes(1);

                List<Models.CRMKnowledgeBaseArticle> crmKBA = new List<Models.CRMKnowledgeBaseArticle>();

                using (cons)
                {
                    HttpResponseMessage res = await cons.GetAsync("CRM/SearchKB/" + retrieveUser.searchTerm + "/");
                    if (res.IsSuccessStatusCode)
                    {
                        crmKBA = await res.Content.ReadAsAsync<List<Models.CRMKnowledgeBaseArticle>>();
                    }
                }
                cons.Dispose();

                if (crmKBA.Count > 0)
                {
                    foreach (Models.CRMKnowledgeBaseArticle kb in crmKBA)
                    {
                        Attachment a = BotHelper.GetHeroCard(
                                        kb.title + " (" + kb.articleNumber + ")",
                                        "Published: " + kb.publishedDate.ToShortDateString(),
                                        kb.description,
                                        new CardImage(url: "https://azurecomcdn.azureedge.net/cvt-5daae9212bb433ad0510fbfbff44121ac7c759adc284d7a43d60dbbf2358a07a/images/page/services/functions/01-develop.png"),
                                        new CardAction(ActionTypes.OpenUrl, "Learn more", value: "https://askkylie.microsoftcrmportals.com/knowledgebase/article/" + kb.articleNumber));
                        llist.Add(a);
                    }
                }
            }
            return llist;
        }
    }
}