using System.Text.Json.Serialization;

namespace mmcli;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<MonitorConfiguration.MonitorInfo>), TypeInfoPropertyName = "MonitorList")]
internal partial class ProfileJsonContext : JsonSerializerContext
{
}
