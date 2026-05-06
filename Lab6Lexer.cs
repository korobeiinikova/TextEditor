using System.Text;

namespace TextEditor;

internal enum Lab6TokenKind
{
    Identifier,
    Number,
    Plus,
    Minus,
    Multiply,
    Divide,
    Modulo,
    LeftParen,
    RightParen,
    End
}

internal sealed class Lab6Token
{
    public Lab6TokenKind Kind { get; }
    public string Lexeme { get; }
    public int Position { get; }

    public Lab6Token(Lab6TokenKind kind, string lexeme, int position)
    {
        Kind = kind;
        Lexeme = lexeme;
        Position = position;
    }
}

internal sealed class Lab6Error
{
    public string Stage { get; }
    public string Message { get; }
    public int Position { get; }

    public Lab6Error(string stage, string message, int position)
    {
        Stage = stage;
        Message = message;
        Position = position;
    }
}

internal sealed class Lab6Quadruple
{
    public string Op { get; }
    public string Arg1 { get; }
    public string Arg2 { get; }
    public string Result { get; }

    public Lab6Quadruple(string op, string arg1, string arg2, string result)
    {
        Op = op;
        Arg1 = arg1;
        Arg2 = arg2;
        Result = result;
    }
}

internal static class Lab6Lexer
{
    public static List<Lab6Token> Tokenize(string input, List<Lab6Error> errors)
    {
        var tokens = new List<Lab6Token>();
        var i = 0;

        while (i < input.Length)
        {
            var c = input[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsLetter(c))
            {
                var start = i;
                var sb = new StringBuilder();
                while (i < input.Length && char.IsLetterOrDigit(input[i]))
                {
                    sb.Append(input[i]);
                    i++;
                }

                tokens.Add(new Lab6Token(Lab6TokenKind.Identifier, sb.ToString(), start + 1));
                continue;
            }

            if (char.IsDigit(c))
            {
                var start = i;
                var sb = new StringBuilder();
                while (i < input.Length && char.IsDigit(input[i]))
                {
                    sb.Append(input[i]);
                    i++;
                }

                tokens.Add(new Lab6Token(Lab6TokenKind.Number, sb.ToString(), start + 1));
                continue;
            }

            var kind = c switch
            {
                '+' => Lab6TokenKind.Plus,
                '-' => Lab6TokenKind.Minus,
                '*' => Lab6TokenKind.Multiply,
                '/' => Lab6TokenKind.Divide,
                '%' => Lab6TokenKind.Modulo,
                '(' => Lab6TokenKind.LeftParen,
                ')' => Lab6TokenKind.RightParen,
                _ => Lab6TokenKind.End
            };

            if (kind == Lab6TokenKind.End)
            {
                errors.Add(new Lab6Error("Лексика", $"Недопустимый символ '{c}'", i + 1));
                i++;
                continue;
            }

            tokens.Add(new Lab6Token(kind, c.ToString(), i + 1));
            i++;
        }

        tokens.Add(new Lab6Token(Lab6TokenKind.End, "<eof>", input.Length + 1));
        return tokens;
    }
}

