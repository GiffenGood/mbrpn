using Microsoft.JSInterop;

namespace Mb12c.Web.Services;

/// <summary>Continuous memory: saves/restores the calculator state in localStorage.</summary>
public sealed class PersistenceService(IJSRuntime js)
{
    private const string StorageKey = "mb12c.state.v1";

    public async Task SaveAsync(string json)
    {
        await js.InvokeVoidAsync("mb12c.save", StorageKey, json);
    }

    public async Task<string?> LoadAsync()
    {
        return await js.InvokeAsync<string?>("mb12c.load", StorageKey);
    }
}
