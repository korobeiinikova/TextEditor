using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor
{
    public class Token
    {
        public int code;
        public string type;
        public string token_name;
        public Token_Location location;
        public Token(int T_Code, string T_Type, string T_Token, Token_Location T_Location)
        {
            code = T_Code;
            type = T_Type;
            token_name = T_Token;
            location = T_Location;
        }
        public Token()
        {
            code = 0;
            type = "no";
            token_name = "no";
            location = new Token_Location();
        }
    }
    public struct Token_Location
    {
        public int row;
        public int start;
        public int end;
        public string To_String()
        {
            return $"{row} строка, {start}-{end}";
        }
        public Token_Location(int Row, int Start, int End)
        {
            row = Row;
            start = Start;
            end = End;
        }
    }
    public class SyntaxError
    {
        public string Message;
        public string Fragment;
        public int Line;
        public int Column;
        public Token Token;

        public SyntaxError() { }

        public SyntaxError(string message, Token token)
        {
            Message = message;
            Token = token;
        }
    }
}
