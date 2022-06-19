using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SQLite;
using ChessLibrary;

namespace ChessWebsite.Pages
{
    public class ProfileModel : PageModel
    {
        public string Username;
        public int GameCount;
        public IActionResult OnGet(bool isRedirect, int index = -1)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Register");
                return Content("");
            }
            bool init = index < 0;
            if (init)
            {
                string username = Request.QueryString.Value.Replace("?user=", "");
                if (username == "")
                    username = HttpContext.User.Identity.Name;
                HttpContext.Session.SetString("user", username);
                Username = username;
            }
            List<Board> pastGames = GetProfileGames();
            if (init || index > pastGames.Count)
            {
                GameCount = pastGames.Count;
                return Page();
            }
            if (isRedirect)
            {
                string pgn = pastGames[index].GetPGN();
                return Content($"goto^{pgn}");
            }
            string whiteName = pastGames[index].GetTag("White");
            string blackName = pastGames[index].GetTag("Black");
            string res = pastGames[index].GetTag("Result");
            return Content($"game^{whiteName}|{blackName}|{res}");
        }
        public List<Board> GetProfileGames()
        {
            List<Board> pastGames = new List<Board>();
            string username = HttpContext.Session.GetString("user");
            SQLiteDataReader sqlite_reader = SQLClass.ReadData("Games", "*", $"White == '{username}' OR Black == '{username}'");
            while (sqlite_reader.Read())
            {
                Board board = new Board();
                board.PlayByPGN(sqlite_reader.GetString(0));
                board.SetOneTag("White", sqlite_reader.GetString(1));
                board.SetOneTag("Black", sqlite_reader.GetString(2));
                pastGames.Add(board);
            }
            return pastGames;
        }
    }
}
