using System;

namespace Gomoku
{
    /// <summary>
    /// Класс содержит методы, необходимые для реализации механики игры Гомоку.
    /// Создан для того, чтобы обнаруживать ошибки со стороны пользователя или ИИ
    /// и управлять процессом игры
    /// </summary>
    public class Engine
    {
        // Массив, который отображает игровую доску.
        // Выбран jagged array, т.к. он быстрее, чем multidimensional array
        // Доказательство: https://stackoverflow.com/questions/597720
        private char[][] board = new char[15][];

        // Текущий игрок
        public char CurrentPlayer;

        public Engine()
        {
            Reset();
        }

        // При обращении к игровой доске возвращаем копию массива, чтобы алгоритм его не изменил
        public char[][] Board
        {
            get
            {
                return (char[][])board.Clone();
            }
        }

        // Счётчик ходов
        public int MoveCounter { get; private set; }

        // Победитель
        public char Winner { get; private set; }
        
        /// <summary>
        /// Метод сбрасывает игру, устанавливая всем переменным значения по умолчанию
        /// </summary>
        public void Reset()
        {
            // Заполняем массив пустыми клетками
            for (int i = 0; i < board.Length; ++i)
                board[i] = new char[15] { '_', '_', '_', '_', '_', '_', '_', '_', '_', '_', '_', '_', '_', '_', '_' };

            // Сброс счётчика ходов
            MoveCounter = 1;

            // Сброс победителя
            Winner = '_';

            // Сброс текущего игрока (первыми ходят крестики)
            CurrentPlayer = 'x';
        }
        
        /// <summary>
        /// Метод получает строку и столбец хода, полученного от пользователя/ИИ 
        /// и проверяет является ли ход допустимым. В случае допустимого хода 
        /// метод изменяет массив и определяет является ли ход победным
        /// </summary>
        public void CheckAndMakeMove(int row, int column)
        {
            if (Winner != '_')
                throw new InvalidOperationException("Попытка продолжить оконченную партию");

            if (board[row][column] != '_')
                throw new ArgumentException("Попытка сделать ход в занятую клетку");
            else // Изменяем массив, если ход сделан в свободную клетку
                board[row][column] = CurrentPlayer;

            // Проверка победы
            CheckForWin(row, column);

            // Передаём ход другому игроку
            MoveCounter++;
            CurrentPlayer = (MoveCounter % 2) == 0 ? 'o' : 'x';
        }

        /// <summary>
        /// Метод проверяет, является ли сделанный ход победным
        /// </summary>
        private void CheckForWin(int moveRow, int moveColumn)
        {
            // Строки, в которых будем искать победную комбинацию ----------> // X____X____X Antidiagonal
            string checkRow = null;                                           // _X___X___X_
            string checkColumn = null;                                        // __X__X__X__
            string checkMainDiagonal = null;                                  // ___X_X_X___                        
            string checkAntidiagonal = null;                                  // ____XXX____
                                                                              // XXXXXXXXXXX Row
            // Формируем строки. Достаточно проверить все клетки, которые     // ____XXX____
            // удалены от сделанного хода на 5 позиций                        // ___X_X_X___                                                          
            for (int checkDistance = -5; checkDistance <= 5; ++checkDistance) // __X__X__X__
            {                                                                 // _X___X___X_
                // Увеличивающиеся индексы массива                            // X____X____X Main diagonal
                int column = moveColumn + checkDistance;
                int row = moveRow + checkDistance;
                int rowInverse = moveRow - checkDistance;

                // Проверки выхода индексов за границы массива
                if (column >= 0 && column < 15)
                {
                    // Добавление в строку символа из массива
                    checkRow += board[moveRow][column];

                    if (row >= 0 && row < 15)
                        checkMainDiagonal += board[row][column];

                    if (rowInverse >= 0 && rowInverse < 15)
                        checkAntidiagonal += board[rowInverse][column];
                }

                if (row >= 0 && row < 15)
                    checkColumn += board[row][moveColumn];
            }

            // Проверка каждой из строк на наличие победной последовательности
            if (DoesStringContainWinningSequence(checkRow)
                || DoesStringContainWinningSequence(checkColumn)
                || DoesStringContainWinningSequence(checkMainDiagonal)
                || DoesStringContainWinningSequence(checkAntidiagonal)
                )
                Winner = CurrentPlayer;
        }

        /// <summary>
        /// Метод проверяет наличие победной последовательности в строке
        /// </summary>
        private bool DoesStringContainWinningSequence(string checkString)
        {
            if (!checkString.Contains(new string(CurrentPlayer, 6))) // 6 и более одинаковых знаков подряд победой не является
                if (checkString.Contains(new string(CurrentPlayer, 5))) // Пять одинаковых знаков является победой
                    return true;

            return false;
        }
    }
}
