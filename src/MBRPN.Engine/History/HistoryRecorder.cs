using MBRPN.Engine.Core;
using MBRPN.Engine.Keys;

namespace MBRPN.Engine.History;

/// <summary>
/// Records meaningful interactions from the live key pipeline. Digit-entry keys
/// accumulate in a pending buffer; the action that terminates the entry flushes one
/// combined <see cref="HistoryEntry"/> ("100 ENTER", "25 +"). Program-mode keystrokes
/// and program execution never reach this class.
/// </summary>
public sealed class HistoryRecorder
{
    public const int Capacity = 200;

    private readonly List<HistoryEntry> _entries = new();
    private readonly List<KeyId> _pendingEntryKeys = new();
    private int _nextSequence = 1;

    /// <summary>Oldest → newest. The UI shows it reversed.</summary>
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>Raised when an entry is added or the list is cleared — not per keystroke.</summary>
    public event Action? Changed;

    public void Clear()
    {
        _entries.Clear();
        _pendingEntryKeys.Clear();
        Changed?.Invoke();
    }

    /// <summary>Drops pending number-entry keys (error mid-sequence, CLx correction…).</summary>
    internal void DiscardPending() => _pendingEntryKeys.Clear();

    /// <summary>
    /// Called by the engine after an action executed successfully in the live pipeline.
    /// </summary>
    /// <param name="wasEntering">Whether digit entry was in progress before the action ran.</param>
    /// <param name="entryText">Display text of the entry before the action ran (the
    /// number as the user saw it), when <paramref name="wasEntering"/>.</param>
    /// <param name="resultText">Display text after execution; null when unavailable.</param>
    internal void OnActionExecuted(ResolvedAction action, bool wasEntering, string? entryText, string? resultText)
    {
        switch (Classify(action, wasEntering))
        {
            case ActionClass.EntryKey:
                _pendingEntryKeys.AddRange(action.Sequence);
                break;

            case ActionClass.CancelEntry:
            case ActionClass.Suppressed:
                _pendingEntryKeys.Clear();
                break;

            case ActionClass.Terminator:
                var keys = new List<KeyId>(_pendingEntryKeys);
                keys.AddRange(action.Sequence);
                _pendingEntryKeys.Clear();
                string operation = HistoryLabeler.Label(action);
                string label = wasEntering && entryText is not null
                    ? $"{TrimEntryText(entryText)} {operation}"
                    : operation;
                _entries.Add(new HistoryEntry(_nextSequence++, keys, label, resultText));
                if (_entries.Count > Capacity) _entries.RemoveAt(0);
                Changed?.Invoke();
                break;
        }
    }

    /// <summary>"125." reads better as "125"; interior points ("1.5") are kept.</summary>
    private static string TrimEntryText(string text) =>
        text.EndsWith('.') ? text[..^1] : text;

    private enum ActionClass { EntryKey, CancelEntry, Suppressed, Terminator }

    private static ActionClass Classify(ResolvedAction action, bool wasEntering)
    {
        switch (action)
        {
            case GtoAction or GtoDotAction:
                return ActionClass.Suppressed;
            case FnAction fn:
                if (fn.Fn is >= Fn.Digit0 and <= Fn.Digit9 or Fn.Point or Fn.Eex)
                    return ActionClass.EntryKey;
                if (fn.Fn == Fn.Chs && wasEntering)
                    return ActionClass.EntryKey;
                if (fn.Fn == Fn.CLx && wasEntering)
                    return ActionClass.CancelEntry;
                if (fn.Fn is Fn.OnOff or Fn.ProgramRun or Fn.Sst or Fn.Bst or Fn.ClearPrgm
                    or Fn.ClearPrefix or Fn.Mantissa or Fn.Mem or Fn.Pse
                    or Fn.XLessOrEqualY or Fn.XEqualZero)
                    return ActionClass.Suppressed;
                return ActionClass.Terminator;
            default:
                // StoAction, RclAction, RclFinAction, RclCashFlowAction, FixAction,
                // ToggleOddPeriodAction — all real operations.
                return ActionClass.Terminator;
        }
    }
}
