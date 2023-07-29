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

    public int[] pieceValue = { 0, 100, 300, 300, 500, 900, 2000 };

    public Move Think(Board board, Timer timer)
    {
        int plyDepth = 5,
            plyIndex = 5,
            currentMoveIndexValue;
        Move[] currentMoves;

        alpha[5] = -32000;
        beta[5] = 32000;

        goto StartNewNode;

        ReturnFrom:
        // START: Returning from a lower node
        //ConsoleHelper.Log("Exit: " + plyDepth.ToString(), false, ConsoleColor.Green);
        plyDepth += 1;
        plyIndex -= 1;
        board.UndoMove(currentMove[plyIndex]);

        if (score[plyIndex] >= beta[plyIndex]) // Will never occur at starting ply depth
        {
            score[plyIndex - 1] = -beta[plyIndex]; // Fail-Hard
            goto ReturnFrom;
        }

        if (score[plyIndex] > alpha[plyIndex])
        {
            alpha[plyIndex] = score[plyIndex];
            bestOrRefutation[plyIndex] = currentMove[plyIndex];
        }

        currentMoveIndex[plyIndex]++;
        try{
        if (currentMoveIndex[plyIndex] >= moves[plyIndex].Length)
        {
            if (plyDepth == 5)
                return bestOrRefutation[plyIndex];

            // Return alpha
            score[plyIndex - 1] = -alpha[plyIndex];
            goto ReturnFrom;
        } } catch {
            ConsoleHelper.Log("Exit: " + moves[plyIndex].Length , false, ConsoleColor.Green);
            throw;
        }

        goto CheckNextMove;
        // GOTO: Continue checking moves
        // END: Returning from a lower node

        CheckNextMove:
        // START: Checking a move
        currentMoveIndexValue = currentMoveIndex[plyIndex];
        currentMoves = moves[plyIndex];
        currentMove[plyIndex] = currentMoves[currentMoveIndexValue];
        board.MakeMove(currentMove[plyIndex]);
        alpha[plyIndex + 1] = -beta[plyIndex];
        beta[plyIndex + 1] = -alpha[plyIndex];
        plyDepth -= 1;
        plyIndex += 1;
        //ConsoleHelper.Log("Enter: " + plyDepth.ToString(), false, ConsoleColor.Green);
        goto StartNewNode;
        // END: Enter new node

        StartNewNode:
        // START: Node Entry
        if (board.IsInCheckmate())
        {
            score[plyIndex] = 32010 - plyIndex;
            goto ReturnFrom;
        }

        if (plyDepth <= 0)
        { //QSearch
            // TODO: Evaluate board
            score[plyIndex] = 0;
            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                score[plyIndex] +=
                    pieceList.Count
                    * pieceValue[(int)pieceList.TypeOfPieceInList]
                    * (pieceList.IsWhitePieceList ^ board.IsWhiteToMove ? -1 : 1);
            }

            if (score[plyIndex] >= beta[plyIndex])
            {
                score[plyIndex - 1] = -beta[plyIndex]; // Fail-hard
                goto ReturnFrom;
            }

            if (score[plyIndex] > alpha[plyIndex])
                alpha[plyIndex] = score[plyIndex];
        }
        moves[plyIndex] = board.GetLegalMoves(plyDepth <= 0);
        currentMoves = moves[plyIndex];
        if (currentMoves.Length <= 0)
        {
            score[plyIndex - 1] = 0;
            goto ReturnFrom;
        }

        // Sort Moves
        int[] moveOrder = new int[moves[plyIndex].Length];
        for (int i = 0; i < moves[plyIndex].Length; i++)
        {
            board.MakeMove(currentMoves[i]);

            moveOrder[i] = // TODO: Improve Ordering
                (board.IsInCheckmate() ? -32000 : 0)
                - (currentMoves[i].IsCapture ? 4000 : 0)
                - (board.IsInCheck() ? 2000 : 0)
                + pieceValue[(int)currentMoves[i].MovePieceType]
                - pieceValue[(int)currentMoves[i].CapturePieceType]
                - pieceValue[(int)currentMoves[i].PromotionPieceType]
                - (currentMove[i].Equals(bestOrRefutation[plyIndex - 2]) ? 4000 : 0);
            board.UndoMove(currentMoves[i]);
        }

        Array.Sort(moveOrder, moves[plyIndex]);

        currentMoveIndex[plyIndex] = 0;
        goto CheckNextMove;
        // END: Node Entry
    }
}
