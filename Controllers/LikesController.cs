using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projetodweb_connectify.Data;
using projetodweb_connectify.Models;
using System.Security.Claims; // Required for getting the logged-in user's ID

namespace projetodweb_connectify.Controllers
{
    [Authorize] // IMPORTANT: Ensures only logged-in users can perform these actions.
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// A helper method to get the current user's Profile ID.
        /// This is the corrected version that handles the string-to-int ID conversion.
        /// </summary>
        /// <returns>The integer ID of the current user's Profile, or null if not found.</returns>
        private int? GetCurrentProfileId()
        {
            // Step 1: Get the USERNAME from the login cookie. This is our reliable link.
            var identityUserName = User.Identity?.Name;
    
            // If we can't get a username, they are not properly logged in.
            if (string.IsNullOrEmpty(identityUserName))
            {
                return null;
            }

            // Step 2: Find the user in YOUR custom 'Users' table that has this username.
            // This assumes the 'Username' column in your 'Users' table matches the login name.
            var appUser = _context.Users.FirstOrDefault(u => u.Username == identityUserName);
    
            // If no user is found, it means there's a data mismatch.
            if (appUser == null)
            {
                return null;
            }

            // Step 3: Now that we have your custom user (with its 'int' ID), find the associated profile.
            // This is a correct int-to-int comparison.
            var profile = _context.Profiles.FirstOrDefault(p => p.UserId == appUser.Id);
    
            // Step 4: Return the Profile's ID. If no profile exists for that user, this will return null.
            return profile?.Id;
        }

        /// <summary>
        /// Handles both liking and unliking a post.
        /// </summary>
        /// <param name="id">The ID of the TopicPost to like/unlike.</param>
        [HttpPost]
        [ValidateAntiForgeryToken] // Security measure against CSRF attacks.
        public async Task<IActionResult> TogglePostLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                // If we can't get a profile ID, the user is not authorized.
                return Unauthorized(new { message = "You must be logged in to like a post." });
            }

            // Check if a "like" from this profile for this post already exists.
            var existingLike = await _context.TopicPostLikes
                .FirstOrDefaultAsync(l => l.TopicPostId == id && l.ProfileId == profileId.Value);

            if (existingLike != null)
            {
                // If it exists, the user is "unliking" the post. Remove it.
                _context.TopicPostLikes.Remove(existingLike);
            }
            else
            {
                // If it doesn't exist, the user is "liking" the post. Add a new one.
                var newLike = new TopicPostLike
                {
                    TopicPostId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicPostLikes.Add(newLike);
            }

            await _context.SaveChangesAsync(); // Commit the change to the database.

            // Get the new, updated count of likes for the post.
            var newLikeCount = await _context.TopicPostLikes.CountAsync(l => l.TopicPostId == id);
            
            // Return the new count to the front-end.
            return Json(new { count = newLikeCount });
        }

        /// <summary>
        /// Handles both liking and unliking a comment.
        /// </summary>
        /// <param name="id">The ID of the TopicComment to like/unlike.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCommentLike(int id)
        {
            var profileId = GetCurrentProfileId();
            if (profileId == null)
            {
                return Unauthorized(new { message = "You must be logged in to like a comment." });
            }

            // Check if a "like" from this profile for this comment already exists.
            var existingLike = await _context.TopicCommentLikes
                .FirstOrDefaultAsync(l => l.TopicCommentId == id && l.ProfileId == profileId.Value);

            if (existingLike != null)
            {
                // Unlike the comment.
                _context.TopicCommentLikes.Remove(existingLike);
            }
            else
            {
                // Like the comment.
                var newLike = new TopicCommentLike
                {
                    TopicCommentId = id,
                    ProfileId = profileId.Value
                };
                _context.TopicCommentLikes.Add(newLike);
            }

            await _context.SaveChangesAsync();

            // Get the new, updated count of likes for the comment.
            var newLikeCount = await _context.TopicCommentLikes.CountAsync(l => l.TopicCommentId == id);
            
            // Return the new count to the front-end.
            return Json(new { count = newLikeCount });
        }
    }
}