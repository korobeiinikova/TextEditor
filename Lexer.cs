using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor
{
     class Lexer : Form1
     {
        private string text;
        private string token_name;
        private Token before_token = new Token();
        private List<string> texts = new List<string>();
        private string row;
        private List<Token> tokens = new List<Token>();
        public Lexer(string t)
        {
            text = t;
        }
        private void AddToken(int code, string type, string value, int row, int start, int end)
        {
            Token_Location location = new Token_Location
            {
                row = row,
                start = start,
                end = end
            };
            tokens.Add(new Token(code, type, value, location));
        }
        private void FinishToken(int row, int endCol)
        {
            if (string.IsNullOrEmpty(token_name)) return;

            if (int.TryParse(token_name, out _))
            {
                AddToken(1, "digit", token_name, row, endCol - token_name.Length + 1, endCol);
            }
            else if (token_name == "int" || token_name == "final")
            {
                AddToken(3, "keyword", token_name, row, endCol - token_name.Length + 1, endCol);
            }
            else
            {
                int start = endCol - token_name.Length + 1;
                int i = 0;

                while (i < token_name.Length)
                {
                    int partStart = i;
                    bool isLetter = char.IsLetter(token_name[i]);

                    while (i < token_name.Length && char.IsLetter(token_name[i]) == isLetter)
                        i++;

                    string part = token_name.Substring(partStart, i - partStart);
                    int partStartCol = start + partStart;
                    int partEndCol = partStartCol + part.Length - 1;

                    if (isLetter)
                    {
                        AddToken(4, "identifier", part, row, partStartCol, partEndCol);
                    }
                    else
                    {
                        AddToken(7, "error", part, row, partStartCol, partEndCol);
                    }
                }
            }

            token_name = "";
        }
        public List<Token> analyze()
        {
            foreach (char c in text)
            {
                if (c != '\n') row += c;
                else
                {
                    texts.Add(row);
                    row = "";
                }
            }
            if (!string.IsNullOrEmpty(row)) texts.Add(row);

            for (int number_row = 1; number_row <= texts.Count; number_row++)
            {
                int tokenStart = -1;
                string line = texts[number_row - 1];

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    int col = i + 1;

                    if (c == ' ')
                    {
                        FinishToken(number_row, col - 1);
                        AddToken(8, "whitespace", " ", number_row, col, col);
                        continue;
                    }
                    if (c == '+' || c == '-' || c == '=')
                    {
                        FinishToken(number_row, col - 1);
                        AddToken(5, "operator", c.ToString(), number_row, col, col);
                        continue;
                    }
                    if (c == ';')
                    {
                        FinishToken(number_row, col - 1);
                        AddToken(6, "separator", ";", number_row, col, col);
                        continue;
                    }
                    if (string.IsNullOrEmpty(token_name))
                        tokenStart = col;

                    token_name += c;
                }
                FinishToken(number_row, line.Length);
            }

            return tokens;
        }
    }
}
