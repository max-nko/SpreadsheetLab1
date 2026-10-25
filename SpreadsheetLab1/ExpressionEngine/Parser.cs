using System.Collections.Generic;
using System.Numerics;

namespace SpreadsheetLab1.ExpressionEngine
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current;
        public HashSet<string> ReferencedCells { get; } = new();

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _current = 0;
        }

        private Token Peek() => _tokens[_current];
        private Token Advance() => _tokens[_current++];

        private Token Consume(TokenType expected, string errorMessage)
        {
            if (Peek().Type == expected)
                return Advance();
            throw new SyntaxErrorException(errorMessage, Peek().Position);
        }

        public AstNode ParseExpression()
        {
            if (Peek().Type == TokenType.EOF)
                throw new SyntaxErrorException("Порожній вираз.", 0);

            AstNode root = ParseComparison();

            if (Peek().Type != TokenType.EOF)
                throw new SyntaxErrorException($"Неочікуваний символ '{Peek().Lexeme}' у кінці виразу.", Peek().Position);

            return root;
        }

        private AstNode ParseComparison()
        {
            AstNode left = ParseAdditive();

            while (Peek().Type is TokenType.Equal or TokenType.Less or TokenType.Greater)
            {
                Token op = Advance();
                AstNode right = ParseAdditive();
                left = new BinaryOpNode(left, op.Type, right, op.Position);
            }

            return left;
        }

        private AstNode ParseAdditive()
        {
            AstNode left = ParseMultiplicative();

            while (Peek().Type is TokenType.Plus or TokenType.Minus)
            {
                Token op = Advance();
                AstNode right = ParseMultiplicative();
                left = new BinaryOpNode(left, op.Type, right, op.Position);
            }

            return left;
        }

        private AstNode ParseMultiplicative()
        {
            AstNode left = ParseUnary();

            while (Peek().Type is TokenType.Multiply or TokenType.Divide)
            {
                Token op = Advance();
                AstNode right = ParseUnary();
                left = new BinaryOpNode(left, op.Type, right, op.Position);
            }

            return left;
        }

        private AstNode ParseUnary()
        {
            if (Peek().Type == TokenType.Not)
            {
                Token op = Advance();
                AstNode operand = ParseUnary();
                return new UnaryNotNode(operand, op.Position);
            }

            if (Peek().Type is TokenType.Plus or TokenType.Minus)
            {
                throw new SyntaxErrorException(
                    $"Унарний '{Peek().Lexeme}' не входить до варіанта 50.",
                    Peek().Position);
            }

            return ParsePrimary();
        }

        private AstNode ParsePrimary()
        {
            Token token = Peek();

            switch (token.Type)
            {
                case TokenType.Number:
                    Advance();
                    return new NumberNode(BigInteger.Parse(token.Lexeme), token.Position);

                case TokenType.CellRef:
                    Advance();
                    ReferencedCells.Add(token.Lexeme);
                    return new CellRefNode(token.Lexeme, token.Position);

                case TokenType.LParen:
                    Advance();
                    AstNode inside = ParseComparison();
                    Consume(TokenType.RParen, "Пропущено закриваючу дужку ')'.");
                    return inside;

                case TokenType.Inc or TokenType.Dec:
                    Advance();
                    Consume(TokenType.LParen, $"Після '{token.Lexeme}' має бути '('");
                    AstNode arg = ParseComparison();
                    Consume(TokenType.RParen, $"Пропущено ')' після аргументу '{token.Lexeme}'");
                    return new UnaryFuncNode(token.Type, arg, token.Position);

                case TokenType.Max or TokenType.Min:
                    Advance();
                    Consume(TokenType.LParen, $"Після '{token.Lexeme}' має бути '('");
                    AstNode arg1 = ParseComparison();
                    Consume(TokenType.Comma, $"Для '{token.Lexeme}' потрібно два аргументи через кому ','");
                    AstNode arg2 = ParseComparison();
                    Consume(TokenType.RParen, $"Пропущено ')' після другого аргументу '{token.Lexeme}'");
                    return new BinaryFuncNode(token.Type, arg1, arg2, token.Position);

                default:
                    throw new SyntaxErrorException(
                        token.Type == TokenType.EOF
                            ? "Неочікуваний кінець виразу."
                            : $"Неочікуваний символ '{token.Lexeme}'.",
                        token.Position);
            }
        }
    }
}
