using System.Net;

public class ProgressableContent : HttpContent
{
    private readonly HttpContent _content;
    private readonly IProgress<long> _progress;

    public ProgressableContent(HttpContent content, IProgress<long> progress)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));

        foreach (var header in _content.Headers)
        {
            Headers.Add(header.Key, header.Value);
        }
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var buffer = new byte[81920]; // Increased buffer size to 80 KB
        TryComputeLength(out long size);
        var uploaded = 0L;

        using (var contentStream = await _content.ReadAsStreamAsync())
        {
            while (true)
            {
                var length = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (length <= 0) break;

                await stream.WriteAsync(buffer, 0, length);
                uploaded += length;
                _progress.Report(uploaded);
            }
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _content.Headers.ContentLength ?? -1;
        return length >= 0;
    }
}
