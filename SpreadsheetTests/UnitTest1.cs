using Xunit;
using SpreadsheetLab1.Domain;

namespace SpreadsheetTests
{
    public class SpreadsheetLogicTests
    {
        [Fact]
        public void Test_Variant50_ArithmeticAndFunctions()
        {
            var sheet = new Spreadsheet(5, 5);

            sheet.SetCellExpression(0, 0, "15");
            sheet.SetCellExpression(1, 0, "25");
            sheet.SetCellExpression(2, 0, "inc(A1) + dec(A2)");
            sheet.SetCellExpression(3, 0, "max(A1, A2) * min(A1, 10)");

            Assert.Equal("15", sheet.GetDisplayText(0, 0, false));
            Assert.Equal("25", sheet.GetDisplayText(1, 0, false));
            Assert.Equal("40", sheet.GetDisplayText(2, 0, false));
            Assert.Equal("250", sheet.GetDisplayText(3, 0, false));
        }

        [Fact]
        public void Test_Variant50_LogicalOperationsAndBigInteger()
        {
            var sheet = new Spreadsheet(5, 5);

            sheet.SetCellExpression(0, 0, "10");
            sheet.SetCellExpression(1, 0, "20");
            sheet.SetCellExpression(0, 1, "A2 > A1");
            sheet.SetCellExpression(1, 1, "not (A1 = A2)");
            sheet.SetCellExpression(2, 1, "999999999999999999999999999999 + 1");

            Assert.Equal("TRUE (1)", sheet.GetDisplayText(0, 1, false));
            Assert.Equal("TRUE (1)", sheet.GetDisplayText(1, 1, false));
            Assert.Equal("1000000000000000000000000000000", sheet.GetDisplayText(2, 1, false));
        }

        [Fact]
        public void Test_Variant50_ErrorLocalizationAndCycleDetection()
        {
            var sheet = new Spreadsheet(5, 5);

            sheet.SetCellExpression(0, 0, "max(10, )");
            Assert.True(sheet.IsErrorCell(0, 0));
            Assert.Contains("СИНТАКСИС", sheet.GetDisplayText(0, 0, false));

            sheet.SetCellExpression(1, 0, "B2 + 1");
            sheet.SetCellExpression(1, 1, "A2 + 1");
            Assert.True(sheet.IsErrorCell(1, 0));
            string errorText = sheet.GetDisplayText(1, 0, false);
            Assert.True(errorText.Contains("Цикл", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
