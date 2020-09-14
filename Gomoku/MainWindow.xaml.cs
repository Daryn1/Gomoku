using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Gomoku
{
    /// <summary>
    /// Класс содержит методы, реализующие логику работы главного окна, описанного в MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Объект для управление игрой
        /// </summary>
        private Engine Game = new Engine();

        // Игра против ИИ
        private bool playervsAI = false;
        
        public MainWindow()
        {
            InitializeComponent();
            ResetGame();
        }

        // Данный метод вызывается сразу после инициализации всего окна
        // Используется для автоматического запуска метода StartGame()
        private void OnLoaded(object sender, EventArgs e)
        {
            StartGame();
        }

        /// <summary>
        /// Метод запускает игру ИИ против ИИ
        /// </summary>
        private async void StartGame()
        {
            // Пока не определился победитель
            while (Game.Winner == '_')
            {
                // Получаем ход от ИИ
                int[] AIMove = AI.GetMove(Game.Board);

                // Отображаем выполнение хода на интерфейсе, путём изменения текста клетки
                int squareIndex = AIMove[0] * 15 + AIMove[1];
                Button square = (Button)GomokuBoard.Children[squareIndex];
                square.Content = char.ToUpper(Game.CurrentPlayer);
                square.IsHitTestVisible = false;

                // Передаём ход движку игры
                Game.CheckAndMakeMove(AIMove[0], AIMove[1]);

                // Отображаем текущего игрока и номер хода
                CurrentPlayerLabel.Content = Game.CurrentPlayer;
                MoveNumberLabel.Content = Game.MoveCounter;

                // Время для отрисовки хода
                await Task.Delay(100);           
            }
            
            // Отобразим победителя в случае победы
            MessageBox.Show("Победили " + ((Game.Winner == 'o') ? "нолики" : "крестики"));

            // Запрещаем пользователю нажимать на доску
            GomokuBoard.IsHitTestVisible = false;
        }

        // Данный метод вызывается при нажатии на кнопку Reset.
        // Позволяет пользователю начать новую игру
        private void OnResetButtonClick(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        /// <summary>
        /// Метод сбрасывает игру, возвращая главное окно в исходное состояние
        /// </summary>
        private void ResetGame()
        {
            // Сброс всех изменений в игре
            Game.Reset();

            // Разрешаем пользователю нажимать на доску
            GomokuBoard.IsHitTestVisible = true;

            // Чистим содержимое и разрешаем нажатие каждой кнопки на доске 
            foreach (Button square in GomokuBoard.Children)
            {
                square.Content = ' ';
                square.IsHitTestVisible = true;
            }

            playervsAI = false;
        }

        // Данный метод вызывается при нажатии на любую из клеток доски
        // Позволяет пользователю делать ходы в игре
        private void OnSquareClick(object sender, RoutedEventArgs e)
        {
            Button square = sender as Button;

            // Определяем строку и столбец нажатой клетки
            int row = (int)square.GetValue(Grid.RowProperty);
            int column = (int)square.GetValue(Grid.ColumnProperty);

            // Отображаем выполнение хода на интерфейсе, путём изменения текста клетки
            square.Content = char.ToUpper(Game.CurrentPlayer);
            square.IsHitTestVisible = false;

            // Передаём ход движку игры
            Game.CheckAndMakeMove(row, column);

            // Отображаем текущего игрока и номер хода
            CurrentPlayerLabel.Content = Game.CurrentPlayer;
            MoveNumberLabel.Content = Game.MoveCounter;

            // Отобразить окно сообщения в случае победы
            if (Game.Winner != '_')
            {
                MessageBox.Show("Победили " + ((Game.Winner == 'o') ? "нолики" : "крестики"));
                GomokuBoard.IsHitTestVisible = false;
                return;
            }

            if (playervsAI)
            {
                // Получаем ход от ИИ
                int[] AIMove = AI.GetMove(Game.Board);

                // Отображаем выполнение хода на интерфейсе, путём изменения текста клетки
                int squareIndex = AIMove[0] * 15 + AIMove[1];
                Button square2 = (Button)GomokuBoard.Children[squareIndex];
                square2.Content = char.ToUpper(Game.CurrentPlayer);
                square2.IsHitTestVisible = false;

                // Передаём ход движку игры
                Game.CheckAndMakeMove(AIMove[0], AIMove[1]);

                // Отображаем текущего игрока и номер хода
                CurrentPlayerLabel.Content = Game.CurrentPlayer;
                MoveNumberLabel.Content = Game.MoveCounter;

                // Отобразить окно сообщения в случае победы
                if (Game.Winner != '_')
                {
                    MessageBox.Show("Победили " + ((Game.Winner == 'o') ? "нолики" : "крестики"));
                    GomokuBoard.IsHitTestVisible = false;
                }
            }
        }

        // Данный метод вызывается при нажатии на кнопку Player vs AI
        private void OnPlayervsAIButtonClick(object sender, RoutedEventArgs e)
        {
            ResetGame();
            playervsAI = true;

            // Получаем ход от ИИ
            int[] AIMove = AI.GetMove(Game.Board);

            // Отображаем выполнение хода на интерфейсе, путём изменения текста клетки
            int squareIndex = AIMove[0] * 15 + AIMove[1];
            Button square2 = (Button)GomokuBoard.Children[squareIndex];
            square2.Content = char.ToUpper(Game.CurrentPlayer);
            square2.IsHitTestVisible = false;

            // Передаём ход движку игры
            Game.CheckAndMakeMove(AIMove[0], AIMove[1]);

            // Отображаем текущего игрока и номер хода
            CurrentPlayerLabel.Content = Game.CurrentPlayer;
            MoveNumberLabel.Content = Game.MoveCounter;
        }

        // Данный метод вызывается при нажатии на кнопку AI vs AI
        private void OnAIvsAIButtonClick(object sender, RoutedEventArgs e)
        {
            ResetGame();
            StartGame();
        }

    }
}
