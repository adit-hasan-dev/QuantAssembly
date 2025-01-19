using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using QuantAssembly.Common.Models;

public class LedgerService
{
    private IQueryable<Position> positions;

    public async Task LoadLedgerFileAsync(Stream fileStream)
    {
        var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var positionList = await JsonSerializer.DeserializeAsync<List<Position>>(fileStream, options) ?? new List<Position>();
            positions = positionList.ToArray().AsQueryable();
    }

    public IQueryable<Position> GetAllPositions()
    {
        return positions;
    }
}
