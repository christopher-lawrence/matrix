namespace Matrix.Core.Protocol;

public sealed record HelpData(IReadOnlyList<HelpCommandData> Commands);

public sealed record HelpCommandData(string Type, string Description, string Example);
