using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace FiZZ;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed record TrackerRequestParameters(
    ReadOnlyMemory<byte> InfoHash,
    ReadOnlyMemory<byte> PeerId,
    ushort Port,
    long Uploaded,
    long Downloaded,
    long Left,
    bool Compact = true,
    bool NoPeerId = false,
    string? Event = null,
    string? Ip = null,
    long? NumWant = null,
    string? Key = null,
    string? TrackerId = null
);

public sealed record TrackerResponsePeerData(
    string? PeerId,
    IPAddress Ip,
    ushort Port
);

public class TrackerResponse : IBencodable
{
    public string? FailureReason;
    public string? WarningMessage;
    public long? Interval;
    public long? MinInterval;
    public string? TrackerId;
    public long? Complete;
    public long? Incomplete;
    public List<TrackerResponsePeerData>? PeersList;

    public bool Deserialize(BencodeDict dict)
    {
        if (dict.GetString("failure reason", out var failureReason))
        {
            FailureReason = failureReason;
            return true;
        }

        if (!dict.GetLong("interval", out var interval)) return false;

        var gotPeerDict = dict.GetList("peers", out var peersDictList);
        var gotPeerBin = dict.GetByteArray("peers", out var peersBin);

        if (!gotPeerBin && !gotPeerDict) return false;

        PeersList = [];

        if (gotPeerBin)
        {
            for (var i = 0; i < peersBin.Length; i += 6)
            {
                var peerSlice = peersBin.AsSpan(i, 6);
                var ipBytes = peerSlice[..4];
                var ip = new IPAddress(ipBytes);
                
                var portBytes = peerSlice.Slice(4, 2);
                var port = BinaryPrimitives.ReadUInt16BigEndian(portBytes);
                
                PeersList.Add(new TrackerResponsePeerData(PeerId: null, Ip: ip, Port: port));
            }
        }
        else if (gotPeerDict)
        {
            foreach (var peer in peersDictList)
            {
                if (peer is not BencodeDict peerDict) continue;
                
                if (!peerDict.GetString("peer id", out var peerId)) continue;
                if (!peerDict.GetString("ip", out var ipStr)) continue;
                if (!peerDict.GetLong("port", out var port)) continue;
                if (!IPAddress.TryParse(ipStr, out var ip)) continue;
                
                PeersList.Add(new TrackerResponsePeerData(PeerId: peerId, Ip: ip, Port: (ushort)port));
            }
        }

        if (dict.GetLong("complete", out var complete))
        {
            Complete = complete;
        }

        if (dict.GetLong("incomplete", out var incomplete))
        {
            Incomplete = incomplete;
        }

        if (dict.GetLong("min interval", out var minInterval))
        {
            MinInterval = minInterval;
        }

        if (dict.GetString("warning message", out var warningMessage))
        {
            WarningMessage = warningMessage;
        }
        
        if (dict.GetString("tracker id", out var trackerId))
        {
            TrackerId = trackerId;
        }

        return true;
    }
};

public static class TrackerRequestBuilder
{
    public static TrackerRequestParameters BuildTrackerRequestParameters(
        byte[] infoHash,
        byte[] peerId,
        ushort port,
        long left,
        long downloaded = 0,
        long uploaded = 0,
        string tEvent = "started",
        long numWant = 50)
    {
        return new TrackerRequestParameters(
            InfoHash: infoHash,
            PeerId: peerId,
            Port: port,
            Uploaded: uploaded,
            Downloaded: downloaded,
            Left: left,
            Event: tEvent,
            NumWant: numWant);
    }
    
    public static Uri BuildRequestUri(Uri baseTrackerUri, TrackerRequestParameters parameters)
    {
        var estimatedLength = 256 + baseTrackerUri.AbsoluteUri.Length;
        var sb = new StringBuilder(estimatedLength);

        sb.Append(baseTrackerUri.AbsoluteUri);
        sb.Append(baseTrackerUri.Query.Contains('?') ? '&' : '?');
        
        sb.Append("info_hash=");
        AppendPercentEncodedBytes(sb, parameters.InfoHash.Span);
        
        sb.Append("&peer_id=");
        AppendPercentEncodedBytes(sb, parameters.PeerId.Span);
        
        sb.Append("&port=").Append(parameters.Port);
        sb.Append("&uploaded=").Append(parameters.Uploaded);
        sb.Append("&downloaded=").Append(parameters.Downloaded);
        sb.Append("&left=").Append(parameters.Left);
        sb.Append("&compact=").Append(parameters.Compact ? 1 : 0);

        if (parameters.NoPeerId)
        {
            sb.Append("&no_peer_id=1");
        }

        if (!string.IsNullOrEmpty(parameters.Event))
        {
            sb.Append("&event=").Append(Uri.EscapeDataString(parameters.Event));
        }

        if (!string.IsNullOrEmpty(parameters.Ip))
        {
            sb.Append("&ip=").Append(Uri.EscapeDataString(parameters.Ip));
        }

        if (parameters.NumWant.HasValue)
        {
            sb.Append("&numwant=").Append(parameters.NumWant.Value);
        }

        if (!string.IsNullOrEmpty(parameters.Key))
        {
            sb.Append("&key=").Append(Uri.EscapeDataString(parameters.Key));
        }

        if (!string.IsNullOrEmpty(parameters.TrackerId))
        {
            sb.Append("&trackerid=").Append(Uri.EscapeDataString(parameters.TrackerId));
        }

        return new Uri(sb.ToString());
    }

    private static void AppendPercentEncodedBytes(StringBuilder sb, ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
        {
            sb.Append('%');
            sb.Append(GetHexChar(b >> 4));
            sb.Append(GetHexChar(b & 0x0F));
        }
    }

    private static char GetHexChar(int value) =>
        (char)(value < 10 ? '0' + value : 'A' + (value - 10));
}

public class TrackerClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    public async Task<Stream> SendAnnounceAsync(Uri trackerUri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, trackerUri);
        request.Headers.Add("User-Agent", "FiZZ/0.0.1");

        var response = await _httpClient.SendAsync(
            request, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken);

        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}