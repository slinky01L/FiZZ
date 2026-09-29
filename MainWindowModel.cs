using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;

namespace FiZZ;

public class MainWindowModel(Action<string> showInfoToUser, Action<string> showErrorToUser) : ModelBase
{
    private const ushort ListeningPort = 6889;

    private Torrent? _torrent;

    private readonly byte[] _peerId = PeerIdGenerator.GeneratePeerId();

    public Torrent? Torrent
    {
        get => _torrent;
        set { _torrent = value; NotifyPropertyChanged(); }
    }

    public async Task AddTorrent(byte[] infoData, Torrent torrent)
    {
        _torrent = torrent;

        var infoHash = HashUtility.ComputeHash(infoData);

        var trRequestParams = TrackerRequestBuilder.BuildTrackerRequestParameters(infoHash, _peerId, ListeningPort, _torrent.Info.TotalSize);
        
        var baseUri = TorrentUriParser.GetHttpAnnounceUri(_torrent.Announce);
        if (baseUri is null && _torrent.AnnounceList is not null)
        {
            foreach (var announce in _torrent.AnnounceList)
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

            var response = new TrackerResponse();
            if (!response.Deserialize(responseDict))
            { 
                showErrorToUser("Failed to construct tracker response object");
                return;
            }
             
            showInfoToUser($"Found {response.PeersList!.Count} peers");
        }
        catch (OperationCanceledException)
        {
            showErrorToUser("Tracker request timed out or was canceled.");
        }
        catch (HttpRequestException ex)
        {
            showErrorToUser($"HTTP request failed with status {ex.StatusCode}: {ex.Message}");
        }
    }
}