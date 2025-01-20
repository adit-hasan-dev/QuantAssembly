using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using QuantAssembly.Common.Models;

public class LedgerService
{
    internal class PositionWrapper
    {
        public IList<Position> positions { get; set; }
    }
    private IQueryable<Position> positions;

    public async Task LoadLedgerFileAsync(Stream fileStream)
    {
        var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var positionWrapper = await JsonSerializer.DeserializeAsync<PositionWrapper>(fileStream, options);
            var result = positionWrapper?.positions ?? new List<Position>();
            positions = result.ToArray().AsQueryable();
    }

    public IQueryable<Position> GetAllPositions()
    {
        return positions;
    }
}
