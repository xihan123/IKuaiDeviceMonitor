using Newtonsoft.Json;

namespace IKuaiDeviceMonitor.Models;

public class Device
{
    [JsonProperty("mac")] public string Mac { get; set; } = "";

    [JsonProperty("ip_addr")] public string IpAddr { get; set; } = "";

    [JsonProperty("hostname")] public string Hostname { get; set; } = "";

    [JsonProperty("client_type")] public string ClientType { get; set; } = "";

    [JsonProperty("client_device")] public string ClientDevice { get; set; } = "";

    [JsonProperty("uptime")] public string Uptime { get; set; } = "";

    [JsonProperty("comment")] public string Comment { get; set; } = "";

    [JsonProperty("connect_num")] public int ConnectNum { get; set; }
}

public class DeviceData
{
    [JsonProperty("data")] public List<Device> Data { get; set; } = new();

    [JsonProperty("total")] public int Total { get; set; }
}

public class DeviceResponse
{
    [JsonProperty("Result")] public int Result { get; set; }

    [JsonProperty("Data")] public DeviceData? Data { get; set; }
}