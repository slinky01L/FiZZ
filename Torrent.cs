using System.Security.Cryptography;

namespace FiZZ;

public class TorrentSingleFileInfo
{
    public long Length = 0;
    public string Md5 = string.Empty;
}

public class TorrentMultiFileInfo
{
    public long Length = 0;
    public string Md5 = string.Empty;
    public List<string> Path = [];
}

public class TorrentInfo
{
    public long PieceLength = 0;
    public string Pieces = string.Empty;
    public bool Private = false;
    
    public string Name = string.Empty;
    public TorrentSingleFileInfo? SingleFileInfo;
    public List<TorrentMultiFileInfo>? MultiFileInfo;
    
    public long TotalSize => SingleFileInfo?.Length ?? (MultiFileInfo?.Sum(f => f.Length) ?? 0);
}

public class Torrent
{
    public TorrentInfo Info;
    public string Announce = string.Empty;
    
    public List<string>? AnnounceList;
    public long CreationDate = 0;
    public string Comment = string.Empty;
    public string CreatedBy = string.Empty;
    public string Encoding = string.Empty;
}

public static class TorrentBuilder
{
    public static Torrent? BuildTorrent(BencodeDict dictionary)
    {
        var torrent = new Torrent();
        
        if (!dictionary.GetString("announce", out torrent.Announce)) return null;
        
        if (!dictionary.GetDict("info", out var infoDict)) return null;
        
        var info = BuildTorrentInfo(infoDict);
        if (info is null)
            return null;
        torrent.Info = info;
        
        if (dictionary.GetList("announce-list", out var announceList))
        {
            var list = BuildAnnounceList(announceList);
            if (list is not null) torrent.AnnounceList = list;
        }
        dictionary.GetLong("creation date", out torrent.CreationDate);
        dictionary.GetString("comment", out torrent.Comment);
        dictionary.GetString("created by", out torrent.CreatedBy);
        dictionary.GetString("encoding", out torrent.Encoding);

        return torrent;
    }

    private static List<string>? BuildAnnounceList(BencodeList lists)
    {
        List<string> announces = [];
        foreach (var list in lists)
        {
            if (list is not BencodeList announceList) return null;
            foreach (var announce in announceList)
            {
                if (announce is not string announceStr) continue;
                announces.Add(announceStr);
            }
        }
        return announces;
    }

    private static TorrentInfo? BuildTorrentInfo(BencodeDict infoDict)
    {
        var torrentInfo = new TorrentInfo();

        if (!infoDict.GetLong("piece length", out torrentInfo.PieceLength)) return null;
        if (!infoDict.GetString("pieces", out torrentInfo.Pieces)) return null;
        if (!infoDict.GetString("name", out torrentInfo.Name)) return null;

        var foundLength = infoDict.ContainsKey("length");
        var foundFiles = infoDict.ContainsKey("files");

        if (foundLength)
        {
            torrentInfo.SingleFileInfo = new TorrentSingleFileInfo();
            if (!infoDict.GetLong("length", out torrentInfo.SingleFileInfo.Length)) return null;
            if (!infoDict.GetString("md5sum", out torrentInfo.SingleFileInfo.Md5)) return null;
        }

        if (foundFiles)
        {
            if (!infoDict.GetList("files", out var list)) return null;
            
            torrentInfo.MultiFileInfo = [];
            
            foreach (var file in list)
            {
                if (file is not BencodeDict fileDict) return null;
                
                TorrentMultiFileInfo mfInfo = new();
                
                if (!fileDict.GetLong("length", out mfInfo.Length)) return null;
                if (!fileDict.GetList("path", out var pathList)) return null;
                foreach (var path in pathList)
                {
                    if (path is not string p) return null;
                    mfInfo.Path.Add(p);
                }
                fileDict.GetString("md5sum", out mfInfo.Md5);
                
                torrentInfo.MultiFileInfo.Add(mfInfo);
            }
        }
        
        infoDict.GetLong("private", out torrentInfo.PieceLength);

        return torrentInfo;
    }
}