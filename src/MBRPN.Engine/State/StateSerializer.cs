using System.Globalization;
using System.Text.Json;
using MBRPN.Engine.Core;
using MBRPN.Engine.Keys;

namespace MBRPN.Engine.State;

public static class StateSerializer
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static string EncodeReal(Real value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value.M}|{value.E}");

    public static Real DecodeReal(string encoded)
    {
        int split = encoded.IndexOf('|');
        if (split < 0) return Real.Zero;
        decimal m = decimal.Parse(encoded[..split], CultureInfo.InvariantCulture);
        int e = int.Parse(encoded[(split + 1)..], CultureInfo.InvariantCulture);
        return Real.FromParts(m, e);
    }

    public static string ToJson(CalculatorState state) => JsonSerializer.Serialize(state, Options);

    public static CalculatorState? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CalculatorState>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static KeyId? ParseKey(string name) =>
        Enum.TryParse<KeyId>(name, out var key) ? key : null;
}
