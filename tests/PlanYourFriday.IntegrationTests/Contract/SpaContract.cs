using System.Globalization;
using System.Text.Json;

namespace PlanYourFriday.IntegrationTests.Contract;

internal static class SpaContract
{
    private static readonly Lazy<IReadOnlyDictionary<string, TsType>> Declarations = new(Load);

    public static void Verify(JsonElement json, string type)
    {
        var errors = new List<string>();
        Check(json, TsParser.ParseType(type), "$", errors);
        Assert.True(errors.Count == 0, $"Response does not match `{type}` in web/src/api/types.ts:\n{string.Join('\n', errors)}");
    }

    private static void Check(JsonElement json, TsType type, string path, List<string> errors)
    {
        switch (type)
        {
            case TsReference reference:
                if (Declarations.Value.TryGetValue(reference.Name, out var declared))
                {
                    Check(json, declared, path, errors);
                }
                else
                {
                    errors.Add($"{path}: unknown type {reference.Name}");
                }

                break;
            case TsUnion union:
                if (!union.Members.Any(member => Matches(json, member, path)))
                {
                    errors.Add($"{path}: {Describe(json)} is none of {string.Join(" | ", union.Members.Select(Name))}");
                }

                break;
            case TsArray array:
                if (json.ValueKind != JsonValueKind.Array)
                {
                    errors.Add($"{path}: expected an array, got {Describe(json)}");
                    break;
                }

                var index = 0;
                foreach (var item in json.EnumerateArray())
                {
                    Check(item, array.Item, $"{path}[{index++}]", errors);
                }

                break;
            case TsObject shape:
                CheckObject(json, shape, path, errors);
                break;
            case TsLiteral literal:
                var actual = json.ValueKind switch
                {
                    JsonValueKind.String => json.GetString(),
                    JsonValueKind.Number => json.GetDouble().ToString(CultureInfo.InvariantCulture),
                    _ => null,
                };
                if (actual != literal.Value)
                {
                    errors.Add($"{path}: expected '{literal.Value}', got {Describe(json)}");
                }

                break;
            case TsPrimitive primitive when !MatchesPrimitive(json, primitive.Name):
                errors.Add($"{path}: expected {primitive.Name}, got {Describe(json)}");
                break;
        }
    }

    private static void CheckObject(JsonElement json, TsObject shape, string path, List<string> errors)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{path}: expected an object, got {Describe(json)}");
            return;
        }

        var expected = shape.Properties.ToDictionary(p => p.Name, StringComparer.Ordinal);
        foreach (var property in json.EnumerateObject())
        {
            if (!expected.ContainsKey(property.Name))
            {
                errors.Add($"{path}.{property.Name}: not declared in the SPA type");
            }
        }

        foreach (var property in shape.Properties)
        {
            if (json.TryGetProperty(property.Name, out var value))
            {
                Check(value, property.Type, $"{path}.{property.Name}", errors);
            }
            else if (!property.Optional)
            {
                errors.Add($"{path}.{property.Name}: missing");
            }
        }
    }

    private static bool Matches(JsonElement json, TsType type, string path)
    {
        var errors = new List<string>();
        Check(json, type, path, errors);
        return errors.Count == 0;
    }

    private static bool MatchesPrimitive(JsonElement json, string name)
    {
        return name switch
        {
            "string" => json.ValueKind == JsonValueKind.String,
            "number" => json.ValueKind == JsonValueKind.Number,
            "boolean" => json.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => json.ValueKind == JsonValueKind.Null,
            _ => true,
        };
    }

    private static string Name(TsType type)
    {
        return type switch
        {
            TsReference reference => reference.Name,
            TsPrimitive primitive => primitive.Name,
            TsLiteral literal => $"'{literal.Value}'",
            TsArray array => $"{Name(array.Item)}[]",
            _ => "{...}",
        };
    }

    private static string Describe(JsonElement json)
    {
        var text = json.ValueKind == JsonValueKind.Undefined ? "nothing" : json.GetRawText();
        return text.Length > 80 ? text[..80] + "..." : text;
    }

    private static IReadOnlyDictionary<string, TsType> Load()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PlanYourFriday.slnx")))
        {
            directory = directory.Parent;
        }

        var path = Path.Combine(
            directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."),
            "web", "src", "api", "types.ts");
        return TsParser.ParseDeclarations(File.ReadAllText(path));
    }
}
