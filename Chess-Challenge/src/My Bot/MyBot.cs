using ChessChallenge.API;
using System;

//using System.Numerics;
//using System.Collections.Generic;
//using System.Linq;

using ChessChallenge.Application;

//   40 tokens, Start

public class MyBot : IChessBot
{
    Move[] bestOrRefutation = new Move[1024];
    Move[] currentMove = new Move[1024];
    int[] currentMoveIndex = new int[1024];
    int[] alpha = new int[1024],
        beta = new int[1024],
        score = new int[1024];

    Move[][] moves = new Move[1024][];
    int thinkCount = 0;
    int evals = 0;

    public Move Think(Board board, Timer timer)
    {
        thinkCount = 0;
        evals = 0;
        int plyIndex = 5,
            currentMoveIndexValue;
        Move[] currentMoves;

        alpha[plyIndex] = -32000;
        beta[plyIndex] = 32000;

        goto StartNewNode;

        ReturnFrom:
        // START: Returning from a lower node
        //ConsoleHelper.Log("Exit: " + plyIndex.ToString(), false, ConsoleColor.Green);
        score[plyIndex - 1] = -score[plyIndex];
        plyIndex--;
        board.UndoMove(currentMove[plyIndex]);

        if (score[plyIndex] >= beta[plyIndex]) // Will never occur at starting ply depth
        {
            score[plyIndex] = beta[plyIndex]; // Fail-Hard

            goto ReturnFrom;
        }

        if (score[plyIndex] > alpha[plyIndex])
        {
            alpha[plyIndex] = score[plyIndex];
            bestOrRefutation[plyIndex] = currentMove[plyIndex];
        }

        currentMoveIndex[plyIndex]++;
        if (currentMoveIndex[plyIndex] >= moves[plyIndex].Length)
        {
            if (plyIndex == 5)
            {
                ConsoleHelper.Log(
                    "Last Thought: " + thinkCount.ToString() + " " + evals.ToString(),
                    false,
                    ConsoleColor.Green
                );
                return bestOrRefutation[5];
            }
            // Return alpha
            score[plyIndex] = alpha[plyIndex];
            goto ReturnFrom;
        }
        // GOTO: Continue checking moves
        // END: Returning from a lower node

        CheckNextMove:
        // START: Checking a move
        currentMove[plyIndex] = moves[plyIndex][currentMoveIndex[plyIndex]];
        board.MakeMove(currentMove[plyIndex]);
        alpha[plyIndex + 1] = -beta[plyIndex];
        beta[plyIndex + 1] = -alpha[plyIndex];
        plyIndex++;
        //ConsoleHelper.Log("Enter: " + plyIndex.ToString(), false, ConsoleColor.Green);
        // END: Enter new node

        StartNewNode:
        // START: Node Entry
        thinkCount++;
        if (board.IsInCheckmate())
        {
            score[plyIndex] = -32010 + plyIndex;
            goto ReturnFrom;
        }

        if (board.IsInsufficientMaterial() || board.IsRepeatedPosition())
        {
            score[plyIndex] = -plyIndex;
            goto ReturnFrom;
        }

        if (plyIndex >= 10)
        { //QSearch
            // Evaluate board
            evals++;
            bool pieceColor;

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
                pieceColor = (piece & 1) == 0;
                pieceBitboard = board.GetPieceBitboard((PieceType)(pieceType + 1), pieceColor);
                if (!pieceColor)
                {
                    reversedBitboard = 0;
                    for (int scoreboardIndex = 0; scoreboardIndex < 8; scoreboardIndex++)
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
            score[plyIndex] =
                ((openingScore * phase + endingScore * (24 - phase)) / 24)
                * (board.IsWhiteToMove ? 1 : -1);

            if (score[plyIndex] >= beta[plyIndex])
            {
                score[plyIndex] = beta[plyIndex]; // Fail-hard
                goto ReturnFrom;
            }
            /*
                        if(score[plyIndex] + 900 <= alpha[plyIndex]) {
                            score[plyIndex - 1] = -alpha[plyIndex];
                            goto ReturnFrom;
                        }*/

            if (score[plyIndex] > alpha[plyIndex])
                alpha[plyIndex] = score[plyIndex];
        }
        moves[plyIndex] = board.GetLegalMoves(plyIndex >= 10);
        currentMoves = moves[plyIndex];
        if (currentMoves.Length <= 0)
        {
            score[plyIndex] = -plyIndex;
            goto ReturnFrom;
        }

        // Sort Moves
        int[] moveOrder = new int[currentMoves.Length];
        for (int i = 0; i < moves[plyIndex].Length; i++)
        {
            board.MakeMove(currentMoves[i]);

            moveOrder[i] = -(
                (board.IsInCheckmate() ? 1 << 14 : 0)
                | (currentMoves[i].Equals(bestOrRefutation[plyIndex - 2]) ? 1 << 13 : 0)
                | (currentMoves[i].IsCapture ? 1 << 12 : 0)
                | (currentMoves[i].IsPromotion ? 1 << 11 : 0)
                | (currentMoves[i].Equals(bestOrRefutation[plyIndex]) ? 1 << 10 : 0)
                | (board.IsInCheck() ? 1 << 9 : 0)
                | (int)currentMoves[i].MovePieceType << 6
                | (int)currentMoves[i].PromotionPieceType << 3
                | (int)currentMoves[i].CapturePieceType
            );

            board.UndoMove(currentMoves[i]);
        }

        Array.Sort(moveOrder, moves[plyIndex]);

        currentMoveIndex[plyIndex] = 0;
        goto CheckNextMove;
        // END: Node Entry
    }

    // PST Borrowed from PeSTO's Evaluation Function
    // as found here: https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    // until I write a Tuner to set these for me
    public ulong[,,] PST = new ulong[2, 6, 8] // Opening/Ending, PieceType, Scoreboard
    {
        {
            { // Opening Pawn
                0x00D80EA95B5A7100,
                0x00439C67A8F0D600,
                0x00E2F1D57C517D00,
                0x00F5607245B77400,
                0x00CC71689AB67500,
                0x007BF3E18185E700,
                0x00F9F1E18189DD00,
                0xFF060E1E7E7EFEFF
            },
            { // Opening Knight
                0xDBD9CE96FCB6CB5F,
                0xF8CE5884A304CF74,
                0xBE39605A9D6DDF7D,
                0x5B59B2190047253C,
                0x903E4FAFF3CCCC2F,
                0x4FBBC1819865954A,
                0x7FFBC181849E6132,
                0x00043E7E7F7F3E08
            },
            { // Opening Bishop
                0xDB4F74606E6A159A,
                0xB9487F9A332DBEBE,
                0xC4CC7865CF43A86E,
                0xD3407DF681312C31,
                0xEE240690B1855F15,
                0x7D0200889DFFB4A9,
                0xFF0000808181B19D,
                0x00FFFF7F7E7E4E42
            },
            { // Opening Rook
                0xF61DDB1D64CE841F,
                0x471EDB5234E0B65B,
                0x85B851F26116110A,
                0x822F139D9BA6B34F,
                0x5ADBA3B45AEBBA1A,
                0xC17B7275C79671F9,
                0xC3FAF3F5C3800C00,
                0x3C040C0A3C7FFFFF
            },
            { // Opening Queen
                0xAEA729A3CDEB742B,
                0xF534DEFA0AE2210A,
                0xE58633FDCA6A03B5,
                0xD1EE1A0D0A5FE63F,
                0xA8C2B8BD3E8D6F28,
                0xEE42B8FDFAC7AD8F,
                0xEFC2B8FDFAC0E880,
                0x103D4702053F177F
            },
            { // Opening King
                0x80CA03F39480D1D1,
                0x1140F4CA8EA743C6,
                0x654809E7F58EE4C5,
                0x3B2320D3C149FE8D,
                0x822CDA5E23D2DEF4,
                0xC424E760FE997D90,
                0x943CFFFFFF997F1C,
                0x6BC3000000668063
            },
        },
        {
            { // Ending Pawn
                0x008956FB3334FF00,
                0x001266030892FF00,
                0x0088C2A13CE5FF00,
                0x00F927FD6888FF00,
                0x0001273D4BAFFF00,
                0x0001273D884CFF00,
                0x0001273D08F3FF00,
                0xFFFED8C2F7FFFFFF
            },
            { // Ending Knight
                0xF012F414C41FAC2F,
                0x8EB839C1FD2DBCE0,
                0x46ED7683B94FBD95,
                0x6E5EFFD187FA5341,
                0x10B9647C380D5861,
                0xBC7EE7C181CEFE3C,
                0xFFFFE7C181CFFFFE,
                0x0000183E7E300000
            },
            { // Ending Bishop
                0xFB374F7BD2102C6E,
                0x5BD414DD0F9C25C6,
                0x495DC82BA81D7A26,
                0xB3707AA6FC58CA5B,
                0x5EB6C3938058DFBC,
                0xFFF7C3838058DFFF,
                0xFFF7C3838058DFFF,
                0x00083C7C7FA72000
            },
            { // Ending Rook
                0xBC0F38C976F7FD91,
                0xFCDC308C4BE29570,
                0x930F9451A2FD689D,
                0x19C1BA2E2207FADE,
                0x9CCFBF0F02070820,
                0x9DCFBF0F02070800,
                0x9DCFBF0F02070800,
                0x623040F0FDF8F7FF
            },
            { // Ending Queen
                0x99446D3B9A3F949C,
                0xA9A032BFC04E8AFE,
                0xD30271DB51C8C2E1,
                0xAAC62AD83EA99E1A,
                0x991A84696A124EFD,
                0x6EFDC0961F9CB880,
                0xFFFFC08000808080,
                0x00003F7FFF7F7F7F
            },
            { // Ending King
                0xB1D3FB2F15755B4D,
                0xE40B2B8C5BB027B5,
                0x59FDDBE5403FA6FF,
                0xE21BE0DABA972175,
                0x55425D7DFA68DAC8,
                0x3EC3C1C1840684B9,
                0xFFC3C1C180008079,
                0x003C3E3E7FFF7F06
            }
        }
    };

    public int[,] pieceValue = new int[2, 6]
    {
        { 82, 337, 365, 477, 1025, 0 },
        { 94, 281, 297, 512, 936, 0 }
    };
    public int[] piecePhase = { 0, 1, 1, 2, 4, 0 };
}
