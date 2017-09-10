using KylieBot.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KylieBot.Helpers
{
    public class BotHelper
    {
        public static Attachment GetHeroCard(string title, string subtitle, string text, CardImage cardImage, CardAction cardAction)
        {
            var heroCard = new HeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Images = new List<CardImage>() { cardImage },
                Buttons = new List<CardAction>() { cardAction },
            };

            return heroCard.ToAttachment();
        }

        public static Attachment GetThumbnailCard(string title, string subtitle, string text, CardImage cardImage, CardAction cardAction)
        {
            var heroCard = new ThumbnailCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Images = new List<CardImage>() { cardImage },
                Buttons = new List<CardAction>() { cardAction },
            };

            return heroCard.ToAttachment();
        }

        public static User createUser(Activity act)
        {
            Activity activity = act;

            Models.User u = new Models.User();
            u.MessageCount++;
            u.ConversationId = activity.Conversation.Id;
            u.Name = activity.From.Name;
            u.Id = activity.From.Id;
            u.MessageCount = 1;
            u.dateAdded = DateTime.Now;
            return u;
        }
    }
}