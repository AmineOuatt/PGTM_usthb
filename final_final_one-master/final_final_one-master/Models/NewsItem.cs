using System;

namespace DataGridNamespace.Models
{
    public class NewsItem
    {
        public int NewsItemID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PublishedDate { get; set; }
        public string Author { get; set; } // Nullable in database
    }
} 