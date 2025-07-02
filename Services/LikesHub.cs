// Ficheiro: Services/LikesHub.cs (ou onde preferir)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

namespace projetodweb_connectify.Services
{
    [Authorize]
    public class LikesHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public LikesHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task TogglePostLike(string postIdStr)
        {
            var username = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            var userProfile = await _context.Profiles
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(p => p.User.Username == username);

            if (userProfile == null) return;

            if (!int.TryParse(postIdStr, out int postId)) return;

            var existingLike = await _context.TopicPostLikes
                                             .FirstOrDefaultAsync(l => l.TopicPostId == postId && l.ProfileId == userProfile.Id);

            if (existingLike != null)
            {
                _context.TopicPostLikes.Remove(existingLike);
            }
            else
            {
                var newLike = new TopicPostLike
                {
                    TopicPostId = postId,
                    ProfileId = userProfile.Id
                };
                _context.TopicPostLikes.Add(newLike);
            }

            await _context.SaveChangesAsync();

            var newLikeCount = await _context.TopicPostLikes.CountAsync(l => l.TopicPostId == postId);

            // Nome do método "ReceiveLikeUpdate" corresponde ao que o JS está à espera
            await Clients.All.SendAsync("ReceiveLikeUpdate", postId, newLikeCount);
        }
    }
}