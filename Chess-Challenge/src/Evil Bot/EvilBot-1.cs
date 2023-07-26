using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot1 : IChessBot
    {
        public Move Think(Board board, Timer timer)
        {
            Move[] moves = board.GetLegalMoves();

            // If there isn't much time, play randomly and hope for the best
            if (timer.MillisecondsRemaining <= 1 || moves.Length <= 1)
                return moves[0];

            int score;
            int bestScore = -2147483648;
            Move bestMove = moves[0];

            foreach (Move move in moves)
            {
                board.MakeMove(move);

                if (board.IsInCheckmate())
                    return move;

                score = Evaluate(board) + (move.IsPromotion ? 180 : 0);

                board.UndoMove(move);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        public int[] pieceValue = { 0, 100, 300, 300, 500, 900, 0 };
        public int[] pieceVisibilityValue = { 0, 90, 70, 70, 50, 10, 5 };

        public int Evaluate(Board board)
        {
            if (board.IsDraw())
                return 0;

            bool skipped = board.TrySkipTurn();

            int score = 0;

            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                score +=
                    pieceList.Count
                    * pieceValue[(int)pieceList.TypeOfPieceInList]
                    * (pieceList.IsWhitePieceList ^ board.IsWhiteToMove ? -1 : 1);

                foreach (Piece piece in pieceList)
                {
                    score +=
                        (piece.IsWhite ^ board.IsWhiteToMove ? -1 : 1)
                        * pieceVisibilityValue[(int)piece.PieceType]
                        * BitboardHelper.GetNumberOfSetBits(GetAttacks(piece, piece.Square, board));
                    if (board.SquareIsAttackedByOpponent(piece.Square))
                        score -= pieceValue[(int)piece.PieceType];
                }
            }

            if (skipped)
                board.UndoSkipTurn();

            return score;
        }

        public static ulong GetAttacks(Piece piece, Square square, Board board)
        {
            return (int)piece.PieceType switch
            {
                1 => BitboardHelper.GetPawnAttacks(square, piece.IsWhite),
                2 => BitboardHelper.GetKnightAttacks(square),
                6 => BitboardHelper.GetKingAttacks(square),
                _ => BitboardHelper.GetSliderAttacks(piece.PieceType, square, board)
            };
        }
    }
}
