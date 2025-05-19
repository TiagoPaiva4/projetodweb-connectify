// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

using projetodweb_connectify.Data;
using projetodweb_connectify.Models;

using static System.Runtime.InteropServices.JavaScript.JSType;

using projetodweb_connectify.Services.Email;




namespace projetodweb_connectify.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ICustomEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
          UserManager<IdentityUser> userManager,
          IUserStore<IdentityUser> userStore,
          SignInManager<IdentityUser> signInManager,
          ILogger<RegisterModel> logger,
          ICustomEmailSender emailSender,
          ApplicationDbContext context
          ) {
          _userManager = userManager;
          _userStore = userStore;
          _emailStore = GetEmailStore();
          _signInManager = signInManager;
          _logger = logger;
          _emailSender = emailSender;
          _context = context;
        }


        /// <summary>
        /// este objeto será usado para fazer a transposição de dados entre este
        /// ficheiro (de programação) e a sua respetiva visualização
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// se for instanciado, este atributo terá o link para onde a aplicação
        /// será redirecionada, após Registo
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Se estiver especificado a Autenticação por outros fornecedores
        /// de autenticação, este atributo terá essa lista de outros fornecedores
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }


        /// <summary>
        /// define os atributos que estarão presentes na interface da página
        /// </summary>
        public class InputModel
        {
            /*
            // Adicione os campos necessários para a sua entidade Users
            [Required(ErrorMessage = "O {0} é de preenchimento obrigatório.")]
            [MaxLength(50)]
            [Display(Name = "Nome de Utilizador")]
            public string Username { get; set; }*/

            /// <summary>
            /// email do novo utilizador
            /// </summary>
            [Required(ErrorMessage = "O {0} é de preenchimento obrigatório.")]
            [EmailAddress(ErrorMessage = "Tem de escrever um {0} válido.")]
            [Display(Name = "Email")]
            public string Email { get; set; }


            /// <summary>
            /// password associada ao utilizador
            /// </summary>
            [Required(ErrorMessage = "O {0} é de preenchimento obrigatório.")]
            [StringLength(20, ErrorMessage = "A {0} tem de ter, pelo menos, {2} e um máximo de {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            /// confirmação da password
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar password")]
            [Compare(nameof(Password), ErrorMessage = "A password e a sua confirmação não coincidem.")]
            public string ConfirmPassword { get; set; }


            /// <summary>
            /// Incorporação dos dados de um Utilizador
            /// no formulário de Registo
            /// </summary>
            [Required]
            public Users User { get; set; }
        }

        /// <summary>
        /// Este método 'responde' aos pedidos feitos em HTTP GET
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// Este método 'responde' aos pedidos do browser, quando feitos em HTTP POST
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            // ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList(); // Usually for GET

            if (ModelState.IsValid)
            {
                var identityUser = CreateUser(); // This is an IdentityUser

                // Correctly set UserName for AspNetUsers from the form's Username field
                // Use _userStore for SetUserNameAsync
                await _userStore.SetUserNameAsync(identityUser, Input.User.Username, CancellationToken.None);
                // Correctly set Email for AspNetUsers from the form's Email field
                await _emailStore.SetEmailAsync(identityUser, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(identityUser, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Populate your custom Users table
                    // Input.User.Username is already set from form binding.
                    // Input.User.Phone is already set from form binding.

                    // ***** CORRECTLY POPULATE EMAIL FOR YOUR CUSTOM USERS TABLE *****
                    Input.User.Email = Input.Email;

                    // The PasswordHash should NOT be set here manually if you removed it from Users.cs
                    // Input.User.PasswordHash = "somehash"; // DO NOT DO THIS if Identity handles passwords

                    // Ensure CreatedAt is set (though it has a default)
                    Input.User.CreatedAt = DateTime.UtcNow;

                    try
                    {
                        _context.Add(Input.User); // Input.User now has Username, Email, Phone
                        await _context.SaveChangesAsync(); // This will generate Input.User.Id

                        int registeredUserId = Input.User.Id;

                        var newProfile = new Profile
                        {
                            UserId = registeredUserId,
                            Name = Input.User.Username, // Use username for initial profile name, or make it empty
                            Type = "Pessoal",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Profiles.Add(newProfile);
                        await _context.SaveChangesAsync(); // This will generate newProfile.Id

                        var personalTopic = new Topic
                        {
                            Title = "Publicações Pessoais",
                            Description = "Tópico privado para o utilizador.",
                            CreatedBy = newProfile.Id, // Use the ID of the just-created profile
                            IsPersonal = true,
                            IsPrivate = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Topics.Add(personalTopic);
                        await _context.SaveChangesAsync();

                        // Email confirmation logic
                        var userId = await _userManager.GetUserIdAsync(identityUser);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                            protocol: Request.Scheme);

                        // Use Input.Email for sending the confirmation, and Input.User.Username for display
                        await _emailSender.SendEmailAsync(Input.Email, Input.User.Username ?? Input.Email, callbackUrl);

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                        }
                        else
                        {
                            await _signInManager.SignInAsync(identityUser, isPersistent: false);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the detailed error
                        _logger.LogError(ex, "Error saving custom user data, profile, or topic.");
                        // Potentially roll back Identity user creation or mark it for cleanup
                        await _userManager.DeleteAsync(identityUser); // Basic rollback
                        ModelState.AddModelError(string.Empty, "Ocorreu um erro ao guardar os dados do utilizador. Tente novamente.");
                        // return Page(); // Or rethrow if you want a generic error page
                    }
                }
                else // if result for _userManager.CreateAsync did not succeed
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Cria um objeto vazio do tipo IdentityUser
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
