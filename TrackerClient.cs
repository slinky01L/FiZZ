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
    string PeerId,
    string Ip,
    string Port
);

public sealed record TrackerResponse(
    string FailureReason,
    string? WarningMessage,
    long Interval,
    long? MinInterval,
    string TrackerId,
    long Complete,
    long Incomplete,
    List<TrackerResponsePeerData> PeersDictList,
    byte[] PeersBytes
);

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