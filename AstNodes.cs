using System.Numerics;
using System.Text;

namespace TextEditor
{
    public abstract class AstNode
    {
        public Token_Location Location { get; init; }
        public abstract string NodeName { get; }
        public abstract void Write(StringBuilder builder, string indent, bool isLast);

        protected static void WriteLine(StringBuilder builder, string indent, bool isLast, string text)
        {
            builder.Append(indent);
            builder.Append(isLast ? "└── " : "├── ");
            builder.AppendLine(text);
        }

        protected static string NextIndent(string indent, bool isLast)
        {
            return indent + (isLast ? "    " : "│   ");
        }
    }

    public sealed class ProgramNode : AstNode
    {
        public List<FinalIntDeclarationNode> Declarations { get; } = new List<FinalIntDeclarationNode>();

        public override string NodeName => "ProgramNode";

        public override void Write(StringBuilder builder, string indent, bool isLast)
        {
            if (builder.Length == 0)
            {
                builder.AppendLine(NodeName);
            }
            else
            {
                WriteLine(builder, indent, isLast, NodeName);
            }

            for (int i = 0; i < Declarations.Count; i++)
            {
                Declarations[i].Write(builder, string.Empty, i == Declarations.Count - 1);
            }
        }
    }

    public sealed class FinalIntDeclarationNode : AstNode
    {
        public string Name { get; init; } = string.Empty;
        public TypeNode Type { get; init; } = new TypeNode();
        public IntLiteralNode Value { get; init; } = new IntLiteralNode();
        public List<string> Modifiers { get; } = new List<string>();

        public override string NodeName => "FinalIntDeclarationNode";

        public override void Write(StringBuilder builder, string indent, bool isLast)
        {
            WriteLine(builder, indent, isLast, NodeName);
            var childIndent = NextIndent(indent, isLast);

            WriteLine(builder, childIndent, false, $"name: \"{Name}\"");
            WriteLine(builder, childIndent, false, $"modifiers: [{string.Join(", ", Modifiers.Select(m => $"\"{m}\""))}]");
            Type.Write(builder, childIndent, false);
            Value.Write(builder, childIndent, true);
        }
    }

    public sealed class TypeNode : AstNode
    {
        public string Name { get; init; } = string.Empty;

        public override string NodeName => "TypeNode";

        public override void Write(StringBuilder builder, string indent, bool isLast)
        {
            WriteLine(builder, indent, isLast, $"type: {NodeName}");
            WriteLine(builder, NextIndent(indent, isLast), true, $"name: \"{Name}\"");
        }
    }

    public sealed class IntLiteralNode : AstNode
    {
        public BigInteger Value { get; init; }
        public string RawValue { get; init; } = string.Empty;

        public override string NodeName => "IntLiteralNode";

        public override void Write(StringBuilder builder, string indent, bool isLast)
        {
            WriteLine(builder, indent, isLast, $"value: {NodeName}");
            WriteLine(builder, NextIndent(indent, isLast), true, $"value: {RawValue}");
        }
    }

    public sealed class ParserResult
    {
        public ProgramNode Program { get; init; } = new ProgramNode();

        public string GetAstText()
        {
            if (Program.Declarations.Count == 0)
            {
                return "ProgramNode\n└── declarations: []";
            }

            var builder = new StringBuilder();
            Program.Write(builder, string.Empty, true);
            return builder.ToString().TrimEnd();
        }
    }
}
