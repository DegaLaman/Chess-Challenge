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
    public ulong[,,] PST = new ulong[2, 6, 8] // Opening/Ending, PieceType, Scoreboard
    {
        {
            { // Opening/Midgame, Pawn, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Opening/Midgame, Knight, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Opening/Midgame, Bishop, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Opening/Midgame, Rook, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Opening/Midgame, Queen, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Opening/Midgame, King, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            }
        },
        {
            { // Ending, Pawn, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Ending, Knight, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Ending, Bishop, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Ending, Rook, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Ending, Queen, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
            },
            { // Ending, King, Scoreboards
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000
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

    public Move Think(Board board, Timer timer)
    {
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, false);

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
