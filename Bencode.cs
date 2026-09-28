using System.Text;

namespace FiZZ
{
    public class BencodeList : List<object>;
    
    public class BencodeDict : Dictionary<string, object>
    {
        private string? GetString(string key)
        {
            return TryGetValue(key, out var value) && value is string s
                ? s
                : null;
        }
    
        public bool GetString(string key, out string value)
        {
            value = string.Empty;
            var v = GetString(key);
            if (v is null)
                return false;
            value = v;
            return true;
        }

        private long? GetLong(string key)
        {
            return TryGetValue(key, out var value) && value is long i
                ? i
                : null;
        }

        public bool GetLong(string key, out long value)
        {
            value = 0;
            var v = GetLong(key);
            if (v is null)
                return false;
            value = v.Value;
            return true;
        }

        private BencodeList? GetList(string key)
        {
            return TryGetValue(key, out var value) && value is BencodeList l
                ? l
                : null;
        }
    
        public bool GetList(string key, out BencodeList value)
        {
            value = null;
            var v = GetList(key);
            if (v is null)
                return false;
            value = v;
            return true;
        }

        private BencodeDict? GetDict(string key)
        {
            return TryGetValue(key, out var value) && value is BencodeDict d
                ? d
                : null;
        }
        
        public bool GetDict(string key, out BencodeDict value)
        {
            value = null;
            var v = GetDict(key);
            if (v is null)
                return false;
            value = v;
            return true;
        }
    }
    
    public class BencodeParser(byte[] data)
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

        public int _infoStart = 0;
        public int _infoEnd = 0;

        public BencodeDict? Read()
        {
            if (data.Length == 0) return null;
            Eat();
            return ReadDictionary();
        }

        public byte[]? InfoDictData => _infoStart == _infoEnd ? null : data[_infoStart.._infoEnd];

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

                var foundInfoKey = false;
                if (key.Equals("info"))
                {
                    foundInfoKey = true;
                    _infoStart = _i;
                }

                var value = ReadNextElement();
                if (value is null)
                {
                    ErrorMessages.Push($"Error reading value");
                    return null;
                }

                if (foundInfoKey)
                {
                    _infoEnd = _i;
                }

                dic[key] = value;
            }

            if (_curr != ItemEndByte) return null;
            Eat();

            return dic;
        }

        private BencodeList? ReadList()
        {
            if (_curr != ListStartByte) return null;
            Eat();

            BencodeList list = [];

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
            
            if (_curr != ItemEndByte) return null;
            Eat();

            return list;
        }

        private long? ReadNumber()
        {
            if (_curr != NumberStartByte) return null;
            Eat();

            StringBuilder sb = new();
            
            if ((char)_curr == '-')
            {
                sb.Append('-');
                Eat();
            }
            
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

            if (long.TryParse(intStr, out var value)) return value;
            
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

            if (_i + strLen > data.Length)
            {
                return null;
            }

            string result;
            try 
            {
                result = Encoding.UTF8.GetString(data, _i, strLen.Value);
            }
            catch (Exception ex)
            {
                ErrorMessages.Push($"Failed to decode UTF8 string at index {_i}: {ex.Message}");
                return null;
            }
            
            _i += (strLen.Value - 1); 
            Eat();

            return result;
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
