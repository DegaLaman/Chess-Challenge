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
// 691, -2
// 631, -30 Opted out of PSTs
// 601, -30 Cleaned up unused tokens
// 619, +18     ELO >= +43 against Tier 1 in 10 sec, Got Eval Up and Running,
// 625, +6      Forgot what I did here.
// 869, +244    ELO >= -20 against Tier 1 in 10 sec! (It got worse!?), Added PVS and LMR
// 873, +4      ELO >= -18 against Tier 1 in 10 sec, Limited PVS
// 873, +14     ELO >= +178 against Tier 1 in 10 sec, fixed sign issue in initial eval
// 916, +43     ELO >= +215 against Tier 1 in 10 sec, added some light time management and incremental deeping
// 937, +21     ELO >= +406 against Tier 1 in 10 sec, additional time management
// 985, +48     ELO >= ???? against Tier 2 in 10 sec, added Transposition Table and modified move ordering
// 995, +10     ELO >= ???? against Tier 2 in 10 sec, fail-soft, commented out PVS and LMR
// 992, -3      ELO >= -596 against Tier 2 in 10 sec, move ordering, mate detection changes
// 994, +2      ELO >= -534 against Tier 2 in 10 sec, ???

public class MyBot : IChessBot
{
    int CurrentDepth,
        allowedElapse;
    const int TTsize = 1 << 20;

    (Move, Move)[] refutation = new (Move, Move)[64];

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

    (ulong, Move)[] TT = new (ulong, Move)[TTsize];

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
        refutation[0].Item1 = Move.NullMove; // #DEBUG

        for (int piece = 2; piece < 14; piece++)
        {
            int pieceType = piece >> 1,
                pieceColor = piece & 1,
                colorMult = pieceColor == 0 ^ board.IsWhiteToMove ? -1 : 1,
                pieceCount = BitboardHelper.GetNumberOfSetBits(
                    board.GetPieceBitboard((PieceType)pieceType, pieceColor == 0)
                );

            evaluation += colorMult * pieceCount * pieceValue[pieceType];
            phase += pieceCount * piecePhase[pieceType];
        }

        // End Intial Eval Calc

        // Timer Control Setup

        allowedElapse = Math.Min(
            timer.MillisecondsRemaining / 2,
            timer.MillisecondsRemaining / 30 + timer.IncrementMilliseconds
        );

        // End Timer Control
        try
        {
            for (CurrentDepth = 1; CurrentDepth <= 100; CurrentDepth++)
                Search(board, timer, -32000, 32000, CurrentDepth, 0, evaluation, phase);
        }
        catch { }

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
                + " Depth Attained: "
                + CurrentDepth.ToString()
        ); // #DEBUG

        return refutation[0].Item1;
    }

    public int Search(
        Board board,
        Timer timer,
        int alpha,
        int beta,
        int depth,
        int ply,
        long evaluation,
        int phase
    )
    {
        if (timer.MillisecondsElapsedThisTurn > allowedElapse) // Time Management
            throw new Exception();

        if (
            board.IsInsufficientMaterial()
            || board.IsRepeatedPosition()
            || board.FiftyMoveCounter >= 100
        )
            return ply;

        int bestScore = -64000;

        bool qsearch = depth <= 0;

        int score;

        if (qsearch)
        {
            // Stand Pat Evaluation

            decimal openingEval = Math.Round(evaluation / 40000m);

            score =
                // Decode Evaluation
                (
                    phase * (int)openingEval
                    + (24 - phase) * (int)(evaluation - 40000m * openingEval)
                ) / 24
                + ply
                - board.FiftyMoveCounter;

            // End of Standpat Eval

            if (score >= beta)
                return beta;
            if (score < alpha - 975 - 40 * (24 - phase))
                return alpha;
            alpha = Math.Max(alpha, score);
        }

        ulong key = board.ZobristKey;
        (ulong, Move) entry = TT[key % TTsize];

        // Move Ordering
        Move[] moves = board.GetLegalMoves(qsearch);

        int[] moveOrder = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            moveOrder[i] = -(
                move == entry.Item2
                    ? 100000
                    : move == refutation[ply].Item1
                        ? 99999 + (ply == 0 ? 2 : 0)

                    : move == refutation[ply].Item2 ? 99998
                        : move.IsCapture
                            ? (int)move.CapturePieceType * 50 - (int)move.MovePieceType + 1000
                            : move.IsPromotion
                                ? (int)move.PromotionPieceType + 50
                                : move.IsCastles
                                    ? 50
                                    : (int)move.MovePieceType * phase
                                        - (int)move.MovePieceType * (24 - phase)
            );
        }

        Array.Sort(moveOrder, moves);

        if (ply == 0)
            refutation[0].Item1 = moves[0];

        // Setup for PVS and Late Move Reductions
        int sinceLastIncrease = 0;

        bool PVS = false;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int capturedPiece = (int)move.CapturePieceType,
                movedPiece = (int)move.MovePieceType,
                promotedPiece = (int)move.PromotionPieceType,
                resultPiece = move.IsPromotion ? promotedPiece : movedPiece,
                currentPhase = Math.Clamp(
                    phase - piecePhase[capturedPiece] + piecePhase[promotedPiece],
                    0,
                    24
                );

            long currentEval =
                -evaluation
                - pieceValue[capturedPiece]
                + pieceValue[movedPiece]
                - pieceValue[resultPiece];

            bool LMR = sinceLastIncrease < 3
                    ? false
                    : sinceLastIncrease < 7
                        ? !(capturedPiece > 0 || board.IsInCheck() || PVS)
                        : !(capturedPiece > 1 || board.IsInCheck() || PVS),
                research;

            int PVSwindow = PVS ? -alpha - 1 : -beta,
                LMRdepth = LMR ? depth - 3 : depth - 1;

            score = -Search(
                board,
                timer,
                PVSwindow,
                -alpha,
                LMRdepth,
                ply + 1,
                currentEval,
                currentPhase
            );

            research = score > alpha && score < beta && (PVS || LMR);

            searches++; // #DEBUG
            reducedsearches += PVS || LMR ? 1 : 0; // #DEBUG
            PVSs += PVS ? 1 : 0; // #DEBUG
            LMRs += LMR ? 1 : 0; // #DEBUG
            researches += research ? 1 : 0; // #DEBUG

            if (research)
                score = -Search(
                    board,
                    timer,
                    -beta,
                    -alpha,
                    depth - 1,
                    ply + 1,
                    currentEval,
                    currentPhase
                );

            board.UndoMove(move);

            sinceLastIncrease++;

            if (score > bestScore)
            {
                bestScore = score;
                alpha = Math.Max(alpha, bestScore);
                if(refutation[ply].Item1 != move) refutation[ply].Item2 = refutation[ply].Item1;
                refutation[ply].Item1 = move;
                PVS = false; //beta - alpha > 1; // Only use PVS for large windows
                sinceLastIncrease = 0;

                if (alpha >= beta)
                    break;
            }
        }

        if (moves.Length <= 0 && !qsearch)
            return board.IsInCheck() ? -32000 + ply : ply;

        TT[key % TTsize] = (key, refutation[ply].Item1);
        return bestScore;
    }
}
