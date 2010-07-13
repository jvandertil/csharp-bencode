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
using System.Security.Cryptography;
using System.Text;

namespace BEncodeLib
{
    public class InfoHash
    {
        private byte[] _contents;
        private int _index;
        private readonly SHA1 _algo;

        public InfoHash()
        {
            _contents = new byte[1024];
            _algo = new SHA1Managed();
            _index = 0;
        }

        public void Update(byte c)
        {
            if (_index == _contents.Length)
                GrowContents();

            _contents[_index++] = c;
        }

        public void Update(byte[] array, int position, int length)
        {
            if ((_index + length) > _contents.Length)
                GrowContents(length);

            for (int i = position; i < length; i++)
            {
                _contents[_index++] = array[i];
            }
        }

        public byte[] Digest()
        {
            byte[] result = _algo.ComputeHash(_contents, 0, _index);
            return result;
        }

        private void GrowContents()
        {
            var newArray = new byte[_contents.Length * 2];

            Array.Copy(_contents, newArray, _index);

            _contents = newArray;
        }
        
        private void GrowContents(int minLength)
        {
            var newArray = new byte[minLength + (_contents.Length * 2)];

            Array.Copy(_contents, newArray, _index);

            _contents = newArray;
        }
    }
}