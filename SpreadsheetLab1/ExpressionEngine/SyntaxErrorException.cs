using System;

namespace SpreadsheetLab1.ExpressionEngine
{
    public class SyntaxErrorException : Exception
    {
        public int Position { get; }

        public SyntaxErrorException(string message, int position)
            : base($"[Позиція {position + 1}]: {message}")
        {
            Position = position;
        }
    }

    public class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) { }
    }
}
