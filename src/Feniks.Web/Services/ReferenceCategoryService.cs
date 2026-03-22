using Feniks.Shared.Models;
using System.Net.Http.Json;

namespace Feniks.Web.Services;

public class ReferenceCategoryService
{
    private readonly HttpClient _httpClient;

    public ReferenceCategoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ReferenceCategory>> GetCategoryTreeAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReferenceCategory>>("api/ReferenceCategories/tree") ?? new();
        }
        catch
        {
            return new List<ReferenceCategory>();
        }
    }

    public async Task<List<ReferenceCategory>> GetCategoriesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReferenceCategory>>("api/ReferenceCategories") ?? new();
        }
        catch
        {
            return new List<ReferenceCategory>();
        }
    }

    public async Task<ReferenceCategory?> GetCategoryAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ReferenceCategory>($"api/ReferenceCategories/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ReferenceItem>> GetCategoryItemsAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReferenceItem>>($"api/ReferenceCategories/{id}/items") ?? new();
        }
        catch
        {
            return new List<ReferenceItem>();
        }
    }

    public async Task<ReferenceCategory?> CreateCategoryAsync(ReferenceCategory category)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ReferenceCategories", category);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ReferenceCategory>();
        }
        catch { }
        return null;
    }

    public async Task<bool> UpdateCategoryAsync(int id, ReferenceCategory category)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/ReferenceCategories/{id}", category);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/ReferenceCategories/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}