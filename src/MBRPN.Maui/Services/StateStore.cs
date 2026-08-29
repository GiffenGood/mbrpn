namespace MBRPN.Maui.Services;

/// <summary>
/// Continuous-memory storage: the serialized CalculatorState JSON in the app data
/// directory. Best-effort, like the web app's localStorage persistence — a failed
/// read or write never takes the calculator down.
/// </summary>
public sealed class StateStore
{
    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, "mbrpn.state.v1.json");

    public string? Load()
    {
        try
        {
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
        }
        catch
        {
            return null;
        }
    }

    public void Save(string json)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // storage unavailable: continuous memory is best-effort
        }
    }
}
