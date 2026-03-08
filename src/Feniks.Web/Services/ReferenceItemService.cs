using Feniks.Shared.Models;
using System.Net.Http.Json;

namespace Feniks.Web.Services;

public class ReferenceItemService
{
    private readonly HttpClient _httpClient;

    public ReferenceItemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ReferenceItem>> GetItemsAsync(int? referenceId = null, string? search = null)
    {
        try
        {
            var url = "api/ReferenceItems";
            var query = new List<string>();
            
            if (referenceId.HasValue)
                query.Add($"referenceId={referenceId.Value}");
            
            if (!string.IsNullOrEmpty(search))
                query.Add($"search={Uri.EscapeDataString(search)}");
            
            if (query.Any())
                url += "?" + string.Join("&", query);
            
            return await _httpClient.GetFromJsonAsync<List<ReferenceItem>>(url) ?? new();
        }
        catch
        {
            return new List<ReferenceItem>();
        }
    }

    public async Task<ReferenceItem?> GetItemAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ReferenceItem>($"api/ReferenceItems/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ReferenceItem?> CreateItemAsync(ReferenceItem item)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ReferenceItems", item);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ReferenceItem>();
        }
        catch { }
        return null;
    }

    public async Task<bool> UpdateItemAsync(int id, ReferenceItem item)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/ReferenceItems/{id}", item);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/ReferenceItems/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> ImportItemsAsync(List<ReferenceItem> items)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ReferenceItems/import", items);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<int>();
        }
        catch { }
        return 0;
    }
}