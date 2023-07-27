using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    public class EvilBot : IChessBot
    {
        public int[] pieceValue = { 0, 100, 300, 300, 500, 900, 2000 };

        public Move[] GetLegalMoves(Board board, bool capturesOnly)
        {
            Move[] moves = board.GetLegalMoves(capturesOnly);
            int[] moveOrder = new int[moves.Length];

            Move move;

            for (int index = 0; index < moves.Length; index++)
            {
                move = moves[index];
                board.MakeMove(move);
                moveOrder[index] =
                    pieceValue[(int)move.MovePieceType]
                    - pieceValue[(int)move.CapturePieceType]
                    - pieceValue[(int)move.PromotionPieceType]
                    - (move.IsCapture ? 1000 : 0)
                    - (board.IsInCheckmate() ? 3000 : 0);
                board.UndoMove(move);
            }

            Array.Sort(moveOrder, moves);
            return moves;
        }

        public Move Think(Board board, Timer timer)
        {
            Move[] moves = GetLegalMoves(board, false);

            int score;
            int bestScore = -2147483647;
            Move bestMove = moves[0];

            foreach (Move move in moves)
            {
                board.MakeMove(move);

                score = -Search(board, 4, -2147483647, 2147483647);

                board.UndoMove(move);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        public int Search(Board board, int depth, int alpha, int beta)
        {
            int newAlpha = alpha;
            int score;

            if (depth <= 0)
            { //QSearch
                score = Evaluate(board);
                if (score >= beta)
                    return beta;
                if (score > newAlpha)
                    newAlpha = score;
            }

            Move[] moves = GetLegalMoves(board, depth <= 0);

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                score = -Search(board, depth - 1, -beta, -newAlpha);
                board.UndoMove(move);

                if (score >= beta)
                    return beta; // Hard-fail beta
                if (score > newAlpha)
                    newAlpha = score;
            }

            return newAlpha;
        }

        public int Evaluate(Board board)
        {
            if (board.IsDraw())
                //Log("Draw", false, ConsoleColor.Red);
                return 0;

            if (board.IsInCheckmate())
                //Log("Checkmate", false, ConsoleColor.Red);
                return -2147483648;

            int score = 0;

            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                score +=
                    pieceList.Count
                    * pieceValue[(int)pieceList.TypeOfPieceInList]
                    * (pieceList.IsWhitePieceList ^ board.IsWhiteToMove ? -1 : 1);
            }

            return score;
        }
    }
}
