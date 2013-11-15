using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BEncodeLib.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            TorrentFile torrent;

            using (var stream = new FileStream("ubuntu-13.10-desktop-amd64.iso.torrent", FileMode.Open))
            using (var decoded = new TorrentBDecoder(stream, Encoding.UTF8))
            {
                var torrentAsDictionary = decoded.Decode() as Dictionary<object, object>;

                torrent = new TorrentFile(torrentAsDictionary, decoded.GetInfoHash());
            }

            if (torrent.IsMultiAnnounce)
            {
                // Trackers are grouped.
                foreach (var trackerList in torrent.AnnounceList)
                {
                    foreach(var tracker in trackerList)
                        Console.WriteLine("Tracker: " + tracker);
                }
            }
            else
            {
                Console.WriteLine("Tracker: " + torrent.Announce);
            }
        }
    }
}
