using System.Collections.Generic;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Models
{
    public class Feed
    {
        public List<TopicPost> GeneralPosts { get; set; } = new List<TopicPost>();
        public List<TopicPost> FriendsPosts { get; set; } = new List<TopicPost>();
        public int? CurrentUserProfileId { get; set; }
        public bool IsUserAuthenticated { get; set; }
        public bool UserHasFriends { get; set; }
        public List<Profile> FriendshipSuggestions { get; set; } = new List<Profile>();
    }
}
