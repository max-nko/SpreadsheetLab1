using System;
using System.Numerics;
using SpreadsheetLab1.Domain;

namespace SpreadsheetLab1.ExpressionEngine
{
    public class Evaluator
    {
        private readonly Func<string, CellValue> _cellResolver;

        public Evaluator(Func<string, CellValue> cellResolver)
        {
            _cellResolver = cellResolver;
        }

        public CellValue Evaluate(AstNode node)
        {
            switch (node)
            {
                case NumberNode num:
                    return CellValue.FromInt(num.Value);

                case CellRefNode cellRef:
                    CellValue refVal = _cellResolver(cellRef.CellName);
                    if (refVal.Kind == ValueKind.Error)
                        throw new EvaluationException($"Помилка у {cellRef.CellName}: {refVal.ErrorMessage}");
                    return refVal;

                case UnaryNotNode unaryNot:
                    bool operandBool = Evaluate(unaryNot.Operand).AsBoolean();
                    return CellValue.FromBool(!operandBool);

                case UnaryFuncNode uFunc:
                    BigInteger arg = Evaluate(uFunc.Argument).AsInteger();
                    return uFunc.Func switch
                    {
                        TokenType.Inc => CellValue.FromInt(arg + BigInteger.One),
                        TokenType.Dec => CellValue.FromInt(arg - BigInteger.One),
                        _ => throw new EvaluationException("Невідома функція.")
                    };

                case BinaryFuncNode bFunc:
                    BigInteger a1 = Evaluate(bFunc.Arg1).AsInteger();
                    BigInteger a2 = Evaluate(bFunc.Arg2).AsInteger();
                    return bFunc.Func switch
                    {
                        TokenType.Max => CellValue.FromInt(BigInteger.Max(a1, a2)),
                        TokenType.Min => CellValue.FromInt(BigInteger.Min(a1, a2)),
                        _ => throw new EvaluationException("Невідома функція.")
                    };

                case BinaryOpNode binOp:
                    CellValue leftVal = Evaluate(binOp.Left);
                    CellValue rightVal = Evaluate(binOp.Right);

                    return binOp.Operator switch
                    {
                        TokenType.Plus => CellValue.FromInt(leftVal.AsInteger() + rightVal.AsInteger()),
                        TokenType.Minus => CellValue.FromInt(leftVal.AsInteger() - rightVal.AsInteger()),
                        TokenType.Multiply => CellValue.FromInt(leftVal.AsInteger() * rightVal.AsInteger()),
                        TokenType.Divide => rightVal.AsInteger() == BigInteger.Zero
                            ? throw new EvaluationException("Ділення на нуль!")
                            : CellValue.FromInt(leftVal.AsInteger() / rightVal.AsInteger()),

                        TokenType.Equal => CellValue.FromBool(CompareEqual(leftVal, rightVal)),
                        TokenType.Less => CellValue.FromBool(leftVal.AsInteger() < rightVal.AsInteger()),
                        TokenType.Greater => CellValue.FromBool(leftVal.AsInteger() > rightVal.AsInteger()),
                        _ => throw new EvaluationException("Непідтримувана дія.")
                    };

                default:
                    throw new EvaluationException("Невідомий вузол дерева.");
            }
        }

        private static bool CompareEqual(CellValue left, CellValue right)
        {
            if (left.Kind == ValueKind.Boolean && right.Kind == ValueKind.Boolean)
                return left.BoolValue == right.BoolValue;
            return left.AsInteger() == right.AsInteger();
        }
    }
}
