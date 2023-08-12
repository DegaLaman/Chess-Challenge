﻿#define verbose
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
// 691, -2
// 631, -30 Opted out of PSTs
// 601, -30 Cleaned up unused tokens
// 619, +18 Got Eval Up and Running, Eval >= +43 over Tier 1 in 10 sec
// 625, +6  Forgot what I did here.


public class MyBot : IChessBot
{
    int MAX_PLY = 5; // #DEBUG

    Move[] refutation = new Move[64];

    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    long[] pieceValue =
    { //
        0,
        3280094,
        13480281,
        14600297,
        19080512,
        41000936,
        0,
    };

    int researches,
        searches,
        reducedsearches,
        PVSs,
        LMRs; // #DEBUG

    public Move Think(Board board, Timer timer)
    {
        // Start Intial Eval Calc
        long evaluation = 0;
        int phase = 0;

        researches = 0; // #DEBUG
        searches = 0; // #DEBUG
        reducedsearches = 0; // #DEBUG
        PVSs = 0; // #DEBUG
        LMRs = 0; // #DEBUG

        for (int piece = 2; piece < 14; piece++)
        {
            int pieceType = piece >> 1,
                pieceColor = piece & 1,
                colorMult = pieceColor == 0 ^ board.IsWhiteToMove ? 1 : -1,
                pieceCount = BitboardHelper.GetNumberOfSetBits(
                    board.GetPieceBitboard((PieceType)pieceType, pieceColor == 0)
                );

            evaluation += colorMult * pieceCount * pieceValue[pieceType];
            phase += pieceCount * piecePhase[pieceType];
        }

        // End Intial Eval Calc

        for (int i = MAX_PLY; i <= MAX_PLY; i++)
        {
            Search(board, -32000, 32000, i, evaluation, phase);
        }

        ConsoleHelper.Log(
            "Searches: "
                + searches.ToString()
                + " Researches: "
                + researches.ToString()
                + " Reduced Searches: "
                + reducedsearches.ToString()
                + " Primary Variation Searches: "
                + PVSs.ToString()
                + " Late Move Reductions: "
                + LMRs.ToString()
        ); // #DEBUG

        return refutation[0];
    }

    public int Search(Board board, int alpha, int beta, int depth, long evaluation, int phase)
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

            decimal openingEval = Math.Round(evaluation / 40000m);

            score =
                // Decode Evaluation
                (
                    phase * (int)openingEval
                    + (24 - phase) * (int)(evaluation - 40000m * openingEval)
                ) / 24
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
            moveOrder[i] = 0;
            for (
                int j = MAX_PLY - depth, shift = 12;
                j >= Math.Max(MAX_PLY - depth - 6, 0);
                j -= 2, shift++
            )
            {
                moveOrder[i] |= (move.Equals(refutation[j]) ? 1 << shift : 0);
            }
            moveOrder[i] |=
                (move.IsCapture ? 1 << 11 : 0)
                | (move.IsPromotion ? 1 << 10 : 0)
                | (board.IsInCheck() ? 1 << 9 : 0)
                | (int)move.MovePieceType << 6
                | (int)move.PromotionPieceType << 3
                | (int)move.CapturePieceType;

            moveOrder[i] = -moveOrder[i];

            board.UndoMove(move);
        }

        Array.Sort(moveOrder, moves);

        // Setup for PVS and Late Move Reductions
        int sinceLastIncrease = 0;

        bool PVS = false;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int capturedPiece = (int)move.CapturePieceType,
                movedPiece = (int)move.MovePieceType,
                promotedPiece = (int)move.PromotionPieceType,
                resultPiece = move.IsPromotion ? promotedPiece : movedPiece;

            long currentEval =
                -evaluation
                - pieceValue[capturedPiece]
                + pieceValue[movedPiece]
                - pieceValue[resultPiece];

            bool LMR = !(capturedPiece > 0 || board.IsInCheck() || PVS) && sinceLastIncrease > 3,
                research = false;

            int PVSwindow = PVS ? -alpha - 1 : -beta,
                LMRdepth = LMR ? depth - 3 : depth - 1;

            score = -Search(
                board,
                PVSwindow,
                -alpha,
                LMRdepth,
                currentEval,
                Math.Clamp(phase - piecePhase[capturedPiece] + piecePhase[promotedPiece], 0, 24)
            );

            research = score > alpha && score < beta;

            searches++; // #DEBUG
            reducedsearches += PVS || LMR ? 1 : 0; // #DEBUG
            PVSs += PVS ? 1 : 0; // #DEBUG
            LMRs += LMR ? 1 : 0; // #DEBUG
            researches += research ? 1 : 0; // #DEBUG

            if (research)
                score = -Search(
                    board,
                    -beta,
                    -alpha,
                    depth - 1,
                    currentEval,
                    Math.Clamp(phase - piecePhase[capturedPiece] + piecePhase[promotedPiece], 0, 24)
                );
            board.UndoMove(move);

            sinceLastIncrease++;

            if (score > alpha)
            {
                alpha = score;
                refutation[MAX_PLY - depth] = move; // MAX_PLY - depth
                PVS = true;
                sinceLastIncrease = 0;
            }

            if (alpha >= beta)
                return beta;
        }

        return alpha;
    }
}
