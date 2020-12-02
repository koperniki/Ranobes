using System.Collections.Generic;

namespace Ranobes.Models
{
    public class Favorite
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public List<string> Status { get; set; }

        public string Url { get; set; }

        public string ChaptersUrl { get; set; }

        public List<Chapter> Chapters { get; set; }
    }
}