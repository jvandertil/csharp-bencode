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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BEncodeLib
{
    public class TorrentBDecoder : IDisposable
    {
        private const string InfoMapKey = "info";
        private readonly InfoHash _infoHash;
        private readonly BinaryReader _reader;

        private bool _inInfoMap;
        private byte _indicator;
        private Encoding _streamEncoding;

        public TorrentBDecoder(Stream stream, Encoding encoding)
        {
            _reader = new BinaryReader(stream);
            _streamEncoding = encoding;
            _infoHash = new InfoHash();
            _indicator = 0;
            _inInfoMap = false;
        }

        public byte[] GetInfoHash()
        {
            return _infoHash.Digest();
        }

        #region IDisposable Members

        public void Dispose()
        {
            _reader.Close();
            _streamEncoding = null;
        }

        #endregion

        public object Decode()
        {
            _indicator = GetNextIndicator();

            if (_indicator == -1)
            {
                return null;
            }

            if (_indicator >= '0' && _indicator <= '9')
            {
                return DecodeByteString();
            }
            else if (_indicator == 'i')
            {
                return DecodeLong();
            }
            else if (_indicator == 'l')
            {
                return DecodeList();
            }
            else if (_indicator == 'd')
            {
                return DecodeDictionary();
            }
            else
            {
                throw new ArgumentException(string.Format("Unknown bencoded type: '{0}'", (char) _indicator));
            }
        }

        private byte GetNextIndicator()
        {
            if (_indicator == 0)
            {
                _indicator = _reader.ReadByte();

                if (_inInfoMap)
                    _infoHash.Update((byte) _indicator);
            }

            return _indicator;
        }

        private int Read()
        {
            var c = _reader.ReadByte();

            if (c == -1)
                throw new EndOfStreamException();

            if (_inInfoMap)
                _infoHash.Update(c);

            return c;
        }

        private byte[] Read(int length)
        {
            var result = _reader.ReadBytes(length);

            if (_inInfoMap)
                _infoHash.Update(result, 0, length);

            return result;
        }

        private long DecodeLong()
        {
            int c = GetNextIndicator();

            if (c != 'i')
                throw new FormatException("Expected 'i', not '" + (char) c + "'");

            _indicator = 0;

            c = Read();
            if (c == '0')
            {
                c = Read();
                if (c == 'e')
                    return 0;
                else
                    throw new FormatException("'e' expected after zero," + " not '" + (char) c + "'");
            }

            var chars = new char[256];
            int off = 0;

            if (c == '-')
            {
                c = Read();
                if (c == '0')
                    throw new FormatException("Negative zero not allowed");
                chars[off] = (char) c;
                off++;
            }

            if (c < '1' || c > '9')
                throw new FormatException("Invalid Integer start '" + (char) c + "'");

            chars[off] = (char) c;
            off++;

            c = Read();
            int i = c - '0';
            while (i >= 0 && i <= 9)
            {
                chars[off] = (char) c;
                off++;
                c = Read();
                i = c - '0';
            }

            if (c != 'e')
                throw new FormatException("Integer should end with 'e'");

            var s = new String(chars, 0, off);

            return long.Parse(s);
        }

        private string DecodeString()
        {
            return _streamEncoding.GetString(DecodeByteString());
        }

        private byte[] DecodeByteString()
        {
            int c = GetNextIndicator();
            int num = c - '0';

            if (num < 0 || num > 9)
                throw new FormatException(string.Format("Number expected, '{0}' received", (char) c));

            _indicator = 0;

            c = Read();
            int i = c - '0';
            while (i >= 0 && i <= 9)
            {
                // XXX - This can overflow!
                num = num*10 + i;
                c = Read();
                i = c - '0';
            }

            if (c != ':')
                throw new FormatException("Colon expected, not '" + (char) c + "'");

            return Read(num);
        }

        private IList<object> DecodeList()
        {
            int c = GetNextIndicator();

            if (c != 'l')
                throw new FormatException("Expected 'l', not '"
                                          + (char) c + "'");
            _indicator = 0;

            var result = new List<object>();

            c = GetNextIndicator();

            while (c != 'e')
            {
                result.Add(Decode());
                c = GetNextIndicator();
            }

            _indicator = 0;

            return result;
        }

        private IDictionary<object, object> DecodeDictionary()
        {
            int c = GetNextIndicator();
            if (c != 'd')
                throw new FormatException("Expected 'd', not '" + (char) c + "'");
            _indicator = 0;

            var result = new Dictionary<object, object>();
            c = GetNextIndicator();

            while (c != 'e')
            {
                // Dictionary keys are always strings.
                var key = _streamEncoding.GetString((byte[]) Decode());

                bool isInfoMap = InfoMapKey == key;

                if (isInfoMap)
                    _inInfoMap = true;

                object value = Decode();
                result.Add(key, value);

                if (isInfoMap)
                    _inInfoMap = false;

                c = GetNextIndicator();
            }

            _indicator = 0;

            return result;
        }
    }
}