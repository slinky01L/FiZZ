using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace FiZZ;

public class MainWindowModel(Action<string> showInfoToUser, Action<string> showErrorToUser) : ModelBase
{
    private const ushort ListeningPort = 6889;

    private readonly byte[] _peerId = PeerIdGenerator.GeneratePeerId();

    private byte[]? _infoHash;
    
    public async Task AddTorrentAsync(byte[] infoData, Torrent torrent)
    {
        _infoHash = HashUtility.ComputeHash(infoData);

        var trRequestParams = TrackerRequestBuilder.BuildTrackerRequestParameters(_infoHash, _peerId, ListeningPort, torrent.Info.TotalSize);
        
        var baseUri = TorrentUriParser.GetHttpAnnounceUri(torrent.Announce);
        if (baseUri is null && torrent.AnnounceList is not null)
        {
            foreach (var announce in torrent.AnnounceList)
            {
                baseUri = TorrentUriParser.GetHttpAnnounceUri(announce);
                if (baseUri is not null) break;
            }
        }

        if (baseUri is null)
        {
            showErrorToUser("Error: No valid HTTP/HTTPS tracker found in torrent metadata.");
            return;
        }

        var trRequestUri = TrackerRequestBuilder.BuildRequestUri(baseUri, trRequestParams);

        HttpClient httpClient = new();
        TrackerClient client = new(httpClient);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        var trackerResponse = new TrackerResponse();
        try
        {
            await using var responseStream = await client.SendAnnounceAsync(trRequestUri, cts.Token);
            
            using var ms = new MemoryStream();
            await responseStream.CopyToAsync(ms, cts.Token);
            
            var responseBytes = ms.ToArray();
            var bencode = new BencodeParser(responseBytes);
            
            var responseDict = bencode.Read();
            if (responseDict is null)
            {
                showErrorToUser("Failed to read bencode from tracker response");
                return;
            }
            
            if (!trackerResponse.Deserialize(responseDict))
            { 
                showErrorToUser("Failed to construct tracker response object");
                return;
            }
             
            showInfoToUser($"Found {trackerResponse.PeersList!.Count} peers, start download?");
        }
        catch (OperationCanceledException)
        {
            showErrorToUser("Tracker request timed out or was canceled.");
            return;
        }
        catch (HttpRequestException ex)
        {
            showErrorToUser($"HTTP request failed with status {ex.StatusCode}: {ex.Message}");
            return;
        }

        var peerList = trackerResponse.PeersList!;
        foreach (var peer in peerList)
        {
            var peerClient = new TcpClient();
            HandleNewClientConnectionAsync(peerClient);
        }
    }

    private async Task HandleNewClientConnectionAsync(TcpClient client)
    {
        
    }
    
    private void HandlePeerMessage(string peerId)
}