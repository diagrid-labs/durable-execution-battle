using System.Text.Json;
using EnterpriseDiagnostics.Conductor.Models;
using Xunit;

namespace EnterpriseDiagnostics.Conductor.Tests;

public class SeverityJsonTests
{
    private static JsonSerializerOptions Options() => new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy    = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Priority_serializes_property_names_as_snake_case_and_keeps_severity_wire_value()
    {
        var priority = new Priority(Severity.RedAlert, "warp_core critical");
        var json = JsonSerializer.Serialize(priority, Options());

        Assert.Contains("\"severity\":\"red_alert\"", json);
        Assert.Contains("\"summary\":\"warp_core critical\"", json);
    }

    [Theory]
    [InlineData("red_alert")]
    [InlineData("yellow_alert")]
    [InlineData("green")]
    public void Priority_round_trips_each_severity(string wire)
    {
        var json = $"{{\"severity\":\"{wire}\",\"summary\":\"x\"}}";
        var parsed = JsonSerializer.Deserialize<Priority>(json, Options())!;

        Assert.Equal(wire, parsed.Severity);
        Assert.Equal("x", parsed.Summary);
    }

    [Fact]
    public void Severity_constants_match_wire_strings()
    {
        Assert.Equal("green",        Severity.Green);
        Assert.Equal("yellow_alert", Severity.YellowAlert);
        Assert.Equal("red_alert",    Severity.RedAlert);
    }
}
