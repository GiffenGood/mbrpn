using Microsoft.JSInterop;

namespace MBRPN.Web.Services;

/// <summary>Continuous memory: saves/restores the calculator state in localStorage.</summary>
public sealed class PersistenceService(IJSRuntime js)
{
    private const string StorageKey = "mbrpn.state.v1";

    public async Task SaveAsync(string json)
    {
        await js.InvokeVoidAsync("mbrpn.save", StorageKey, json);
    }

    public async Task<string?> LoadAsync()
    {
        return await js.InvokeAsync<string?>("mbrpn.load", StorageKey);
    }
}
