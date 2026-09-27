using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FiZZ
{
    internal class BencodeParser
    {
        private static readonly char DictionaryStart = 'd';
        private static readonly char ListStart = 'l';
        private static readonly char NumberStart = 'i';
        private static readonly char ByteStringSeparator = ':';
        private static readonly char ItemEnd = 'e';

        private int i = -1;
        private char curr;
        private char[] _data;

        public Stack<string> ErrorMessages { get; private set; }

        public BencodeParser(char[] data)
        {
            _data = data;
            ErrorMessages = [];
        }

        public Dictionary<string, object>? Read()
        {
            Eat();
            return ReadDictionary();
        }

        private object? ReadNextElement()
        {
            switch (curr)
            {
                case var d when d == DictionaryStart:
                    return ReadDictionary();
                case var l when l == ListStart:
                    return ReadList();
                case var i when i == NumberStart:
                    return ReadNumber();
                default:
                    if (char.IsNumber(curr))
                    {
                        return ReadString();
                    }
                    return null;
            }
        }

        private Dictionary<string, object>? ReadDictionary()
        {
            if (curr != DictionaryStart) return null;
            Eat();

            Dictionary<string, object> dic = [];
            
            while (curr != ItemEnd)
            {
                string? key = ReadString();
                if (key is null)
                {
                    ErrorMessages.Push($"Error reading key");
                    return null;
                }

                object? value = ReadNextElement();
                if (value is null)
                {
                    ErrorMessages.Push($"Error reading value");
                    return null;
                }

                dic[key] = value;
            }

            return dic;
        }

        private List<object>? ReadList()
        {
            if (curr != ListStart) return null;
            Eat();

            List<object> list = [];

            while (curr != ItemEnd)
            {
                object? value = ReadNextElement();
                if (value is null)
                {
                    ErrorMessages.Push($"Error reading list");
                    return null;
                }
                list.Add(value);
            }

            return list;
        }

        private int? ReadNumber()
        {
            if (curr != NumberStart) return null;
            Eat();

            StringBuilder sb = new();
            while (char.IsNumber(curr))
            {
                sb.Append(curr);
                Eat();
            }

            if (curr != ItemEnd)
            {
                ErrorMessages.Push($"Unexpected char {curr} at end of number");
                return null;
            }
            Eat();

            string intStr = sb.ToString();

            if (!int.TryParse(intStr, out int value))
            {
                ErrorMessages.Push($"Failed to parse int from '{intStr}' for number");
                return null;
            }

            return value;
        }

        private string? ReadString()
        {
            var strLen = ReadStringLength();
            if (strLen is null)
            {
                return null;
            }

            StringBuilder sb = new();
            for (int i = 0; i < strLen; i++)
            {
                //if (!char.IsAscii(curr))
                //{
                //    ErrorMessages.Add($"Bad char {curr} in byte string");
                //    return null;
                //}
                sb.Append(curr);
                Eat();
            }

            return sb.ToString();
        }

        private int? ReadStringLength()
        {
            StringBuilder sb = new();
            while (char.IsNumber(curr))
            {
                sb.Append(curr);
                Eat();
            }

            if (curr != ByteStringSeparator)
            {
                ErrorMessages.Push($"Unexpected char {curr}, expected {ByteStringSeparator}");
                return null;
            }
            Eat();

            string intStr = sb.ToString();

            if (!int.TryParse(intStr, out int value))
            {
                ErrorMessages.Push($"Failed to parse int from '{intStr}' for string length");
                return null;
            }
            
            return value;
        }

        private char Eat()
        {
            curr = _data[++i];
            return curr;
        }
    }
}
