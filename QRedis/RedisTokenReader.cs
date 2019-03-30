using System;
using System.Collections.Generic;
using System.IO;

namespace QRedis
{
    internal class RedisTokenReader
    {
        private TextReader _reader;

        public RedisTokenReader(TextReader reader)
        {
            _reader = reader;
        }

        public char ReadPeek0()
        {
            return (char)_reader.Peek();
        }

        public char ReadPeek0(char peek0)
        {
            var p = (char)_reader.Read();
            if (peek0 != p)
                throw new Exception($"Wrong peek0: expected '{peek0}' got '{p}'");
            return peek0;
        }

        public int ReadInt()
        {
            return int.Parse(ReadToDelimiter());
        }

        public string ReadString()
        {
            return ReadToDelimiter();
        }

        public string ReadString(int length)
        {
            var buffer = new char[length];
            _reader.Read(buffer, 0, length);
            for (int i = 0; i < RedisProtocol.Delimiter.Length; ++i)
                _reader.Read();
            return new string(buffer);
        }

        private string ReadToDelimiter()
        {
            var read = new List<char>();
            while (true)
            {
                var c = (char)_reader.Read();
                if (c == RedisProtocol.Delimiter[0])
                {
                    for (int i = 1; i < RedisProtocol.Delimiter.Length; ++i)
                        _reader.Read();
                    return new string(read.ToArray());
                }
                read.Add(c);
            }
        }
    }
}
