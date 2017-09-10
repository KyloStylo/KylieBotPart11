using System;

namespace CRMApi.Models
{
    public class CRMKnowledgeBaseArticle
    {
        public string title { get; set; }
        public string description { get; set; }
        public DateTime publishedDate { get; set; }
        public string articleNumber { get; set; }

    }
}