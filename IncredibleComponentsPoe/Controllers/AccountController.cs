using IncredibleComponentsPoe.Models;
using IncredibleComponentsPOE.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IncredibleComponentsPoe.Controllers
{
    public class AccountController : Controller
    {
        private readonly IncredibleComponentPoe _context;

        public AccountController(IncredibleComponentPoe context)
        {
            _context = context;
        }

        // GET: Register
        public IActionResult Register() => View();

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string username, string password, string role = "User")
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Username already exists.";
                return View();
            }

            var hashedPassword = ComputeSha256Hash(password);
            var user = new User
            {
                Username = username,
                PasswordHash = hashedPassword,
                Role = role.Trim() // must be "Admin" for admin users
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // GET: Login
        public IActionResult Login() => View();

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var hashedPassword = ComputeSha256Hash(password);
            var user = _context.Users.FirstOrDefault(u =>
                u.Username == username && u.PasswordHash == hashedPassword);

            if (user != null)
            {
                string role = user.Role?.Trim() ?? "User";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, role) // critical for [Authorize(Roles="Admin")]
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal
                );

                // Redirect based on role
                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Customer"); // admin dashboard

                return RedirectToAction("Dashboard"); // regular user
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        // GET: Dashboard for regular users
        public IActionResult Dashboard()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login");

            ViewBag.Username = User.Identity.Name;
            return View();
        }

        // GET: Admin dashboard
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            ViewBag.Username = User.Identity?.Name ?? "";
            return View();
        }

        // Logout
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Access denied
        public IActionResult AccessDenied() => View();

        // Password hashing helper
        private string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            var builder = new StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }


        
    }
}
