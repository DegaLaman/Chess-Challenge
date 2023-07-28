using ChessChallenge.API;
using System;

//using System.Numerics;
//using System.Collections.Generic;
//using System.Linq;

// Before Evaluation, 93 tokens
// After PST, 344 tokens, up 251 tokens
// After Full Eval, 541 tokes, up 197 token
// After bitboard reversal and cleanup, 454 tokens, down 87 tokens
// After adding taper eval to material, 455 tokens, up 1 token?!
public class MyBot : IChessBot
{
    // PST Borrowed from PeSTO's Evaluation Function 
    // as found here: https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    // until I write a Tuner to set these for me
    public ulong[,,] PST = new ulong[2, 6, 8] // Opening/Ending, PieceType, Scoreboard
    {
        {
            { // Opening/Midgame, Pawn, Scoreboards{
                0x001B7095DA5A8E00,
                0x00C239E6150F6B00,
                0x00478FAB3E8ABE00,
                0x00AF064EA2ED2E00,
                0x00338E16596DAE00,
                0x00DECF8781A1E700,
                0x009F8F878191BB00,
                0xFF6070787E7E7FFF
            },
            { // Opening/Midgame, Knight, Scoreboards
                0xFAD36D3F69739BDB,
                0x2EF320C5211A731F,
                0xBEFBB6B95A069C7D,
                0x3CA4E200984D9ADA,
                0xF43333CFF5F27C09,
                0x52A9A6198183DDF2,
                0x4C8679218183DFFE,
                0x107CFEFE7E7C2000,
            },
            { // Opening/Midgame, Bishop, Scoreboards
                0x59A85676062EF2DB,
                0x7D7DB4CC59FE129D,
                0x7615C2F3A61E3323,
                0x8C348C816FBE02CB,
                0xA8FAA18D09602477,
                0x952DFFB9110040BE,
                0xB98D8181010000FF,
                0x42727E7EFEFFFF00,
            },
            { // Opening/Midgame, Rook, Scoreboards
                0xF8217326B8DBB86F,
                0xDA6D072C4ADB78E2,
                0x508868864F8A1DA1,
                0xF2CD65D9B9C8F441,
                0x585DD75A2DC5DB5A,
                0x9F8E69E3AE4EDE83,
                0x003001C3AFCF5FC3,
                0xFFFFFE3C5030203C,
            },
            { // Opening/Midgame, Queen, Scoreboards
                0xD42ED7B3C594E575,
                0x508447505F7B2CAF,
                0xADC05653BFCC61A7,
                0xFC67FA50B058778B,
                0x14F6B17CBD1D4315,
                0xF1B5E35FBF1D4277,
                0x0117035FBF1D43F7,
                0xFEE8FCA040E2BC08,
            },
            { // Opening/Midgame, King, Scoreboards
                0x8B8B0129CFC05301,
                0x63C2E571532F0288,
                0xA32771AFE79012A6,
                0xB17F9283CB04C4DC,
                0x2F7B4BC47A5B3441,
                0x09BE997F06E72423,
                0x38FE99FFFFFF3C29,
                0xC60166000000C3D6,
            }
        },
        {
            { // Ending, Pawn, Scoreboards
                0x00FF2CCCDF6A9100,
                0x00FF4910C0664800,
                0x00FFA73C85431100,
                0x00FF1116BFE49F00,
                0x00FFF5D2BCE48000,
                0x00FF3211BCE48000,
                0x00FFCF10BCE48000,
                0xFFFFFFEF431B7FFF,
            },
            { // Ending, Knight, Scoreboards
                0x0F482F2823F835F4,
                0x711D9C83BFB43D07,
                0x62B76EC19DF2BDA9,
                0x767AFF8BE15FCA82,
                0x089D263E1CB01A86,
                0x3D7EE78381737F3C,
                0xFFFFE78381F3FF7F,
                0x0000187C7E0C0000,
            },
            { // Ending, Bishop, Scoreboards
                0x7634084BDEF2ECDF,
                0x63A439F0BB282BDA,
                0x645EB815D413BA92,
                0xDA531A3F655E0ECD,
                0x3DFB1A01C9C36D7A,
                0xFFFB1A01C1C3EFFF,
                0xFFFB1A01C1C3EFFF,
                0x0004E5FE3E3C1000,
            },
            { // Ending, Rook, Scoreboards
                0x89BFEF6E931CF03D,
                0x0EA947D2310C3B3F,
                0xB916BF458A29F0C9,
                0x7B5FE044745D8398,
                0x0410E040F0FDF339,
                0x0010E040F0FDF3B9,
                0x0010E040F0FDF3B9,
                0xFFEF1FBF0F020C46,
            },
            { // Ending, Queen, Scoreboards
                0x3929FC59DCB62299,
                0x7F517203FD4C0595,
                0x8743138ADB8E40CB,
                0x5879957C1B546355,
                0xBF72485696215899,
                0x011D39F86903BF76,
                0x010101000103FFFF,
                0xFEFEFEFFFEFC0000,
            },
            { // Ending, King, Scoreboards
                0x8DCBDFF4A8AEDAB2,
                0x27D0D431DA0DE4AD,
                0x9ABFDBA702FC65FF,
                0x47D8075B5DE984AE,
                0xAA42BABE5F165B13,
                0x7CC383832160219D,
                0xFFC383830100019E,
                0x003C7C7CFEFFFE60,
            }
        }
    };

    public int[,] pieceValue = new int[2, 6]
    {
        { 82, 337, 365, 477, 1025, 0 },
        { 94, 281, 297, 512, 936, 0 }
    };
    public int[] piecePhase = { 0, 1, 1, 2, 4, 0 };

    public int Evaluate(Board board)
    {
        int openingScore = 0,
            endingScore = 0,
            pieceCount,
            phase = 0,
            pieceType;
        ulong pieceBitboard,
            reversedBitboard;

        for (int piece = 0; piece < 12; piece++)
        {
            pieceType = piece >> 1;
            pieceBitboard = board.GetPieceBitboard((PieceType)(pieceType + 1), (piece & 1) == 0);
            if ((piece & 1) != 0)
            {
                reversedBitboard = 0;
                while (pieceBitboard > 0)
                {
                    reversedBitboard = reversedBitboard << 8 | (pieceBitboard & 0xFF);
                    pieceBitboard >>= 1;
                }
                pieceBitboard = reversedBitboard;
            }
            pieceCount = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
            openingScore += pieceCount * (pieceValue[0, pieceType] - 128);
            endingScore += pieceCount * (pieceValue[1, pieceType] - 128);
            phase += piecePhase[pieceType] * pieceCount;
            for (int scoreboardIndex = 0; scoreboardIndex < 8; scoreboardIndex++)
            {
                openingScore +=
                    (1 << scoreboardIndex)
                    * BitboardHelper.GetNumberOfSetBits(
                        PST[0, pieceType, scoreboardIndex] & pieceBitboard
                    );
                endingScore +=
                    (1 << scoreboardIndex)
                    * BitboardHelper.GetNumberOfSetBits(
                        PST[1, pieceType, scoreboardIndex] & pieceBitboard
                    );
            }
        }
        phase = phase > 24 ? 24 : phase;
        return ((openingScore * phase + endingScore * (24 - phase)) / 24)
            * (board.IsWhiteToMove ? 1 : -1);
    }
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
                pieceValue[0,(int)move.MovePieceType]
                - pieceValue[0,(int)move.CapturePieceType]
                - pieceValue[0,(int)move.PromotionPieceType]
                - (move.IsCapture ? 1000 : 0)
                - (board.IsInCheckmate() ? 3000 : 0);
                //+ (board.IsRepeatedPosition() ? 3000 : 0);
            board.UndoMove(move);
        }

        MemoryExtensions.Sort(moveOrder, moves);
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

    public Move Think(Board board, Timer timer)
    {
        Span<Move> moves = stackalloc Move[256];
        GetLegalMovesNonAlloc(board, ref moves, false);

        int score,
            bestScore = -32767;
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = Evaluate(board);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }
}
