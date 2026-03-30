using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace TextEditor
{
    class Parser: Form1
    {
        private List<Token> tokens;
        private int pos = 0;
        public List<SyntaxError> Errors = new List<SyntaxError>();

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token Current
        {
            get
            {
                if (pos < tokens.Count)
                    return tokens[pos];
                else
                    return null;
            }
        }

        private void Next() => pos++;

        private void Expect(string type, string value = null)
        {
            if (Current == null)
            {
                return;
            }

            if (Current.type != type || (value != null && Current.token_name != value))
            {
                Errors.Add(new SyntaxError($"Ожидалось {type} {(value ?? "")}", Current));
                Next();
            }
            else
            {
                Next();
            }
        }

        public void Parse()
        {
            ParseProgram();
        }

        private void ParseProgram()
        {
            SkipSpaces();

            while (Current != null)
            {
                ParseFinal();
                SkipSpaces();
            }
        }

        private void ParseFinal()
        {
            Expect("keyword", "final");
            SkipSpaces();
            Expect("keyword", "int");
            ParseId();
            ParseAssign();
            ParseNum();
            ParseSemicolon();
        }

        private void SkipSpaces()
        {
            while (Current != null && Current.type == "whitespace")
                Next();
        }
        private void ParseId()
        {
            SkipSpaces();
            if (Current == null)
            {
                return;
            }
            if (Current.type != "identifier")
            {
                Errors.Add(new SyntaxError("Ожидался идентификатор", Current));
                Next();
            }
            else
            {
                Next();
            }
        }
        private void ParseAssign()
        {
            SkipSpaces();

            Expect("operator", "=");

            SkipSpaces();
        }
        private void ParseNum()
        {
            SkipSpaces();
            if (Current == null)
            {
                return;
            }
            if (Current.type == "operator" &&
                (Current.token_name == "+" || Current.token_name == "-"))
            {
                Next();
            }

            if (Current.type != "digit")
            {
                Errors.Add(new SyntaxError("Ожидалось число", Current));
                Next();
                return;
            }

            while (Current != null && Current.type == "digit")
                Next();
        }
        private void ParseSemicolon()
        {
            SkipSpaces();
            Expect("separator", ";");
        }
    }
}
