using System.Globalization;
using System.Text.RegularExpressions;

namespace PlanYourFriday.IntegrationTests.Contract;

internal static partial class TsParser
{
    private static readonly string[] Primitives = ["string", "number", "boolean", "null", "unknown"];

    public static IReadOnlyDictionary<string, TsType> ParseDeclarations(string source)
    {
        var text = BlockComment().Replace(LineComment().Replace(source, string.Empty), string.Empty);
        var declarations = new Dictionary<string, TsType>(StringComparer.Ordinal);

        foreach (Match match in InterfaceStart().Matches(text))
        {
            var open = match.Index + match.Length - 1;
            var close = MatchingBrace(text, open);
            declarations[match.Groups["name"].Value] = ParseType(text[open..(close + 1)]);
        }

        foreach (Match match in TypeAlias().Matches(text))
        {
            declarations[match.Groups["name"].Value] = ParseType(match.Groups["body"].Value);
        }

        return declarations;
    }

    public static TsType ParseType(string expression)
    {
        var text = expression.Trim().TrimStart('|').Trim();

        var members = SplitTopLevel(text, '|');
        if (members.Count > 1)
        {
            return new TsUnion([.. members.Select(ParseType)]);
        }

        if (text.EndsWith("[]", StringComparison.Ordinal))
        {
            return new TsArray(ParseType(text[..^2]));
        }

        if (text.StartsWith('(') && MatchingBrace(text, 0) == text.Length - 1)
        {
            return ParseType(text[1..^1]);
        }

        if (text.StartsWith('{') && MatchingBrace(text, 0) == text.Length - 1)
        {
            return new TsObject([.. SplitMembers(text[1..^1]).Select(ParseProperty)]);
        }

        if (text.StartsWith('\'') || text.StartsWith('"'))
        {
            return new TsLiteral(text[1..^1]);
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return new TsLiteral(text);
        }

        return Primitives.Contains(text) ? new TsPrimitive(text) : new TsReference(text);
    }

    private static TsProperty ParseProperty(string member)
    {
        var colon = member.IndexOf(':', StringComparison.Ordinal);
        var name = member[..colon].Trim();
        var optional = name.EndsWith('?');
        return new TsProperty(optional ? name[..^1] : name, ParseType(member[(colon + 1)..]), optional);
    }

    private static List<string> SplitMembers(string body)
    {
        return
        [
            .. SplitTopLevel(body.Replace("\r", string.Empty, StringComparison.Ordinal), ';', '\n')
                .Select(m => m.Trim().TrimEnd(','))
                .Where(m => m.Length > 0)
        ];
    }

    private static List<string> SplitTopLevel(string text, params char[] separators)
    {
        var parts = new List<string>();
        var depth = 0;
        var quote = '\0';
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != '\0')
            {
                quote = c == quote ? '\0' : quote;
                continue;
            }

            switch (c)
            {
                case '\'' or '"':
                    quote = c;
                    break;
                case '{' or '[' or '(' or '<':
                    depth++;
                    break;
                case '}' or ']' or ')' or '>':
                    depth--;
                    break;
                default:
                    if (depth == 0 && separators.Contains(c))
                    {
                        parts.Add(text[start..i]);
                        start = i + 1;
                    }

                    break;
            }
        }

        parts.Add(text[start..]);
        return [.. parts.Where(p => p.Trim().Length > 0)];
    }

    private static int MatchingBrace(string text, int open)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] is '{' or '(')
            {
                depth++;
            }
            else if (text[i] is '}' or ')' && --depth == 0)
            {
                return i;
            }
        }

        throw new FormatException($"Unbalanced braces at position {open}.");
    }

    [GeneratedRegex(@"export\s+interface\s+(?<name>\w+)\s*\{")]
    private static partial Regex InterfaceStart();

    [GeneratedRegex(@"export\s+type\s+(?<name>\w+)\s*=(?<body>[^=]*?)(?=\n\s*\n|\nexport|\z)", RegexOptions.Singleline)]
    private static partial Regex TypeAlias();

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();
}
