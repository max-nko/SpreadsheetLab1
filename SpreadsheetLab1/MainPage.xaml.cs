using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SpreadsheetLab1.Domain;

namespace SpreadsheetLab1
{
    public partial class MainPage : ContentPage
    {
        private readonly Spreadsheet _spreadsheet = new(rows: 8, columns: 6);
        private readonly Dictionary<(int r, int c), Entry> _cellEntries = new();

        private int _selectedRow = 0;
        private int _selectedCol = 0;
        private bool _showExpressionsMode = false;
        private bool _isUpdatingUi = false;

        public MainPage()
        {
            InitializeComponent();
            RebuildGridStructure();
            SelectCell(0, 0);
        }

        private void RebuildGridStructure()
        {
            _isUpdatingUi = true;
            SpreadsheetGrid.Children.Clear();
            SpreadsheetGrid.RowDefinitions.Clear();
            SpreadsheetGrid.ColumnDefinitions.Clear();
            _cellEntries.Clear();

            SpreadsheetGrid.RowDefinitions.Add(new RowDefinition { Height = 36 });
            SpreadsheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 55 });

            var cornerBox = new Label
            {
                Text = "№",
                BackgroundColor = Color.FromArgb("#E2E8F0"),
                TextColor = Color.FromArgb("#334155"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold
            };
            SpreadsheetGrid.Add(cornerBox, 0, 0);

            for (int c = 0; c < _spreadsheet.Columns; c++)
            {
                SpreadsheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 145 });
                var colHeader = new Label
                {
                    Text = Spreadsheet.GetColumnName(c),
                    BackgroundColor = Color.FromArgb("#E2E8F0"),
                    TextColor = Color.FromArgb("#1E293B"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontAttributes = FontAttributes.Bold
                };
                SpreadsheetGrid.Add(colHeader, c + 1, 0);
            }

            for (int r = 0; r < _spreadsheet.Rows; r++)
            {
                SpreadsheetGrid.RowDefinitions.Add(new RowDefinition { Height = 38 });

                var rowHeader = new Label
                {
                    Text = (r + 1).ToString(),
                    BackgroundColor = Color.FromArgb("#E2E8F0"),
                    TextColor = Color.FromArgb("#1E293B"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontAttributes = FontAttributes.Bold
                };
                SpreadsheetGrid.Add(rowHeader, 0, r + 1);

                for (int c = 0; c < _spreadsheet.Columns; c++)
                {
                    int capturedRow = r;
                    int capturedCol = c;

                    var entry = new Entry
                    {
                        BackgroundColor = Colors.White,
                        TextColor = Colors.Black,
                        FontSize = 14
                    };

                    entry.Focused += (_, _) => OnCellFocused(capturedRow, capturedCol);
                    entry.Unfocused += (_, _) => OnCellUnfocused(capturedRow, capturedCol, entry.Text);
                    entry.Completed += (_, _) => OnCellCompleted(capturedRow, capturedCol, entry.Text);

                    _cellEntries[(r, c)] = entry;
                    SpreadsheetGrid.Add(entry, c + 1, r + 1);
                }
            }

            _isUpdatingUi = false;
            RefreshCellsDisplay();
        }

        private void RefreshCellsDisplay()
        {
            _isUpdatingUi = true;
            foreach (var ((r, c), entry) in _cellEntries)
            {
                entry.Text = _spreadsheet.GetDisplayText(r, c, _showExpressionsMode);

                bool isSelected = (r == _selectedRow && c == _selectedCol);
                bool isError = _spreadsheet.IsErrorCell(r, c);

                if (isError && !_showExpressionsMode)
                {
                    entry.BackgroundColor = Color.FromArgb("#FEE2E2");
                    entry.TextColor = Color.FromArgb("#B91C1C");
                }
                else if (isSelected)
                {
                    entry.BackgroundColor = Color.FromArgb("#DBEAFE");
                    entry.TextColor = Colors.Black;
                }
                else
                {
                    entry.BackgroundColor = Colors.White;
                    entry.TextColor = Colors.Black;
                }
            }

            string currentCellName = Spreadsheet.GetCellName(_selectedRow, _selectedCol);
            SelectedCellLabel.Text = currentCellName;
            FormulaEntry.Text = _spreadsheet.GetExpression(_selectedRow, _selectedCol);

            string valText = _spreadsheet.GetDisplayText(_selectedRow, _selectedCol, false);
            string exprText = _spreadsheet.GetExpression(_selectedRow, _selectedCol);
            StatusLabel.Text = string.IsNullOrEmpty(exprText)
                ? $"Клітинка {currentCellName}: порожня."
                : $"Клітинка {currentCellName} | Вираз: {exprText} | Значення: {valText}";

            _isUpdatingUi = false;
        }

        private void SelectCell(int row, int col)
        {
            _selectedRow = Math.Clamp(row, 0, _spreadsheet.Rows - 1);
            _selectedCol = Math.Clamp(col, 0, _spreadsheet.Columns - 1);
            RefreshCellsDisplay();
        }

        private void OnCellFocused(int row, int col)
        {
            if (_isUpdatingUi) return;
            _selectedRow = row;
            _selectedCol = col;

            if (_cellEntries.TryGetValue((row, col), out var entry))
            {
                _isUpdatingUi = true;
                entry.Text = _spreadsheet.GetExpression(row, col);
                _isUpdatingUi = false;
            }
            SelectedCellLabel.Text = Spreadsheet.GetCellName(row, col);
            FormulaEntry.Text = _spreadsheet.GetExpression(row, col);
        }

        private void OnCellUnfocused(int row, int col, string? newText)
        {
            if (_isUpdatingUi) return;
            CommitCellChange(row, col, newText ?? string.Empty);
        }

        private void OnCellCompleted(int row, int col, string? newText)
        {
            if (_isUpdatingUi) return;
            CommitCellChange(row, col, newText ?? string.Empty);
        }

        private void OnApplyFormulaClicked(object sender, EventArgs e)
        {
            CommitCellChange(_selectedRow, _selectedCol, FormulaEntry.Text ?? string.Empty);
        }

        private void OnFormulaCompleted(object sender, EventArgs e)
        {
            CommitCellChange(_selectedRow, _selectedCol, FormulaEntry.Text ?? string.Empty);
        }

        private void CommitCellChange(int row, int col, string newExpression)
        {
            _spreadsheet.SetCellExpression(row, col, newExpression);
            RefreshCellsDisplay();
        }

        private void OnToggleModeClicked(object sender, EventArgs e)
        {
            _showExpressionsMode = !_showExpressionsMode;
            ModeToggleButton.Text = _showExpressionsMode ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
            ModeToggleButton.BackgroundColor = _showExpressionsMode
                ? Color.FromArgb("#4F46E5")
                : Color.FromArgb("#059669");
            RefreshCellsDisplay();
        }

        private void OnStrictBoolChanged(object sender, CheckedChangedEventArgs e)
        {
            _spreadsheet.ForceStrictLogicalResult = e.Value;
            RefreshCellsDisplay();
        }

        private void OnAddRowClicked(object sender, EventArgs e)
        {
            _spreadsheet.AddRow();
            RebuildGridStructure();
        }

        private async void OnRemoveRowClicked(object sender, EventArgs e)
        {
            if (_spreadsheet.Rows <= 1)
            {
                await DisplayAlert("Увага", "Має бути щонайменше 1 рядок.", "ОК");
                return;
            }
            _spreadsheet.RemoveRow();
            SelectCell(Math.Min(_selectedRow, _spreadsheet.Rows - 1), _selectedCol);
            RebuildGridStructure();
        }

        private void OnAddColClicked(object sender, EventArgs e)
        {
            _spreadsheet.AddColumn();
            RebuildGridStructure();
        }

        private async void OnRemoveColClicked(object sender, EventArgs e)
        {
            if (_spreadsheet.Columns <= 1)
            {
                await DisplayAlert("Увага", "Має бути щонайменше 1 стовпчик.", "ОК");
                return;
            }
            _spreadsheet.RemoveColumn();
            SelectCell(_selectedRow, Math.Min(_selectedCol, _spreadsheet.Columns - 1));
            RebuildGridStructure();
        }

        private async void OnClearClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Очищення", "Очистити таблицю?", "Так", "Ні");
            if (!confirm) return;

            _spreadsheet.Clear();
            RefreshCellsDisplay();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "table_v50.json");
                string? input = await DisplayPromptAsync("Збереження", "Вкажіть шлях для збереження:", initialValue: path, accept: "Зберегти", cancel: "Скасувати");
                if (string.IsNullOrWhiteSpace(input)) return;

                File.WriteAllText(input, _spreadsheet.SerializeToJson());
                await DisplayAlert("Успіх", $"Файл збережено в:\n{input}", "ОК");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", ex.Message, "ОК");
            }
        }

        private async void OnOpenClicked(object sender, EventArgs e)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "table_v50.json");
                string? input = await DisplayPromptAsync("Відкриття", "Вкажіть шлях до файлу:", initialValue: path, accept: "Відкрити", cancel: "Скасувати");
                if (string.IsNullOrWhiteSpace(input)) return;

                if (!File.Exists(input))
                {
                    await DisplayAlert("Помилка", "Файл не знайдено.", "ОК");
                    return;
                }

                _spreadsheet.LoadFromJson(File.ReadAllText(input));
                RebuildGridStructure();
                SelectCell(0, 0);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", ex.Message, "ОК");
            }
        }

        private void OnLoadDemoClicked(object sender, EventArgs e)
        {
            _spreadsheet.Clear();
            _spreadsheet.SetCellExpression(0, 0, "15");
            _spreadsheet.SetCellExpression(1, 0, "25");
            _spreadsheet.SetCellExpression(2, 0, "inc(A1) + dec(A2)");
            _spreadsheet.SetCellExpression(3, 0, "max(A1, A2) * min(A1, 10)");
            _spreadsheet.SetCellExpression(0, 1, "A3 > A1");
            _spreadsheet.SetCellExpression(1, 1, "not (A1 = A2)");
            _spreadsheet.SetCellExpression(2, 1, "not (max(A1, A2) > 20)");
            _spreadsheet.SetCellExpression(3, 1, "999999999999999999999999999999 + 1");
            _spreadsheet.SetCellExpression(4, 1, "max(10, )");

            RefreshCellsDisplay();
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            string msg =
                "Лабораторна робота №1: Електронні таблиці.\n" +
                "Варіант: 50 (Операції: 1, 5, 6, 8, 10)\n\n" +
                "Операції:\n" +
                "• Бінарні: +, -, *, /\n" +
                "• Функції: inc(x), dec(x), max(x, y), min(x, y)\n" +
                "• Порівняння: =, <, >\n" +
                "• Логічне заперечення: not x\n\n" +
                "Двоетапне виконання:\n" +
                "1. Синтаксична перевірка і локалізація помилок (Lexer + Parser -> AST).\n" +
                "2. Обчислення дерева та детекція циклів (Evaluator).\n\n" +
                "Підтримується режим показу значень і формул.";

            await DisplayAlert("Довідка (Варіант 50)", msg, "ОК");
        }
    }
}
