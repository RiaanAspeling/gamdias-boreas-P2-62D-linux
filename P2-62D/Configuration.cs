// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2024 BOREAS Linux Project Contributors

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Boreas;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisplayMode
{
    CpuTempCelsius,
    CpuTempFahrenheit,
    CpuFanSpeed
}

public class DisplayItem
{
    public DisplayMode Mode { get; set; } = DisplayMode.CpuTempCelsius;
    public int DurationSeconds { get; set; } = 5;
}

public class SensorConfig
{
    public string? CpuTempPath { get; set; }
    public string? CpuFanPath { get; set; }
}

public class BoreasConfig
{
    public int UpdateIntervalMs { get; set; } = 1000;
    public SensorConfig Sensors { get; set; } = new();
    public List<DisplayItem> Display { get; set; } = new() { new DisplayItem { Mode = DisplayMode.CpuTempCelsius, DurationSeconds = 5 } };

    public static BoreasConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        return JsonSerializer.Deserialize<BoreasConfig>(json, options)
            ?? throw new InvalidOperationException("Failed to parse configuration");
    }

    public static void SaveSample(string path)
    {
        var sample = new BoreasConfig
        {
            UpdateIntervalMs = 1000,
            Sensors = new SensorConfig { CpuTempPath = null, CpuFanPath = null },
            Display = new List<DisplayItem>
            {
                new() { Mode = DisplayMode.CpuTempCelsius, DurationSeconds = 10 },
                new() { Mode = DisplayMode.CpuFanSpeed, DurationSeconds = 5 }
            }
        };

        var json = JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
