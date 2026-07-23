using System.Net.Http.Json;
using RestaurantPOS.Contracts;

namespace RestaurantPOS.Client.Services;

public sealed class RestaurantPosApiClient(HttpClient httpClient, ILogger<RestaurantPosApiClient> logger)
{
    public async Task<ApiHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking backend API health.");
        return await httpClient.GetFromJsonAsync<ApiHealthResponse>(ApiRoutes.Health, cancellationToken);
    }

    public async Task<IReadOnlyList<MenuCategoryDto>> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading menu from backend API.");
        return await httpClient.GetFromJsonAsync<IReadOnlyList<MenuCategoryDto>>(ApiRoutes.Menu, cancellationToken)
            ?? [];
    }
}
