using ChessChallenge.API;
using System;
//using System.Numerics;
//using System.Collections.Generic;
//using System.Linq;

// Before Evaluation, 93 tokens
public class MyBot : IChessBot
{
    //public int[] pieceValue = { 0, 100, 300, 300, 500, 900, 2000 };

    public int Evaluate(Board board)
    {
        return 0;
    }

    public Move Think(Board board, Timer timer)
    {
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, false);

        int score, bestScore = -32767;
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = Evaluate(board);
            board.UndoMove(move);
            
            if(score > bestScore) {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }
}
