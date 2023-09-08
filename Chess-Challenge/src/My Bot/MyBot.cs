using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        return MoveOnce(board).Item1;
        // return board.GetLegalMoves()[0];
    }

    (Move, int) MoveOnce(Board board, int current = 0)
    {
        Move[] allMoves = board.GetLegalMoves();
        if (current == 3 || allMoves.Length == 0)
        {
            return (new(), 0);
        }

        // Pick a random move to play if nothing better is found
        Random rng = new();
        // Console.WriteLine("allMoves.Length:" + allMoves.Length);
        // return (allMoves[0], 5);
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            board.MakeMove(move);
            // Console.WriteLine("board.IsWhiteToMove: " + board.IsWhiteToMove);
            var (_, score) = MoveOnce(board, current + 1);
            board.UndoMove(move);
            var completeScore = score;
            if (current % 2 == 0)
            {
                completeScore += capturedPieceValue;
            } else
            {
                completeScore -= capturedPieceValue;
            }

            if (completeScore > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = completeScore;
            }
        }

        return (moveToPlay, highestValueCapture);
    }

    // int CheckTree(Board board, Move move, int max, int current = 0)
    // {
    //     if (max == current) return 0;
    //     board.MakeMove(move);
    //     int val = CheckTree(board, max, current + 1);
    //     board.UndoMove(move);
    //     return val;
    // }

    // int CheckTree(Board board, int max, int current = 0)
    // {
    //     if (max == current) return 0;
    //     var (move, score) = MoveOnce(board);
    //     board.MakeMove(move);
    //     int val = CheckTree(board, max, current + 1);
    //     board.UndoMove(move);
    //     return val + score;
    // }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}