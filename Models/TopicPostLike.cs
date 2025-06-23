using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models;

public class TopicPostLike
{
    
    [Required] 
    public int TopicPostId { get; set; }
    [ForeignKey(nameof(TopicPostId))]
    public virtual TopicPost TopicPost { get; set; }

    // This links the "Like" to the user's Profile
    [Required]
    public int ProfileId { get; set; }
    [ForeignKey(nameof(ProfileId))]
    public virtual Profile Profile { get; set; }

}