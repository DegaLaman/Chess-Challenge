#define verbose
using ChessChallenge.API;
using System;
using System.Numerics;
//using System.Collections.Generic;
//using System.Linq;
using ChessChallenge.Application; // #DEBUG
using System.Diagnostics; // #DEBUG

// 762 tokens
// 742, -20
// 753, +11
// 697, -56
// 689, -8
// 693, +4

public class MyBot : IChessBot
{
    int MAX_PLY = 4; // #DEBUG

    Move[] refutation = new Move[64];
    int[,,] PST = new int[2,7,64];
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };

    public MyBot()
    {
    }

    public Move Think(Board board, Timer timer)
    {

        // Start Intial Eval Calc
        int openingScore = 0,
            endingScore = 0,
            phase = 24;

        for (int piece = 0; piece < 12; piece++)
        {
            int pieceType = piece >> 1,
                pieceColor = piece & 1,
                indexFlip = pieceColor == 0 ? 0 : 56,
                colorMult = pieceColor == 0 ^ board.IsWhiteToMove ? 1 : -1,
                index = 0;
            for (
                ulong pieceBitboard = board.GetPieceBitboard((PieceType)(pieceType - 1), pieceColor == 0);
                pieceBitboard > 0;
                pieceBitboard >>= 1, index++
            )
            {
                if ((pieceBitboard & 1) == 1)
                {
                    openingScore +=
                        colorMult
                        * (PST[0, pieceType, index ^ indexFlip]);
                    endingScore +=
                        colorMult
                        * (PST[1, pieceType, index ^ indexFlip]);
                    phase -= piecePhase[pieceType];
                }
            }
        }

        // End Intial Eval Calc


        for (int i = MAX_PLY; i <= MAX_PLY; i++)
        {
            Search(board, -32000, 32000, i, openingScore, endingScore, phase);
        }

        return refutation[0];
    }

    public int Search(
        Board board,
        int alpha,
        int beta,
        int depth,
        int openingEval,
        int endingEval,
        int phase
    )
    {
        if (
            board.IsInsufficientMaterial()
            || board.IsRepeatedPosition()
            || board.FiftyMoveCounter >= 100
        )
            return MAX_PLY - depth;

        int score;

        if (depth <= 0)
        {
            // Stand Pat Evaluation
            score =
                ((openingEval * phase + endingEval * (24 - phase)) / 24)
                + MAX_PLY
                - depth
                - board.FiftyMoveCounter;

            // End of Standpat Eval

            if (score >= beta)
                return beta;
            if (score < alpha - 975 - 40 * (24 - phase))
                return alpha;
            alpha = Math.Max(alpha, score);
        }

        // Move Ordering
        Move[] moves = board.GetLegalMoves(depth <= 0);
        if (moves.Length <= 0)
        {
            if (depth <= 0)
                return board.IsDraw() ? MAX_PLY - depth : alpha;
            return MAX_PLY - depth;
        }

        int[] moveOrder = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                refutation[MAX_PLY - depth] = move;
                return 32000 - MAX_PLY + depth;
            }

            moveOrder[i] = -(
                (move.Equals(refutation[Math.Max(MAX_PLY - depth - 2, 0)]) ? 1 << 13 : 0) // MAX_PLY - (depth - 2) = 5 + 2 - depth = 7 - depth
                | (move.Equals(refutation[MAX_PLY - depth]) ? 1 << 12 : 0) // MAX_PLY - depth
                | (move.IsCapture ? 1 << 11 : 0)
                | (move.IsPromotion ? 1 << 10 : 0)
                | (board.IsInCheck() ? 1 << 9 : 0)
                | (int)move.MovePieceType << 6
                | (int)move.PromotionPieceType << 3
                | (int)move.CapturePieceType
            );

            board.UndoMove(move);
        }

        Array.Sort(moveOrder, moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int whiteToMove = board.IsWhiteToMove ? 56 : 0,
                capturedPiece = (int)move.CapturePieceType,
                movedPiece = (int)move.MovePieceType - 1,
                resultPiece = move.IsPromotion ? (int)move.PromotionPieceType - 1 : movedPiece,
                startIndex = move.StartSquare.Index ^ whiteToMove,
                targetIndex = move.TargetSquare.Index ^ whiteToMove;

            // Increment Eval
            // Add Capture, Add Promotion, Subtract Move from PST, Add Move to PST
            score = -Search(
                board,
                -beta,
                -alpha,
                depth - 1,
                82 // pieceValues[0, 1]
                -openingEval
                    - PST[0, capturedPiece, targetIndex ^ 56]
                    + PST[0, movedPiece, startIndex]
                    - PST[0, resultPiece, targetIndex],
                94 // pieceValues[1, 1]
                -endingEval
                    - PST[1, capturedPiece, targetIndex ^ 56]
                    + PST[1, movedPiece, startIndex]
                    - PST[1, resultPiece, targetIndex],
                phase - piecePhase[capturedPiece]
            );
            board.UndoMove(move);

            if (score > alpha)
            {
                alpha = score;
                refutation[MAX_PLY - depth] = move; // MAX_PLY - depth
            }

            if (alpha >= beta) return beta;
            
        }

        return alpha;
    }

    // PST Borrowed from PeSTO's Evaluation Function
    // as found here: https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    // until I write a Tuner to set these for me
    
}
