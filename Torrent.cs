namespace FiZZ;

public class BencodeDict : Dictionary<string, object>
{
    public string? GetString(string key)
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

    public int? GetInt(string key)
    {
        return TryGetValue(key, out var value) && value is int i
            ? i
            : null;
    }

    public bool GetInt(string key, out int value)
    {
        value = 0;
        var v = GetInt(key);
        if (v is null)
            return false;
        value = v.Value;
        return true;
    }

    public BencodeList? GetList(string key)
    {
        return TryGetValue(key, out var value) && value is BencodeList l
            ? l
            : null;
    }

    public BencodeDict? GetDict(string key)
    {
        return TryGetValue(key, out var value) && value is BencodeDict d
            ? d
            : null;
    }
}

public class BencodeList : List<object>;

public class TorrentInfo
{
    public int PieceLength = 0;
    public string Pieces = string.Empty;
    public bool Private = false;
    public string Name = string.Empty;
    public int Length = 0;
    public string Md5 = string.Empty;
}

public class Torrent
{
    public TorrentInfo Info;
    public string Announce = string.Empty;
}

public static class TorrentBuilder
{
    public static Torrent? BuildTorrent(BencodeDict dictionary)
    {
        var torrent = new Torrent();

        if (!dictionary.GetString("announce", out torrent.Announce)) return null;

        var infoDict = dictionary.GetDict("info");
        if (infoDict is null)
            return null;
        
        var info = BuildTorrentInfo(infoDict);
        if (info is null)
            return null;
        
        torrent.Info = info;

        return torrent;
    }

    private static TorrentInfo? BuildTorrentInfo(BencodeDict infoDict)
    {
        var torrentInfo = new TorrentInfo();

        if (!infoDict.GetInt("piece length", out torrentInfo.PieceLength))
        {
            return null;
        }

        if (!infoDict.GetString("pieces", out torrentInfo.Pieces))
        {
            return null;
        }

        if (!infoDict.GetString("name", out torrentInfo.Name))
        {
            return null;
        }

        if (!infoDict.GetInt("length", out torrentInfo.Length))
        {
            return null;
        }

        infoDict.GetString("md5sum", out torrentInfo.Md5);
        infoDict.GetInt("private", out torrentInfo.PieceLength);

        return torrentInfo;
    }
}