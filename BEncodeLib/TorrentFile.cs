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
using System.Linq;
using System.Text;

namespace BEncodeLib
{
    public class TorrentFile
    {
        private const int PieceHashLength = 20;
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        private static readonly DateTime EpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        #region Torrent File Dictionary Keys

        private const string AnnounceKey = "announce";
        private const string MultiAnnounceKey = "announce-list";
        private const string CreationDateKey = "creation date";
        private const string CommentKey = "comment";
        private const string CreatedByKey = "created by";
        private const string InfoDictionaryKey = "info";
        private const string EncodingKey = "encoding"; //UNUSED, included for completeness

        #region Info Dictionary Keys

        private const string PieceLengthKey = "piece length";
        private const string PiecesKey = "pieces";
        private const string PrivateKey = "private";

        #endregion

        #region Single File Info Dictionary Keys

        private const string FileNameKey = "name";
        private const string FileSizeKey = "length";

        #endregion

        #region Multi File Info Dictionary Keys

        private const string FilesKey = "files";
        private const string DirNameKey = "name";
        private const string FilePathKey = "path";

        #endregion

        #endregion

        public TorrentFile(IDictionary<object, object> bareDictionary, byte[] infoHash)
        {
            InfoHash = infoHash;

            IsMultiAnnounce = bareDictionary.ContainsKey(MultiAnnounceKey);

            if (!IsMultiAnnounce)
            {
                Announce = DefaultEncoding.GetString((byte[]) bareDictionary[AnnounceKey]);
            }
            else
            {
                AnnounceList = new List<IList<string>>();
                var announceList = bareDictionary[MultiAnnounceKey] as IList<object>;

                /*
                 * Functionality of query below:
                foreach (object t in announceList)
                {
                    var sourceList = t as IList<object>;
                    var resultList = new List<string>();
                    
                    for(int j = 0; j < sourceList.Count; j++)
                    {
                        resultList.Add(DefaultEncoding.GetString(sourceList[j] as byte[]));
                    }

                    AnnounceList.Add(resultList);
                }
                 */
                foreach (var resultList in
                    announceList.Select(t => t as IList<object>)
                        .Select(
                            sourceList =>
                            sourceList.Select(
                                sourceListElement => DefaultEncoding.GetString(sourceListElement as byte[])).ToList()))
                {
                    AnnounceList.Add(resultList);
                }
            }

            HasCreationDate = bareDictionary.ContainsKey(CreationDateKey);

            if (HasCreationDate)
            {
                CreationDate = EpochDateTime.AddSeconds((long) bareDictionary[CreationDateKey]);
            }

            var info = bareDictionary["info"] as IDictionary<object, object>;

            PieceSize = (long) info[PieceLengthKey];
            Pieces = (byte[]) info[PiecesKey];
            IsPrivate = (info.ContainsKey(PrivateKey)) && ((long) info[PrivateKey] == 1);

            IsMultiFile = info.ContainsKey(FilesKey);

            if (IsMultiFile)
            {
                DirectoryName = DefaultEncoding.GetString(info[DirNameKey] as byte[]);

                Files = new List<TorrentFileFileEntry>();
                foreach (var objDic in (info[FilesKey] as IList<object>).Cast<IDictionary<object, object>>())
                {
                    var fileSize = (long) objDic[FileSizeKey];
                    List<string> pathList = (from pathObj in (objDic[FilePathKey] as IList<object>)
                                             select DefaultEncoding.GetString(pathObj as byte[])).ToList();

                    Files.Add(new TorrentFileFileEntry(fileSize, pathList));
                }
            }
            else
            {
                FileName = DefaultEncoding.GetString(info[FileNameKey] as byte[]);
                FileSize = (long) info[FileSizeKey];
            }

            HasComment = bareDictionary.ContainsKey(CommentKey);
            if (HasComment)
            {
                Comment = DefaultEncoding.GetString(bareDictionary[CommentKey] as byte[]);
            }

            HasCreatedBy = bareDictionary.ContainsKey(CreatedByKey);
            if (HasCreatedBy)
            {
                CreatedBy = DefaultEncoding.GetString(bareDictionary[CreatedByKey] as byte[]);
            }
        }

        public byte[] InfoHash { get; protected set; }

        public bool IsMultiAnnounce { get; protected set; }
        public IList<IList<string>> AnnounceList { get; protected set; }

        public string Announce { get; protected set; }

        public bool HasCreationDate { get; protected set; }
        public DateTime CreationDate { get; protected set; }

        public bool HasCreatedBy { get; protected set; }
        public string CreatedBy { get; protected set; }

        public bool HasComment { get; protected set; }
        public string Comment { get; protected set; }

        public bool IsMultiFile { get; protected set; }

        #region Single File & Multi File

        public long PieceSize { get; protected set; }

        protected byte[] Pieces { get; set; }

        public bool IsPrivate { get; protected set; }

        #endregion

        #region Single File

        private string _fileName;

        private long _fileSize;

        public string FileName
        {
            get { return AssertIsSingleFile() ? _fileName : null; }
            set { _fileName = value; }
        }

        public long FileSize
        {
            get { return AssertIsSingleFile() ? _fileSize : -1; }

            set { _fileSize = value; }
        }

        private bool AssertIsSingleFile()
        {
            if (IsMultiFile)
                throw new InvalidOperationException("Not available on torrents with multiple files.");

            return true;
        }

        #endregion

        #region Multi File

        private string _directoryName;

        private IList<TorrentFileFileEntry> _files;

        public string DirectoryName
        {
            get { return AssertIsMultiFile() ? _directoryName : null; }

            protected set { _directoryName = value; }
        }

        public IList<TorrentFileFileEntry> Files
        {
            get { return AssertIsMultiFile() ? _files : null; }

            protected set { _files = value; }
        }

        private bool AssertIsMultiFile()
        {
            if (!IsMultiFile)
                throw new InvalidOperationException("Not available on torrents with a single file.");

            return true;
        }

        #endregion

        #region BEncode Support

        public IDictionary<object, object> AsDictionary()
        {
            var resultDictionary = new Dictionary<object, object>();

            if (IsMultiAnnounce)
            {
                List<object> list = AnnounceList.Select(anList => anList.Cast<object>().ToList())
                    .Cast<object>()
                    .ToList();

                resultDictionary.Add(MultiAnnounceKey, list);
            }
            else
            {
                resultDictionary.Add(AnnounceKey, Announce);
            }

            if (HasCreationDate)
            {
                TimeSpan t = CreationDate - EpochDateTime;
                resultDictionary.Add(CreationDateKey, (long) t.TotalSeconds);
            }

            if (HasComment)
            {
                resultDictionary.Add(CommentKey, Comment);
            }

            if (HasCreatedBy)
            {
                resultDictionary.Add(CreatedByKey, CreatedBy);
            }

            var infoDictionary = new Dictionary<object, object>();

            if (IsMultiFile)
            {
                List<object> fileList = Files.Select(file => file.ToDictionary())
                                             .Cast<object>()
                                             .ToList();

                infoDictionary.Add(FilesKey, fileList);
                infoDictionary.Add(DirNameKey, DirectoryName);
            }
            else
            {
                infoDictionary.Add(FileSizeKey, FileSize);
                infoDictionary.Add(FileNameKey, FileName);
            }

            infoDictionary.Add(PieceLengthKey, PieceSize);
            infoDictionary.Add(PiecesKey, Pieces);
            infoDictionary.Add(PrivateKey, (IsPrivate ? 1L : 0L));

            resultDictionary.Add(InfoDictionaryKey, infoDictionary);

            return resultDictionary;
        }

        #endregion

        #region Nested type: TorrentFileFileEntry

        public class TorrentFileFileEntry
        {
            public TorrentFileFileEntry(long fileSize, IList<string> path)
            {
                FileSize = fileSize;
                Path = path;
            }

            public long FileSize { get; protected set; }

            public IList<string> Path { get; protected set; }

            public IDictionary<object, object> ToDictionary()
            {
                return new Dictionary<object, object>
                           {
                               {FileSizeKey, FileSize},
                               {FilePathKey, Path.Cast<object>().ToList()}
                           };
            }
        }

        #endregion
    }
}