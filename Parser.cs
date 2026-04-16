using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor
{
    class Parser
    {
        private sealed class ExpectedSymbol
        {
            public string DisplayName { get; }
            public bool AllowLexerError { get; }
            private readonly Func<Token, bool> matcher;

            public ExpectedSymbol(Func<Token, bool> matcher, string displayName, bool allowLexerError = false)
            {
                this.matcher = matcher;
                DisplayName = displayName;
                AllowLexerError = allowLexerError;
            }

            public bool Matches(Token token)
            {
                return matcher(token);
            }
        }

        private enum RecoveryActionKind
        {
            InsertMissing,
            DeleteUnexpected,
            ReportLexerError
        }

        private sealed class RecoveryAction
        {
            public RecoveryActionKind Kind { get; }
            public int TokenIndex { get; }
            public int ExpectedFrom { get; }
            public int ExpectedToExclusive { get; }

            public RecoveryAction(RecoveryActionKind kind, int tokenIndex, int expectedFrom, int expectedToExclusive)
            {
                Kind = kind;
                TokenIndex = tokenIndex;
                ExpectedFrom = expectedFrom;
                ExpectedToExclusive = expectedToExclusive;
            }
        }

        private sealed class RecoveryPlan
        {
            public int RecoveryCount { get; }
            public int DeletedTokenCount { get; }
            public int InsertedSymbolCount { get; }
            public List<RecoveryAction> Actions { get; }
            public ExpectedSymbol[] Pattern { get; }

            public RecoveryPlan(
                int recoveryCount,
                int deletedTokenCount,
                int insertedSymbolCount,
                List<RecoveryAction> actions,
                ExpectedSymbol[] pattern)
            {
                RecoveryCount = recoveryCount;
                DeletedTokenCount = deletedTokenCount;
                InsertedSymbolCount = insertedSymbolCount;
                Actions = actions ?? new List<RecoveryAction>();
                Pattern = pattern;
            }
        }

        private static readonly ExpectedSymbol[] StatementPattern =
        {
            new ExpectedSymbol(token => token.type == "keyword" && token.token_name == "final", "'final'"),
            new ExpectedSymbol(token => token.type == "keyword" && token.token_name == "int", "'int'"),
            new ExpectedSymbol(token => token.type == "identifier", "идентификатор"),
            new ExpectedSymbol(token => token.type == "operator" && token.token_name == "=", "'='"),
            new ExpectedSymbol(token => token.type == "digit", "целое число", allowLexerError: true),
            new ExpectedSymbol(token => token.type == "separator" && token.token_name == ";", "';'")
        };

        private static readonly ExpectedSymbol[] SignedPositiveStatementPattern =
        {
            new ExpectedSymbol(token => token.type == "keyword" && token.token_name == "final", "'final'"),
            new ExpectedSymbol(token => token.type == "keyword" && token.token_name == "int", "'int'"),
            new ExpectedSymbol(token => token.type == "identifier", "идентификатор"),
            new ExpectedSymbol(token => token.type == "operator" && token.token_name == "=", "'='"),
            new ExpectedSymbol(token => token.type == "operator" && token.token_name == "+", "'+'"),
            new ExpectedSymbol(token => token.type == "digit", "целое число", allowLexerError: true),
            new ExpectedSymbol(token => token.type == "separator" && token.token_name == ";", "';'")
        };

        private static readonly ExpectedSymbol[] SignedNegativeStatementPattern =
        {
            new ExpectedSymbol(token => token.type == "keyword" && token.token_name == "final", "'final'"),
            new ExpectedSymbol(token => token.type == "keyword" && token.token_name == "int", "'int'"),
            new ExpectedSymbol(token => token.type == "identifier", "идентификатор"),
            new ExpectedSymbol(token => token.type == "operator" && token.token_name == "=", "'='"),
            new ExpectedSymbol(token => token.type == "operator" && token.token_name == "-", "'-'"),
            new ExpectedSymbol(token => token.type == "digit", "целое число", allowLexerError: true),
            new ExpectedSymbol(token => token.type == "separator" && token.token_name == ";", "';'")
        };

        private static readonly ExpectedSymbol[][] StatementPatterns =
        {
            StatementPattern,
            SignedPositiveStatementPattern,
            SignedNegativeStatementPattern
        };

        private List<Token> tokens;
        private int pos = 0;
        public List<SyntaxError> Errors = new List<SyntaxError>();


        public Parser(List<Token> Tokens)
        {
            tokens = Tokens;
        }

        private Token Current
        {
            get
            {
                if (pos < tokens.Count)
                    return tokens[pos];
                else
                    return null!;
            }
        }

        private void Next()
        {
            pos++;
        }

        public void Parse()
        {
            Errors = new List<SyntaxError>();
            var syntaxTokens = tokens.Where(t => t.type != "whitespace").ToList();
            pos = 0;

            if (tokens.Count == 0)
            {
                Errors.Add(new SyntaxError
                {
                    Message = "Пустой входной поток",
                    Fragment = "",
                    Line = 1,
                    Column = 1,
                    Token = null
                });
                return;
            }

            if (syntaxTokens.Count == 0)
            {
                Errors.Add(new SyntaxError
                {
                    Message = "Пустой входной поток (только пробелы)",
                    Fragment = "",
                    Line = 1,
                    Column = 1,
                    Token = null
                });
                return;
            }

            ParseProgram(syntaxTokens);
        }

        private void ParseProgram(List<Token> syntaxTokens)
        {
            while (pos < syntaxTokens.Count)
            {
                ParseStatement(syntaxTokens);
            }
        }

        private void ParseStatement(List<Token> syntaxTokens)
        {
            if (pos >= syntaxTokens.Count)
            {
                return;
            }
            var statementTokens = new List<Token>();

            while (pos < syntaxTokens.Count)
            {
                var current = CurrentToken(syntaxTokens);
                if (current == null)
                {
                    break;
                }

                statementTokens.Add(current);
                pos++;

                if (current.type == "separator" && current.token_name == ";")
                {
                    break;
                }
            }

            if (statementTokens.Count > 0)
            {
                ValidateStatement(statementTokens);
            }
        }

        private void ValidateStatement(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return;
            }

            RecoveryPlan bestPlan = null;

            foreach (var pattern in StatementPatterns)
            {
                var memo = new RecoveryPlan[tokens.Count + 1, pattern.Length + 1];
                var calculated = new bool[tokens.Count + 1, pattern.Length + 1];
                var plan = BuildRecoveryPlan(tokens, 0, 0, pattern, memo, calculated);
                bestPlan = ChooseBetter(bestPlan, plan);
            }

            if (bestPlan == null)
            {
                return;
            }

            for (var i = 0; i < bestPlan.Actions.Count; i++)
            {
                var action = bestPlan.Actions[i];

                if (action.Kind == RecoveryActionKind.InsertMissing)
                {
                    var fragmentInfo = GetFragmentInfoForInsertion(tokens, bestPlan.Actions, action);
                    AddMissingSequenceError(
                        bestPlan.Pattern,
                        action.ExpectedFrom,
                        action.ExpectedToExclusive,
                        fragmentInfo.Fragment,
                        fragmentInfo.Line,
                        fragmentInfo.Column);
                    continue;
                }

                if (action.Kind == RecoveryActionKind.DeleteUnexpected)
                {
                    var shouldSuppressUnexpectedFragment = IsUnexpectedFragmentCoveredByInsertion(bestPlan.Actions, i);
                    var fragmentInfo = GetFragmentInfoForUnexpectedTokens(tokens, bestPlan.Actions, ref i);
                    if (!shouldSuppressUnexpectedFragment)
                    {
                        AddUnexpectedFragmentError(fragmentInfo.Fragment, fragmentInfo.Line, fragmentInfo.Column);
                    }

                    continue;
                }

                if (action.Kind == RecoveryActionKind.ReportLexerError
                    && action.TokenIndex >= 0
                    && action.TokenIndex < tokens.Count)
                {
                    var token = tokens[action.TokenIndex];
                    Errors.Add(new SyntaxError
                    {
                        Message = ConvertLexerErrorToMessage(token),
                        Fragment = token.token_name,
                        Line = token.location.row,
                        Column = token.location.start,
                        Token = token
                    });
                }
            }
        }

        private RecoveryPlan BuildRecoveryPlan(
            List<Token> tokens,
            int tokenIndex,
            int expectedIndex,
            ExpectedSymbol[] pattern,
            RecoveryPlan[,] memo,
            bool[,] calculated)
        {
            if (calculated[tokenIndex, expectedIndex])
            {
                return memo[tokenIndex, expectedIndex];
            }

            RecoveryPlan bestPlan;

            if (tokenIndex >= tokens.Count)
            {
                if (expectedIndex >= pattern.Length)
                {
                    bestPlan = new RecoveryPlan(0, 0, 0, new List<RecoveryAction>(), pattern);
                }
                else
                {
                    bestPlan = new RecoveryPlan(
                        1,
                        0,
                        pattern.Length - expectedIndex,
                        new List<RecoveryAction>
                        {
                            new RecoveryAction(RecoveryActionKind.InsertMissing, tokenIndex, expectedIndex, pattern.Length)
                        },
                        pattern);
                }
            }
            else if (expectedIndex >= pattern.Length)
            {
                bestPlan = new RecoveryPlan(0, tokens.Count - tokenIndex, 0, new List<RecoveryAction>(), pattern);
            }
            else
            {
                bestPlan = null;
                var currentToken = tokens[tokenIndex];

                if (IsMatch(currentToken, expectedIndex, pattern))
                {
                    var matchPlan = BuildRecoveryPlan(tokens, tokenIndex + 1, expectedIndex + 1, pattern, memo, calculated);
                    RecoveryAction matchAction = null;

                    if (pattern[expectedIndex].AllowLexerError && currentToken.type == "error")
                    {
                        matchAction = new RecoveryAction(
                            RecoveryActionKind.ReportLexerError,
                            tokenIndex,
                            expectedIndex,
                            expectedIndex + 1);
                    }

                    var candidate = PrependAction(matchPlan, matchAction, 0, 0, 0, pattern);
                    bestPlan = ChooseBetter(bestPlan, candidate);
                }

                var deletePlan = BuildRecoveryPlan(tokens, tokenIndex + 1, expectedIndex, pattern, memo, calculated);
                var deleteAction = new RecoveryAction(
                    RecoveryActionKind.DeleteUnexpected,
                    tokenIndex,
                    expectedIndex,
                    expectedIndex);
                bestPlan = ChooseBetter(bestPlan, PrependAction(deletePlan, deleteAction, 0, 1, 0, pattern));

                for (var syncIndex = expectedIndex + 1; syncIndex < pattern.Length; syncIndex++)
                {
                    if (!IsMatch(currentToken, syncIndex, pattern))
                    {
                        continue;
                    }

                    var syncPlan = BuildRecoveryPlan(tokens, tokenIndex, syncIndex, pattern, memo, calculated);
                    var insertAction = new RecoveryAction(
                        RecoveryActionKind.InsertMissing,
                        tokenIndex,
                        expectedIndex,
                        syncIndex);

                    bestPlan = ChooseBetter(
                        bestPlan,
                        PrependAction(syncPlan, insertAction, 1, 0, syncIndex - expectedIndex, pattern));
                }
            }

            calculated[tokenIndex, expectedIndex] = true;
            memo[tokenIndex, expectedIndex] = bestPlan;
            return bestPlan;
        }

        private static RecoveryPlan PrependAction(
            RecoveryPlan basePlan,
            RecoveryAction action,
            int recoveryDelta,
            int deletedDelta,
            int insertedDelta,
            ExpectedSymbol[] pattern)
        {
            var actions = new List<RecoveryAction>();
            if (action != null)
            {
                actions.Add(action);
            }

            if (basePlan != null && basePlan.Actions.Count > 0)
            {
                actions.AddRange(basePlan.Actions);
            }

            return new RecoveryPlan(
                (basePlan != null ? basePlan.RecoveryCount : 0) + recoveryDelta,
                (basePlan != null ? basePlan.DeletedTokenCount : 0) + deletedDelta,
                (basePlan != null ? basePlan.InsertedSymbolCount : 0) + insertedDelta,
                actions,
                basePlan?.Pattern ?? pattern);
        }

        private static RecoveryPlan ChooseBetter(RecoveryPlan current, RecoveryPlan candidate)
        {
            if (candidate == null)
            {
                return current;
            }

            if (current == null)
            {
                return candidate;
            }

            if (candidate.DeletedTokenCount != current.DeletedTokenCount)
            {
                return candidate.DeletedTokenCount < current.DeletedTokenCount ? candidate : current;
            }

            if (candidate.RecoveryCount != current.RecoveryCount)
            {
                return candidate.RecoveryCount < current.RecoveryCount ? candidate : current;
            }

            if (candidate.InsertedSymbolCount != current.InsertedSymbolCount)
            {
                return candidate.InsertedSymbolCount < current.InsertedSymbolCount ? candidate : current;
            }

            return candidate.Actions.Count < current.Actions.Count ? candidate : current;
        }

        private static bool IsMatch(Token token, int expectedIndex, ExpectedSymbol[] pattern)
        {
            var expected = pattern[expectedIndex];
            if (expected.Matches(token))
            {
                return true;
            }

            return expected.AllowLexerError && token.type == "error";
        }

        private (string Fragment, int Line, int Column) GetFragmentInfoForInsertion(
            List<Token> tokens,
            List<RecoveryAction> actions,
            RecoveryAction insertAction)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return ("", 1, 1);
            }

            var insertActionIndex = actions.IndexOf(insertAction);
            if (insertActionIndex > 0)
            {
                var deletedTokenIndexes = new List<int>();

                for (var i = insertActionIndex - 1; i >= 0; i--)
                {
                    var action = actions[i];
                    if (action.Kind != RecoveryActionKind.DeleteUnexpected)
                    {
                        break;
                    }

                    deletedTokenIndexes.Add(action.TokenIndex);
                }

                if (deletedTokenIndexes.Count > 0)
                {
                    deletedTokenIndexes.Sort();

                    var startIndex = deletedTokenIndexes[0];
                    var endIndex = deletedTokenIndexes[^1];
                    var startToken = tokens[startIndex];

                    return (
                        BuildFragment(tokens, startIndex, endIndex),
                        startToken.location.row,
                        startToken.location.start);
                }
            }

            if (insertAction.TokenIndex >= 0 && insertAction.TokenIndex < tokens.Count)
            {
                var token = tokens[insertAction.TokenIndex];
                return (token.token_name, token.location.row, token.location.start);
            }

            if (tokens.Count > 0)
            {
                var lastToken = tokens[^1];
                return (lastToken.token_name, lastToken.location.row, lastToken.location.end);
            }

            return ("EOF", 1, 1);
        }

        private static string BuildFragment(List<Token> tokens, int startIndex, int endIndex)
        {
            if (tokens == null
                || tokens.Count == 0
                || startIndex < 0
                || endIndex < startIndex
                || endIndex >= tokens.Count)
            {
                return "";
            }

            var parts = new List<string>();
            for (var i = startIndex; i <= endIndex; i++)
            {
                parts.Add(tokens[i].token_name);
            }

            return string.Join(" ", parts);
        }

        private (string Fragment, int Line, int Column) GetFragmentInfoForUnexpectedTokens(
            List<Token> tokens,
            List<RecoveryAction> actions,
            ref int actionIndex)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return ("", 1, 1);
            }

            var tokenIndexes = new List<int>();

            while (actionIndex < actions.Count && actions[actionIndex].Kind == RecoveryActionKind.DeleteUnexpected)
            {
                var tokenIndex = actions[actionIndex].TokenIndex;
                if (tokenIndex >= 0 && tokenIndex < tokens.Count)
                {
                    tokenIndexes.Add(tokenIndex);
                }

                actionIndex++;
            }

            actionIndex--;

            if (tokenIndexes.Count == 0)
            {
                return ("", 1, 1);
            }

            tokenIndexes.Sort();
            var firstToken = tokens[tokenIndexes[0]];

            return (
                BuildFragmentFromIndexes(tokens, tokenIndexes),
                firstToken.location.row,
                firstToken.location.start);
        }

        private static bool IsUnexpectedFragmentCoveredByInsertion(List<RecoveryAction> actions, int actionIndex)
        {
            if (actions == null || actionIndex < 0 || actionIndex >= actions.Count)
            {
                return false;
            }

            var nextActionIndex = actionIndex;
            while (nextActionIndex < actions.Count && actions[nextActionIndex].Kind == RecoveryActionKind.DeleteUnexpected)
            {
                nextActionIndex++;
            }

            return nextActionIndex < actions.Count
                && actions[nextActionIndex].Kind == RecoveryActionKind.InsertMissing;
        }

        private static string BuildFragmentFromIndexes(List<Token> tokens, List<int> tokenIndexes)
        {
            if (tokens == null || tokenIndexes == null || tokenIndexes.Count == 0)
            {
                return "";
            }

            var parts = new List<string>();
            foreach (var tokenIndex in tokenIndexes)
            {
                if (tokenIndex >= 0 && tokenIndex < tokens.Count)
                {
                    parts.Add(tokens[tokenIndex].token_name);
                }
            }

            return string.Join(" ", parts);
        }

        private void AddMissingSequenceError(
            ExpectedSymbol[] pattern,
            int expectedFrom,
            int expectedToExclusive,
            string fragment,
            int line,
            int column)
        {
            if (expectedFrom >= expectedToExclusive)
            {
                return;
            }

            for (var i = expectedFrom; i < expectedToExclusive; i++)
            {
                var errorToken = new Token(0, "error", fragment, new Token_Location(line, column, column + Math.Max(fragment.Length, 1) - 1));
                Errors.Add(new SyntaxError
                {
                    Message = "Ожидалось " + pattern[i].DisplayName,
                    Fragment = fragment,
                    Line = line,
                    Column = column,
                    Token = errorToken
                });
            }
        }

        private void AddUnexpectedFragmentError(string fragment, int line, int column)
        {
            if (string.IsNullOrWhiteSpace(fragment))
            {
                return;
            }

            var errorToken = new Token(0, "error", fragment, new Token_Location(line, column, column + fragment.Length - 1));
            Errors.Add(new SyntaxError
            {
                Message = "Неверный фрагмент",
                Fragment = fragment,
                Line = line,
                Column = column,
                Token = errorToken
            });
        }

        private Token CurrentToken(List<Token> syntaxTokens)
        {
            if (pos < syntaxTokens.Count)
            {
                return syntaxTokens[pos];
            }

            return null;
        }

        private int CurrentLine(List<Token> syntaxTokens)
        {
            if (pos < syntaxTokens.Count)
            {
                return syntaxTokens[pos].location.row;
            }

            return 1;
        }

        private static string ConvertLexerErrorToMessage(Token token)
        {
            return "Неверный формат числа";
        }
    }
}
