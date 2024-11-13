using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShikakuGame
{
    public partial class MainWindow : Window
    {
        private int gridSize = 5;
        private int maxRectangleArea = 5; // Початкове значення для 5x5 сітки

        private bool isSelecting = false;
        private Border startCell;

        private Dictionary<Border, RectangleInfo> rectangles = new Dictionary<Border, RectangleInfo>();
        private List<RectangleInfo> generatedRectangles;

        private int[,] numbers;

        private bool[,] occupied;
        private List<RectangleInfo> rectangleList;
        private Random rand;


        private Rectangle selectionRectangle; // Для відображення границі виділення

        // Список для постійних прямокутників
        private List<Rectangle> permanentRectangles = new List<Rectangle>();

        public MainWindow()
        {
            InitializeComponent();

            // Встановлюємо вікно по центру екрану
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            SetWindowAndGridSize(gridSize + 2);
            CreateGrid();
            GenerateNewPuzzle();

            // Ініціалізуємо selectionRectangle
            selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection() { 2 }, // Пунктирна лінія
                Fill = Brushes.Transparent,
                Visibility = Visibility.Collapsed, // Приховуємо його спочатку
                IsHitTestVisible = false // Не перехоплює події миші
            };

            // Встановлюємо високий Z-індекс, щоб прямокутник був над іншими елементами
            Panel.SetZIndex(selectionRectangle, 100);

            // Додаємо його до GameGrid
            GameGrid.Children.Add(selectionRectangle);
        }

        private void SetWindowAndGridSize(int gridSize)
        {
            int cellSize = 40; // Розмір однієї клітинки в пікселях
            int gridPixelSize = gridSize * cellSize;

            // Встановлюємо розміри GameGrid
            GameGrid.Width = gridPixelSize;
            GameGrid.Height = gridPixelSize;

            // Встановлюємо розміри вікна з урахуванням додаткових елементів (кнопок тощо)
            this.MinWidth = gridPixelSize + 60; // Додаткові пікселі для відступів
            this.MinHeight = gridPixelSize + 160; // Додаткові пікселі для кнопок та відступів

            this.Width = this.MinWidth;
            this.Height = this.MinHeight;
        }

        private void CreateGrid()
        {
            GameGrid.Children.Clear();
            GameGrid.RowDefinitions.Clear();
            GameGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < gridSize; i++)
            {
                GameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    Border cellBorder = new Border
                    {
                        Style = (Style)FindResource("GameCellBorder")
                    };

                    TextBlock cellText = new TextBlock
                    {
                        Text = "",
                        Style = (Style)FindResource("GameCellText"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    cellBorder.Child = cellText;
                    cellBorder.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
                    cellBorder.MouseLeftButtonUp += Cell_MouseLeftButtonUp;
                    cellBorder.MouseRightButtonDown += Cell_MouseRightButtonDown;

                    // Додаємо обробник MouseMove
                    cellBorder.MouseMove += Cell_MouseMove;

                    GameGrid.Children.Add(cellBorder);
                    Grid.SetRow(cellBorder, row);
                    Grid.SetColumn(cellBorder, col);
                }
            }

            // Додаємо selectionRectangle після створення клітинок
            if (selectionRectangle != null && !GameGrid.Children.Contains(selectionRectangle))
            {
                GameGrid.Children.Add(selectionRectangle);
                Panel.SetZIndex(selectionRectangle, 100);
            }
        }

        private void GenerateNewPuzzle()
        {
            InitializePuzzle();

            bool success = GenerateRectangles();

            if (!success)
            {
                // Якщо генерація не вдалася, повторюємо спробу
                GenerateNewPuzzle();
                return;
            }

            PlaceNumbersInRectangles();
            PlaceNumbers();
            rectangles.Clear();

            // Зберігаємо згенеровані прямокутники
            generatedRectangles = rectangleList;
        }

        private void InitializePuzzle()
        {
            numbers = new int[gridSize, gridSize];
            occupied = new bool[gridSize, gridSize];
            rand = new Random();
            rectangleList = new List<RectangleInfo>();
        }

        private bool GenerateRectangles()
        {
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (!occupied[row, col])
                    {
                        bool rectangleCreated = TryCreateRectangle(row, col);

                        if (!rectangleCreated)
                        {
                            bool extended = ExtendExistingRectangle(row, col);

                            if (!extended)
                            {
                                // Якщо не вдалося створити або розширити прямокутник, перезапускаємо генерацію
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool TryCreateRectangle(int row, int col)
        {
            int maxWidth = Math.Min(maxRectangleArea, gridSize - col);
            int maxHeight = Math.Min(maxRectangleArea, gridSize - row);

            List<Tuple<int, int>> possibleSizes = GetPossibleRectangleSizes(row, col, maxWidth, maxHeight);

            if (possibleSizes.Count > 0)
            {
                var size = possibleSizes[rand.Next(possibleSizes.Count)];
                int width = size.Item1;
                int height = size.Item2;

                MarkCellsAsOccupied(row, col, width, height);

                rectangleList.Add(new RectangleInfo(row, col, width, height));

                return true;
            }
            else
            {
                return false;
            }
        }

        private List<Tuple<int, int>> GetPossibleRectangleSizes(int row, int col, int maxWidth, int maxHeight)
        {
            List<Tuple<int, int>> possibleSizes = new List<Tuple<int, int>>();

            for (int width = 1; width <= maxWidth; width++)
            {
                for (int height = 1; height <= maxHeight; height++)
                {
                    int area = width * height;
                    if (area >= 2 && area <= maxRectangleArea)
                    {
                        if (!DoesOverlap(row, col, width, height))
                        {
                            possibleSizes.Add(Tuple.Create(width, height));
                        }
                    }
                }
            }

            return possibleSizes;
        }

        private bool DoesOverlap(int row, int col, int width, int height)
        {
            for (int r = row; r < row + height; r++)
            {
                for (int c = col; c < col + width; c++)
                {
                    if (occupied[r, c])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void MarkCellsAsOccupied(int row, int col, int width, int height)
        {
            for (int r = row; r < row + height; r++)
            {
                for (int c = col; c < col + width; c++)
                {
                    occupied[r, c] = true;
                }
            }
        }

        private bool ExtendExistingRectangle(int row, int col)
        {
            // Спробуємо розширити прямокутник зліва
            if (col > 0 && occupied[row, col - 1])
            {
                if (TryExtendRectangleLeft(row, col))
                    return true;
            }

            // Спробуємо розширити прямокутник зверху
            if (row > 0 && occupied[row - 1, col])
            {
                if (TryExtendRectangleUp(row, col))
                    return true;
            }

            return false;
        }

        private bool TryExtendRectangleLeft(int row, int col)
        {
            foreach (var rect in rectangleList)
            {
                if (rect.StartRow <= row && row <= rect.EndRow && rect.EndCol + 1 == col)
                {
                    int newWidth = rect.Width + 1;
                    int area = newWidth * rect.Height;
                    if (area >= 2 && area <= maxRectangleArea)
                    {
                        // Оновлюємо прямокутник
                        rect.Width = newWidth;

                        // Маркуємо клітинку як зайняту
                        occupied[row, col] = true;

                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryExtendRectangleUp(int row, int col)
        {
            foreach (var rect in rectangleList)
            {
                if (rect.StartCol <= col && col <= rect.EndCol && rect.EndRow + 1 == row)
                {
                    int newHeight = rect.Height + 1;
                    int area = rect.Width * newHeight;
                    if (area >= 2 && area <= maxRectangleArea)
                    {
                        // Оновлюємо прямокутник
                        rect.Height = newHeight;

                        // Маркуємо клітинку як зайняту
                        occupied[row, col] = true;

                        return true;
                    }
                }
            }
            return false;
        }

        private void PlaceNumbersInRectangles()
        {
            foreach (var rect in rectangleList)
            {
                int numRow = rect.StartRow + rand.Next(rect.Height);
                int numCol = rect.StartCol + rand.Next(rect.Width);

                numbers[numRow, numCol] = rect.Area;
            }
        }

        private void PlaceNumbers()
        {
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    var cell = GetCell(row, col);
                    if (cell != null)
                    {
                        if (cell.Child is TextBlock textBlock)
                        {
                            textBlock.Text = numbers[row, col] != 0 ? numbers[row, col].ToString() : "";
                        }
                        // Очищаємо фон клітинки
                        cell.Background = Brushes.White;
                    }
                }
            }
        }

        private Border GetCell(int row, int col)
        {
            foreach (var child in GameGrid.Children)
            {
                if (child is Border cell && Grid.GetRow(cell) == row && Grid.GetColumn(cell) == col)
                {
                    return cell;
                }
            }
            return null;
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            // Очищаємо постійні прямокутники
            foreach (var rect in permanentRectangles)
            {
                GameGrid.Children.Remove(rect);
            }
            permanentRectangles.Clear();

            // Очищаємо списки прямокутників
            rectangles.Clear();
            rectangleList.Clear();

            // Оновлюємо розміри вікна
            if (gridSize == 5 || gridSize == 7 || gridSize == 10)
            {
                SetWindowAndGridSize(gridSize + 2);
            }
            else
            {
                SetWindowAndGridSize(gridSize);
            }
            CreateGrid();
            GenerateNewPuzzle();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in GameGrid.Children)
            {
                if (child is Border cell)
                {
                    cell.Background = Brushes.White;
                    if (cell.Child is TextBlock textBlock)
                    {
                        textBlock.Text = numbers[Grid.GetRow(cell), Grid.GetColumn(cell)] != 0 ? numbers[Grid.GetRow(cell), Grid.GetColumn(cell)].ToString() : "";
                    }
                }
            }

            // Видаляємо постійні прямокутники з GameGrid
            foreach (var rect in permanentRectangles)
            {
                GameGrid.Children.Remove(rect);
            }
            permanentRectangles.Clear();

            rectangles.Clear();
            rectangleList.Clear();
        }

        private void ShowSolution_Click(object sender, RoutedEventArgs e)
        {
            if (generatedRectangles != null)
            {
                // Очищаємо поточні кольори
                PlaceNumbers();
                // Відображаємо прямокутники
                DisplayRectangles(generatedRectangles);
            }
        }

        private void DisplayRectangles(List<RectangleInfo> rectangleList)
        {
            List<Brush> colors = new List<Brush>
            {
                Brushes.LightBlue,
                Brushes.LightGreen,
                Brushes.LightPink,
                Brushes.LightYellow,
                Brushes.LightCoral,
                Brushes.LightCyan,
                Brushes.LightGoldenrodYellow,
                Brushes.LightGray,
                Brushes.LightSalmon,
                Brushes.LightSeaGreen
            };

            int colorIndex = 0;

            foreach (var rect in rectangleList)
            {
                Brush rectColor = colors[colorIndex % colors.Count];
                colorIndex++;

                for (int row = rect.StartRow; row <= rect.EndRow; row++)
                {
                    for (int col = rect.StartCol; col <= rect.EndCol; col++)
                    {
                        var cell = GetCell(row, col);
                        if (cell != null)
                        {
                            cell.Background = rectColor;
                        }
                    }
                }
            }
        }

        private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cell)
            {
                isSelecting = true;
                startCell = cell;

                // Відображаємо selectionRectangle
                selectionRectangle.Visibility = Visibility.Visible;
                UpdateSelectionRectangle(cell);
            }
        }

        private void Cell_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting && sender is Border cell)
            {
                UpdateSelectionRectangle(cell);
            }
        }

        private void UpdateSelectionRectangle(Border currentCell)
        {
            int startRow = Grid.GetRow(startCell);
            int startCol = Grid.GetColumn(startCell);
            int currentRow = Grid.GetRow(currentCell);
            int currentCol = Grid.GetColumn(currentCell);

            int minRow = Math.Min(startRow, currentRow);
            int maxRow = Math.Max(startRow, currentRow);
            int minCol = Math.Min(startCol, currentCol);
            int maxCol = Math.Max(startCol, currentCol);

            int rowSpan = maxRow - minRow + 1;
            int colSpan = maxCol - minCol + 1;

            // Встановлюємо позицію та розмір selectionRectangle
            Grid.SetRow(selectionRectangle, minRow);
            Grid.SetColumn(selectionRectangle, minCol);
            Grid.SetRowSpan(selectionRectangle, rowSpan);
            Grid.SetColumnSpan(selectionRectangle, colSpan);
        }

        private void Cell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isSelecting && startCell != null && sender is Border endCell)
            {
                int startRow = Grid.GetRow(startCell);
                int startCol = Grid.GetColumn(startCell);
                int endRow = Grid.GetRow(endCell);
                int endCol = Grid.GetColumn(endCell);

                // Приховуємо selectionRectangle
                selectionRectangle.Visibility = Visibility.Collapsed;

                // Фіксуємо виділення
                DrawRectangle(startRow, startCol, endRow, endCol);

                // Перевірка завершення гри
                CheckCompletion();

                // Завершення виділення
                isSelecting = false;
                startCell = null;
            }
        }

        private void DrawRectangle(int startRow, int startCol, int endRow, int endCol)
        {
            int minRow = Math.Min(startRow, endRow);
            int maxRow = Math.Max(startRow, endRow);
            int minCol = Math.Min(startCol, endCol);
            int maxCol = Math.Max(startCol, endCol);

            int width = maxCol - minCol + 1;
            int height = maxRow - minRow + 1;
            int actualArea = width * height;

            // Дозволяємо створювати області будь-якого розміру, тому видаляємо перевірки на валідність площі

            // Перевірка на перекриття з існуючими прямокутниками
            List<KeyValuePair<Border, RectangleInfo>> overlappingRectangles = new List<KeyValuePair<Border, RectangleInfo>>();

            foreach (var rectEntry in rectangles)
            {
                var rect = rectEntry.Value;
                if (!(maxRow < rect.StartRow || minRow > rect.EndRow || maxCol < rect.StartCol || minCol > rect.EndCol))
                {
                    overlappingRectangles.Add(rectEntry);
                }
            }

            // Видалення перекритих прямокутників
            foreach (var rectEntry in overlappingRectangles)
            {
                // Очищення фону клітинок
                for (int row = rectEntry.Value.StartRow; row <= rectEntry.Value.EndRow; row++)
                {
                    for (int col = rectEntry.Value.StartCol; col <= rectEntry.Value.EndCol; col++)
                    {
                        var cellToClear = GetCell(row, col);
                        if (cellToClear != null)
                        {
                            cellToClear.Background = Brushes.White;
                            // Відновлення числа, якщо воно є
                            if (cellToClear.Child is TextBlock textBlock)
                            {
                                textBlock.Text = numbers[row, col] != 0 ? numbers[row, col].ToString() : "";
                            }
                        }
                    }
                }

                // Видалення постійного прямокутника
                RemovePermanentRectangle(rectEntry.Value);

                // Видалення з словника
                rectangles.Remove(rectEntry.Key);
            }

            // Фіксація нової області
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    var cell = GetCell(row, col);
                    if (cell != null)
                    {
                        cell.Background = Brushes.LightGreen;
                    }
                }
            }

            // Додавання постійного чорного периметра
            AddPermanentRectangle(minRow, minCol, width, height);

            // Додавання нової області до словника
            rectangles[startCell] = new RectangleInfo(minRow, minCol, width, height);
        }

        private void AddPermanentRectangle(int row, int col, int width, int height)
        {
            Rectangle rect = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 3, // Збільшено товщину для видимості
                Fill = Brushes.Transparent,
                IsHitTestVisible = false // Не перехоплює події миші
            };

            // Встановлюємо позицію та розмір прямокутника
            Grid.SetRow(rect, row);
            Grid.SetColumn(rect, col);
            Grid.SetRowSpan(rect, height);
            Grid.SetColumnSpan(rect, width);

            // Додаємо його до GameGrid
            GameGrid.Children.Add(rect);

            // Зберігаємо у список, щоб мати можливість очистити пізніше
            permanentRectangles.Add(rect);
        }

        private void RemovePermanentRectangle(RectangleInfo rectInfo)
        {
            Rectangle rectToRemove = null;

            foreach (var rect in permanentRectangles)
            {
                int row = Grid.GetRow(rect);
                int col = Grid.GetColumn(rect);
                int rowSpan = Grid.GetRowSpan(rect);
                int colSpan = Grid.GetColumnSpan(rect);

                if (row == rectInfo.StartRow && col == rectInfo.StartCol && rowSpan == rectInfo.Height && colSpan == rectInfo.Width)
                {
                    rectToRemove = rect;
                    break;
                }
            }

            if (rectToRemove != null)
            {
                GameGrid.Children.Remove(rectToRemove);
                permanentRectangles.Remove(rectToRemove);
            }
        }

        private void Cell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cell)
            {
                Border keyToRemove = null;
                RectangleInfo rectToRemoveInfo = null;

                // Знайти ключ, що відповідає прямокутнику, який містить цю клітинку
                foreach (var rectEntry in rectangles)
                {
                    var rect = rectEntry.Value;
                    if (IsCellInsideRectangle(cell, rect))
                    {
                        keyToRemove = rectEntry.Key;
                        rectToRemoveInfo = rect;
                        break;
                    }
                }

                if (keyToRemove != null && rectToRemoveInfo != null)
                {
                    // Видалити фон з клітинок
                    for (int row = rectToRemoveInfo.StartRow; row <= rectToRemoveInfo.EndRow; row++)
                    {
                        for (int col = rectToRemoveInfo.StartCol; col <= rectToRemoveInfo.EndCol; col++)
                        {
                            var cellToClear = GetCell(row, col);
                            if (cellToClear != null)
                            {
                                cellToClear.Background = Brushes.White;
                                // Відновлення числа, якщо воно є
                                if (cellToClear.Child is TextBlock textBlock)
                                {
                                    textBlock.Text = numbers[row, col] != 0 ? numbers[row, col].ToString() : "";
                                }
                            }
                        }
                    }

                    // Видалити постійний прямокутник
                    RemovePermanentRectangle(rectToRemoveInfo);

                    // Видалити з словника
                    rectangles.Remove(keyToRemove);
                }
            }
        }

        private bool IsCellInsideRectangle(Border cell, RectangleInfo rect)
        {
            int cellRow = Grid.GetRow(cell);
            int cellCol = Grid.GetColumn(cell);

            return cellRow >= rect.StartRow && cellRow <= rect.EndRow &&
                   cellCol >= rect.StartCol && cellCol <= rect.EndCol;
        }

        private void CheckCompletion()
        {
            int totalArea = gridSize * gridSize;
            int coveredArea = 0;

            foreach (var rect in rectangles.Values)
            {
                coveredArea += rect.Area;
            }

            if (coveredArea == totalArea)
            {
                MessageBox.Show("Вітаємо! Ви успішно завершили гру!", "Гра завершена", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Обробники подій для вибору розміру гри
        private void Game5x5_Click(object sender, RoutedEventArgs e)
        {
            gridSize = 5;
            maxRectangleArea = 5;

            // Очищаємо постійні прямокутники
            foreach (var rect in permanentRectangles)
            {
                GameGrid.Children.Remove(rect);
            }
            permanentRectangles.Clear();

            // Очищаємо списки прямокутників
            rectangles.Clear();
            rectangleList.Clear();

            SetWindowAndGridSize(gridSize + 2);
            CreateGrid();
            GenerateNewPuzzle();
        }

        private void Game7x7_Click(object sender, RoutedEventArgs e)
        {
            gridSize = 7;
            maxRectangleArea = 8;

            // Очищаємо постійні прямокутники
            foreach (var rect in permanentRectangles)
            {
                GameGrid.Children.Remove(rect);
            }
            permanentRectangles.Clear();

            // Очищаємо списки прямокутників
            rectangles.Clear();
            rectangleList.Clear();

            SetWindowAndGridSize(gridSize + 2);
            CreateGrid();
            GenerateNewPuzzle();
        }

        private void Game10x10_Click(object sender, RoutedEventArgs e)
        {
            gridSize = 10;
            maxRectangleArea = 16;

            // Очищаємо постійні прямокутники
            foreach (var rect in permanentRectangles)
            {
                GameGrid.Children.Remove(rect);
            }
            permanentRectangles.Clear();

            // Очищаємо списки прямокутників
            rectangles.Clear();
            rectangleList.Clear();

            SetWindowAndGridSize(gridSize + 2);
            CreateGrid();
            GenerateNewPuzzle();
        }

        private void Game15x15_Click(object sender, RoutedEventArgs e)
        {
            gridSize = 15;
            maxRectangleArea = 30;

            // Очищаємо постійні прямокутники
            foreach (var rect in permanentRectangles)
            {
                GameGrid.Children.Remove(rect);
            }
            permanentRectangles.Clear();

            // Очищаємо списки прямокутників
            rectangles.Clear();
            rectangleList.Clear();

            SetWindowAndGridSize(gridSize);
            CreateGrid();
            GenerateNewPuzzle();
        }
    }

    // Клас RectangleInfo
    public class RectangleInfo
    {
        public int StartRow { get; set; }
        public int StartCol { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Area => Width * Height;

        public int EndRow => StartRow + Height - 1;
        public int EndCol => StartCol + Width - 1;

        public RectangleInfo(int startRow, int startCol, int width, int height)
        {
            StartRow = startRow;
            StartCol = startCol;
            Width = width;
            Height = height;
        }
    }
}
