using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SQLite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ChessWebsite.Pages
{
    public class RegisterModel : PageModel
    {
        public string ErrorMsg = "";
        public void OnGet()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                Response.Redirect("/");
        }
        public void OnPost(string username, string password, string pconfirm)
        {   
            //password and confirmation match
            if (password != pconfirm)
            {
                ErrorMsg = "Passwords do not match.";
                return;
            }
            //username is valid
            if (username is null || username.Length < 1)
            {
                ErrorMsg = "Username must be at least 1 character long.";
                return;
            }
            if (username.Length > 20)
            {
                ErrorMsg = "Username cannot exceed 20 characters.";
                return;
            }
            foreach (char ch in username)
                if (!(ch >= 'a' && ch <= 'z') && !(ch >= 'A' && ch <= 'Z') && !(ch >= '0' && ch <= '9'))
                    {
                        ErrorMsg = "Please only include letters and numbers in your username.";
                        return;
                }
            //password is valid
            if (password is null || password.Length < 8)
            {
                ErrorMsg = "Password must be at least 8 characters long.";
                return;
            }
            if (password.Length > 20)
            {
                ErrorMsg = "Password cannot exceed 20 characters.";
                return;
            }
            foreach (char ch in password)
                if(!(ch >= 'a' && ch <= 'z') && !(ch >= 'A' && ch <= 'Z') && !(ch >= '0' && ch <= '9'))
                {
                    ErrorMsg = "Please only include letters and numbers in your password.";
                    return;
                }
            //username is not taken
            SQLiteDataReader sqlite_datareader = SQLClass.ReadData("Users", "Username", "");
            while (sqlite_datareader.Read())
            {
                if (sqlite_datareader.GetString(0) == username)
                {
                    ErrorMsg = "This username is already taken.";
                    return;
                }
            }
            SQLClass.WriteData("Users", "(Username, Password)", $"('{username}', '{password}')");
            Authenticate(username);
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