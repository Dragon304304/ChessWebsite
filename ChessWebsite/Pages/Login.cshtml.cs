using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SQLite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ChessWebsite.Pages
{
    public class LoginModel : PageModel
    {
        public string ErrorMsg = "";
        public void OnGet()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                Response.Redirect("/");
        }
        public void OnPost(string username, string password)
        {
            //user exists
            SQLiteDataReader sqlite_datareader = SQLClass.ReadData("Users", "*", "");
            while (sqlite_datareader.Read())
            {
                if (sqlite_datareader.GetString(0) == username && sqlite_datareader.GetString(1) == password)
                {
                    Authenticate(username);
                    Response.Redirect("/");
                    break;
                }
            }
            ErrorMsg = "Your username or password is incorrect.";
        }
        public async void Authenticate(string username)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "User"),
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = false,
                IsPersistent = false,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            Response.Redirect("/");
        }
    }
}
