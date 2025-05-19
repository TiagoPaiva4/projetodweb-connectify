// Areas/Identity/Pages/Account/Login.cshtml.cs
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks; // Added for Task
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace projetodweb_connectify.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Ensure this page is accessible without login
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager; // Added UserManager
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager,
                          ILogger<LoginModel> logger,
                          UserManager<IdentityUser> userManager) // Added UserManager
        {
            _userManager = userManager; // Added UserManager
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O campo Email é obrigatório.")]
            [EmailAddress(ErrorMessage = "O Email não é um endereço de email válido.")]
            public string Email { get; set; } // This will be used to FIND the user

            [Required(ErrorMessage = "O campo Password é obrigatório.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Lembrar-me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList(); // Populate for redisplay if needed

            if (ModelState.IsValid)
            {
                // *** THIS IS THE KEY CHANGE ***
                // Attempt to find the user by their email address first.
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    // User not found by email, this is an invalid attempt.
                    // Don't reveal that the user does not exist or is not confirmed.
                    ModelState.AddModelError(string.Empty, "Tentativa de login inválida.");
                    return Page();
                }

                // Now, use the user's actual UserName (which you set during registration) for PasswordSignInAsync.
                // The 'Input.Email' from the form is only used to find the user.
                // The 'user.UserName' is what Identity stored in the UserName column.
                var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User '{user.UserName}' logged in."); // Log with actual username
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"User account '{user.UserName}' locked out.");
                    return RedirectToPage("./Lockout");
                }
                if (result.IsNotAllowed) // This can happen if email is not confirmed and RequireConfirmedAccount is true
                {
                    _logger.LogWarning($"User '{user.UserName}' login not allowed. Email confirmed: {user.EmailConfirmed}");
                    ModelState.AddModelError(string.Empty, "Tentativa de login inválida. Verifique se o seu email foi confirmado.");
                    // You could also add logic here to resend confirmation if you want.
                    // For example, redirect to a page that explains email needs confirmation
                    // and offers to resend the link.
                    // return RedirectToPage("./ResendEmailConfirmation", new { Email = Input.Email });
                    return Page();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tentativa de login inválida.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}