using System.Text.Json;
using Capitec.Dispute.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Capitec.Dispute.Infrastructure.Services;

public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TranslationService> _logger;

    // Common ISO 639-1 codes returned by MyMemory → display name
    private static readonly Dictionary<string, string> LanguageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AF"] = "Afrikaans",
        ["SQ"] = "Albanian",
        ["AM"] = "Amharic",
        ["AR"] = "Arabic",
        ["HY"] = "Armenian",
        ["AZ"] = "Azerbaijani",
        ["EU"] = "Basque",
        ["BE"] = "Belarusian",
        ["BN"] = "Bengali",
        ["BS"] = "Bosnian",
        ["BG"] = "Bulgarian",
        ["CA"] = "Catalan",
        ["CEB"] = "Cebuano",
        ["ZH"] = "Chinese",
        ["CO"] = "Corsican",
        ["HR"] = "Croatian",
        ["CS"] = "Czech",
        ["DA"] = "Danish",
        ["NL"] = "Dutch",
        ["EO"] = "Esperanto",
        ["ET"] = "Estonian",
        ["FI"] = "Finnish",
        ["FR"] = "French",
        ["FY"] = "Frisian",
        ["GL"] = "Galician",
        ["KA"] = "Georgian",
        ["DE"] = "German",
        ["EL"] = "Greek",
        ["GU"] = "Gujarati",
        ["HT"] = "Haitian Creole",
        ["HA"] = "Hausa",
        ["HI"] = "Hindi",
        ["HU"] = "Hungarian",
        ["IS"] = "Icelandic",
        ["IG"] = "Igbo",
        ["ID"] = "Indonesian",
        ["GA"] = "Irish",
        ["IT"] = "Italian",
        ["JA"] = "Japanese",
        ["JV"] = "Javanese",
        ["KN"] = "Kannada",
        ["KK"] = "Kazakh",
        ["KM"] = "Khmer",
        ["KO"] = "Korean",
        ["KU"] = "Kurdish",
        ["KY"] = "Kyrgyz",
        ["LO"] = "Lao",
        ["LA"] = "Latin",
        ["LV"] = "Latvian",
        ["LT"] = "Lithuanian",
        ["LB"] = "Luxembourgish",
        ["MK"] = "Macedonian",
        ["MG"] = "Malagasy",
        ["MS"] = "Malay",
        ["ML"] = "Malayalam",
        ["MT"] = "Maltese",
        ["MI"] = "Maori",
        ["MR"] = "Marathi",
        ["MN"] = "Mongolian",
        ["MY"] = "Myanmar",
        ["NE"] = "Nepali",
        ["NO"] = "Norwegian",
        ["NY"] = "Nyanja",
        ["OR"] = "Odia",
        ["PS"] = "Pashto",
        ["FA"] = "Persian",
        ["PL"] = "Polish",
        ["PT"] = "Portuguese",
        ["PA"] = "Punjabi",
        ["RO"] = "Romanian",
        ["RU"] = "Russian",
        ["SM"] = "Samoan",
        ["GD"] = "Scottish Gaelic",
        ["SR"] = "Serbian",
        ["ST"] = "Sesotho",
        ["SN"] = "Shona",
        ["SD"] = "Sindhi",
        ["SI"] = "Sinhala",
        ["SK"] = "Slovak",
        ["SL"] = "Slovenian",
        ["SO"] = "Somali",
        ["ES"] = "Spanish",
        ["SU"] = "Sundanese",
        ["SW"] = "Swahili",
        ["SV"] = "Swedish",
        ["TL"] = "Filipino",
        ["TG"] = "Tajik",
        ["TA"] = "Tamil",
        ["TT"] = "Tatar",
        ["TE"] = "Telugu",
        ["TH"] = "Thai",
        ["TR"] = "Turkish",
        ["TK"] = "Turkmen",
        ["UK"] = "Ukrainian",
        ["UR"] = "Urdu",
        ["UG"] = "Uyghur",
        ["UZ"] = "Uzbek",
        ["VI"] = "Vietnamese",
        ["CY"] = "Welsh",
        ["XH"] = "Xhosa",
        ["YI"] = "Yiddish",
        ["YO"] = "Yoruba",
        ["ZU"] = "Zulu",
    };

    public TranslationService(HttpClient httpClient, ILogger<TranslationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(string? TranslatedText, string? SourceLanguage)> TranslateToEnglishAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);

        try
        {
            // Google Translate unofficial endpoint — returns translation + detected language code.
            // Response: [[[seg0, orig0,...], ...], null, "detected-lang-code", ...]
            var query = text.Length > 500 ? text[..500] : text;
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={Uri.EscapeDataString(query)}";

            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Concatenate translated segments from root[0][i][0]
            var sb = new System.Text.StringBuilder();
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                foreach (var seg in root[0].EnumerateArray())
                    if (seg.ValueKind == JsonValueKind.Array && seg.GetArrayLength() > 0)
                        sb.Append(seg[0].GetString());
            }

            var translated = sb.ToString().Trim();

            // Detected language is root[2]
            string? detectedCode = null;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 2
                && root[2].ValueKind == JsonValueKind.String)
                detectedCode = root[2].GetString();

            if (!string.IsNullOrWhiteSpace(translated) && translated != text)
            {
                var normCode = NormaliseCode(detectedCode);

                // Google frequently misidentifies Afrikaans as Dutch (nl) due to linguistic similarity.
                // In a South African banking context, remap nl → af.
                if (normCode == "NL") normCode = "AF";

                var displayName = normCode != null && LanguageNames.TryGetValue(normCode, out var name)
                    ? name
                    : normCode;
                return (translated, displayName);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Translation API call failed — original summary will be used");
            return (null, null);
        }
    }

    /// Strip region suffixes like "-GB" from "EN-GB" and return the base code in upper-case.
    private static string? NormaliseCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var dash = code.IndexOf('-');
        return (dash > 0 ? code[..dash] : code).ToUpperInvariant();
    }
}
