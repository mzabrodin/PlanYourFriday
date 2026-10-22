namespace PlanYourFriday.IntegrationTests.Contract;

internal abstract record TsType;

internal sealed record TsPrimitive(string Name) : TsType;

internal sealed record TsLiteral(string Value) : TsType;

internal sealed record TsArray(TsType Item) : TsType;

internal sealed record TsUnion(IReadOnlyList<TsType> Members) : TsType;

internal sealed record TsObject(IReadOnlyList<TsProperty> Properties) : TsType;

internal sealed record TsReference(string Name) : TsType;

internal sealed record TsProperty(string Name, TsType Type, bool Optional);
