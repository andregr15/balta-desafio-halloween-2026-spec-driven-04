using EncurtaUrl.Api.Contracts;
using EncurtaUrl.Api.Data;
using EncurtaUrl.Api.Models;
using EncurtaUrl.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EncurtaUrl.Api.Endpoints;

public static class ShortUrlEndpoints
{
    private const int MaxGenerationAttempts = 5;

    public static IEndpointRouteBuilder MapShortUrlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/urls").WithTags("URLs");

        group.MapPost("/", CreateAsync).WithName("CreateShortUrl");
        group.MapGet("/", ListAsync).WithName("ListShortUrls");
        group.MapGet("/{code}", GetByCodeAsync).WithName("GetShortUrl");

        // Navegação: GET /{code} redireciona para a URL original
        app.MapGet("/{code}", RedirectAsync)
            .WithName("RedirectToOriginalUrl")
            .WithTags("Redirect");

        return app;
    }

    private static async Task<Results<Created<ShortUrlResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CreateShortUrlRequest request,
        AppDbContext db,
        IShortCodeGenerator generator,
        ICustomCodeValidator validator,
        HttpRequest http,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (!IsValidUrl(request.Url))
            errors["url"] = ["Informe uma URL absoluta válida, começando com http:// ou https://."];

        // Código em branco é tratado como não informado
        var customCode = !string.IsNullOrWhiteSpace(request.Code) ? request.Code : null;

        if (customCode is not null)
        {
            var codeErrors = validator.Validate(customCode);
            if (codeErrors.Length > 0)
                errors["code"] = codeErrors;
        }

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        string? code = null;

        if (customCode is not null)
        {
            if (await db.ShortUrls.AnyAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == customCode, ct))
                return CodeInUse(customCode);

            code = customCode;
        }
        else
        {
            for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                var candidate = generator.Generate();
                if (!await db.ShortUrls.AnyAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == candidate, ct))
                {
                    code = candidate;
                    break;
                }
            }
        }

        if (code is null)
        {
            return TypedResults.Problem(
                title: "Não foi possível gerar um código único.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var shortUrl = new ShortUrl
        {
            Code = code,
            OriginalUrl = request.Url
        };

        db.ShortUrls.Add(shortUrl);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (customCode is not null
            && ex.InnerException is SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 })
        {
            // Corrida: outra requisição gravou o mesmo código entre a checagem e o SaveChanges
            return CodeInUse(customCode);
        }

        var response = ToResponse(shortUrl, http);
        return TypedResults.Created($"/urls/{shortUrl.Code}", response);
    }

    private static async Task<Ok<List<ShortUrlResponse>>> ListAsync(
        AppDbContext db,
        HttpRequest http,
        CancellationToken ct)
    {
        var items = await db.ShortUrls
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return TypedResults.Ok(items.Select(x => ToResponse(x, http)).ToList());
    }

    private static async Task<Results<Ok<ShortUrlResponse>, NotFound>> GetByCodeAsync(
        string code,
        AppDbContext db,
        HttpRequest http,
        CancellationToken ct)
    {
        var shortUrl = await db.ShortUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == code, ct);

        return shortUrl is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(shortUrl, http));
    }

    private static async Task<Results<RedirectHttpResult, NotFound>> RedirectAsync(
        string code,
        AppDbContext db,
        CancellationToken ct)
    {
        var shortUrl = await db.ShortUrls.FirstOrDefaultAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == code, ct);

        if (shortUrl is null)
            return TypedResults.NotFound();

        shortUrl.Clicks++;
        await db.SaveChangesAsync(ct);

        return TypedResults.Redirect(shortUrl.OriginalUrl);
    }

    private static ValidationProblem CodeInUse(string code)
        => TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["code"] = [$"O código '{code}' já está em uso."]
        });

    private static bool IsValidUrl(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static ShortUrlResponse ToResponse(ShortUrl x, HttpRequest http)
        => new(
            x.Code,
            $"{http.Scheme}://{http.Host}/{x.Code}",
            x.OriginalUrl,
            x.CreatedAt,
            x.Clicks);
}
