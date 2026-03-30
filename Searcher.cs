using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TextEditor.Form1;

namespace TextEditor
{
    public class Searcher
    {
        public string fragment;
        public Token_Location location;
        public Searcher(string Fragment, Token_Location Location)
        {
            fragment = Fragment;
            location = Location;
        }
    }
    public class Search
    {
        public List<Searcher> searchers;
        public List<string> texts;
        public Search(List<Searcher> Searchers, List<string> Texts)
        {
            searchers = Searchers;
            texts = Texts;
        }
        public List<Searcher> SearchLetters()
        {
            List<char> letters = new List<char>{ 'е','ё','и','о','у','ы','э','ю','я',
                                          'Е','Ё','И','О','У','Ы','Э','Ю','Я' };
            for (int number_row = 1; number_row <= texts.Count; number_row++)
            {
                string line = texts[number_row - 1];

                for (int i = 0; i < line.Length; i++)
                {
                    if (letters.Contains(line[i]))
                    {
                        searchers.Add(new Searcher(line[i].ToString(), new Token_Location(number_row, i + 1, i + 1)));
                    }
                }
            }
            return searchers;
        }

        private bool ReadDigits(string line, ref int index, int count, ref string fragment)
        {
            for (int i = 0; i < count; i++)
            {
                if (index >= line.Length || !char.IsDigit(line[index]))
                    return false;

                fragment += line[index];
                index++;
            }
            return true;
        }
        public List<Searcher> SearchPhoneNumber()
        {
            for (int number_row = 1; number_row <= texts.Count; number_row++)
            {
                string line = texts[number_row - 1];

                for (int i = 0; i < line.Length; i++)
                {
                    int start = i;
                    int index = i;
                    string fragment = "";

                    if (line[index] == '8')
                    {
                        fragment += line[index];
                        index++;
                    }
                    else if (line[index] == '+' && index + 1 < line.Length && line[index + 1] == '7')
                    {
                        fragment += "+7";
                        index += 2;
                    }
                    else
                    {
                        continue;
                    }
                    if (index < line.Length && (line[index] == '(' || line[index] == ' ' || line[index] == '-'))
                    {
                        fragment += line[index];
                        index++;
                    }
                    if (!ReadDigits(line, ref index, 3, ref fragment)) continue;

                    if (index < line.Length && (line[index] == ')' || line[index] == ' ' || line[index] == '-'))
                    {
                        fragment += line[index];
                        index++;
                    }

                    if (!ReadDigits(line, ref index, 3, ref fragment)) continue;

                    if (index < line.Length && (line[index] == ' ' || line[index] == '-'))
                    {
                        fragment += line[index];
                        index++;
                    }

                    if (!ReadDigits(line, ref index, 2, ref fragment)) continue;

                    if (index < line.Length && (line[index] == ' ' || line[index] == '-'))
                    {
                        fragment += line[index];
                        index++;
                    }
                    if (!ReadDigits(line, ref index, 2, ref fragment)) continue;

                    searchers.Add(new Searcher(fragment, new Token_Location(number_row, start + 1, index)));
                    i = index - 1;
                }
            }

            return searchers;
        }

        private bool ReadIPv4Octet(string line, ref int index, ref string fragment)
        {
            int start = index;
            int value = 0;
            int digits = 0;

            while (index < line.Length && char.IsDigit(line[index]) && digits < 3)
            {
                value = value * 10 + (line[index] - '0');
                fragment += line[index];
                index++;
                digits++;
            }

            if (digits == 0 || value > 255)
            {
                index = start;
                return false;
            }

            return true;
        }
        private bool ReadChar(string line, ref int index, char expected, ref string fragment)
        {
            if (index >= line.Length || line[index] != expected)
                return false;

            fragment += line[index];
            index++;
            return true;
        }
        private bool ReadMask(string line, ref int index, ref string fragment)
        {
            int start = index;
            int value = 0;
            int digits = 0;

            while (index < line.Length && char.IsDigit(line[index]) && digits < 2)
            {
                value = value * 10 + (line[index] - '0');
                fragment += line[index];
                index++;
                digits++;
            }

            if (digits == 0 || value < 1 || value > 32)
            {
                index = start;
                return false;
            }

            return true;
        }
        private bool ReadPort(string line, ref int index, ref string fragment)
        {
            int start = index;
            int value = 0;
            int digits = 0;

            while (index < line.Length && char.IsDigit(line[index]) && digits < 5)
            {
                value = value * 10 + (line[index] - '0');
                fragment += line[index];
                index++;
                digits++;
            }

            if (digits == 0 || value > 65535)
            {
                index = start;
                return false;
            }

            return true;
        }
        public List<Searcher> SearchIP()
        {
            List<Searcher> searchers = new List<Searcher>();

            for (int row = 1; row <= texts.Count; row++)
            {
                string line = texts[row - 1];

                for (int i = 0; i < line.Length; i++)
                {
                    int start = i;
                    int index = i;
                    string fragment = "";

                    if (!ReadIPv4Octet(line, ref index, ref fragment)) continue;
                    if (!ReadChar(line, ref index, '.', ref fragment)) continue;

                    if (!ReadIPv4Octet(line, ref index, ref fragment)) continue;
                    if (!ReadChar(line, ref index, '.', ref fragment)) continue;

                    if (!ReadIPv4Octet(line, ref index, ref fragment)) continue;
                    if (!ReadChar(line, ref index, '.', ref fragment)) continue;

                    if (!ReadIPv4Octet(line, ref index, ref fragment)) continue;

                    if (!ReadChar(line, ref index, '/', ref fragment)) continue;

                    if (!ReadMask(line, ref index, ref fragment)) continue;

                    if (!ReadChar(line, ref index, ':', ref fragment)) continue;

                    if (!ReadPort(line, ref index, ref fragment)) continue;

                    searchers.Add(new Searcher(fragment,new Token_Location(row, start + 1, index)));
                    i = index - 1;
                }
            }
            return searchers;
        }
    }
}
