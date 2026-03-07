using Feniks.Shared.Models;
using System.Net.Http.Json;

namespace Feniks.Web.Services;

public class ReferenceService
{
    private readonly HttpClient _httpClient;

    public ReferenceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Reference>> GetReferencesAsync(int? categoryId = null, string? search = null)
    {
        try
        {
            var url = "api/RefCatalogs";
            var query = new List<string>();
            
            if (categoryId.HasValue)
                query.Add($"categoryId={categoryId.Value}");
            
            if (!string.IsNullOrEmpty(search))
                query.Add($"search={Uri.EscapeDataString(search)}");
            
            if (query.Any())
                url += "?" + string.Join("&", query);
            
            return await _httpClient.GetFromJsonAsync<List<Reference>>(url) ?? new();
        }
        catch
        {
            return new List<Reference>();
        }
    }

    public async Task<Reference?> GetReferenceAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Reference>($"api/RefCatalogs/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ReferenceItem>> GetReferenceItemsAsync(int referenceId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReferenceItem>>($"api/RefCatalogs/{referenceId}/items") ?? new();
        }
        catch
        {
            return new List<ReferenceItem>();
        }
    }

    public async Task<Reference?> CreateReferenceAsync(Reference reference)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/RefCatalogs", reference);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Reference>();
        }
        catch { }
        return null;
    }

    public async Task<bool> UpdateReferenceAsync(int id, Reference reference)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/RefCatalogs/{id}", reference);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteReferenceAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/RefCatalogs/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}