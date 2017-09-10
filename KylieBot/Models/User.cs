using System;
using System.Collections.Generic;

namespace KylieBot.Models
{
    [Serializable]
    public class User
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Token { get; set; }
        public bool WantsToBeAuthenticated { get; set; }
        public string AADEmail { get; set; }
        public string AADUsername { get; set; }
        public Guid existingChatID { get; set; }
        public Guid CRMContactId { get; set; }
        public string searchTerm { get; set; }
        public int MessageCount { get; set; }
        public string ConversationId { get; set; }
        public DateTime dateAdded { get; set; }
    }
}