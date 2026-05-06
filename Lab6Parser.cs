namespace TextEditor;

internal sealed class Lab6ParseResult
{
    public bool IsSuccess { get; set; }
    public string RootTempOrValue { get; set; } = string.Empty;
}

internal sealed class Lab6RecursiveDescentParser
{
    private readonly List<Lab6Token> tokens;
    private readonly List<Lab6Error> errors;
    private readonly List<Lab6Quadruple> quads;
    private int pos;
    private int tempCounter;

    public Lab6RecursiveDescentParser(List<Lab6Token> tokens, List<Lab6Error> errors, List<Lab6Quadruple> quads)
    {
        this.tokens = tokens;
        this.errors = errors;
        this.quads = quads;
    }

    public Lab6ParseResult ParseExpression()
    {
        var result = new Lab6ParseResult();
        var value = ParseE();

        if (errors.Count == 0 && Current().Kind != Lab6TokenKind.End)
        {
            errors.Add(new Lab6Error("Синтаксис", $"Лишний фрагмент '{Current().Lexeme}'", Current().Position));
        }

        result.IsSuccess = errors.Count == 0;
        result.RootTempOrValue = value;
        return result;
    }

    private string ParseE()
    {
        var left = ParseT();

        while (Current().Kind is Lab6TokenKind.Plus or Lab6TokenKind.Minus)
        {
            var op = Current();
            Advance();
            var right = ParseT();
            left = Emit(op.Lexeme, left, right);
        }

        return left;
    }

    private string ParseT()
    {
        var left = ParseF();

        while (Current().Kind is Lab6TokenKind.Multiply or Lab6TokenKind.Divide or Lab6TokenKind.Modulo)
        {
            var op = Current();
            Advance();
            var right = ParseF();
            left = Emit(op.Lexeme, left, right);
        }

        return left;
    }

    private string ParseF()
    {
        var token = Current();

        if (token.Kind is Lab6TokenKind.Number or Lab6TokenKind.Identifier)
        {
            Advance();
            return token.Lexeme;
        }

        if (token.Kind == Lab6TokenKind.LeftParen)
        {
            Advance();
            var nested = ParseE();
            if (Current().Kind != Lab6TokenKind.RightParen)
            {
                errors.Add(new Lab6Error("Синтаксис", "Пропущена закрывающая скобка ')'.", Current().Position));
                return nested;
            }

            Advance();
            return nested;
        }

        if (token.Kind == Lab6TokenKind.RightParen)
        {
            errors.Add(new Lab6Error("Синтаксис", "Лишняя закрывающая скобка ')'.", token.Position));
            Advance();
            return "?";
        }

        errors.Add(new Lab6Error("Синтаксис", "Ожидался операнд (число, идентификатор или скобка).", token.Position));
        Advance();
        return "?";
    }

    private string Emit(string op, string left, string right)
    {
        var temp = $"t{++tempCounter}";
        quads.Add(new Lab6Quadruple(op, left, right, temp));
        return temp;
    }

    private Lab6Token Current() => tokens[Math.Min(pos, tokens.Count - 1)];

    private void Advance()
    {
        if (pos < tokens.Count - 1)
        {
            pos++;
        }
    }
}

internal sealed class Lab6RunResult
{
    public string ErrorsText { get; init; } = string.Empty;
    public string QuadsText { get; init; } = string.Empty;
    public string PolizText { get; init; } = string.Empty;
    public List<Lab6ErrorRow> ErrorRows { get; init; } = new();
    public int QuadCount { get; init; }
}

internal sealed class Lab6ErrorRow
{
    public string Fragment { get; }
    public string Location { get; }
    public string Description { get; }

    public Lab6ErrorRow(string fragment, string location, string description)
    {
        Fragment = fragment;
        Location = location;
        Description = description;
    }
}

internal static class Lab6Processor
{
    private enum DeclTokenKind
    {
        Final,
        Int,
        Identifier,
        Assign,
        Expression,
        Semicolon,
        End
    }

    private sealed class DeclToken
    {
        public DeclTokenKind Kind { get; }
        public string Lexeme { get; }
        public int StartPos { get; }
        public int EndPosExclusive0 { get; }

        public DeclToken(DeclTokenKind kind, string lexeme, int startPos, int endPosExclusive0)
        {
            Kind = kind;
            Lexeme = lexeme;
            StartPos = startPos;
            EndPosExclusive0 = endPosExclusive0;
        }
    }

    public static Lab6RunResult Run(string source)
    {
        var errorsSb = new System.Text.StringBuilder();
        var quadsSb = new System.Text.StringBuilder();
        var polizSb = new System.Text.StringBuilder();
        var errorRows = new List<Lab6ErrorRow>();
        var quadCount = 0;

        var lines = source.Replace("\r", string.Empty).Split('\n');
        var hadAnySyntaxOrLexError = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNo = i + 1;
            var rawLine = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var errors = new List<Lab6Error>();
            var expression = ExtractExpressionByRecursiveDescent(rawLine, errors);
            if (errors.Count > 0)
            {
                hadAnySyntaxOrLexError = true;
                AppendErrors(errorsSb, lineNo, rawLine, errors);
                AddErrorRows(errorRows, lineNo, rawLine, errors, 0);
                continue;
            }

            var expressionOffset = rawLine.IndexOf(expression, StringComparison.Ordinal);
            if (expressionOffset < 0)
            {
                expressionOffset = 0;
            }

            var tokens = Lab6Lexer.Tokenize(expression, errors);
            tokens = NormalizeUnaryOperators(tokens);
            var quads = new List<Lab6Quadruple>();

            if (errors.Count == 0)
            {
                var parser = new Lab6RecursiveDescentParser(tokens, errors, quads);
                parser.ParseExpression();
            }

            if (errors.Count > 0)
            {
                hadAnySyntaxOrLexError = true;
                AppendErrors(errorsSb, lineNo, rawLine, errors);
                AddErrorRows(errorRows, lineNo, rawLine, errors, expressionOffset);
                continue;
            }

            quadsSb.AppendLine($"Строка {lineNo}: {expression}");
            if (quads.Count == 0)
            {
                quadsSb.AppendLine("  Тетрады не требуются (одно значение).");
            }
            else
            {
                for (var q = 0; q < quads.Count; q++)
                {
                    var quad = quads[q];
                    quadsSb.AppendLine($"  {q + 1}. ({quad.Op}, {quad.Arg1}, {quad.Arg2}, {quad.Result})");
                }
            }

            quadCount += quads.Count;
            quadsSb.AppendLine();

            var rpn = Lab6Poliz.ToRpn(tokens);
            var hasOnlyIntegers = tokens.All(t => t.Kind != Lab6TokenKind.Identifier);

            polizSb.AppendLine($"Строка {lineNo}: {expression}");
            polizSb.AppendLine($"  ПОЛИЗ: {string.Join(" ", rpn)}");

            if (hasOnlyIntegers)
            {
                if (Lab6Poliz.TryEvaluateIntegerRpn(rpn, out var value, out var evalError))
                {
                    polizSb.AppendLine($"  Значение: {value}");
                }
                else
                {
                    polizSb.AppendLine($"  Результат: {evalError}");
                }
            }
            else
            {
                polizSb.AppendLine("  Результата нет (есть идентификаторы).");
            }

            polizSb.AppendLine();
        }

        if (!hadAnySyntaxOrLexError)
        {
            errorsSb.AppendLine("Лексические и синтаксические ошибки не обнаружены.");
        }

        if (hadAnySyntaxOrLexError)
        {
            quadsSb.AppendLine("Построение тетрад выполнено только для корректных строк.");
        }

        if (polizSb.Length == 0)
        {
            polizSb.AppendLine("Нет выражений для преобразования в ПОЛИЗ.");
        }

        if (quadsSb.Length == 0)
        {
            quadsSb.AppendLine("Нет корректных выражений для построения тетрад.");
        }

        return new Lab6RunResult
        {
            ErrorsText = errorsSb.ToString(),
            QuadsText = quadsSb.ToString(),
            PolizText = polizSb.ToString(),
            ErrorRows = errorRows,
            QuadCount = quadCount
        };
    }

    private static void AppendErrors(System.Text.StringBuilder errorsSb, int lineNo, string rawLine, List<Lab6Error> errors)
    {
        errorsSb.AppendLine($"Строка {lineNo}: {rawLine}");
        foreach (var error in errors)
        {
            errorsSb.AppendLine($"  [{error.Stage}] позиция {error.Position}: {error.Message}");
        }

        errorsSb.AppendLine();
    }

    private static void AddErrorRows(List<Lab6ErrorRow> rows, int lineNo, string rawLine, List<Lab6Error> errors, int positionOffset)
    {
        foreach (var error in errors)
        {
            var position = error.Position + positionOffset;
            var fragment = GetErrorFragment(rawLine, ref position);

            rows.Add(new Lab6ErrorRow(
                fragment,
                $"{lineNo} строка, позиция {position}",
                $"[{error.Stage}] {error.Message}"
            ));
        }
    }

    private static string GetErrorFragment(string line, ref int position)
    {
        if (string.IsNullOrEmpty(line))
        {
            position = 1;
            return string.Empty;
        }

        if (position < 1)
        {
            position = 1;
        }

        if (position > line.Length)
        {
            position = line.Length;
        }

        return line[position - 1].ToString();
    }

    private static string ExtractExpressionByRecursiveDescent(string rawLine, List<Lab6Error> errors)
    {
        var hasDeclMarkers = rawLine.Contains('=') || rawLine.Contains(';') || rawLine.StartsWith("final") || rawLine.StartsWith("int");
        if (!hasDeclMarkers)
        {
            return rawLine;
        }

        var tokens = TokenizeDeclaration(rawLine, errors);
        if (errors.Count > 0)
        {
            return string.Empty;
        }

        var pos = 0;

        bool Expect(DeclTokenKind kind, string expectedMessage, out DeclToken matched)
        {
            matched = tokens[Math.Min(pos, tokens.Count - 1)];
            if (matched.Kind == kind)
            {
                pos++;
                return true;
            }

            errors.Add(new Lab6Error("Синтаксис", expectedMessage, matched.StartPos));
            return false;
        }

        if (!Expect(DeclTokenKind.Final, "Ожидалось ключевое слово 'final'.", out _))
            return string.Empty;
        if (!Expect(DeclTokenKind.Int, "Ожидалось ключевое слово 'int'.", out _))
            return string.Empty;
        if (!Expect(DeclTokenKind.Identifier, "Ожидался идентификатор.", out _))
            return string.Empty;
        if (!Expect(DeclTokenKind.Assign, "Ожидалось '='.", out _))
            return string.Empty;
        if (!Expect(DeclTokenKind.Expression, "После '=' отсутствует выражение.", out var expressionToken))
            return string.Empty;
        if (!Expect(DeclTokenKind.Semicolon, "Ожидалось ';'.", out _))
            return string.Empty;
        if (!Expect(DeclTokenKind.End, "Лишние символы после ';'.", out _))
            return string.Empty;

        return expressionToken.Lexeme;
    }

    private static List<DeclToken> TokenizeDeclaration(string line, List<Lab6Error> errors)
    {
        var result = new List<DeclToken>();
        var i = 0;

        while (i < line.Length)
        {
            var c = line[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsLetter(c))
            {
                var start = i;
                var sb = new System.Text.StringBuilder();
                while (i < line.Length && char.IsLetterOrDigit(line[i]))
                {
                    sb.Append(line[i]);
                    i++;
                }

                var lexeme = sb.ToString();
                var kind = lexeme switch
                {
                    "final" => DeclTokenKind.Final,
                    "int" => DeclTokenKind.Int,
                    _ => DeclTokenKind.Identifier
                };

                result.Add(new DeclToken(kind, lexeme, start + 1, i));
                continue;
            }

            if (c == '=')
            {
                result.Add(new DeclToken(DeclTokenKind.Assign, "=", i + 1, i + 1));
                i++;

                var exprStart = i;
                while (i < line.Length && line[i] != ';')
                {
                    i++;
                }

                var expression = line.Substring(exprStart, i - exprStart).Trim();
                if (string.IsNullOrWhiteSpace(expression))
                {
                    errors.Add(new Lab6Error("Синтаксис", "После '=' отсутствует выражение.", exprStart + 1));
                    return result;
                }

                result.Add(new DeclToken(DeclTokenKind.Expression, expression, exprStart + 1, i));
                continue;
            }

            if (c == ';')
            {
                result.Add(new DeclToken(DeclTokenKind.Semicolon, ";", i + 1, i + 1));
                i++;
                continue;
            }

            errors.Add(new Lab6Error("Лексика", $"Недопустимый символ '{c}'", i + 1));
            i++;
        }

        result.Add(new DeclToken(DeclTokenKind.End, "<eof>", line.Length + 1, line.Length));
        return result;
    }

    private static List<Lab6Token> NormalizeUnaryOperators(List<Lab6Token> tokens)
    {
        var normalized = new List<Lab6Token>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind == Lab6TokenKind.End)
            {
                break;
            }

            var isUnarySign = token.Kind is Lab6TokenKind.Plus or Lab6TokenKind.Minus;
            if (isUnarySign)
            {
                var prev = normalized.Count == 0 ? null : normalized[^1];
                var prevAllowsUnary = prev == null
                    || prev.Kind is Lab6TokenKind.LeftParen
                    or Lab6TokenKind.Plus
                    or Lab6TokenKind.Minus
                    or Lab6TokenKind.Multiply
                    or Lab6TokenKind.Divide;

                if (prevAllowsUnary)
                {
                    normalized.Add(new Lab6Token(Lab6TokenKind.Number, "0", token.Position));
                }
            }

            normalized.Add(token);
        }

        normalized.Add(new Lab6Token(Lab6TokenKind.End, "<eof>", tokens.Count == 0 ? 1 : tokens[^1].Position));
        return normalized;
    }
}

