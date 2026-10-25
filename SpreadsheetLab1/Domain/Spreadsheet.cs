using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SpreadsheetLab1.ExpressionEngine;

namespace SpreadsheetLab1.Domain
{
    public class Cell
    {
        public string Expression { get; set; } = string.Empty;
        public CellValue Value { get; set; } = CellValue.Empty();
        public AstNode? CachedAst { get; set; }
        public HashSet<string> Dependencies { get; set; } = new();
    }

    public class SpreadsheetDataDto
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Dictionary<string, string> Cells { get; set; } = new();
    }

    public class Spreadsheet
    {
        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public bool ForceStrictLogicalResult { get; set; } = false;

        private readonly Dictionary<string, Cell> _cells = new();

        public Spreadsheet(int rows = 10, int columns = 8)
        {
            Rows = rows;
            Columns = columns;
        }

        public static string GetColumnName(int colIndex)
        {
            string name = string.Empty;
            int n = colIndex;
            while (n >= 0)
            {
                name = (char)('A' + (n % 26)) + name;
                n = (n / 26) - 1;
            }
            return name;
        }

        public static (int Row, int Col)? ParseCellCoordinates(string cellName)
        {
            int i = 0;
            int col = 0;
            while (i < cellName.Length && char.IsLetter(cellName[i]))
            {
                col = col * 26 + (char.ToUpperInvariant(cellName[i]) - 'A' + 1);
                i++;
            }
            col -= 1;

            if (!int.TryParse(cellName[i..], out int row))
                return null;

            return (row - 1, col);
        }

        public static string GetCellName(int row, int col) => $"{GetColumnName(col)}{row + 1}";

        public string GetExpression(int row, int col)
        {
            string name = GetCellName(row, col);
            return _cells.TryGetValue(name, out var cell) ? cell.Expression : string.Empty;
        }

        public string GetDisplayText(int row, int col, bool showExpressions)
        {
            string name = GetCellName(row, col);
            if (!_cells.TryGetValue(name, out var cell) || string.IsNullOrWhiteSpace(cell.Expression))
                return string.Empty;

            if (showExpressions)
                return cell.Expression;

            return cell.Value.ToDisplayString(ForceStrictLogicalResult);
        }

        public bool IsErrorCell(int row, int col)
        {
            string name = GetCellName(row, col);
            return _cells.TryGetValue(name, out var cell) && cell.Value.Kind == ValueKind.Error;
        }

        public void SetCellExpression(int row, int col, string expression)
        {
            string name = GetCellName(row, col);
            string trimmed = expression?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(trimmed))
            {
                _cells.Remove(name);
                RecalculateAll();
                return;
            }

            if (trimmed.StartsWith('='))
                trimmed = trimmed[1..].Trim();

            if (!_cells.TryGetValue(name, out var cell))
            {
                cell = new Cell();
                _cells[name] = cell;
            }

            cell.Expression = trimmed;
            RecalculateAll();
        }

        public void Resize(int newRows, int newCols)
        {
            if (newRows < 1 || newCols < 1) return;
            Rows = newRows;
            Columns = newCols;
            RecalculateAll();
        }

        public void AddRow() => Resize(Rows + 1, Columns);
        public void RemoveRow() { if (Rows > 1) Resize(Rows - 1, Columns); }
        public void AddColumn() => Resize(Rows, Columns + 1);
        public void RemoveColumn() { if (Columns > 1) Resize(Rows, Columns - 1); }

        public void Clear() => _cells.Clear();

        public void RecalculateAll()
        {
            foreach (var (name, cell) in _cells)
            {
                cell.Dependencies.Clear();
                cell.CachedAst = null;

                try
                {
                    var lexer = new Lexer(cell.Expression);
                    var tokens = lexer.Tokenize();
                    var parser = new Parser(tokens);
                    cell.CachedAst = parser.ParseExpression();
                    cell.Dependencies = parser.ReferencedCells;
                }
                catch (SyntaxErrorException synEx)
                {
                    cell.Value = CellValue.FromError($"#СИНТАКСИС {synEx.Message}");
                }
                catch (Exception ex)
                {
                    cell.Value = CellValue.FromError($"#ПОМИЛКА: {ex.Message}");
                }
            }

            var state = new Dictionary<string, int>();

            CellValue EvaluateCellByName(string cellName)
            {
                var coords = ParseCellCoordinates(cellName);
                if (coords == null || coords.Value.Row < 0 || coords.Value.Row >= Rows || coords.Value.Col < 0 || coords.Value.Col >= Columns)
                    throw new EvaluationException($"Клітинка {cellName} за межами таблиці!");

                if (!_cells.TryGetValue(cellName, out var targetCell) || string.IsNullOrWhiteSpace(targetCell.Expression))
                    return CellValue.Empty();

                int currentState = state.GetValueOrDefault(cellName, 0);
                if (currentState == 1)
                    throw new EvaluationException($"Циклічне посилання за участю {cellName}!");
                if (currentState == 2)
                    return targetCell.Value;

                if (targetCell.CachedAst == null)
                {
                    state[cellName] = 2;
                    return targetCell.Value;
                }

                state[cellName] = 1;
                try
                {
                    var evaluator = new Evaluator(EvaluateCellByName);
                    targetCell.Value = evaluator.Evaluate(targetCell.CachedAst);
                }
                catch (EvaluationException evalEx)
                {
                    targetCell.Value = CellValue.FromError($"#ОБЧИСЛЕННЯ: {evalEx.Message}");
                }
                finally
                {
                    state[cellName] = 2;
                }

                return targetCell.Value;
            }

            foreach (var name in _cells.Keys)
            {
                if (state.GetValueOrDefault(name, 0) == 0)
                {
                    try
                    {
                        EvaluateCellByName(name);
                    }
                    catch (EvaluationException ex)
                    {
                        _cells[name].Value = CellValue.FromError($"#ЦИКЛ: {ex.Message}");
                        state[name] = 2;
                    }
                }
            }
        }

        public string SerializeToJson()
        {
            var dto = new SpreadsheetDataDto { Rows = Rows, Columns = Columns };
            foreach (var (k, v) in _cells)
            {
                if (!string.IsNullOrWhiteSpace(v.Expression))
                    dto.Cells[k] = v.Expression;
            }
            return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        }

        public void LoadFromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<SpreadsheetDataDto>(json)
                      ?? throw new InvalidDataException("Неможливо прочитати файл.");

            Rows = Math.Max(1, dto.Rows);
            Columns = Math.Max(1, dto.Columns);
            _cells.Clear();

            foreach (var (k, expr) in dto.Cells)
                _cells[k] = new Cell { Expression = expr };

            RecalculateAll();
        }
    }
}
