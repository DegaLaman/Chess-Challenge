using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
public class EvilBot : IChessBot
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
                - pieceValue[(int)move.PromotionPieceType]
                - (move.IsCapture ? 1000 : 0)
                - (board.IsInCheckmate() ? 3000 : 0);
                //+ (board.IsRepeatedPosition() ? 3000 : 0);
            board.UndoMove(move);
        }

        MemoryExtensions.Sort(moveOrder, moves);
    }

    public Move Think(Board board, Timer timer)
    {
        Span<Move> moves = stackalloc Move[256];
        GetLegalMovesNonAlloc(board, ref moves, false);

        int score;
        int bestScore = -2147483647;
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            score = -Search(board, 400, -2147483647, 2147483647, 2); // depth in centiply

            board.UndoMove(move);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    public int Search(Board board, int depth, int alpha, int beta, int nullMoveDepth)
    {
        int score = alpha; // Prevents unassigned local variable error; Doesn't actually do anything.

        if (depth <= 0)
        { //QSearch
            score = Evaluate(board);
            if (score >= beta)
                return beta;
            
            if (score + 900 < alpha)
                return alpha;

            if (score > alpha)
                alpha = score;
        }

        // Null Move Pruning
        
        if (!board.IsInCheck() && nullMoveDepth > 0 && depth > 0) {
            board.TrySkipTurn();

            score = -Search(board, depth - 300, -beta, -beta + 1, nullMoveDepth - 1);

            board.UndoSkipTurn();

            if (score >= beta)
                return beta;
        }

        Span<Move> moves = stackalloc Move[256];
        GetLegalMovesNonAlloc(board, ref moves, depth <= 0);

        bool foundPV = false;
        bool PVSFailed = false;

        foreach (Move move in moves)
        {

            board.MakeMove(move);

            if(foundPV) {
                score = -Search(board, depth - 100, -alpha - 1, -alpha, 2);
                PVSFailed = (score > alpha) && (score < beta);
            }

            if(!foundPV || PVSFailed)
                score = -Search(board, depth - 100, -beta, -alpha, nullMoveDepth);

            board.UndoMove(move);

            if (score >= beta)
                return beta; // Hard-fail beta
            if (score > alpha)
                alpha = score; // new PV
                foundPV = true;
        }
        return alpha;
    }

    public int Evaluate(Board board)
    {

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return -2147483647;

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
