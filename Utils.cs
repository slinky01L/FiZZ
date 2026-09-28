using System.Security.Cryptography;

namespace FiZZ;

public static class HashUtility
{
    public static byte[] ComputeHash(ReadOnlySpan<byte> bytes)
    {
        var hash = new byte[20];
        SHA1.HashData(bytes, hash);
        return hash;
    }
}

public static class PeerIdGenerator
{
    private const int PeerIdSize = 20;

    public static byte[] GeneratePeerId()
    {
        var prefix = "-FiZZ001-"u8;
        
        var peerId = new byte[PeerIdSize];
        prefix.CopyTo(peerId);
        
        RandomNumberGenerator.Fill(peerId.AsSpan(prefix.Length, PeerIdSize - prefix.Length));

        return peerId;
    }
}

public static class TorrentUriParser
{
    public static Uri? GetHttpAnnounceUri(string announceString)
    {
        if (string.IsNullOrWhiteSpace(announceString))
            return null;
        
        if (!Uri.TryCreate(announceString.Trim(), UriKind.Absolute, out var parsedUri))
        {
            return null;
        }
        
        if (!parsedUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !parsedUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null; 
        }
        
        var builder = new UriBuilder(parsedUri)
        {
            Query = string.Empty
        };

        return builder.Uri;
    }
}