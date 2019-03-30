using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QRedis.RedisModel
{
    internal class RedisArray : IRedisModel, IEnumerable
    {
        public const char Peek0 = '*';

        public static readonly RedisArray Nil = new RedisArray();

        private readonly List<IRedisModel> _elements;

        public RedisArray(IEnumerable<IRedisModel> elements) =>_elements = elements.ToList();
        private RedisArray() => _elements = new List<IRedisModel>();

        public void Add(IRedisModel element) => _elements.Add(element);
        public IEnumerator GetEnumerator() => _elements.GetEnumerator();

        public void Serialize(RedisTokenWriter writer)
        {
            writer.WritePeek0(Peek0);
            writer.WriteInt(this == Nil ? -1 : _elements.Count);

            foreach (var i in _elements)
                i.Serialize(writer);
        }

        public static RedisArray Parse(RedisTokenReader reader)
        {
            reader.ReadPeek0(Peek0);
            var count = reader.ReadInt();
            if (count == -1)
                return Nil;

            return new RedisArray(Enumerable.Range(0, count).Select(x => RedisProtocol.Parse(reader)));
        }
    }
}
