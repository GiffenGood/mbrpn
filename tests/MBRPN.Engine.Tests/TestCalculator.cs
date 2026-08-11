using MBRPN.Engine;
using MBRPN.Engine.Core;
using MBRPN.Engine.Keys;
using Xunit;

namespace MBRPN.Engine.Tests;

/// <summary>
/// Fluent driver: presses keys by their printed legend (shifted legends map to their
/// physical key, so tests read like handbook keystroke listings) and asserts on the
/// display text, exactly what a user would see.
/// </summary>
public sealed class TestCalculator
{
    public CalculatorEngine Engine { get; } = new();

    private static readonly Dictionary<string, KeyId> KeyByLegend = BuildLegendMap();

    private static Dictionary<string, KeyId> BuildLegendMap()
    {
        var map = new Dictionary<string, KeyId>(StringComparer.OrdinalIgnoreCase)
        {
            // Row 1 primaries
            ["n"] = KeyId.N, ["i"] = KeyId.I, ["PV"] = KeyId.PV, ["PMT"] = KeyId.PMT,
            ["FV"] = KeyId.FV, ["CHS"] = KeyId.CHS, ["/"] = KeyId.Divide, ["÷"] = KeyId.Divide,
            // Row 2
            ["y^x"] = KeyId.YtoX, ["1/x"] = KeyId.Reciprocal, ["%T"] = KeyId.PercentT,
            ["d%"] = KeyId.DeltaPercent, ["Δ%"] = KeyId.DeltaPercent, ["%"] = KeyId.Percent,
            ["EEX"] = KeyId.EEX, ["*"] = KeyId.Multiply, ["×"] = KeyId.Multiply,
            // Row 3
            ["R/S"] = KeyId.RunStop, ["SST"] = KeyId.SST, ["Rv"] = KeyId.RollDown,
            ["R↓"] = KeyId.RollDown, ["x<>y"] = KeyId.XExchangeY, ["x≷y"] = KeyId.XExchangeY,
            ["CLx"] = KeyId.CLx, ["ENTER"] = KeyId.Enter, ["-"] = KeyId.Subtract,
            ["−"] = KeyId.Subtract,
            // Row 4
            ["ON"] = KeyId.On, ["f"] = KeyId.F, ["g"] = KeyId.G, ["STO"] = KeyId.STO,
            ["RCL"] = KeyId.RCL, ["."] = KeyId.Point, ["S+"] = KeyId.SigmaPlus,
            ["Σ+"] = KeyId.SigmaPlus, ["+"] = KeyId.Add,
            // Gold (f) legends → their physical keys
            ["AMORT"] = KeyId.N, ["INT"] = KeyId.I, ["NPV"] = KeyId.PV, ["RND"] = KeyId.PMT,
            ["IRR"] = KeyId.FV, ["PRICE"] = KeyId.YtoX, ["YTM"] = KeyId.Reciprocal,
            ["SL"] = KeyId.PercentT, ["SOYD"] = KeyId.DeltaPercent, ["DB"] = KeyId.Percent,
            ["P/R"] = KeyId.RunStop, ["CLEARS"] = KeyId.SST, ["CLEARΣ"] = KeyId.SST,
            ["CLEARPRGM"] = KeyId.RollDown, ["CLEARFIN"] = KeyId.XExchangeY,
            ["CLEARREG"] = KeyId.CLx, ["CLEARPREFIX"] = KeyId.Enter,
            // Blue (g) legends → their physical keys
            ["12x"] = KeyId.N, ["12÷"] = KeyId.I, ["12/"] = KeyId.I, ["CF0"] = KeyId.PV,
            ["CFj"] = KeyId.PMT, ["Nj"] = KeyId.FV, ["DATE"] = KeyId.CHS,
            ["BEG"] = KeyId.D7, ["END"] = KeyId.D8, ["MEM"] = KeyId.D9,
            ["sqrt"] = KeyId.YtoX, ["√x"] = KeyId.YtoX, ["e^x"] = KeyId.Reciprocal,
            ["LN"] = KeyId.PercentT, ["FRAC"] = KeyId.DeltaPercent, ["INTG"] = KeyId.Percent,
            ["DDYS"] = KeyId.EEX, ["ΔDYS"] = KeyId.EEX, ["D.MY"] = KeyId.D4, ["M.DY"] = KeyId.D5,
            ["xw"] = KeyId.D6, ["x̄w"] = KeyId.D6, ["PSE"] = KeyId.RunStop, ["BST"] = KeyId.SST,
            ["GTO"] = KeyId.RollDown, ["x<=y"] = KeyId.XExchangeY, ["x≤y"] = KeyId.XExchangeY,
            ["x=0"] = KeyId.CLx, ["LSTx"] = KeyId.Enter, ["xr"] = KeyId.D1, ["x̂r"] = KeyId.D1,
            ["yr"] = KeyId.D2, ["ŷr"] = KeyId.D2, ["n!"] = KeyId.D3, ["mean"] = KeyId.D0,
            ["x̄"] = KeyId.D0, ["s"] = KeyId.Point, ["S-"] = KeyId.SigmaPlus, ["Σ-"] = KeyId.SigmaPlus,
        };
        for (int d = 0; d <= 9; d++)
        {
            map[d.ToString()] = d switch
            {
                0 => KeyId.D0, 1 => KeyId.D1, 2 => KeyId.D2, 3 => KeyId.D3, 4 => KeyId.D4,
                5 => KeyId.D5, 6 => KeyId.D6, 7 => KeyId.D7, 8 => KeyId.D8, _ => KeyId.D9,
            };
        }
        return map;
    }

    public TestCalculator Press(params string[] legends)
    {
        foreach (string legend in legends)
        {
            if (!KeyByLegend.TryGetValue(legend, out var key))
                throw new ArgumentException($"Unknown key legend '{legend}'");
            Engine.Press(key);
        }
        return this;
    }

    /// <summary>Types a number digit by digit ("-1234.56" presses 1 2 3 4 . 5 6 CHS).</summary>
    public TestCalculator Type(string number)
    {
        bool negative = number.StartsWith('-');
        foreach (char ch in negative ? number[1..] : number)
        {
            if (ch == '.') Press(".");
            else if (char.IsAsciiDigit(ch)) Press(ch.ToString());
            else throw new ArgumentException($"Cannot type '{ch}'");
        }
        if (negative) Press("CHS");
        return this;
    }

    public TestCalculator DisplayIs(string expected)
    {
        Assert.Equal(expected, Engine.DisplayText);
        return this;
    }

    public TestCalculator XIs(decimal expected)
    {
        Assert.Equal(expected, Engine.Stack.X.ToDecimal());
        return this;
    }

    public TestCalculator XIsAbout(double expected, double tolerance = 5e-9)
    {
        Assert.True(Math.Abs(Engine.Stack.X.ToDouble() - expected) <= tolerance * Math.Max(1, Math.Abs(expected)),
            $"X = {Engine.Stack.X.ToDouble()} expected ≈ {expected}");
        return this;
    }

    public TestCalculator RunProgram()
    {
        Engine.RunToHalt();
        return this;
    }

    public Real X => Engine.Stack.X;
    public Real Y => Engine.Stack.Y;
    public Real Z => Engine.Stack.Z;
    public Real T => Engine.Stack.T;
}
