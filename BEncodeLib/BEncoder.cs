/*
    This file is part of BEncodeLib.

    BEncodeLib is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BEncodeLib is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BEncodeLib.  If not, see <http://www.gnu.org/licenses/>.
    
    Written by Jos van der Til (c) 2010.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BEncodeLib
{
    public class BEncoder
    {
        private static readonly Encoding StreamEncoding = Encoding.UTF8;
        private static readonly byte ListMarker = StreamEncoding.GetBytes(new[] { 'l' })[0];
        private static readonly byte MapMarker = StreamEncoding.GetBytes(new[] { 'd' })[0];
        private static readonly byte NumMarker = StreamEncoding.GetBytes(new[] { 'i' })[0];
        private static readonly byte EndMarker = StreamEncoding.GetBytes(new[] { 'e' })[0];
        private static readonly byte ColonMarker = StreamEncoding.GetBytes(new[] { ':' })[0];

        public static void Bencode(long n, Stream s)
        {
            s.WriteByte(NumMarker);

            var bytes = StreamEncoding.GetBytes(n.ToString());
            s.Write(bytes, 0, bytes.Length);

            s.WriteByte(EndMarker);
        }

        public static void Bencode(byte[] b, Stream s)
        {
            var lengthBytes = StreamEncoding.GetBytes(b.Length.ToString());

            s.Write(lengthBytes, 0, lengthBytes.Length);
            s.WriteByte(ColonMarker);
            s.Write(b, 0, b.Length);
        }

        public static void Bencode(string str, Stream s)
        {
            var bytes = StreamEncoding.GetBytes(str);
            var lengthBytes = StreamEncoding.GetBytes(bytes.Length.ToString());

            s.Write(lengthBytes, 0, lengthBytes.Length);
            s.WriteByte(ColonMarker);
            s.Write(bytes, 0, bytes.Length);
        }

        public static void Bencode(IList<object> list, Stream s)
        {
            s.WriteByte(ListMarker);

            foreach (var o in list)
            {
                Bencode(o, s);
            }

            s.WriteByte(EndMarker);
        }

        public static void Bencode(IDictionary<object, object> map, Stream s)
        {
            s.WriteByte(MapMarker);

            var keys = map.Keys;
            foreach (var key in keys)
            {
                Bencode((string)key, s);
                Bencode(map[key], s);
            }

            s.WriteByte(EndMarker);
        }

        public static void Bencode(object o, Stream s)
        {
            if (o is string)
            {
                Bencode((string)o, s);
            }
            else if (o is long)
            {
                Bencode((long)o, s);
            }
            else if (o is IList<object>)
            {
                Bencode((IList<object>)o, s);
            }
            else if (o is IDictionary<object, object>)
            {
                Bencode((IDictionary<object, object>)o, s);
            } else if(o is byte[])
            {
                Bencode((byte[])o, s);
            }
            else
            {
                throw new Exception("Type not supported: " + o.GetType());
            }

        }
    }
}