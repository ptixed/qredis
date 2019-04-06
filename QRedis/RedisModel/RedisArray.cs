using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QRedis.RedisModel
{
    internal class RedisArray : IRedisModel, IEnumerable
    {
        public const char Peek0 = '*';

        private readonly List<IRedisModel> _elements;

        public RedisArray(IEnumerable<IRedisModel> elements) =>_elements = elements.ToList();
        private RedisArray() { }

        public IEnumerator GetEnumerator() => _elements == null ? Enumerable.Empty<object>().GetEnumerator() : _elements.GetEnumerator();

        public void Serialize(RedisTokenWriter writer)
        {
            writer.WritePeek0(Peek0);

            if (_elements == null)
            {
                writer.WriteInt(-1);
                return;
            }

            writer.WriteInt(_elements.Count);
            foreach (var i in _elements)
                i.Serialize(writer);
        }

        public static RedisArray Parse(RedisTokenReader reader)
        {
            reader.ReadPeek0(Peek0);
            var count = reader.ReadInt();
            if (count == -1)
                return new RedisArray();

            return new RedisArray(Enumerable.Range(0, count).Select(x => RedisProtocol.Parse(reader)));
        }
    }
}
