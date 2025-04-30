using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Zoom.ViewModel;
using Zoom.Helper;
using System.Security.Claims;

namespace Zoom.Controllers
{
    public class AccountController : Controller
    {
        private readonly LDAPSetting lDAPSetting;
        private readonly LdapAuthClient _ldapAuthClient;

        public AccountController(IOptions<LDAPSetting> lDAPSetting, LdapAuthClient ldapAuthClient)
        {
            //this.signInManager = signInManager;
            //this.userManager = userManager;
            this.lDAPSetting = lDAPSetting.Value;
            this._ldapAuthClient = ldapAuthClient;

        }


        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Call the LDAP authentication method
                var result = await _ldapAuthClient.Authenticate(model.Email, model.Password);

                if (result)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, model.Email) // Store email in the claim
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Store claims in the cookie
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Store email in a cookie for later use
                    Response.Cookies.Append("UserEmail", model.Email, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(30),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                    // Add email to ViewBag for the next page
                    ViewBag.Email = model.LoginAsOthers ? model.EmailAs : model.Email;

                    // Redirect to the Home/Index page after successful login
                    return RedirectToAction("Index", "Home", new { email = model.LoginAsOthers ? model.EmailAs : model.Email });
                }
                else
                {
                    ModelState.AddModelError("", "Invalid Email or password.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Sign out the user and remove the authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the login page after logout
            return RedirectToAction("Login", "Account");
        }


    }
}
