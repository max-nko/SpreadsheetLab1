using System;
using System.Collections.Generic;

namespace SpreadsheetLab1.ExpressionEngine
{
    public class Lexer
    {
        private readonly string _text;
        private int _pos;

        public Lexer(string text)
        {
            _text = text ?? string.Empty;
            _pos = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (_pos < _text.Length)
            {
                char current = _text[_pos];

                if (char.IsWhiteSpace(current))
                {
                    _pos++;
                    continue;
                }

                int startPos = _pos;

                if (char.IsDigit(current))
                {
                    while (_pos < _text.Length && char.IsDigit(_text[_pos]))
                        _pos++;

                    if (_pos < _text.Length && char.IsLetter(_text[_pos]))
                        throw new SyntaxErrorException("Некоректний запис: літера одразу після цифри.", _pos);

                    tokens.Add(new Token(TokenType.Number, _text[startPos.._pos], startPos));
                    continue;
                }

                if (char.IsLetter(current))
                {
                    while (_pos < _text.Length && char.IsLetterOrDigit(_text[_pos]))
                        _pos++;

                    string word = _text[startPos.._pos];
                    string lower = word.ToLowerInvariant();

                    switch (lower)
                    {
                        case "not":
                            tokens.Add(new Token(TokenType.Not, word, startPos));
                            break;
                        case "inc":
                            tokens.Add(new Token(TokenType.Inc, word, startPos));
                            break;
                        case "dec":
                            tokens.Add(new Token(TokenType.Dec, word, startPos));
                            break;
                        case "max":
                            tokens.Add(new Token(TokenType.Max, word, startPos));
                            break;
                        case "min":
                            tokens.Add(new Token(TokenType.Min, word, startPos));
                            break;
                        default:
                            if (IsValidCellReference(word))
                                tokens.Add(new Token(TokenType.CellRef, word.ToUpperInvariant(), startPos));
                            else
                                throw new SyntaxErrorException($"Невідомий ідентифікатор '{word}'.", startPos);
                            break;
                    }
                    continue;
                }

                switch (current)
                {
                    case '+':
                        tokens.Add(new Token(TokenType.Plus, "+", startPos));
                        _pos++;
                        break;
                    case '-':
                        tokens.Add(new Token(TokenType.Minus, "-", startPos));
                        _pos++;
                        break;
                    case '*':
                        tokens.Add(new Token(TokenType.Multiply, "*", startPos));
                        _pos++;
                        break;
                    case '/':
                        tokens.Add(new Token(TokenType.Divide, "/", startPos));
                        _pos++;
                        break;
                    case '=':
                        tokens.Add(new Token(TokenType.Equal, "=", startPos));
                        _pos++;
                        break;
                    case '<':
                        if (_pos + 1 < _text.Length && (_text[_pos + 1] == '=' || _text[_pos + 1] == '>'))
                            throw new SyntaxErrorException($"Оператор '{_text.Substring(_pos, 2)}' не підтримується у варіанті 50.", startPos);
                        tokens.Add(new Token(TokenType.Less, "<", startPos));
                        _pos++;
                        break;
                    case '>':
                        if (_pos + 1 < _text.Length && _text[_pos + 1] == '=')
                            throw new SyntaxErrorException("Оператор '>=' не підтримується у варіанті 50.", startPos);
                        tokens.Add(new Token(TokenType.Greater, ">", startPos));
                        _pos++;
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LParen, "(", startPos));
                        _pos++;
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RParen, ")", startPos));
                        _pos++;
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.Comma, ",", startPos));
                        _pos++;
                        break;
                    default:
                        throw new SyntaxErrorException($"Неприпустимий символ '{current}'.", startPos);
                }
            }

            tokens.Add(new Token(TokenType.EOF, string.Empty, _pos));
            return tokens;
        }

        private static bool IsValidCellReference(string s)
        {
            int i = 0;
            while (i < s.Length && char.IsLetter(s[i])) i++;
            if (i == 0 || i == s.Length) return false;
            if (s[i] == '0') return false;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            return i == s.Length;
        }
    }
}
