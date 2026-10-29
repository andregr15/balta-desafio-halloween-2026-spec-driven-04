namespace EncurtaUrl.Api.Contracts;

public record CreateShortUrlRequest(string Url, string? Code = null);

public record ShortUrlResponse(
    string Code,
    string ShortUrl,
    string OriginalUrl,
    DateTime CreatedAt,
    int Clicks);
