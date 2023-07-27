using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    public int[] pieceValue = { 0, 100, 300, 300, 500, 900, 2000 };

    public void GetLegalMovesNonAlloc(Board board, ref Span<Move> moves, bool capturesOnly)
    {
        board.GetLegalMovesNonAlloc(ref moves, capturesOnly);
        Span<int> moveOrder = stackalloc int[moves.Length];

        Move move;

        for (int index = 0; index < moves.Length; index++)
        {
            move = moves[index];
            board.MakeMove(move);
            moveOrder[index] =
                pieceValue[(int)move.MovePieceType]
                - pieceValue[(int)move.CapturePieceType]
                - pieceValue[(int)move.PromotionPieceType];
            board.UndoMove(move);
        }

        MemoryExtensions.Sort(moveOrder, moves);
    }

    public Move Think(Board board, Timer timer)
    {
        Span<Move> moves = stackalloc Move[256];
        GetLegalMovesNonAlloc(board, ref moves, false);
    }

    public int Search(Board board, int depth, int alpha, int beta)
    {
        // Alpha is a lower bound of a score for this node
        // Beta is an upper bound of a score for this node
        
        // this is a   PV-Node if the score returned is Alpha <  Score < Beta
        // this is a  Cut-Node if the score returned is  Beta <= Score
        // this is an All-Node if the score returned is Score <= Alpha
        int score;

        if (depth <= 0)
        { //QSearch
            score = Evaluate(board);
            if (beta <= score) // Is Cut-Node
                return beta; // Return a fail-hard lower bound

            if (alpha < score) // < Beta, this is now a confirmed PV-Node
                alpha = score;
        }

        Span<Move> moves = stackalloc Move[256];
        GetLegalMovesNonAlloc(board, ref moves, depth <= 0);

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            score = -Search(board, depth - 100, -beta, -alpha);

            board.UndoMove(move);

            if (beta <= score) // Is Cut-Node
                return beta; // Return a fail-hard lower bound
            if (alpha < score) // < Beta, this is now a confirmed PV-Node
                alpha = score; // This value is exact
        }

        // if alpha changed, this is a PV-Node and the value is exact
        // otherwise, this is an All-Node and the value is an upper bound
        return alpha;
    }

    public int Evaluate(Board board)
    {
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
