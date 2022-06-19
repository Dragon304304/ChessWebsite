using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChessLibrary;

namespace ChessWebsite.Pages
{
    public class AnalysisModel : PageModel
    {
        public int MoveCount;
        public int Color;
        public IActionResult OnGet(bool isNotStartup, int index, bool names)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Register");
                return Content("");
            }
            if (!isNotStartup)
            {
                PageInit();
                return Page();
            }
            string returnData;
            if (names)
            {
                string whitePlayerName = HttpContext.Session.GetString("whiteName");
                string blackPlayerName = HttpContext.Session.GetString("blackName");
                returnData = $"names^{whitePlayerName}|{blackPlayerName}";
            }
            else 
            {
                string[] positions = HttpContext.Session.GetString("pos").Split('*');
                returnData = $"board^{positions[index]}";
            }
            return Content(returnData);
        }
        private void PageInit()
        {
            Board board = new Board();
            string pgn = Request.QueryString.ToString();
            pgn = pgn.Replace("?pgn=", "");
            pgn = pgn.Replace("%20", " ");
            pgn = pgn.Replace("%5B", "[");
            pgn = pgn.Replace("%5D", "]");
            pgn = pgn.Replace("%22", "\"");
            pgn = pgn.Replace("%2F", "/");
            pgn = pgn.Replace("%2C", ",");
            pgn = pgn.Replace("%3D", "=");
            try 
            {
                board.PlayByPGN(pgn);
            }
            catch
            {
                return;
            }
            HttpContext.Session.SetString("whiteName", board.GetTag("White"));
            HttpContext.Session.SetString("blackName", board.GetTag("Black"));
            string positions = "";
            string prevPos = "";
            foreach (string pos in board.GetPositions())
            {
                positions += pos + '|';
                string pieces = "";
                foreach (char ch in pos)
                    if (ch == '/')
                        continue;
                    else if (!char.IsDigit(ch))
                        pieces += ch;
                    else
                        for (int i = 0; i < ch - '0'; i++)
                            pieces += ' ';
                List<int> differences = new List<int>();
                if (prevPos != "")
                {
                    for (int i = 0; i < pieces.Length; i++)
                        if(pieces[i] != prevPos[i])
                            differences.Add(i);
                    if (differences.Count == 2)
                        positions += $"{differences[0] % 8},{differences[0] / 8},{differences[1] % 8},{differences[1] / 8}";
                    else if (differences.Count == 4)
                    {
                        foreach (int diff in differences)
                        {
                            if(char.ToLower(pieces[diff]) == 'k' || char.ToLower(prevPos[diff]) == 'k')
                                positions += $"{diff % 8},{diff / 8},";
                        }
                        positions.Substring(0, positions.Length - 1);
                    }
                }
                prevPos = pieces;
                positions += '*';
            }
            positions = positions.Substring(0, positions.Length - 1);
            HttpContext.Session.SetString("pos", positions);
            MoveCount = board.GetPositions().Count;
            Color = 1;
            if (HttpContext.User.Identity.Name == HttpContext.Session.GetString("blackName"))
                Color = -1;
        }
    }
}