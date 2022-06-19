using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChessLibrary;

namespace ChessWebsite.Pages
{
    public class PlayCPUModel : PageModel
    {
        //dictionary containing all games
        private static Dictionary<string, CPUData> games = new Dictionary<string, CPUData>();
        //variable for ease of use
        private string myName;
        //variables return value to user
        public int Color { get; set; }
        public int Turn { get; set; }
        public string Pieces { get; set; }
        public string WhiteName { get; set; }
        public string BlackName { get; set; }
        public IActionResult OnGet(string type, int clientX, int clientY, char promoteTo)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Register");
                return Content("");
            }
            myName = HttpContext.User.Identity.Name;
            if (type is null || type == "")
            {
                //initialize page if necessary
                if (HttpContext.Session.GetInt32("initCpu") is null || HttpContext.Session.GetInt32("initCpu") == 0)
                    CpuInit();
                HttpContext.Session.SetInt32("cpuLastX", -1);
                HttpContext.Session.SetInt32("cpuLastY", -1);
                Color = games[myName].color;
                Turn = games[myName].board.GetTurn();
                Pieces = games[myName].board.GetPieceConfig();
                WhiteName = Color == 1 ? myName : "!CPU";
                BlackName = Color == 1 ? "!CPU" : myName;
                return Page();
            }
            //get return string
            string returnData = HandleRequest(type, clientX, clientY, promoteTo);
            if (games[myName].gameOver != "*")//Reset game is done
                HttpContext.Session.SetInt32("initCpu", 0);
            return Content(returnData);
        }
        private void CpuInit()
        {
            //create game
            games[myName] = new CPUData(new Board(), new Random().Next(0, 2) * 2 - 1);
            //setup game info
            string[] ddmmyyyy = DateTime.Now.ToString().Split(' ', 2)[0].Split('/');
            string time = $"{ddmmyyyy[2]}.{ddmmyyyy[1]}.{ddmmyyyy[0]}";
            games[myName].board.SetOneTag("Event", "CPU Game");
            games[myName].board.SetOneTag("Site", "gilboa-chess");
            games[myName].board.SetOneTag("Date", time);
            games[myName].board.SetOneTag("Round", "1");
            games[myName].board.SetOneTag("White", games[myName].color == 1 ? myName : "!CPU");
            games[myName].board.SetOneTag("Black", games[myName].color == 1 ? "!CPU" : myName);
            games[myName].board.SetOneTag("Result", "*");
            //initCpuialized session
            HttpContext.Session.SetInt32("initCpu", 1);
        }
        private string HandleRequest(string requestType, int clientX, int clientY, char promoteTo)
        {
            if (requestType == "getPgn")//special request that must be allowed after game is done
                return "gmpgn^" + games[myName].board.GetPGN();
            if (games[myName].gameOver != "*")//if game is over, no requests should be answered other than getPgn
                return "empty^";
            switch (requestType)//check request type
            {
                case "gamePress"://normal game press
                    {
                        if (clientX == -1)
                        {
                            HttpContext.Session.SetInt32("cpuLastX", -1);
                            HttpContext.Session.SetInt32("cpuLastY", -1);
                            return "empty^";
                        }
                        return HandleGamePress(clientX, clientY, promoteTo);
                    }
                case "waitForMove"://wait for opponent to play
                    {
                        string moveNotation = games[myName].board.GetBestMove(2).Item1;
                        Tuple<bool, int> gameState = games[myName].board.PlayTurn(moveNotation);
                        string pieceConfig = games[myName].board.GetPieceConfig();
                        string diffs = GetDifferences(ToConfig(pieceConfig), ToConfig(games[myName].prevPos));
                        string res = GetGameOverString(gameState);
                        UpdateGameOver(res);
                        games[myName].prevPos = pieceConfig;
                        return $"board^{pieceConfig}|{diffs}|{res}";
                    }
                case "resign"://resign, only during your turn
                    {
                        if (games[myName].color != games[myName].board.GetTurn())//check if it is your turn
                            return "empty^";
                        string pieceConfig = games[myName].board.GetPieceConfig();
                        string res = games[myName].color == 1 ? "0-1" : "1-0";
                        UpdateGameOver(res);
                        return $"board^{pieceConfig}||{res}";
                    }
                default://unknown request
                    {
                        return "empty^";
                    }
            }
        }
        private string HandleGamePress(int x, int y, char promoteTo)
        {
            if (x == -1 || games[myName].board.GetTurn() != games[myName].color) //de-highlighted square or clicked out of turn
                return "empty^";
            if (HttpContext.Session.GetInt32("cpuLastX") == -1) //doesn't have square highlighted
            {
                HttpContext.Session.SetInt32("cpuLastX", x);
                HttpContext.Session.SetInt32("cpuLastY", y);
                return "moves^" + GetMoveString(x, y);
            }
            //try to play move
            Tuple<bool, int> gameState = null;
            Piece temp = games[myName].board.GetPiece((int)HttpContext.Session.GetInt32("cpuLastX"), (int)HttpContext.Session.GetInt32("cpuLastY"));
            if (!(temp is null))
                gameState = games[myName].board.PlayTurn((int)HttpContext.Session.GetInt32("cpuLastX"), (int)HttpContext.Session.GetInt32("cpuLastY"), x, y, promoteTo);
            if (gameState is null) //move is not legal
            {
                HttpContext.Session.SetInt32("cpuLastX", x);
                HttpContext.Session.SetInt32("cpuLastY", y);
                return "moves^" + GetMoveString(x, y);
            }
            //move is legal
            HttpContext.Session.SetInt32("cpuLastX", -1);
            HttpContext.Session.SetInt32("cpuLastY", -1);
            string res = GetGameOverString(gameState);
            UpdateGameOver(res);
            string pieceConfig = games[myName].board.GetPieceConfig();
            string diffs = GetDifferences(ToConfig(pieceConfig), ToConfig(games[myName].prevPos));
            games[myName].prevPos = pieceConfig;
            return $"board^{pieceConfig}|{diffs}|{res}";
        }
        private string GetMoveString(int x, int y)
        {
            Piece temp = games[myName].board.GetPiece(x, y);
            if (temp is null || games[myName].board.GetTurn() != temp.GetColor())
                return "";
            temp.UpdateLegalMoves();
            if (temp is King k)
                k.UpdateCastles();
            temp.RemoveBadMoves();
            temp.NotateMoves();
            List<Move> moveList = temp.GetLegalMoves();
            string moveString = "";
            foreach (Move move in moveList)
                moveString += $"{move.X2},{move.Y2}|";
            if (moveString.Length > 0)
                moveString = moveString.Substring(0, moveString.Length - 1);
            return moveString;
        }
        private string GetGameOverString(Tuple<bool, int> gameState)
        {
            string gameOver = "*";
            if (gameState.Item1)
            {
                switch (gameState.Item2)
                {
                    case 1:
                        {//white wins
                            gameOver = "1-0";
                            break;
                        }
                    case -1:
                        {//black wins
                            gameOver = "0-1";
                            break;
                        }
                    case 0:
                        {//draw
                            gameOver = "1/2-1/2";
                            break;
                        }
                }
            }
            return gameOver;
        }
        private string ToConfig(string pos)
        {
            string pieces = "";
            foreach (char ch in pos)
                if (ch == '/')
                    continue;
                else if (!char.IsDigit(ch))
                    pieces += ch;
                else
                    for (int i = 0; i < ch - '0'; i++)
                        pieces += ' ';
            return pieces;
        }
        private string GetDifferences(string pos1, string pos2)
        {
            string diffString = "";
            List<int> differences = new List<int>();
            for (int i = 0; i < pos1.Length; i++)
                if (pos1[i] != pos2[i])
                    differences.Add(i);
            if (differences.Count == 2)
                diffString += $"{differences[0] % 8},{differences[0] / 8},{differences[1] % 8},{differences[1] / 8}";
            else if (differences.Count == 4)
            {
                foreach (int diff in differences)
                {
                    if (char.ToLower(pos1[diff]) == 'k' || char.ToLower(pos2[diff]) == 'k')
                        diffString += $"{diff % 8},{diff / 8},";
                }
                diffString.Substring(0, diffString.Length - 1);
            }
            return diffString;
        }
        private void UpdateGameOver(string res)
        {
            games[myName].gameOver = res;
            games[myName].board.SetOneTag("Result", res);
            if (res != "*")
            {
                bool isWhite = games[myName].color == 1;
                string whiteName = isWhite ? myName : "!CPU";
                string blackName = isWhite ? "!CPU" : myName;
                SQLClass.WriteData("Games", "(Pgn, White, Black)", $"('{games[myName].board.GetPGN()}', '{whiteName}', '{blackName}')");
            }
        }
    }
    class CPUData
    {
        public Board board { get; set; }
        public int color { get; }
        public string gameOver { get; set; }
        public string prevPos { get; set; }
        public CPUData(Board board, int color)
        {
            this.board = board;
            this.color = color;
            this.gameOver = "*";
            this.prevPos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        }
        public override string ToString()
        { return $"pieces={board},color={color},gamestate={gameOver}"; }
    }
}
