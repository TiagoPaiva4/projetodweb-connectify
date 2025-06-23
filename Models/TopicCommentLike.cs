using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace projetodweb_connectify.Models;

public class TopicCommentLike
{
    [Required]
    public int TopicCommentId { get; set; }
    [ForeignKey(nameof(TopicCommentId))]
    public virtual TopicComment TopicComment { get; set; }

    // This links the "Like" to the user's Profile
    [Required]
    public int ProfileId { get; set; }
    [ForeignKey(nameof(ProfileId))]
    public virtual Profile Profile { get; set; }
}