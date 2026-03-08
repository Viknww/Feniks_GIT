using Feniks.Shared.Models;
using System.Net.Http.Json;

namespace Feniks.Web.Services;

public class RefCatalogService
{
    private readonly HttpClient _httpClient;

    public RefCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RefCatalog>> GetCatalogsAsync(int? categoryId = null, string? search = null)
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
            
            return await _httpClient.GetFromJsonAsync<List<RefCatalog>>(url) ?? new();
        }
        catch
        {
            return new List<RefCatalog>();
        }
    }

    public async Task<RefCatalog?> GetCatalogAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RefCatalog>($"api/RefCatalogs/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<RefCatalog?> CreateCatalogAsync(RefCatalog catalog)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/RefCatalogs", catalog);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<RefCatalog>();
        }
        catch { }
        return null;
    }

    public async Task<bool> UpdateCatalogAsync(int id, RefCatalog catalog)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/RefCatalogs/{id}", catalog);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteCatalogAsync(int id)
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