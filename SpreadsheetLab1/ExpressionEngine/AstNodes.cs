using System.Numerics;

namespace SpreadsheetLab1.ExpressionEngine
{
    public abstract record AstNode(int Position);

    public record NumberNode(BigInteger Value, int Position) : AstNode(Position);
    public record CellRefNode(string CellName, int Position) : AstNode(Position);
    public record UnaryNotNode(AstNode Operand, int Position) : AstNode(Position);
    public record BinaryOpNode(AstNode Left, TokenType Operator, AstNode Right, int Position) : AstNode(Position);
    public record UnaryFuncNode(TokenType Func, AstNode Argument, int Position) : AstNode(Position);
    public record BinaryFuncNode(TokenType Func, AstNode Arg1, AstNode Arg2, int Position) : AstNode(Position);
}
