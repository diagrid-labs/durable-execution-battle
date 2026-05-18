using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

/// <summary>
/// Bridges Conductor's Newtonsoft-based wire format (camelCase from .NET PascalCase records by
/// default) and the snake_case wire format the workflow definition expects (e.g.
/// <c>${prioritize.output.severity}</c>).
/// </summary>
internal static class WorkerSerialization
{
    private static readonly JsonSerializerSettings SnakeCaseSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    /// <summary>
    /// Pull a typed input record out of <see cref="ConductorTask.InputData"/>.
    /// Conductor delivers inputs as a dict (sometimes nested JObjects); round-trip via
    /// Newtonsoft so positional records bind correctly.
    /// </summary>
    public static T ReadInput<T>(ConductorTask task)
    {
        var json = JsonConvert.SerializeObject(task.InputData);
        return JsonConvert.DeserializeObject<T>(json, SnakeCaseSettings)
            ?? throw new InvalidOperationException(
                $"Could not deserialize task input as {typeof(T).Name}");
    }

    /// <summary>
    /// Serialize a typed result into a snake_case-keyed dict, which is what the workflow
    /// definition references (e.g. <c>${scan_warp_core.output.health}</c>).
    /// </summary>
    public static Dictionary<string, object> AsSnakeCaseDict<T>(T value)
    {
        var json = JsonConvert.SerializeObject(value, SnakeCaseSettings);
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
            ?? throw new InvalidOperationException(
                $"Could not serialize {typeof(T).Name} as snake_case dict");
    }
}
