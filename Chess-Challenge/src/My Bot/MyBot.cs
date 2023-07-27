using ChessChallenge.API;
using System;

//using System.Numerics;
//using System.Collections.Generic;
//using System.Linq;

// Before Evaluation, 93 tokens
// After PST, 344 tokens, up 251 tokens
// After Full Eval, 541 tokes, up 197 tokens
public class MyBot : IChessBot
{
    public ulong[,,,] PST = new ulong[2,2,6,8] // Opening/Ending, Color, PieceType, Scoreboard
    {
        {
            {
                { // Opening/Midgame, White, Pawn, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, White, Knight, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, White, Bishop, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, White, Rook, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, White, Queen, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, White, King, Scoreboards
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
                { // Opening/Midgame, Black, Pawn, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, Black, Knight, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, Black, Bishop, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, Black, Rook, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, Black, Queen, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Opening/Midgame, Black, King, Scoreboards
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
        },
        {
            {
                { // Ending, White, Pawn, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, White, Knight, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, White, Bishop, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, White, Rook, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, White, Queen, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, White, King, Scoreboards
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
                { // Ending, Black, Pawn, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, Black, Knight, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, Black, Bishop, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, Black, Rook, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, Black, Queen, Scoreboards
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000,
                    0x0000000000000000
                },
                { // Ending, Black, King, Scoreboards
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
        }
    };

    public int[] pieceValue = { 100, 300, 300, 500, 900, 2000 };
    public int[] piecePhase = { 0, 1, 1, 2, 4, 0 };

    public int Evaluate(Board board)
    {
        int openingScore = 0, endingScore = 0, materialScore = 0, pieceCount, phase = 0, pieceType;
        ulong pieceBitboard;

        for (int piece = 0; piece < 12; piece++) {
            pieceType = piece >> 1;
            pieceBitboard = board.GetPieceBitboard((PieceType) (pieceType + 1), (piece & 1) == 0);
            pieceCount = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
            openingScore -= pieceCount << 7;
            endingScore -= pieceCount << 7;
            materialScore += pieceCount*pieceValue[pieceType]*((piece & 1) == 0 ? 1 : -1);
            phase += piecePhase[pieceType]*pieceCount;
            for (int scoreboardIndex = 0; scoreboardIndex < 8; scoreboardIndex++) {
                openingScore += (1 << scoreboardIndex)*BitboardHelper.GetNumberOfSetBits(PST[0,(piece & 1),pieceType,scoreboardIndex] & pieceBitboard);
                endingScore += (1 << scoreboardIndex)*BitboardHelper.GetNumberOfSetBits(PST[1,(piece & 1),pieceType,scoreboardIndex] & pieceBitboard);
            }
        }
        phase = phase > 24 ? 24 : phase;
        return ((openingScore * phase + endingScore * (24 - phase))/24 + materialScore) * (board.IsWhiteToMove ? 1: -1);
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
