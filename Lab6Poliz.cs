namespace TextEditor;

internal static class Lab6Poliz
{
    public static List<string> ToRpn(List<Lab6Token> tokens)
    {
        var output = new List<string>();
        var ops = new Stack<Lab6Token>();

        foreach (var token in tokens)
        {
            if (token.Kind == Lab6TokenKind.End)
            {
                break;
            }

            if (token.Kind == Lab6TokenKind.Number || token.Kind == Lab6TokenKind.Identifier)
            {
                output.Add(token.Lexeme);
                continue;
            }

            if (token.Kind == Lab6TokenKind.LeftParen)
            {
                ops.Push(token);
                continue;
            }

            if (token.Kind == Lab6TokenKind.RightParen)
            {
                while (ops.Count > 0 && ops.Peek().Kind != Lab6TokenKind.LeftParen)
                {
                    output.Add(ops.Pop().Lexeme);
                }

                if (ops.Count > 0 && ops.Peek().Kind == Lab6TokenKind.LeftParen)
                {
                    ops.Pop();
                }

                continue;
            }

            while (ops.Count > 0 && IsOperator(ops.Peek()) && Priority(ops.Peek()) >= Priority(token))
            {
                output.Add(ops.Pop().Lexeme);
            }

            ops.Push(token);
        }

        while (ops.Count > 0)
        {
            output.Add(ops.Pop().Lexeme);
        }

        return output;
    }

    public static bool TryEvaluateIntegerRpn(List<string> rpn, out int value, out string error)
    {
        value = 0;
        error = string.Empty;
        var stack = new Stack<int>();

        foreach (var item in rpn)
        {
            if (int.TryParse(item, out var number))
            {
                stack.Push(number);
                continue;
            }

            if (stack.Count < 2)
            {
                error = "Недостаточно операндов для вычисления ПОЛИЗ.";
                return false;
            }

            var b = stack.Pop();
            var a = stack.Pop();

            switch (item)
            {
                case "+":
                    stack.Push(a + b);
                    break;
                case "-":
                    stack.Push(a - b);
                    break;
                case "*":
                    stack.Push(a * b);
                    break;
                case "/":
                    if (b == 0)
                    {
                        error = "Деление на ноль при вычислении ПОЛИЗ.";
                        return false;
                    }

                    stack.Push(a / b);
                    break;
                case "%":
                    if (b == 0)
                    {
                        error = "Остаток по модулю нуля при вычислении ПОЛИЗ.";
                        return false;
                    }

                    stack.Push(a % b);
                    break;
                default:
                    error = "Выражение содержит идентификаторы, результата нет.";
                    return false;
            }
        }

        if (stack.Count != 1)
        {
            error = "Некорректный стек после вычисления ПОЛИЗ.";
            return false;
        }

        value = stack.Pop();
        return true;
    }

    private static bool IsOperator(Lab6Token token)
    {
        return token.Kind is Lab6TokenKind.Plus or Lab6TokenKind.Minus or Lab6TokenKind.Multiply or Lab6TokenKind.Divide or Lab6TokenKind.Modulo;
    }

    private static int Priority(Lab6Token token)
    {
        return token.Kind is Lab6TokenKind.Multiply or Lab6TokenKind.Divide or Lab6TokenKind.Modulo ? 2 : 1;
    }
}

