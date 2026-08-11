using MBRPN.Engine.Keys;

namespace MBRPN.Engine.History;

/// <summary>
/// One meaningful calculator interaction: a coalesced number entry and/or an operation.
/// <see cref="Keys"/> is the exact logical key sequence — replaying it against the
/// current calculator state reproduces the interaction. <see cref="Label"/> is generated
/// structurally from the keys (never parsed from the display). <see cref="ResultText"/>
/// is what the display showed after the original execution (null when the action
/// started a program run, whose result arrives asynchronously).
/// </summary>
public sealed record HistoryEntry(
    int Sequence,
    IReadOnlyList<KeyId> Keys,
    string Label,
    string? ResultText);
