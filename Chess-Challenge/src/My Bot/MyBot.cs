//#define VERBOSE  // #DEBUG

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
// 821, -173    ELO >= -364 against Tier 2 in 10 sec, updated mate detection, turned on LMR, fixed missing #DEBUGs
// 822, +1      ELO >= -378 against Tier 2 in 10 sec, modified refutation to only include items greater than alpha (lost ELO, reverting)
// 882, +60      90 - 852 -  58   ELO >= -379 against Tier 2 in 10 sec, Null Move Pruning, other updates
// 891, +9       96 - 848 -  56   ELO >= -371 against Tier 2 in 10 sec, Improved Best Move Detection
// 891, +0       99 - 837 -  64   ELO >= -359 against Tier 2 in 10 sec, Removed ply score from draw condtions

public class MyBot : IChessBot
{
    // GLOBALS
    int allowedElapse;

    (Move, Move)[] refutation = new (Move, Move)[64];

    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    long[] pieceValue = { 0, 3280094, 13480281, 14600297, 19080512, 41000936, 0 };

    const int TTsize = 1 << 20;
    Move[] TT = new Move[TTsize];

    Board _board;
    Timer _timer;
    // END GLOBALS

#if VERBOSE  // #DEBUG
    int researches, // #DEBUG
        searches, // #DEBUG
        reducedsearches, // #DEBUG
        PVSs, // #DEBUG
        LMRs; // #DEBUG
#endif  // #DEBUG

    public int Search(
        int alpha,
        int beta,
        int depth,
        int ply,
        int nullDepth,
        long evaluation,
        int phase
    )
    {
        // Time Management
        if (_timer.MillisecondsElapsedThisTurn > allowedElapse)
            throw new Exception();

        // Draw Detection
        if (
            _board.IsInsufficientMaterial()
            || _board.IsRepeatedPosition()
            || _board.FiftyMoveCounter >= 100
        )
            return 0; //ply;

        int bestScore = -64000,
            score = 0,
            sinceLastIncrease = 0;

        bool qsearch = depth <= 0,
            PVS = false;

        // Quiescence search
        if (qsearch)
        {
            // Stand Pat Evaluation

            decimal openingEval = Math.Round(evaluation / 40000m);

            bestScore =
                // Decode Evaluation
                (
                    phase * (int)openingEval
                    + (24 - phase) * (int)(evaluation - 40000m * openingEval)
                ) / 24
                + ply;

            // End of Standpat Eval

            if (bestScore >= beta)
                return bestScore;
            if (bestScore < alpha - 975 - 40 * (24 - phase))
                return alpha;
            alpha = Math.Max(alpha, bestScore);
        }

        // Transposition Table
        ulong key = _board.ZobristKey;
        Move entry = TT[key % TTsize];

        if (ply > 0 && depth > 5 && nullDepth > 0 && !_board.IsInCheck() && entry != Move.NullMove)
        {
            _board.TrySkipTurn();
            bestScore = -Search(
                -beta,
                -alpha,
                depth - 5,
                ply + 1,
                nullDepth - 1,
                -evaluation,
                phase
            );
            _board.UndoSkipTurn();
            alpha = Math.Max(alpha, bestScore);
            if (alpha >= beta)
                return bestScore; // return bestScore; ???
        }        

        // Move Ordering
        Move[] moves = _board.GetLegalMoves(qsearch);

        int[] moveOrder = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            moveOrder[i] = -(
                move == entry // Transposition Table Entry First
                    ? 188 // MAX: 188
                    : move == refutation[ply].Item1 // Main Killer Table Entry Second
                        ? 187 // MAX: 187
                        : move == refutation[ply].Item2 // Secondary Killer Table Entry Third
                            ? 186 // MAX: 186
                            : move.IsCapture // Captures order by MVV-LVA
                                ? (int)move.CapturePieceType * 7 - (int)move.MovePieceType + 151 // MAX: 5*7 - 1 + 151 = 185
                                : move.IsPromotion // Order Promotions by Q,R,B,N
                                    ? (int)move.PromotionPieceType + 145 // MAX: 5 + 145 = 150
                                    : move.IsCastles // Castles Next
                                        ? 145 // MAX: 145
                                        : (int)move.MovePieceType * phase
                                            - (int)move.MovePieceType * (24 - phase) // MAX: 6*24-6*0 = 144
            );
        }

        Array.Sort(moveOrder, moves);

        if (ply == 0)
            refutation[0].Item1 = moves[0];

        foreach (Move move in moves)
        {
            _board.MakeMove(move);

            // Update Evaluation
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

            // Use LMR?
            bool LMR = sinceLastIncrease > 3 & !(capturedPiece > 0 || _board.IsInCheck() || PVS),
                research;

            int PVSwindow = PVS ? -alpha - 1 : -beta,
                LMRdepth = LMR ? depth - 3 : depth - 1;

            score = -Search(
                PVSwindow,
                -alpha,
                LMRdepth,
                ply + 1,
                nullDepth,
                currentEval,
                currentPhase
            );

            research = score > alpha && score < beta && (PVS || LMR); // Only research if using a reduction
#if VERBOSE  // #DEBUG
            searches++; // #DEBUG
            reducedsearches += PVS || LMR ? 1 : 0; // #DEBUG
            PVSs += PVS ? 1 : 0; // #DEBUG
            LMRs += LMR ? 1 : 0; // #DEBUG
            researches += research ? 1 : 0; // #DEBUG
#endif  // #DEBUG

            if (research)
                score = -Search(
                    -beta,
                    -alpha,
                    depth - 1,
                    ply + 1,
                    nullDepth,
                    currentEval,
                    currentPhase
                );

            _board.UndoMove(move);

            sinceLastIncrease++;

            if (score <= bestScore)
                continue;

            bestScore = score;
            alpha = Math.Max(alpha, bestScore);

            if (refutation[ply].Item1 != move)
                refutation[ply].Item2 = refutation[ply].Item1;
            refutation[ply].Item1 = move;
            PVS = false; //beta - alpha > 1; // Only use PVS for large windows
            sinceLastIncrease = 0;

            if (alpha >= beta)
                break;
        }

        // Mate Detection
        if (moves.Length <= 0)
            if (qsearch)
                return _board.IsInCheckmate() ? -32000 + ply : _board.IsInStalemate() ? 0 : bestScore;
            else
                return _board.IsInCheck()? -32000 + ply : 0;

        TT[key % TTsize] = refutation[ply].Item1;
        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
#if VERBOSE  // #DEBUG
        researches = 0; // #DEBUG
        searches = 0; // #DEBUG
        reducedsearches = 0; // #DEBUG
        PVSs = 0; // #DEBUG
        LMRs = 0; // #DEBUG
        refutation[0].Item1 = Move.NullMove; // #DEBUG
#endif  // #DEBUG

        // Update Globals
        _board = board;
        _timer = timer;

        // Start Intial Eval Calc
        long evaluation = 0;
        int phase = 0;

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

        // Search
        int CurrentDepth = 0;
        Move bestMove = Move.NullMove;

        try
        {
            for (CurrentDepth = 1; CurrentDepth <= 100; CurrentDepth++, bestMove = refutation[0].Item1)
                Search(-32000, 32000, CurrentDepth, 0, 1, evaluation, phase);
        }
        catch { }

        // End Search

# if VERBOSE  // #DEBUG

        ConsoleHelper.Log( // #DEBUG
            "Searches: " // #DEBUG
                + searches.ToString() // #DEBUG
                //+ " Researches: " // #DEBUG
                //+ researches.ToString() // #DEBUG
                //+ " Reduced Searches: " // #DEBUG
                //+ reducedsearches.ToString() // #DEBUG
                //+ " Primary Variation Searches: " // #DEBUG
                //+ PVSs.ToString() // #DEBUG
                //+ " Late Move Reductions: " // #DEBUG
                //+ LMRs.ToString() // #DEBUG
                + " Depth Attained: " // #DEBUG
                + CurrentDepth.ToString() // #DEBUG
        ); // #DEBUG
#endif // #DEBUG

        return bestMove;
    }
}
