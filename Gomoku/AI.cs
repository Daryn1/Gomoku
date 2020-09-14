using System;
using System.Collections.Generic;
using System.Linq;

namespace Gomoku
{
    /// <summary>
    /// Класс содержит методы, необходимые ИИ для нахождения оптимального хода
    /// </summary>
    class AI
    {
        // Знак ИИ и знак противника
        public static char MyMark;
        public static char EnemyMark;

        // Глубина поиска в алгоритме MiniMax
        public static int SearchDepth = 7;
        
        // Для удобства строки и столбцы ходов будем хранить в объектах типа Move
        public class Move
        {
            public int Row { get; private set; }
            public int Column { get; private set; }
            public int Score { get; set; }
            public Move(int row, int column)
            {
                Row = row;
                Column = column;
                Score = 0;
            }

            // Переопределяем метод Equals для возможности нахождения двух одинаковых ходов
            // Нужен для того, чтобы мы могли определить неповторяющиеся ходы
            public override bool Equals(object obj)
            {
                return ((Move)obj).Row == Row && ((Move)obj).Column == Column;
            }

            // Вместе с методом Equals мы должны переопределить метод GetHashCode
            public override int GetHashCode()
            {
                return Row.GetHashCode() + Column.GetHashCode();
            }
        }

        /// <summary>
        /// Метод возвращает наилучший ход, найденный ИИ в результате анализа доски board
        /// </summary>
        public static int[] MakeMove(char[][] board)
        {
            // Определяем номер текущего хода
            int currentMoveNumber = CalculateMoveNumber(board);

            // Определяем свой знак и знак противника, исходя из номера текущего хода
            MyMark = currentMoveNumber % 2 == 0 ? 'o' : 'x';
            EnemyMark = MyMark == 'x' ? 'o' : 'x';

            // Самая оптимальная позиция первого хода - центр доски
            if (currentMoveNumber == 1)
                return new int[] { 7, 7 };

            // Ищем лучшие ходы согласно эвристике
            List<Move> bestMoves = Find3BestMoves(board, MyMark);
            Move bestMove = bestMoves[0];

            // Для третьего хода эвристика и Minimax даёт один и тот же результат - смысла вычислять Minimax нет
            if (currentMoveNumber == 3
                // Если найден только один лучший ход 
                || bestMoves.Count == 1
                // Если лучший ход является победным
                || IsBestMoveWinning(bestMove)
                // Если лучший ход закрывает победную комбинацию у противника
                || DoesBestMovePreventsLosing(bestMove)
                )
                return new int[] { bestMove.Row, bestMove.Column };
            
            // Максимальная прогнозируемая оценка хода. Нужна для поиска максимального значения оценки хода
            int maxPredictedScore = int.MinValue;

            // Для каждого лучшего хода
            foreach (Move move in bestMoves)
            {
                // Изменяем доску так, будто мы сделали этот ход
                board[move.Row][move.Column] = MyMark;

                // Прогнозируемая оценка хода. Используется как мера сравнения лучших ходов
                int predictedScore;

                // Первые 20 ходов в качестве меры сравнения используем эвристическую оценку хода, т.к. в первые 
                // 20 ходов победить/проиграть маловероятно. После 20 хода в качестве качестве меры сравнения 
                // используем глубину победы/поражения. Это позволяет нам выбрать такой ход, который либо приводит 
                // к победе за наименьшее число ходов, либо приводит к поражению за наибольшее число ходов
                if (currentMoveNumber < 20)
                    predictedScore = Minimax(board, SearchDepth, EnemyMark, false);
                else // Также мы увеличиваем глубину поиска на 1, т.к. число неполных веток дерева увеличивается
                    predictedScore = Minimax(board, SearchDepth, EnemyMark, true);

                // Отменяем изменения в доске
                board[move.Row][move.Column] = '_';

                // Поиск хода с максимальным значением score
                if (predictedScore > maxPredictedScore)
                {
                    maxPredictedScore = predictedScore;
                    bestMove = move;
                }
            }

            return new int[] { bestMove.Row, bestMove.Column };
        }

        /// <summary>
        /// Вычисляет номер текущего хода по количеству занятых клеток на доске board
        /// </summary>
        private static int CalculateMoveNumber(char[][] board)
        {
            // Счётчик ходов
            int moveCounter = 1;

            // Проверяем каждую клетку на доске
            for (int row = 0; row < board.GetLength(0); ++row)
                for (int column = 0; column < board.GetLength(0); ++column)
                    // Если клетка не пустая, увеличиваем счётчик
                    if (board[row][column] != '_')
                        moveCounter++;

            return moveCounter;
        }

        /// <summary>
        /// Метод возвращает значение true, если лучший ход является победным
        /// </summary>
        private static bool IsBestMoveWinning(Move bestMove)
        {
            // Оценка хода может быть больше 20000 только 
            // в случае нахождения победной комбинации
            return bestMove.Score > 20000;
        }

        /// <summary>
        /// Метод возвращает значение true, если лучший ход предотвращает проигрыш
        /// </summary>
        private static bool DoesBestMovePreventsLosing(Move bestMove)
        {
            // Оценка хода может быть больше 10000 при этом меньше 20000  
            // только если этот ход закрывает победную комбинацию у противника
            return bestMove.Score > 10000 && bestMove.Score < 20000;
        }
        
        /// <summary>
        /// Метод возвращает оценку хода с учётом будущих оптимальных ходов с обеих сторон 
        /// Данный метод имеет 2 режима работы в зависимости от переменной returnWinLoseDepth.
        /// При вызове метода с returnWinLoseDepth = true он возвращает глубину победы/проигрыша, 
        /// а при returnWinLoseDepth = false возвращает эвристическую оценку хода
        /// </summary>      
        public static int Minimax(char[][] board, int depth, char mark, bool returnWinLoseDepth)
        {
            // Противоположный знак
            char oppositeMark = mark == 'x' ? 'o' : 'x';

            // Ищем лучшие ходы, согласно эвристике
            List<Move> goodMoves = Find3BestMoves(board, mark);

            // Вычисление оценки для конечных узлов дерева решений 
            // Проверка ничьи, победы или проигрыша
            if (goodMoves.Count == 0 || IsBestMoveWinning(goodMoves[0]) || depth == 0)
            {
                // При нахождении победы возвращается положительное значение, в ином случае - отрицательное
                if (returnWinLoseDepth)
                    return mark == MyMark ? depth : -depth;
                else
                    return mark == MyMark ? goodMoves[0].Score : -goodMoves[0].Score;
            }

            // Минимакс вызывается попеременно для каждого из знаков, при этом для своего знака минимакс 
            // возвращает максимальное значение оценки, а для противоположного знака - минимальное
            // Таким образом, моделируется игра, в которой каждая из сторон выбирает наиболее лучший ход
            if (mark == MyMark)
            {
                int maxPredictedScore = int.MinValue;
                foreach (Move move in goodMoves)
                {
                    // Изменяем доску так, будто мы сделали этот ход
                    board[move.Row][move.Column] = mark;

                    // Прогнозируем оценку хода для меньшего значения depth
                    int predictedScore = Minimax(board, depth - 1, EnemyMark, returnWinLoseDepth);

                    // Отменяем изменения в доске
                    board[move.Row][move.Column] = '_';

                    // Максимальная оценка
                    maxPredictedScore = Math.Max(predictedScore, maxPredictedScore);
                }

                return maxPredictedScore;
            }
            else
            {
                int minPredictedScore = int.MaxValue;
                foreach (Move move in goodMoves)
                {
                    // Изменяем доску так, будто мы сделали этот ход
                    board[move.Row][move.Column] = mark;

                    // Прогнозируем оценку хода для меньшего значения depth
                    int predictedScore = Minimax(board, depth - 1, MyMark, returnWinLoseDepth);

                    // Отменяем изменения в доске
                    board[move.Row][move.Column] = '_';

                    // Минимальная оценка
                    minPredictedScore = Math.Min(predictedScore, minPredictedScore);
                }

                return minPredictedScore;
            }
        }
        
        /// <summary>
        /// Метод возвращает 3 лучших хода.
        /// Лучшие ходы - это ходы, имеющие наибольшое значение эвристической оценки
        /// </summary>
        public static List<Move> Find3BestMoves(char[][] board, char mark) // List<Move> movesList,
        {
            // Противоположный знак
            char oppositeMark = mark == 'x' ? 'o' : 'x';

            // Лучшие ходы будем отбирать из хороших ходов
            List<Move> goodMoves = FindGoodMoves(board);

            // Если возможен только один ход, нет смысла вычислять оценки
            if (goodMoves.Count == 1)
                return goodMoves;

            // Для каждого найденного хорошего хода
            foreach (Move move in goodMoves)
            {
                // Определяем ценность хода для атаки
                int attackScore = EvaluateMove(move, mark, board);

                // Лучшая защита - это нападение. Умножение на 2 обеспечивает, в случае возможности либо построить  
                // комбинацию, либо закрыть такую же комбинацю у противника, выбор в пользу первого варианта
                attackScore = 2 * attackScore;

                // Определяем ценность хода для защиты, вызывая функцию EvaluateMove для знака oppositeMark,
                // т.е. в действительности определяется ценность хода для противника, если бы это был его ход
                int defenceScore = EvaluateMove(move, oppositeMark, board);

                // Суммарная оценка хода определяется как сумма оценки атаки и оценки защиты
                move.Score = attackScore + defenceScore;
            }

            // Сортируем ходы в порядке убывания их оценки
            List<Move> bestMoves = goodMoves.OrderByDescending(m => m.Score).Take(3).ToList(); // 

            return bestMoves;
        }

        /// <summary>                                                        
        /// Метод ищет хорошие ходы на доске board.                          _ggg_ 
        /// Хорошими ходами (g) являются ближайшие к занятым клеткам (x) ->  _gxg_
        /// </summary>                                                       _ggg_
        public static List<Move> FindGoodMoves(char[][] board)
        {
            // Список в котором будем хранить ходы                          
            List<Move> goodMoves = new List<Move>();

            // Счётчик ходов, сделанных на доске
            int moveCounter = 1;

            // Пробегаем по всей доске
            for (int row = 0; row < board.GetLength(0); ++row)
                for (int column = 0; column < board.GetLength(0); ++column)
                    // Ищем занятые клетки
                    if (board[row][column] != '_')
                    {
                        // Добавляем соседей занятых клеток в список ходов
                        for (int i = -1; i <= 1; ++i)
                            for (int j = -1; j <= 1; ++j)
                            {
                                int moveRow = row + i;
                                int moveColumn = column + j;

                                if (moveRow >= 0 && moveRow < 15 // Проверка выхода за границы массива
                                    && moveColumn >= 0 && moveColumn < 15
                                    && board[moveRow][moveColumn] == '_' // Клетка не должна быть занята
                                    )
                                    goodMoves.Add(new Move(moveRow, moveColumn));
                            }

                        moveCounter++;
                    }

            // В случае, если на втором ходу другая сторона сделала ход не в клетку (7,7), 
            // то наш второй ход должен сделан в клетку, ближайшую к клетке (7,7)
            if (moveCounter == 2)
            {
                double minDistanceToCenter = 7.0;
                Move bestMove = goodMoves[0];
                foreach (Move move in goodMoves)
                {
                    double distanceToCenter = Math.Sqrt(Math.Pow(move.Row - 7.0, 2) + Math.Pow(move.Column - 7.0, 2));
                    if (distanceToCenter < minDistanceToCenter)
                    {
                        minDistanceToCenter = distanceToCenter;
                        bestMove = move;
                    }
                }
                goodMoves = new List<Move> { bestMove };
            }

            // Возвращаем только неповторяющиеся ходы
            return goodMoves.Distinct().ToList();
        }

        /// <summary>
        /// Метод возвращает оценку хода move для знака mark на доске board
        /// </summary>
        private static int EvaluateMove(Move move, char mark, char[][] board)
        {
            // Противоположный знак
            char oppositeMark = mark == 'x' ? 'o' : 'x';

            // Строки, в которых будем искать комбинации знаков ----------> // X___X___X Antidiagonal
            string evaluatedRow = mark.ToString();                          // _X__X__X_
            string evaluatedColumn = mark.ToString();                       // __X_X_X__
            string evaluatedDiagonal = mark.ToString();                     // ___XXX___
            string evaluatedAntidiagonal = mark.ToString();                 // XXXXXXXXX Row
                                                                            // ___XXX___
            // Логические переменные для остановки добавления знаков        // __X_X_X__
            // в строку. С их помощью мы гарантируем, что в сформированных  // _X__X__X_
            // строках не будет знаков oppositeMark                         // X___X___X Main diagonal
            bool stopAddRowRight = false;
            bool stopAddRowLeft = false;
            bool stopAddColumnRight = false;
            bool stopAddColumnLeft = false;
            bool stopAddDiagonalRight = false;
            bool stopAddDiagonalLeft = false;
            bool stopAddAntidiagonalRight = false;
            bool stopAddAntidiagonalLeft = false;

            // Формируем строки. Добавляем в строки все клетки, которые удалены от 
            // хода move на 5 позиций. Если наткнулись на клетку с противоположным  
            // знаком, то останавливаем заполнение строк в этом направлении 
            for (int i = 1; i <= 4; ++i)
            {
                // Увеличивающиеся индексы массива
                int columnIncreasing = move.Column + i;
                int columnDecreasing = move.Column - i;
                int rowIncreasing = move.Row + i;
                int rowDecreasing = move.Row - i;

                // Проверяем выход индекса за границы массива
                if (columnIncreasing < 15)
                {
                    // Проверяем, остановлено ли добавление в этом направлении. В случае обнаружения
                    // клетки с противоположным знаком, останавливаем добавление в этом направлении
                    if (!stopAddRowRight
                        && !(stopAddRowRight = board[move.Row][columnIncreasing] == oppositeMark)
                        )
                        evaluatedRow += board[move.Row][columnIncreasing];

                    if (!stopAddDiagonalRight && rowIncreasing < 15
                        && !(stopAddDiagonalRight = board[rowIncreasing][columnIncreasing] == oppositeMark)
                        )
                        evaluatedDiagonal += board[rowIncreasing][columnIncreasing];

                    if (!stopAddAntidiagonalLeft && rowDecreasing >= 0
                        && !(stopAddAntidiagonalLeft = board[rowDecreasing][columnIncreasing] == oppositeMark)
                        )
                        evaluatedAntidiagonal = board[rowDecreasing][columnIncreasing] + evaluatedAntidiagonal;
                }

                if (columnDecreasing >= 0)
                {
                    if (!stopAddRowLeft
                        && !(stopAddRowLeft = board[move.Row][columnDecreasing] == oppositeMark)
                        )
                        evaluatedRow = board[move.Row][columnDecreasing] + evaluatedRow;

                    if (!stopAddDiagonalLeft && rowDecreasing >= 0
                        && !(stopAddDiagonalLeft = board[rowDecreasing][columnDecreasing] == oppositeMark)
                        )
                        evaluatedDiagonal = board[rowDecreasing][columnDecreasing] + evaluatedDiagonal;

                    if (!stopAddAntidiagonalRight && rowIncreasing < 15
                        && !(stopAddAntidiagonalRight = board[rowIncreasing][columnDecreasing] == oppositeMark)
                        )
                        evaluatedAntidiagonal += board[rowIncreasing][columnDecreasing];
                }

                if (!stopAddColumnRight && rowIncreasing < 15
                    && !(stopAddColumnRight = board[rowIncreasing][move.Column] == oppositeMark)
                    )
                    evaluatedColumn += board[rowIncreasing][move.Column];

                if (!stopAddColumnLeft && rowDecreasing >= 0
                    && !(stopAddColumnLeft = board[rowDecreasing][move.Column] == oppositeMark)
                    )
                    evaluatedColumn = board[rowDecreasing][move.Column] + evaluatedColumn;
            }

            // Возвращаем сумму оценок всех сформированных строк
            return FindAndEvaluateSequence(evaluatedRow, mark) + FindAndEvaluateSequence(evaluatedColumn, mark)
                + FindAndEvaluateSequence(evaluatedDiagonal, mark) + FindAndEvaluateSequence(evaluatedAntidiagonal, mark);
        }

        /// <summary>
        /// Метод осуществляет поиск комбинаций в строке evaluatedString путём сравнения
        /// с шаблонами и возвращает числовую оценку найденной комбинации
        /// </summary>
        private static int FindAndEvaluateSequence(string evaluatedString, char mark)
        {
            if (mark == 'x')
                if (Templates.Crosses.Five.Any(evaluatedString.Contains))
                    return 10000;
                else if (Templates.Crosses.OpenFour.Any(evaluatedString.Contains))
                    return 65;
                else if (Templates.Crosses.ClosedFours.Any(evaluatedString.Contains))
                    return 33;
                else if (Templates.Crosses.OpenThrees.Any(evaluatedString.Contains))
                    return 17;
                else if (Templates.Crosses.OpenTwos.Any(evaluatedString.Contains))
                    return 9;
                else if (Templates.Crosses.ClosedThrees.Any(evaluatedString.Contains))
                    return 5;
                else if (Templates.Crosses.OpenOnes.Any(evaluatedString.Contains))
                    return 3;
                else if (Templates.Crosses.ClosedTwos.Any(evaluatedString.Contains))
                    return 2;
                else // Закрытые единицы
                    return 0;
            else
            if (Templates.Noughts.Five.Any(evaluatedString.Contains))
                return 10000;
            else if (Templates.Noughts.OpenFour.Any(evaluatedString.Contains))
                return 65;
            else if (Templates.Noughts.ClosedFours.Any(evaluatedString.Contains))
                return 33;
            else if (Templates.Noughts.OpenThrees.Any(evaluatedString.Contains))
                return 17;
            else if (Templates.Noughts.OpenTwos.Any(evaluatedString.Contains))
                return 9;
            else if (Templates.Noughts.ClosedThrees.Any(evaluatedString.Contains))
                return 5;
            else if (Templates.Noughts.OpenOnes.Any(evaluatedString.Contains))
                return 3;
            else if (Templates.Noughts.ClosedTwos.Any(evaluatedString.Contains))
                return 2;
            else // Закрытые единицы
                return 0;
        }

        // Шаблоны комбинаций для крестиков и ноликов
        private static class Templates
        {
            public static class Crosses
            {
                public static readonly List<string> Five = new List<string>
                { "xxxxx" };

                public static readonly List<string> OpenFour = new List<string>
                { "_xxxx_" };

                public static readonly List<string> ClosedFours = new List<string>
                { "_xxxx", "x_xxx", "xx_xx", "xxx_x", "xxxx_" };

                public static readonly List<string> OpenThrees = new List<string>
                { "_xxx__", "__xxx_", "_x_xx_", "_xx_x_" };

                public static readonly List<string> ClosedThrees = new List<string>
                { "__xxx", "_xxx_", "xxx__", "_xx_x", "x_xx_", "_x_xx", "xx_x_",  "x__xx", "xx__x", "x_x_x" };

                public static readonly List<string> OpenTwos = new List<string>
                { "_xx___", "__xx__", "___xx_", "_x_x__", "__x_x_" };

                public static readonly List<string> ClosedTwos = new List<string>
                { "___xx", "__xx_", "_xx__", "xx___", "__x_x", "_x__x", "x___x", "x_x__", "x__x_", "_x_x_" };

                public static readonly List<string> OpenOnes = new List<string>
                { "____x_", "___x__", "__x___", "_x____" };
            }

            public static class Noughts
            {
                public static readonly List<string> Five = new List<string>
                { "ooooo"};

                public static readonly List<string> OpenFour = new List<string>
                { "_oooo_"};

                public static readonly List<string> ClosedFours = new List<string>
                { "_oooo", "o_ooo", "oo_oo", "ooo_o", "oooo_" };

                public static readonly List<string> OpenThrees = new List<string>
                { "_ooo__", "__ooo_", "_o_oo_", "_oo_o_" };

                public static readonly List<string> ClosedThrees = new List<string>
                { "__ooo", "_ooo_", "ooo__", "_oo_o", "o_oo_", "_o_oo", "oo_o_",  "o__oo", "oo__o", "o_o_o" };

                public static readonly List<string> OpenTwos = new List<string>
                { "_oo___", "__oo__", "___oo_", "_o_o__", "__o_o_" };

                public static readonly List<string> ClosedTwos = new List<string>
                { "___oo", "__oo_", "_oo__", "oo___", "__o_o", "_o__o", "o___o", "o_o__", "o__o_", "_o_o_" };

                public static readonly List<string> OpenOnes = new List<string>
                { "____o_", "___o__", "__o___", "_o____" };
            }
        }
    }
}
