using System.Text.RegularExpressions;

namespace EncurtaUrl.Api.Services;

public interface ICustomCodeValidator
{
    string[] Validate(string code);
}

public partial class CustomCodeValidator : ICustomCodeValidator
{
    private const int MinLength = 3;
    private const int MaxLength = 15;

    // Primeiros segmentos de rota publicados pela API
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "urls",
        "openapi"
    };

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex AllowedFormat();

    public string[] Validate(string code)
    {
        var errors = new List<string>();

        if (code.Length < MinLength || code.Length > MaxLength)
            errors.Add($"O código deve ter entre {MinLength} e {MaxLength} caracteres.");

        if (!AllowedFormat().IsMatch(code))
            errors.Add("O código deve conter apenas letras, números, hífen e underscore.");

        if (Reserved.Contains(code))
            errors.Add("O código informado é reservado.");

        return [.. errors];
    }
}
