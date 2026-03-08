using Feniks.Shared.Models;
using System.Net.Http.Json;
namespace Feniks.Web.Services;

public class ObjectsService
{
    private readonly HttpClient _httpClient;
    private List<ConstructionObject>? _cachedObjects;

    public ObjectsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ConstructionObject>?> GetObjectsAsync()
    {
        try
        {
            _cachedObjects = await _httpClient.GetFromJsonAsync<List<ConstructionObject>>("http://localhost:5050/api/ConstructionObjects");
            return _cachedObjects;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки объектов: {ex.Message}");
            return null;
        }
    }

    public async Task<ConstructionObject?> GetObjectAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ConstructionObject>($"http://localhost:5050/api/ConstructionObjects/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки объекта: {ex.Message}");
            return null;
        }
    }

    public async Task<ConstructionObject?> CreateObjectAsync(ConstructionObject obj)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("http://localhost:5050/api/ConstructionObjects", obj);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ConstructionObject>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка создания объекта: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateObjectAsync(int id, ConstructionObject obj)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"http://localhost:5050/api/ConstructionObjects/{id}", obj);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления объекта: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteObjectAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"http://localhost:5050/api/ConstructionObjects/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления объекта: {ex.Message}");
            return false;
        }
    }

    public async Task LoadObjects(HttpClient http)
    {
        _cachedObjects = await GetObjectsAsync();
    }
}