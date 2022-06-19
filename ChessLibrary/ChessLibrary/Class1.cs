using System;
using System.Collections.Generic;

namespace ChessLibrary
{
    public class Board
    {
        private Piece[,] pieces = new Piece[8, 8];
        private King whiteKing;
        private King blackKing;
        private int player;
        private List<string> moveList = new List<string>();
        private string pieceConfigString;
        private List<string> prevPos = new List<string>();
        private int fiftyMoveCounter;
        private Dictionary<string, string> tags = new Dictionary<string, string>();
        public Board()
        {
            //Pieces
            SetupPieces();
            //Other
            player = 1;
            fiftyMoveCounter = 0;
            tags = new Dictionary<string, string>()
            {
                ["Event"] = "",
                ["Site"] = "",
                ["Date"] = "",
                ["Round"] = "",
                ["White"] = "",
                ["Black"] = "",
                ["Result"] = ""
            };
            UpdatePieceConfig();
            prevPos.Add(pieceConfigString);
        }
        public Board(Board original)
        {
            //copy pieces
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    Piece piece = original.pieces[x, y];
                    if (piece is null)
                        pieces[x, y] = null;
                    else
                        pieces[x, y] = piece.GetNewPiece(this);
                    if (pieces[x, y] is King king)
                    {
                        if (pieces[x, y].GetColor() == 1) whiteKing = king;
                        else blackKing = king;
                    }
                }
            //copy other
            pieceConfigString = original.pieceConfigString;
            foreach (string move in original.moveList)
                moveList.Add(move);
            foreach (string pos in original.prevPos)
                prevPos.Add(pos);
            player = original.player;
            fiftyMoveCounter = original.fiftyMoveCounter;
            tags = new Dictionary<string, string>()
            {
                ["Event"] = "",
                ["Site"] = "",
                ["Date"] = "",
                ["Round"] = "",
                ["White"] = "",
                ["Black"] = "",
                ["Result"] = ""
            };
            if (!(tags is null))
                foreach (KeyValuePair<string, string> tag in original.tags)
                    tags[tag.Key] = tag.Value;
        }
        private void SetupPieces()
        {
            //Pawns
            for (int i = 0; i < 8; i++)
                pieces[i, 1] = new Pawn(this, 1, i, 1);
            for (int i = 0; i < 8; i++)
                pieces[i, 6] = new Pawn(this, -1, i, 6);
            //Rooks
            pieces[0, 0] = new Rook(this, 1, 0, 0);
            pieces[7, 0] = new Rook(this, 1, 7, 0);
            pieces[0, 7] = new Rook(this, -1, 0, 7);
            pieces[7, 7] = new Rook(this, -1, 7, 7);
            //Knights
            pieces[1, 0] = new Knight(this, 1, 1, 0);
            pieces[6, 0] = new Knight(this, 1, 6, 0);
            pieces[1, 7] = new Knight(this, -1, 1, 7);
            pieces[6, 7] = new Knight(this, -1, 6, 7);
            //Bishops
            pieces[2, 0] = new Bishop(this, 1, 2, 0);
            pieces[5, 0] = new Bishop(this, 1, 5, 0);
            pieces[2, 7] = new Bishop(this, -1, 2, 7);
            pieces[5, 7] = new Bishop(this, -1, 5, 7);
            //Queens
            pieces[3, 0] = new Queen(this, 1, 3, 0);
            pieces[3, 7] = new Queen(this, -1, 3, 7);
            //Kings
            whiteKing = new King(this, 1, 4, 0);
            pieces[4, 0] = this.whiteKing;
            blackKing = new King(this, -1, 4, 7);
            pieces[4, 7] = this.blackKing;
        }
        public Tuple<bool, int> PlayTurn(int x1, int y1, int x2, int y2, char promoteTo)
        {
            //check if legal
            Piece movingPiece = pieces[x1, y1];
            if (movingPiece is null || !movingPiece.InLegalMoves(x2, y2))
                return null;
            //save move
            string moveNotation = ToNotation(x1, y1, x2, y2, promoteTo);
            moveList.Add(moveNotation);
            //move piece
            MovePiece(x1, y1, x2, y2, promoteTo);
            //check result
            Tuple<bool, int> gameState = CheckGameOver();
            return gameState;
        }
        public Tuple<bool, int> PlayTurn(string moveNotation)
        {
            //get move
            Move move = FromNotation(moveNotation);
            //check if legal
            if (move is null)
                return null;
            Piece movingPiece = pieces[move.X1, move.Y1];
            if (movingPiece is null || !movingPiece.InLegalMoves(move.X2, move.Y2))
                return null;
            //save move
            moveList.Add(moveNotation);
            //move piece
            MovePiece(move.X1, move.Y1, move.X2, move.Y2, move.PromoteTo);
            //check result
            Tuple<bool, int> gameState = CheckGameOver();
            return gameState;
        }
        public void MovePiece(int x1, int y1, int x2, int y2, char promoteTo)
        {
            Piece movingPiece = pieces[x1, y1];
            //update x y values
            movingPiece.UpdateCoords(x2, y2);
            //update fifty move counter and previous positions
            bool isPawn = movingPiece is Pawn;
            bool moveToEmpty = pieces[x2, y2] is null;
            if (isPawn || !moveToEmpty)
                fiftyMoveCounter = 0;
            else
                fiftyMoveCounter++;
            if (isPawn)//check en passant
            {
                if (x1 != x2 && moveToEmpty)
                    pieces[x2, y1] = null;
            }//check for castling
            else if (movingPiece is King)
            {
                int dif = x2 - x1;
                if (dif == 2)
                {
                    pieces[5, y1] = pieces[7, y1];
                    pieces[7, y1] = null;
                    pieces[5, y1].UpdateCoords(5, y1);
                }
                else if (dif == -2)
                {
                    pieces[3, y1] = pieces[0, y1];
                    pieces[0, y1] = null;
                    pieces[3, y1].UpdateCoords(3, y1);
                }
            }
            //check for castling delegalization
            if (y2 == 0)
            {
                if (x2 == 7) whiteKing.canCastleShort = false;
                else if (x2 == 0) whiteKing.canCastleLong = false;
            }
            else if (y2 == 7)
            {
                if (x2 == 7) blackKing.canCastleShort = false;
                else if (x2 == 0) blackKing.canCastleLong = false;
            }
            //move piece
            int pieceColor = movingPiece.GetColor();
            if (!(movingPiece is Pawn) || y2 != 3.5 * pieceColor + 3.5)
                pieces[x2, y2] = movingPiece;
            else //promotion
            {
                switch (promoteTo)
                {
                    case 'N':
                        {
                            pieces[x2, y2] = new Knight(this, pieceColor, x2, y2);
                            break;
                        }
                    case 'B':
                        {
                            pieces[x2, y2] = new Bishop(this, pieceColor, x2, y2);
                            break;
                        }
                    case 'R':
                        {
                            pieces[x2, y2] = new Rook(this, pieceColor, x2, y2);
                            break;
                        }
                    default:
                        {
                            pieces[x2, y2] = new Queen(this, pieceColor, x2, y2);
                            break;
                        }
                }
            }
            pieces[x1, y1] = null;
            UpdatePieceConfig();
            prevPos.Add(pieceConfigString);
            //make en passant unavailable for all pawns other than last
            foreach (Piece piece in pieces)
                if (piece is Pawn pawn && !(piece == movingPiece))
                    pawn.ResetEnPassant();
            //update turn
            player *= -1;
        }
        private Tuple<bool, int> CheckGameOver()
        {
            //checkmate or stalemate
            if (HasNoLegalMoves(player))
                return Tuple.Create(true, IsInCheck(player) ? -player : 0);
            //no significant board change/repeated position/no player can win
            if (fiftyMoveCounter == 101 || CheckRepetition() || InsufficientMaterial())
                return Tuple.Create(true, 0);
            //game continues
            return Tuple.Create(false, 0);

        }
        public void UpdatePieceMoves(int color, bool protectKing, bool addNotation)
        {
            foreach (Piece piece in pieces)
                if (!(piece is null))
                {
                    if (piece.GetColor() == color)
                    {
                        //update piece moves
                        piece.UpdateLegalMoves();
                        //update castles if relevant
                        if (protectKing && piece is King tempKing)
                            tempKing.UpdateCastles();
                        //remove moves that put king in danger if relevant
                        if (protectKing)
                            piece.RemoveBadMoves();
                        //notate moves if relevant
                        if (addNotation)
                            piece.NotateMoves();
                    }
                    else piece.ResetLegalMoves();
                }
        }
        public bool HasNoLegalMoves(int color)
        {
            Board testBoard = new Board(this);
            testBoard.UpdatePieceMoves(color, true, true);
            foreach (Piece piece in testBoard.pieces)
                if (!(piece is null) && piece.GetColor() == color && piece.GetLegalMoves().Count != 0)
                    return false;
            return true;
        }
        public bool IsInCheck(int color)
        {
            Board testBoard = new Board(this);
            testBoard.UpdatePieceMoves(-color, false, false);
            //get relevant king
            King king = color == 1 ? testBoard.whiteKing : testBoard.blackKing;
            foreach (Piece piece in testBoard.pieces)
            {
                //check if piece is relevant
                if (piece is null || piece.GetColor() == color) continue;
                //check if piece has move that captures king
                foreach (Move move in piece.GetLegalMoves())
                    if (move.X2 == king.GetX() && move.Y2 == king.GetY())
                        return true;
            }
            return false;
        }
        public bool CheckRepetition()
        {
            int repeatCounter = 0;
            foreach (string pos in prevPos)
            {
                if (pos == pieceConfigString)
                    repeatCounter++;
                if (repeatCounter == 3)
                    return true;
            }
            return false;
        }
        public bool InsufficientMaterial()
        {
            int whiteMaterial = 0;
            int blackMaterial = 0;
            foreach (Piece piece in pieces)
                if (!(piece is null) && !(piece is King))
                {
                    if (!(piece is Knight) && !(piece is Bishop)) //player has piece other than bishop/knight
                        return false;
                    if (piece.GetColor() == 1)
                    {
                        whiteMaterial++;
                        if (whiteMaterial == 2) //player has more than one piece
                            return false;
                    }
                    else
                    {
                        blackMaterial++;
                        if (blackMaterial == 2) //player has more than one piece
                            return false;
                    }
                }
            return whiteMaterial == 0 || blackMaterial == 0; //at least one player has no pieces
        }
        public Piece GetPiece(int x, int y)
        {
            if (!InBounds(x, y))
                return null;
            return pieces[x, y]; 
        }
        public King GetKing(int color) { return color == 1 ? whiteKing : blackKing; }
        public int GetTurn()
        { return player; }
        private void UpdatePieceConfig()
        {
            string config = "";
            for (int y = 7; y >= 0; y--)
            {
                int emptyCounter = 0;
                for (int x = 0; x < 8; x++)
                {
                    if (pieces[x, y] is null) emptyCounter++;
                    else
                    {
                        if (emptyCounter > 0)
                        {
                            config += emptyCounter;
                            emptyCounter = 0;
                        }
                        config += pieces[x, y].GetPieceInfo();
                    }
                }
                if (emptyCounter > 0)
                    config += emptyCounter;
                if (y != 0)
                    config += '/';
            }
            pieceConfigString = config;
        }
        public string GetPieceConfig()
        { return pieceConfigString; }
        public List<string> GetPositions()
        { return prevPos; }
        public string ToNotation(int x1, int y1, int x2, int y2, char promoteTo)
        {
            Piece piece = pieces[x1, y1];
            if (piece is King)//castling notation
            {
                int dif = x2 - x1;
                if (dif == 2)
                    return "O-O";
                if (dif == -2)
                    return "O-O-O";
            }
            string move = "";//other notation
            bool isPawn = piece is Pawn;
            if (!isPawn)
                move += char.ToUpper(piece.GetPieceInfo());
            Board testBoard = new Board(this);
            bool shareMove = false;
            bool shareRank = false;
            bool shareFile = false;
            if (!isPawn)
                foreach (Piece temp in testBoard.pieces)
                {
                    if (!(temp is null) && temp.GetPieceInfo() == piece.GetPieceInfo() && (temp.GetX() != piece.GetX() || temp.GetY() != piece.GetY()))
                    {
                        temp.UpdateLegalMoves();
                        foreach (Move pieceMove in temp.GetLegalMoves())
                            if (pieceMove.X2 == x2 && pieceMove.Y2 == y2)
                            {
                                shareMove = true;
                                if (temp.GetX() == x1)
                                    shareFile = true;
                                if (temp.GetY() == y1)
                                    shareRank = true;
                                break;
                            }
                    }
                }
            if (shareMove)
            {
                if (!shareFile)
                    move += ToFile(x1);
                else if (!shareRank)
                    move += ToRank(y1);
                else
                {
                    move += ToFile(x1);
                    move += ToRank(y1);
                }
            }
            if (isPawn && x1 != x2)
            {
                move += ToFile(x1);
                move += "x";
            }
            else if (!(pieces[x2, y2] is null))//captures
                move += "x";
            move += ToFile(x2);
            move += ToRank(y2);
            if (promoteTo != 'P')
                move += $"={promoteTo}";
            //check and mate
            testBoard.MovePiece(x1, y1, x2, y2, promoteTo);
            bool isInCheck = testBoard.IsInCheck(-piece.GetColor());
            if (isInCheck)
            {
                testBoard.UpdatePieceMoves(-piece.GetColor(), true, false);
                bool gameOver = testBoard.HasNoLegalMoves(-piece.GetColor());
                if (gameOver)
                    move += "#";
                else
                    move += "+";
            }
            return move;
        }
        public Move FromNotation(string moveNotation)
        {
            try
            {
                
                //castling
                if (moveNotation == "O-O")
                {
                    int x1 = 4; int y1 = (int)(3.5 - 3.5 * player); int x2 = 6; int y2 = y1;
                    return new Move(x1, y1, x2, y2);
                }
                if (moveNotation == "O-O-O")
                {
                    int x1 = 4; int y1 = (int)(3.5 - 3.5 * player); int x2 = 2; int y2 = y1;
                    return new Move(x1, y1, x2, y2);
                }
                int endX, endY;
                //erase irrelevnt
                moveNotation = moveNotation.Replace("x", "");
                moveNotation = moveNotation.Replace("+", "");
                moveNotation = moveNotation.Replace("#", "");
                //piece type
                char pieceType = 'P';
                if (char.IsUpper(moveNotation[0]))
                {
                    pieceType = moveNotation[0];
                    moveNotation = moveNotation.Substring(1);
                }
                if (player == -1)
                    pieceType = char.ToLower(pieceType);
                //promotion
                char promoteTo = 'P';
                if (moveNotation[moveNotation.Length - 2] == '=')
                {
                    promoteTo = moveNotation[moveNotation.Length - 1];
                    moveNotation = moveNotation.Substring(0, moveNotation.Length - 2);
                }
                //get end position
                string endPos = moveNotation.Substring(moveNotation.Length - 2);
                moveNotation = moveNotation.Substring(0, moveNotation.Length - 2);
                endX = FromFile(endPos[0]);
                endY = FromRank(endPos[1]);
                //limit search
                int srchStartX = 0;
                int srchStartY = 0;
                int srchEndX = 8;
                int srchEndY = 8;
                while (moveNotation.Length > 0)
                {
                    if (!char.IsDigit(moveNotation[0]))
                    {
                        srchStartX = FromFile(moveNotation[0]);
                        srchEndX = srchStartX + 1; 
                    }
                    else
                    {
                        srchStartY = FromRank(moveNotation[0]);
                        srchEndY = srchStartY + 1;
                    }
                    moveNotation = moveNotation.Substring(1);
                }
                //find relevant piece
                Board tempBoard = new Board(this);
                for (int x = srchStartX; x < srchEndX; x++)
                    for (int y = srchStartY; y < srchEndY; y++)
                    {
                        Piece temp = tempBoard.pieces[x, y];
                        if (!(temp is null) && temp.GetPieceInfo() == pieceType)
                        {
                            temp.UpdateLegalMoves();
                            temp.RemoveBadMoves();
                            if (temp.InLegalMoves(endX, endY))
                                return new Move(temp.GetX(), temp.GetY(), endX, endY, promoteTo);
                        }
                    }
                //couldn't find
                return null;
            }
            catch //improper notation
            { return null; }
        }
        public void SetOneTag(string key, string value)
        {
            if (tags.ContainsKey(key))
                tags[key] = value;
        }
        private void SetTags(string tagInfo)
        {
            tagInfo = tagInfo.Replace(']', '[');
            string[] tags = tagInfo.Split('[');
            for (int i = 1; i < tags.Length; i += 2)
            {
                string tag = tags[i];
                string[] tagData = tag.Split(' ', 2);
                string key = tagData[0];
                string value = tagData[1];
                value = value.Replace("\"", "");
                SetOneTag(key, value);
            }
        }
        public string GetTag(string key)
        {
            if (tags.ContainsKey(key))
                return tags[key];
            return "";
        }
        public void PlayByPGN(string pgn)
        {
            pgn = pgn.Replace("\r", "");
            pgn = pgn.Replace("\n", " ");
            string tagInfo = "";
            for (int i = pgn.Length - 1; i >= 0; i--)
                if (pgn[i] == ']')
                {
                    tagInfo = pgn.Substring(0, i + 1);
                    SetTags(tagInfo);
                    pgn = pgn.Substring(i + 2);
                    break;
                }
            string[] moves = pgn.Split(' ');
            foreach (string move in moves)
            {
                if (move == "" || char.IsDigit(move[0]))
                    continue;
                UpdatePieceMoves(player, true, true);
                PlayTurn(move);
            }
        }
        public string GetPGN()
        {
            string pgn = "";
            foreach (KeyValuePair<string, string> tag in tags)
                if (tag.Value != "")
                    pgn += $"[{tag.Key} \"{tag.Value}\"]" + '\n';
            if (pgn != "")
                pgn += '\n';
            bool whiteTurn = true;
            int turn = 1;
            int charCounter = 0;
            foreach (string moveNotation in moveList)
            {
                if (whiteTurn)
                {
                    string str = turn + ".";
                    if (charCounter + str.Length >= 80)
                    {
                        pgn += '\n';
                        charCounter = 0;
                    }
                    pgn += str + " ";
                    charCounter += str.Length + 1;
                    turn++;
                }
                if (charCounter + moveNotation.Length >= 80)
                {
                    pgn += '\n';
                    charCounter = 0;
                }
                pgn += moveNotation + " ";
                charCounter += moveNotation.Length + 1;
                whiteTurn = !whiteTurn;
            }
            pgn += ' ';
            pgn += GetTag("Result");
            return pgn;
        }
        public Tuple<string, double> GetBestMove(int depth, double alpha = -1000, double beta = 1000)
        {
            int player = this.player;
            //is game over?
            Tuple<bool, int> gameState = CheckGameOver();
            if (gameState.Item1) //if game is over, give objective evaluation
                return Tuple.Create("", (gameState.Item2 * 1000) + ((double)1 / (depth + 2)) * player);
            string bestMove = "";
            double bestEval = -1000;
            //get all moves
            UpdatePieceMoves(player, true, true);
            List<Tuple<Piece, Move, double>> moves = new List<Tuple<Piece, Move, double>>();
            foreach (Piece piece in pieces)
            {
                if (piece is null)
                    continue;
                foreach (Move move in piece.GetLegalMoves())
                {
                    //play move
                    Board tempBoard = new Board(this);
                    tempBoard.MovePiece(move.X1, move.Y1, move.X2, move.Y2, move.PromoteTo);
                    //evaluate new position
                    Tuple<Piece, Move, double> temp = Tuple.Create(piece, move, tempBoard.GetEval());
                    //use insertion sort to organize moves from "best" to "worst".
                    int index = moves.Count();
                    for (int i = 0; i < moves.Count(); i++)
                        if (temp.Item3 * player > moves[i].Item3 * player)
                        {
                            index = i;
                            break;
                        }
                    moves.Insert(index, temp);
                }
            }
            foreach (Tuple<Piece, Move, double> moveEval in moves)
            {
                Piece piece = moveEval.Item1;
                Move move = moveEval.Item2;
                Board tempBoard = new Board(this);
                //play move
                tempBoard.MovePiece(move.X1, move.Y1, move.X2, move.Y2, move.PromoteTo);
                string line;
                double eval;
                if (depth > 0)//recursively check move tree
                {
                    Tuple<string, double> temp = tempBoard.GetBestMove(depth - 1, alpha, beta);
                    line = move.ID;
                    eval = temp.Item2;
                }
                else//evaluate position
                {
                    line = move.ID;
                    eval = tempBoard.GetEval();
                }//update values if new position is better
                if (eval * player > bestEval)
                {
                    bestMove = line;
                    bestEval = eval * player;
                    //if values are outside the range of alpha/beta, the rest of the branches can be pruned
                    if (player == 1)
                    {
                        if (eval >= beta)
                            break;
                        alpha = Math.Max(alpha, eval);
                    }
                    else
                    {
                        if (eval <= alpha)
                            break;
                        beta = Math.Min(beta, eval);
                    }
                }
            }
            return Tuple.Create(bestMove, bestEval * player);
        }
        public double GetEval()
        {
            double eval = 0;
            //get material
            Tuple<int, int> material = GetMaterial();
            //get material dif
            int baseMaterial = material.Item1 - material.Item2;
            //get minimal material
            int lowerMaterial = Math.Min(material.Item1, material.Item2);
            //get amplifier (positions with less pieces favour the winning player)
            double lowMatAmlifier = (double)(lowerMaterial + 2) / (lowerMaterial + 1);
            //multiply material dif with amplifier
            double matImbalance = baseMaterial * lowMatAmlifier;
            eval += matImbalance;
            //add board control
            eval += GetBoardControl();
            //add piece worth
            foreach (Piece piece in pieces)
            {
                if (piece is null)
                    continue;
                eval += piece.GetPieceWorth();
                if (piece is King king)
                    eval += king.GetKingWorth(king.GetColor() == 1 ? material.Item2 : material.Item1);
            }
            return eval;
        }
        public Tuple<int, int> GetMaterial()
        {
            int wMaterial = 0;
            int bMaterial = 0;
            foreach (Piece piece in pieces)
                if (!(piece is null))
                {
                    if (piece.GetColor() == 1)
                        wMaterial += piece.GetPieceMaterial();
                    else
                        bMaterial += piece.GetPieceMaterial();
                }
            return Tuple.Create(wMaterial, bMaterial);
        }
        public double GetBoardControl()
        {
            double wControl = 0;
            double bControl = 0;
            foreach (Piece piece in pieces)
            {
                if (piece is null || !(piece is Pawn))
                    continue;
                int color = piece.GetColor();
                if ((piece.GetY() + color) * color > 3.5 * color)
                {
                    if (color == 1)
                        wControl += 0.09;
                    else
                        bControl += 0.09;
                }
                double xDif = Math.Abs(3.5 - piece.GetX());
                double yDif = Math.Abs(3.5 - piece.GetY());
                int xCenter = 2 - (int)(Math.Min(2.5, xDif) - 0.5);
                int yCenter = 2 - (int)(Math.Min(2.5, yDif) - 0.5);
                double centerVal = (double)(xCenter * yCenter) / 8;
                if (color == 1)
                    wControl += centerVal;
                else
                    bControl += centerVal;
            }
            return wControl - bControl;
        }
        public override string ToString()
        { return pieceConfigString; }
        static public bool InBounds(int x, int y)
        { return x >= 0 && x < 8 && y >= 0 && y < 8; }
        private static char ToFile(int pos)
        { return (char)(pos + (int)'a'); }
        private static int FromFile(char file)
        { return file - (int)'a'; }
        private static char ToRank(int pos)
        { return (char)(pos + (int)'1'); }
        private static int FromRank(char rank)
        { return rank - (int)'1'; }
    }
    public abstract class Piece
    {
        protected Board board;
        protected int color;
        protected int x;
        protected int y;
        protected List<Move> legalMoves;
        public Piece(Board board, int color, int x, int y)
        {
            this.board = board;
            this.color = color;
            this.x = x;
            this.y = y;
            legalMoves = new List<Move>();
        }
        public abstract void UpdateLegalMoves();
        public void RemoveBadMoves()
        {
            Queue<Move> toRemove = new Queue<Move>();
            //iterate over piece moves
            foreach (Move move in legalMoves)
            {
                //create temporary board identical to current board
                Board newBoard = new Board(board);
                //play current move
                newBoard.MovePiece(x, y, move.X2, move.Y2, move.PromoteTo);
                //if the move leads to my king being in check, it is illegal
                if (newBoard.IsInCheck(color))
                    toRemove.Enqueue(move);//add illegal move to bad list
            }
            //remove illegal moves from list
            foreach (Move move in toRemove)
                legalMoves.Remove(move);
        }
        public void NotateMoves()
        {
            foreach (Move move in legalMoves)
                move.UpdateNotation(board);
        }
        public void ResetLegalMoves()
        { legalMoves = new List<Move>(); }
        public bool InLegalMoves(int x, int y)
        {
            foreach (Move move in legalMoves)
                if (move.X2 == x && move.Y2 == y) return true;
            return false;
        }
        protected void AddMove(int x, int y)
        { legalMoves.Add(new Move(this.x, this.y, x, y)); }
        public virtual void UpdateCoords(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public abstract Piece GetNewPiece(Board newBoard);
        public abstract char GetPieceInfo();
        public abstract int GetPieceMaterial();
        public abstract double GetPieceWorth();
        public int GetColor()
        { return color; }
        public int GetX() { return x; }
        public int GetY() { return y; }
        public List<Move> GetLegalMoves()
        { return legalMoves; }
        public override string ToString()
        { return GetPieceInfo() + $"({x}, {y})"; }
    }
    public class Pawn : Piece
    {
        private bool enPassant;
        public Pawn(Board board, int color, int x, int y) : base(board, color, x, y)
        { enPassant = false; }
        public Pawn(Board board, int color, int x, int y, bool enPassant) : base(board, color, x, y)
        { this.enPassant = enPassant; }
        public override void UpdateLegalMoves()
        {
            ResetLegalMoves();
            if (board.GetPiece(x, y + color) is null)//Move forward
            {
                AddMove(x, y + color);
                if (y == 3.5 - (2.5 * color) && board.GetPiece(x, y + 2 * color) is null)//Move twice
                    AddMove(x, y + 2 * color);
            }
            //Captures
            Piece temp;
            if (Board.InBounds(x, y))
            {
                temp = board.GetPiece(x - 1, y + color);
                if (!(temp is null))//Normal
                {
                    if (temp.GetColor() != color)
                        AddMove(x - 1, y + color);
                }
                else//En passant
                {
                    temp = board.GetPiece(x - 1, y);
                    if (temp is Pawn pawn && pawn.GetEnPassant() && temp.GetColor() != color)
                        AddMove(x - 1, y + color);
                }
            }
            if (Board.InBounds(x, y))
            {
                temp = board.GetPiece(x + 1, y + color);
                if (!(temp is null))//Normal
                {
                    if (temp.GetColor() != color)
                        AddMove(x + 1, y + color);
                }
                else//En passant
                {
                    temp = board.GetPiece(x + 1, y);
                    if (temp is Pawn pawn && pawn.GetEnPassant() && temp.GetColor() != color)
                        AddMove(x + 1, y + color);
                }
            }
        }
        public override void UpdateCoords(int x, int y)
        {
            if (Math.Abs(this.y - y) == 2)
                enPassant = true;
            this.x = x;
            this.y = y;
        }
        public override Piece GetNewPiece(Board newBoard) { return new Pawn(newBoard, color, x, y, enPassant); }
        public override char GetPieceInfo() { return color == 1 ? 'P' : 'p'; }
        public override int GetPieceMaterial() { return 1; }
        public override double GetPieceWorth()
        {
            double eval = 0;
            for (int i = this.y + color; i >= 0 && i < 8; i += color)
            {
                Piece temp = board.GetPiece(x, i);
                if (temp is Pawn && temp.GetColor() == color)
                    eval -= 0.1;
            }
            return eval * color;
        }
        public bool GetEnPassant()
        { return enPassant; }
        public void ResetEnPassant()
        { enPassant = false; }
    }
    public class Knight : Piece
    {
        public Knight(Board board, int color, int x, int y) : base(board, color, x, y) { }
        public override void UpdateLegalMoves()
        {
            ResetLegalMoves();
            //generate possible moves
            Tuple<int, int>[] moves = new Tuple<int, int>[8]
            {
                Tuple.Create(x + 1, y + 2),
                Tuple.Create(x - 1, y + 2),
                Tuple.Create(x + 1, y - 2),
                Tuple.Create(x - 1, y - 2),
                Tuple.Create(x + 2, y + 1),
                Tuple.Create(x - 2, y + 1),
                Tuple.Create(x + 2, y - 1),
                Tuple.Create(x - 2, y - 1)
            };
            //iterate over options
            foreach (Tuple<int, int> move in moves)
            {
                //ignore options if outside board bounds
                if (!Board.InBounds(move.Item1, move.Item2)) continue;
                //ignore option if it lands on a friendly piece
                Piece temp = board.GetPiece(move.Item1, move.Item2);
                if (temp is null || temp.GetColor() != color)
                    AddMove(move.Item1, move.Item2);
            }
        }
        public override Piece GetNewPiece(Board newBoard) { return new Knight(newBoard, color, x, y); }
        public override char GetPieceInfo() { return color == 1 ? 'N' : 'n'; }
        public override int GetPieceMaterial() { return 3; }
        public override double GetPieceWorth()
        {
            Piece temp = GetNewPiece(board);
            temp.UpdateLegalMoves();
            double eval = (2.55 - Math.Sqrt((x - 3.5) * (x - 3.5) + (y - 3.5) * (y - 3.5))) / 8;
            return eval * color;
        }
    }
    public class Bishop : Piece
    {
        public Bishop(Board board, int color, int x, int y) : base(board, color, x, y) { }
        public override void UpdateLegalMoves()
        {
            ResetLegalMoves();
            int posX = x;
            int posY = y;
            while (true)
            {
                posX++;
                posY++;
                if (!Board.InBounds(posX, posY)) break;
                Piece temp = board.GetPiece(posX, posY);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(posX, posY);
                if (!nullFlag) break;
            }
            posX = x;
            posY = y;
            while (true)
            {
                posX--;
                posY++;
                if (!Board.InBounds(posX, posY)) break;
                Piece temp = board.GetPiece(posX, posY);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(posX, posY);
                if (!nullFlag) break;
            }
            posX = x;
            posY = y;
            while (true)
            {
                posX++;
                posY--;
                if (!Board.InBounds(posX, posY)) break;
                Piece temp = board.GetPiece(posX, posY);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(posX, posY);
                if (!nullFlag) break;
            }
            posX = x;
            posY = y;
            while (true)
            {
                posX--;
                posY--;
                if (!Board.InBounds(posX, posY)) break;
                Piece temp = board.GetPiece(posX, posY);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(posX, posY);
                if (!nullFlag) break;
            }
        }
        public override Piece GetNewPiece(Board newBoard) { return new Bishop(newBoard, color, x, y); }
        public override char GetPieceInfo() { return color == 1 ? 'B' : 'b'; }
        public override int GetPieceMaterial() { return 3; }
        public override double GetPieceWorth()
        {
            double eval = 0.5;
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                    if (board.GetPiece(x, y) is Pawn)
                        eval -= (double)1 / 64;
            Piece temp = GetNewPiece(board);
            temp.UpdateLegalMoves();
            eval += (double)temp.GetLegalMoves().Count / 16;
            return eval * color;
        }
    }
    public class Rook : Piece
    {
        public Rook(Board board, int color, int x, int y) : base(board, color, x, y) { }
        public override void UpdateLegalMoves()
        {
            ResetLegalMoves();
            int index = x;
            while (true)
            {
                index++;
                if (!Board.InBounds(index, y)) break;
                Piece temp = board.GetPiece(index, y);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(index, y);
                if (!nullFlag) break;
            }
            index = x;
            while (true)
            {
                index--;
                if (!Board.InBounds(index, y)) break;
                Piece temp = board.GetPiece(index, y);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(index, y);
                if (!nullFlag) break;
            }
            index = y;
            while (true)
            {
                index++;
                if (!Board.InBounds(x, index)) break;
                Piece temp = board.GetPiece(x, index);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(x, index);
                if (!nullFlag) break;
            }
            index = y;
            while (true)
            {
                index--;
                if (!Board.InBounds(x, index)) break;
                Piece temp = board.GetPiece(x, index);
                bool nullFlag = temp is null;
                if (!nullFlag && temp.GetColor() == color) break;
                AddMove(x, index);
                if (!nullFlag) break;
            }
        }
        public override void UpdateCoords(int x, int y)
        {
            King king = board.GetKing(color);
            if (this.y == king.GetY())
            {
                if (this.x == 0)
                    king.canCastleLong = false;
                else if (this.x == 7)
                    king.canCastleShort = false;
            }
            this.x = x;
            this.y = y;
        }
        public override Piece GetNewPiece(Board newBoard) { return new Rook(newBoard, color, x, y); }
        public override char GetPieceInfo() { return color == 1 ? 'R' : 'r'; }
        public override int GetPieceMaterial() { return 5; }
        public override double GetPieceWorth()
        {
            double eval = -0.5;
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                    if (board.GetPiece(x, y) is null)
                        eval += (double)1 / 64;
            if (eval < 0.25)
                for (int i = this.y + color; i >= 0 && i < 8; i += color)
                {
                    Piece temp = board.GetPiece(x, i);
                    if (temp is Pawn && temp.GetColor() == color)
                        break;
                    eval += 0.075;
                }
            return eval * color;
        }
    }
    public class Queen : Piece
    {
        public Queen(Board board, int color, int x, int y) : base(board, color, x, y) { }
        public override void UpdateLegalMoves()
        {
            ResetLegalMoves();
            Rook testRook = new Rook(board, color, x, y);
            testRook.UpdateLegalMoves();
            foreach (Move move in testRook.GetLegalMoves())
                AddMove(move.X2, move.Y2);
            Bishop testBishop = new Bishop(board, color, x, y);
            testBishop.UpdateLegalMoves();
            foreach (Move move in testBishop.GetLegalMoves())
                AddMove(move.X2, move.Y2);
        }
        public override Piece GetNewPiece(Board newBoard) { return new Queen(newBoard, color, x, y); }
        public override char GetPieceInfo() { return color == 1 ? 'Q' : 'q'; }
        public override int GetPieceMaterial() { return 9; }
        public override double GetPieceWorth() { return 0; }
    }
    public class King : Piece
    {
        public bool canCastleShort;
        public bool canCastleLong;
        public King(Board board, int color, int x, int y) : base(board, color, x, y)
        {
            canCastleShort = true;
            canCastleLong = true;
        }
        public King(Board board, int color, int x, int y, bool canCastleShort, bool canCastleLong) : base(board, color, x, y)
        {
            this.canCastleShort = canCastleShort;
            this.canCastleLong = canCastleLong;
        }
        public override void UpdateLegalMoves()
        {
            ResetLegalMoves();
            for (int i = 0; i < 360; i += 45)
            {
                int posX = x + (int)Math.Round(Math.Cos(i * Math.PI / 180) * 1.1);
                int posY = y + (int)Math.Round(Math.Sin(i * Math.PI / 180) * 1.1);
                Piece temp = board.GetPiece(posX, posY);
                if (Board.InBounds(posX, posY) && (temp is null || temp.GetColor() != color)) AddMove(posX, posY);
            }
        }
        public void UpdateCastles()
        {
            //castle short
            if (canCastleShort)
            {
                bool isNotBlocked = true;
                for (int i = 5; i < 7; i++)
                    if (!(board.GetPiece(i, y) is null))
                    { isNotBlocked = false; break; }
                if (isNotBlocked)
                {
                    bool canCastle = true;
                    Board castleBoard = new Board(board);
                    King testKing = castleBoard.GetKing(color);
                    for (int i = 0; i < 3; i++)
                    {
                        if (castleBoard.IsInCheck(testKing.color))
                        { canCastle = false; break; }
                        castleBoard.MovePiece(testKing.x, testKing.y, testKing.x + 1, testKing.y, 'P');
                    }
                    if (canCastle)
                        AddMove(x + 2, y);
                }
            }
            //castle long
            if (canCastleLong)
            {
                bool isNotBlocked = true;
                for (int i = 3; i > 0; i--)
                    if (!(board.GetPiece(i, y) is null))
                    { isNotBlocked = false; break; }
                if (isNotBlocked)
                {
                    bool canCastle = true;
                    Board castleBoard = new Board(board);
                    King testKing = castleBoard.GetKing(color);
                    for (int i = 0; i < 3; i++)
                    {
                        if (castleBoard.IsInCheck(testKing.color))
                        { canCastle = false; break; }
                        castleBoard.MovePiece(testKing.x, testKing.y, testKing.x - 1, testKing.y, 'P');
                    }
                    if (canCastle)
                        AddMove(x - 2, y);
                }
            }
        }
        public override void UpdateCoords(int x, int y)
        {
            canCastleShort = false;
            canCastleLong = false;
            this.x = x;
            this.y = y;
        }
        public override Piece GetNewPiece(Board newBoard) { return new King(newBoard, color, x, y, canCastleShort, canCastleLong); }
        public override char GetPieceInfo() { return color == 1 ? 'K' : 'k'; }
        public override int GetPieceMaterial() { return 0; }
        public override double GetPieceWorth() { return 0; }
        public double GetKingWorth(double oppMaterial)
        {
            double safetyMult = (double)oppMaterial / 30;
            double activityMult = (double)1 / (oppMaterial + 0.5);
            return GetKingSafety() * safetyMult + GetKingActivity() * activityMult;
        }
        private double GetKingSafety()
        {
            double eval = 0;
            int tempY = y;
            if (color == -1)
                tempY = 7 - tempY;
            eval -= (double)tempY / 3.5;
            if (x == 0 || x == 7)
                eval += 0.2;
            else if (x != 3 && x != 4)
                eval += 0.4;
            if (board.IsInCheck(color))
                return eval - 0.6;
            return eval * color;
        }
        private double GetKingActivity()
        {
            double tempY = y;
            if (color == -1)
                tempY = 7 - tempY;
            tempY = Math.Min(tempY, 3.5);
            double eval = 5 - Math.Sqrt((x - 3.5) * (x - 3.5) + (tempY - 3.5) * (tempY - 3.5));
            return eval * color;
        }
    }
    public class Move
    {
        private int x1 { get; }
        private int y1 { get; }
        private int x2 { get; }
        private int y2 { get; }
        private char promoteTo { get; }
        private string id { get; set; }
        public int X1
        { get { return x1; } }
        public int Y1
        { get { return y1; } }
        public int X2
        { get { return x2; } }
        public int Y2
        { get { return y2; } }
        public char PromoteTo
        { get { return promoteTo; } }
        public string ID
        { get { return id; } }
        public Move(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            promoteTo = 'P';
        }
        public Move(int x1, int y1, int x2, int y2, char promoteTo)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.promoteTo = promoteTo;
        }
        public void UpdateNotation(Board board)
        { id = board.ToNotation(x1, y1, x2, y2, promoteTo); }
        public override string ToString()
        { return id + "(" + x1 + "-" + y1 + "-" + x2 + "-" + y2 + ")"; }
    }
}