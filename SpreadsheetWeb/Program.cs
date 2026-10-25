using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using SpreadsheetLab1.Domain;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var spreadsheet = new Spreadsheet(rows: 8, columns: 6);

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(GetHtml());
});

app.MapGet("/api/state", () =>
{
    var cellsData = new Dictionary<string, object>();
    for (int r = 0; r < spreadsheet.Rows; r++)
    {
        for (int c = 0; c < spreadsheet.Columns; c++)
        {
            string name = Spreadsheet.GetCellName(r, c);
            cellsData[name] = new
            {
                expr = spreadsheet.GetExpression(r, c),
                val = spreadsheet.GetDisplayText(r, c, false),
                isError = spreadsheet.IsErrorCell(r, c)
            };
        }
    }

    return Results.Ok(new
    {
        rows = spreadsheet.Rows,
        cols = spreadsheet.Columns,
        forceLogical = spreadsheet.ForceStrictLogicalResult,
        cells = cellsData
    });
});

app.MapPost("/api/cell", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);
    int row = doc.RootElement.GetProperty("row").GetInt32();
    int col = doc.RootElement.GetProperty("col").GetInt32();
    string expr = doc.RootElement.GetProperty("expression").GetString() ?? string.Empty;

    spreadsheet.SetCellExpression(row, col, expr);
    return Results.Ok();
});

app.MapPost("/api/resize", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var doc = JsonDocument.Parse(body);
    string action = doc.RootElement.GetProperty("action").GetString() ?? string.Empty;

    if (action == "addRow") spreadsheet.AddRow();
    else if (action == "remRow") spreadsheet.RemoveRow();
    else if (action == "addCol") spreadsheet.AddColumn();
    else if (action == "remCol") spreadsheet.RemoveColumn();

    return Results.Ok();
});

app.MapPost("/api/toggle-logical", () =>
{
    spreadsheet.ForceStrictLogicalResult = !spreadsheet.ForceStrictLogicalResult;
    return Results.Ok(spreadsheet.ForceStrictLogicalResult);
});

app.MapPost("/api/clear", () =>
{
    spreadsheet.Clear();
    return Results.Ok();
});

app.MapPost("/api/demo", () =>
{
    spreadsheet.Clear();
    spreadsheet.SetCellExpression(0, 0, "15");
    spreadsheet.SetCellExpression(1, 0, "25");
    spreadsheet.SetCellExpression(2, 0, "inc(A1) + dec(A2)");
    spreadsheet.SetCellExpression(3, 0, "max(A1, A2) * min(A1, 10)");
    spreadsheet.SetCellExpression(0, 1, "A3 > A1");
    spreadsheet.SetCellExpression(1, 1, "not (A1 = A2)");
    spreadsheet.SetCellExpression(2, 1, "not (max(A1, A2) > 20)");
    spreadsheet.SetCellExpression(3, 1, "999999999999999999999999999999 + 1");
    spreadsheet.SetCellExpression(4, 1, "max(10, )");
    return Results.Ok();
});

app.Run();

static string GetHtml() => """
<!DOCTYPE html>
<html lang="uk">
<head>
    <meta charset="UTF-8">
    <title>Лабораторна робота №1 — Варіант 50</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; margin: 15px; background: #f8fafc; }
        .toolbar { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 12px; align-items: center; }
        button { padding: 6px 12px; border: 1px solid #cbd5e1; border-radius: 6px; background: #fff; cursor: pointer; font-size: 14px; }
        button:hover { background: #f1f5f9; }
        .btn-primary { background: #2563eb; color: #fff; border: none; }
        .btn-primary:hover { background: #1d4ed8; }
        .btn-green { background: #059669; color: #fff; border: none; }
        .btn-green:hover { background: #047857; }
        .btn-demo { background: #d97706; color: #fff; border: none; }
        .formula-bar { display: flex; gap: 8px; margin-bottom: 12px; align-items: center; }
        .cell-id { padding: 6px 12px; background: #e2e8f0; font-weight: bold; border-radius: 6px; }
        .formula-input { flex: 1; padding: 6px 10px; font-size: 15px; border: 1px solid #cbd5e1; border-radius: 6px; }
        .table-wrap { overflow: auto; max-height: 70vh; border: 1px solid #cbd5e1; background: #fff; border-radius: 6px; }
        table { border-collapse: collapse; }
        th, td { border: 1px solid #e2e8f0; padding: 0; min-width: 120px; height: 32px; text-align: center; }
        th { background: #f1f5f9; font-size: 13px; color: #475569; position: sticky; top: 0; z-index: 1; }
        th.row-header { width: 45px; min-width: 45px; position: sticky; left: 0; z-index: 2; background: #f1f5f9; }
        input.cell-input { width: 100%; height: 100%; border: none; text-align: center; font-size: 14px; outline: none; box-sizing: border-box; }
        input.cell-input:focus { background: #dbeafe !important; }
        .error-cell { background: #fee2e2 !important; color: #b91c1c !important; }
        .status-bar { margin-top: 10px; padding: 8px 12px; background: #fff; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 13px; }
    </style>
</head>
<body>
    <h2>Електронні таблиці — Варіант 50</h2>
    <div class="toolbar">
        <button id="modeBtn" class="btn-green" onclick="toggleMode()">Режим: ЗНАЧЕННЯ</button>
        <button onclick="toggleStrictBool()">Усі результати як TRUE/FALSE</button>
        <button onclick="resizeTable('addRow')">➕ Рядок</button>
        <button onclick="resizeTable('remRow')">➖ Рядок</button>
        <button onclick="resizeTable('addCol')">➕ Стовпчик</button>
        <button onclick="resizeTable('remCol')">➖ Стовпчик</button>
        <button class="btn-demo" onclick="loadDemo()">🧪 Демо Варіант 50</button>
        <button onclick="clearTable()">🧹 Очистити</button>
        <button onclick="showHelp()">❓ Допомога</button>
    </div>

    <div class="formula-bar">
        <div class="cell-id" id="activeCellLabel">A1</div>
        <input class="formula-input" id="formulaInput" placeholder="Введіть формулу (наприклад, max(10, inc(5)) > 12 або not(A1 = B1))..." onkeydown="if(event.key==='Enter') applyFormula()">
        <button class="btn-primary" onclick="applyFormula()">✔ Застосувати</button>
    </div>

    <div class="table-wrap">
        <table id="sheetTable"></table>
    </div>

    <div class="status-bar" id="statusBar">Готово до роботи.</div>

    <script>
        let showExpressions = false;
        let selectedRow = 0;
        let selectedCol = 0;
        let sheetData = null;

        function getColName(n) {
            let s = "";
            while (n >= 0) {
                s = String.fromCharCode(65 + (n % 26)) + s;
                n = Math.floor(n / 26) - 1;
            }
            return s;
        }

        async function fetchState() {
            const res = await fetch('/api/state');
            sheetData = await res.json();
            renderGrid();
        }

        function renderGrid() {
            const table = document.getElementById('sheetTable');
            table.innerHTML = "";

            let headerTr = document.createElement('tr');
            let cornerTh = document.createElement('th');
            cornerTh.className = "row-header";
            cornerTh.innerText = "№";
            headerTr.appendChild(cornerTh);

            for (let c = 0; c < sheetData.cols; c++) {
                let th = document.createElement('th');
                th.innerText = getColName(c);
                headerTr.appendChild(th);
            }
            table.appendChild(headerTr);

            for (let r = 0; r < sheetData.rows; r++) {
                let tr = document.createElement('tr');
                let rowTh = document.createElement('th');
                rowTh.className = "row-header";
                rowTh.innerText = (r + 1);
                tr.appendChild(rowTh);

                for (let c = 0; c < sheetData.cols; c++) {
                    let td = document.createElement('td');
                    let cellName = getColName(c) + (r + 1);
                    let cellInfo = sheetData.cells[cellName] || { expr: "", val: "", isError: false };

                    let input = document.createElement('input');
                    input.className = "cell-input" + (cellInfo.isError && !showExpressions ? " error-cell" : "");
                    input.value = showExpressions ? cellInfo.expr : cellInfo.val;

                    input.onfocus = () => {
                        selectedRow = r;
                        selectedCol = c;
                        document.getElementById('activeCellLabel').innerText = cellName;
                        document.getElementById('formulaInput').value = cellInfo.expr;
                        document.getElementById('statusBar').innerText = `Клітинка ${cellName} | Вираз: ${cellInfo.expr} | Значення: ${cellInfo.val}`;
                        input.value = cellInfo.expr;
                    };

                    input.onblur = async () => {
                        if (input.value !== cellInfo.expr) {
                            await sendCell(r, c, input.value);
                        } else {
                            input.value = showExpressions ? cellInfo.expr : cellInfo.val;
                        }
                    };

                    input.onkeydown = async (e) => {
                        if (e.key === 'Enter') {
                            await sendCell(r, c, input.value);
                            input.blur();
                        }
                    };

                    td.appendChild(input);
                    tr.appendChild(td);
                }
                table.appendChild(tr);
            }
        }

        async function sendCell(row, col, expr) {
            await fetch('/api/cell', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ row, col, expression: expr })
            });
            await fetchState();
        }

        async function applyFormula() {
            const expr = document.getElementById('formulaInput').value;
            await sendCell(selectedRow, selectedCol, expr);
        }

        function toggleMode() {
            showExpressions = !showExpressions;
            const btn = document.getElementById('modeBtn');
            btn.innerText = showExpressions ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
            btn.className = showExpressions ? "btn-primary" : "btn-green";
            renderGrid();
        }

        async function toggleStrictBool() {
            await fetch('/api/toggle-logical', { method: 'POST' });
            await fetchState();
        }

        async function resizeTable(action) {
            await fetch('/api/resize', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ action })
            });
            await fetchState();
        }

        async function clearTable() {
            if (confirm("Очистити таблицю?")) {
                await fetch('/api/clear', { method: 'POST' });
                await fetchState();
            }
        }

        async function loadDemo() {
            await fetch('/api/demo', { method: 'POST' });
            await fetchState();
        }

        function showHelp() {
            alert("Лабораторна робота №1: Електронні таблиці\\nВаріант 50 (Операції: 1, 5, 6, 8, 10)\\n\\nОперації:\\n• +, -, *, /\\n• inc(x), dec(x)\\n• max(x, y), min(x, y)\\n• =, <, >\\n• not x\\n\\nДвоетапна модель: синтаксичний аналіз (AST) та обчислення з детекцією циклів.");
        }

        fetchState();
    </script>
</body>
</html>
""";
