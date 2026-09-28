using System.Text;

namespace FiZZ
{
    internal class BencodeParser(byte[] data)
    {
        private const char DictionaryStart = 'd';
        private const char ListStart = 'l';
        private const char NumberStart = 'i';
        private const char ByteStringSeparator = ':';
        private const char ItemEnd = 'e';

        private const byte DictionaryStartByte = (byte)DictionaryStart;
        private const byte ListStartByte = (byte)ListStart;
        private const byte NumberStartByte = (byte)NumberStart;
        private const byte ByteStringSeparatorByte = (byte)ByteStringSeparator;
        private const byte ItemEndByte = (byte)ItemEnd;

        private int _i = -1;
        private byte _curr;

        public Stack<string> ErrorMessages { get; } = [];

        public BencodeDict? Read()
        {
            Eat();
            return ReadDictionary();
        }

        private object? ReadNextElement()
        {
            return _curr switch
            {
                DictionaryStartByte => ReadDictionary(),
                ListStartByte => ReadList(),
                NumberStartByte => ReadNumber(),
                _ => char.IsNumber((char)_curr) ? ReadString() : null
            };
        }

        private BencodeDict? ReadDictionary()
        {
            if (_curr != DictionaryStartByte) return null;
            Eat();

            BencodeDict dic = [];
            
            while (_curr != ItemEndByte)
            {
                var key = ReadString();
                if (key is null)
                {
                    ErrorMessages.Push($"Error reading key");
                    return null;
                }

                var value = ReadNextElement();
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
            if (_curr != ListStartByte) return null;
            Eat();

            List<object> list = [];

            while (_curr != ItemEndByte)
            {
                var value = ReadNextElement();
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
            if (_curr != NumberStartByte) return null;
            Eat();

            StringBuilder sb = new();
            while (char.IsDigit((char)_curr))
            {
                sb.Append((char)_curr);
                Eat();
            }

            if (_curr != ItemEndByte)
            {
                ErrorMessages.Push($"Unexpected char {_curr} at end of number");
                return null;
            }
            Eat();

            var intStr = sb.ToString();

            if (int.TryParse(intStr, out var value)) return value;
            
            ErrorMessages.Push($"Failed to parse int from '{intStr}' for number");
            return null;
        }

        private string? ReadString()
        {
            var strLen = ReadStringLength();
            if (strLen is null)
            {
                return null;
            }

            StringBuilder sb = new();
            for (var n = 0; n < strLen; n++)
            {
                sb.Append((char)_curr);
                Eat();
            }

            return sb.ToString();
        }

        private int? ReadStringLength()
        {
            StringBuilder sb = new();
            while (char.IsDigit((char)_curr))
            {
                sb.Append((char)_curr);
                Eat();
            }

            if (_curr != ByteStringSeparatorByte)
            {
                ErrorMessages.Push($"Unexpected char {(char)_curr}, expected {ByteStringSeparator}");
                return null;
            }
            Eat();

            var intStr = sb.ToString();

            if (int.TryParse(intStr, out var value)) return value;
            
            ErrorMessages.Push($"Failed to parse int from '{intStr}' for string length");
            return null;

        }

        private bool Eat()
        {
            if (_i + 1 >= data.Length)
            {
                ErrorMessages.Push($"Reached EOF");
                return false;
            }
            _curr = data[++_i];
            return true;
        }
    }
}
