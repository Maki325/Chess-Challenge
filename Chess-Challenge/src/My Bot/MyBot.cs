using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        return MoveOnce(board).move;
    }

    record struct Data(Move move, int score) : IComparable<Data>
    {
        public int CompareTo(Data other)
        {
            // if(this.move.CapturePieceType == 0) return -1;
            return other.score.CompareTo(this.score);
        }

        public override string ToString()
        {
            return "Move: " + move.ToString() + ", Score: " + score;
        }
    }

    record struct Position(Square square, PieceType piece);

    // void stuff(Func<int, Piece> GetPiece, int smallestPossibleEnemy)
    void CheckPiece(Piece piece, ref int smallestPossibleEnemy)
    {
        if (piece.PieceType == PieceType.None) return;
        if (piece.IsRook)
        {
            var value = (int)PieceType.Rook;
            if (value < smallestPossibleEnemy)
            {
                smallestPossibleEnemy = value;
                return;
            }
        }
        if (piece.IsQueen)
        {
            var value = (int)PieceType.Queen;
            if (value < smallestPossibleEnemy)
            {
                smallestPossibleEnemy = value;
                return;
            }
        }

        return;
    }

    int GetSmallestEnemy(Board board, Move move)
    {
        var target = move.TargetSquare;
        var rank = target.Rank;
        var file = target.File;
        var file1 = file + 1;
        var fileOffset = (7 - file);
        var fileOffset1 = fileOffset + 1;

        // 00100000

        var middle = 1 << (7 - file);
        byte mask = 0b11111111;
        byte a = (byte)(mask << file1);
        byte b = (byte)(a >> file1);
        byte c = (byte)((byte)(mask << file1) >> file1);
        ulong d = (byte)((byte)(mask << file1) >> file1);
        // byte bitmaskRankRight = ((byte) ((byte)(mask << file1)) >> file1);
        // ulong bitmaskRankRight = ((ulong)(mask << file1) >> file1) << (8 * rank);
        // ulong bitmaskRankLeft = ((ulong)(mask >> fileOffset1) << file) << (8 * rank);

        ulong bitmaskRankRight = (ulong)((byte)((byte)(mask << file1) >> file1)) << (8 * rank);
        ulong bitmaskRankLeft = (ulong)((byte)((byte)(mask >> fileOffset1) << fileOffset1)) << (8 * rank);

        // ulong bitmaskRank = ((ulong)0b11111111) << (8 * rank);
        // ulong bitmaskRank = bitmaskRankLeft | bitmaskRankRight;

        const ulong StartFileOffset = 0x101010101010101;
        ulong bitmaskFile = StartFileOffset << fileOffset;

        var offsetDown = 8 * (7 - rank + 1);
        var offsetUp = 8 * (rank + 1);
        ulong bitmaskFileDown = StartFileOffset << offsetDown >> offsetDown << fileOffset;
        ulong bitmaskFileUp = StartFileOffset >> offsetUp << offsetUp << fileOffset;

        // ulong bitmask = bitmaskFile | bitmaskRank;
        var bitboard = board.GetPieceBitboard(PieceType.Rook, !board.IsWhiteToMove);

        var smallestPossibleEnemy = (int)PieceType.King + 1;

        if ((bitboard & bitmaskRankRight) != 0)
        {
            for (int x = file + 1; x < 8; x++)
            {
                var piece = board.GetPiece(new(x, rank));
                CheckPiece(piece, ref smallestPossibleEnemy);
            }
        }
        if ((bitboard & bitmaskRankLeft) != 0)
        {
            for (int x = 0; x < file; x++)
            {
                var piece = board.GetPiece(new(x, rank));
                CheckPiece(piece, ref smallestPossibleEnemy);
            }
        }
        if ((bitboard & bitmaskFileUp) != 0)
        {
            for (int y = rank + 1; y < 8; y++)
            {
                var piece = board.GetPiece(new(file, y));
                CheckPiece(piece, ref smallestPossibleEnemy);
            }
        }
        if ((bitboard & bitmaskFileDown) != 0)
        {
            for (int y = 0; y < rank; y++)
            {
                var piece = board.GetPiece(new(file, y));
                CheckPiece(piece, ref smallestPossibleEnemy);
            }
        }

        return smallestPossibleEnemy;
    }

    Data MoveOnce(Board board, int current = 0, List<(Move, bool)> positions = null)
    {
        positions = positions ?? new();

        Move[] allMoves = board.GetLegalMoves();
        if (current == 5 || allMoves.Length == 0)
        {
            return new Data(new(), 0);
        }

        // Pick a random move to play if nothing better is found
        Random rng = new();
        List<Data> moves = new()
        {
            new(allMoves[rng.Next(allMoves.Length)], 1)
        };

        var isOpposing = current % 2 != 0;
        var ourSide = isOpposing ? !board.IsWhiteToMove : board.IsWhiteToMove;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return new Data(move, 10_000);
            }
            /**
             * Check only needed positions
             * I.e. only positions made by the same team
             * 
             * If the other team eats a piece we just moved
             * We discard that move completely, because move is stupid
             */

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            var thePieceWeAreMoving = move.MovePieceType;
            var score = pieceValues[(int)capturedPiece.PieceType];

            /*var amount = board.IsWhiteToMove ? move.TargetSquare.Rank : 8-move.TargetSquare.Rank;
            score += amount * 10;*/

            var smallesEnemy = GetSmallestEnemy(board, move);
            if(smallesEnemy != ((int)PieceType.King + 1) && (int)capturedPiece.PieceType < smallesEnemy)
            {
                Console.WriteLine("smallesEnemy: " + smallesEnemy);
                score -= pieceValues[(int)PieceType.King - smallesEnemy] * 3;
            }

                /**
                 * 
                 * 
                    --------
                    --------
                    --------
                    --------
                    ****X***
                    --------
                    --------
                    --------
                 * 
                    ****X***
                    --------
                    --------
                    --------
                    --------
                    --------
                    --------
                 *
                    11111111
                    00000000
                    00000000
                    00000000
                    00000000
                    00000000
                    00000000
                    00000000


                
                    ----*---
                    ----*---
                    ----*---
                    ----*---
                    ----X---
                    ----*---
                    ----*---
                    ----*---

                00000000
                00000000
                11111111
                00000000
                00000000
                00000000
                00000000
                00000000

                
                00000100
                00000100
                00000100
                00000100
                00000100
                00000100
                00000100
                00000100

                00000001
                00000001
                00000001
                00000001
                00000001
                00000001
                00000001
                00000001

                0 > 4 | 1 > 4 | 1

                1 < 3 | 1 < 3 | 1
                */

                switch (thePieceWeAreMoving)
            {
                case PieceType.Pawn: {
                    var amount = board.IsWhiteToMove ? move.TargetSquare.Rank : 8-move.TargetSquare.Rank;
                    score += amount * 5;
                    break;
                }
            }

            /*if (isOpposing && capturedPiece.PieceType != PieceType.None)
            {
                moves.Add(new(move, -100_000));
            }*/

            if (positions.Any(tuple => tuple.Item2 == board.IsWhiteToMove && tuple.Item1.MovePieceType != PieceType.Pawn && tuple.Item1.MovePieceType == move.MovePieceType && tuple.Item1.StartSquare == move.TargetSquare))
            {
                //continue;
                moves.Add(new Data(move, (isOpposing ? -score : score / 2)));
            }
            else
            {
                moves.Add(new Data(move, (isOpposing ? -score * 2 : score)));
            }
            // Find highest value capture
        }

        moves.Sort();

        if(moves.Count == 0)
        {
            return new Data(new(), 0);
        }

        List<Data> bestMoves = new();
        for (int i = 0; i < Math.Min(5, moves.Count); i++)
        {
            var (move, score) = moves.ElementAt(i);
            board.MakeMove(move);
            positions.Add((move, board.IsWhiteToMove));
            var fullScore = MoveOnce(board, current + 1, positions).score + score;
            positions.RemoveAt(positions.Count - 1);
            // if (current % 2 == 0)
            // {
            //     fullScore = score - fullScore;
            // } else
            // {
            //     fullScore += score;
            // }
            board.UndoMove(move);
            bestMoves.Add(new Data(move, fullScore));
        }

        // Can be done faster, if we just go through the whole list, and find the biggest one
        bestMoves.Sort();

        // Console.WriteLine("allMoves: " + allMoves.Count());
        // Console.WriteLine("moves: " + moves.Count);
        // foreach (var a in moves.ToArray())
        // {
        //     Console.WriteLine(a);
        // }
        // 
        // Console.WriteLine("bestMoves: " + bestMoves.Count);
        // foreach (var a in bestMoves.ToArray())
        // {
        //     Console.WriteLine(a);
        // }

        // return new(allMoves[rng.Next(allMoves.Length)], 1);

        return bestMoves.ElementAt(0);
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}