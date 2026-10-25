using System.Numerics;

namespace SpreadsheetLab1.Domain
{
    public enum ValueKind
    {
        Empty,
        Integer,
        Boolean,
        Error
    }

    public readonly struct CellValue
    {
        public ValueKind Kind { get; }
        public BigInteger IntValue { get; }
        public bool BoolValue { get; }
        public string ErrorMessage { get; }

        private CellValue(ValueKind kind, BigInteger intVal, bool boolVal, string error)
        {
            Kind = kind;
            IntValue = intVal;
            BoolValue = boolVal;
            ErrorMessage = error;
        }

        public static CellValue Empty() => new(ValueKind.Empty, BigInteger.Zero, false, string.Empty);
        public static CellValue FromInt(BigInteger val) => new(ValueKind.Integer, val, val != BigInteger.Zero, string.Empty);
        public static CellValue FromBool(bool val) => new(ValueKind.Boolean, val ? BigInteger.One : BigInteger.Zero, val, string.Empty);
        public static CellValue FromError(string msg) => new(ValueKind.Error, BigInteger.Zero, false, msg);

        public BigInteger AsInteger() => Kind switch
        {
            ValueKind.Integer => IntValue,
            ValueKind.Boolean => BoolValue ? BigInteger.One : BigInteger.Zero,
            ValueKind.Empty => BigInteger.Zero,
            _ => throw new ExpressionEngine.EvaluationException(ErrorMessage)
        };

        public bool AsBoolean() => Kind switch
        {
            ValueKind.Boolean => BoolValue,
            ValueKind.Integer => IntValue != BigInteger.Zero,
            ValueKind.Empty => false,
            _ => throw new ExpressionEngine.EvaluationException(ErrorMessage)
        };

        public string ToDisplayString(bool forceLogicalView = false)
        {
            if (Kind == ValueKind.Empty) return string.Empty;
            if (Kind == ValueKind.Error) return ErrorMessage;

            if (Kind == ValueKind.Boolean || forceLogicalView)
                return AsBoolean() ? "TRUE (1)" : "FALSE (0)";

            return IntValue.ToString();
        }
    }
}
