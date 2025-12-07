// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2024 BOREAS Linux Project Contributors

namespace Boreas;

public class SensorReader
{
    private readonly string? _cpuTempPath;
    private readonly string? _cpuFanPath;

    public SensorReader(string? cpuTempPath = null, string? cpuFanPath = null)
    {
        _cpuTempPath = cpuTempPath ?? DetectCpuTempPath();
        _cpuFanPath = cpuFanPath ?? DetectCpuFanPath();
    }

    public string? CpuTempPath => _cpuTempPath;
    public string? CpuFanPath => _cpuFanPath;

    public double? ReadCpuTemperature()
    {
        if (_cpuTempPath == null || !File.Exists(_cpuTempPath)) return null;
        try
        {
            var content = File.ReadAllText(_cpuTempPath).Trim();
            if (int.TryParse(content, out int milliCelsius))
                return milliCelsius / 1000.0;
        }
        catch { }
        return null;
    }

    public int? ReadCpuFanSpeed()
    {
        if (_cpuFanPath == null || !File.Exists(_cpuFanPath)) return null;
        try
        {
            var content = File.ReadAllText(_cpuFanPath).Trim();
            if (int.TryParse(content, out int rpm))
                return rpm;
        }
        catch { }
        return null;
    }

    private static string? DetectCpuTempPath()
    {
        string[] cpuSensorNames = { "coretemp", "k10temp", "zenpower", "cpu_thermal", "acpitz" };
        try
        {
            var hwmonDirs = Directory.GetDirectories("/sys/class/hwmon");
            foreach (var hwmon in hwmonDirs)
            {
                var namePath = Path.Combine(hwmon, "name");
                if (File.Exists(namePath))
                {
                    var name = File.ReadAllText(namePath).Trim().ToLowerInvariant();
                    if (cpuSensorNames.Any(s => name.Contains(s)))
                    {
                        var tempPath = Path.Combine(hwmon, "temp1_input");
                        if (File.Exists(tempPath)) return tempPath;
                    }
                }
            }

            var thermalPath = "/sys/class/thermal/thermal_zone0/temp";
            if (File.Exists(thermalPath)) return thermalPath;
        }
        catch { }
        return null;
    }

    private static string? DetectCpuFanPath()
    {
        string[] fanSensorNames = { "nct6687", "nct6798", "nct6775", "nct", "it87", "asus", "dell" };
        try
        {
            var hwmonDirs = Directory.GetDirectories("/sys/class/hwmon");
            foreach (var hwmon in hwmonDirs)
            {
                var namePath = Path.Combine(hwmon, "name");
                if (!File.Exists(namePath)) continue;

                var name = File.ReadAllText(namePath).Trim().ToLowerInvariant();
                if (!fanSensorNames.Any(s => name.Contains(s))) continue;

                var fanPath = Path.Combine(hwmon, "fan1_input");
                if (File.Exists(fanPath))
                {
                    var content = File.ReadAllText(fanPath).Trim();
                    if (int.TryParse(content, out int rpm) && rpm > 0)
                        return fanPath;
                }
            }
        }
        catch { }
        return null;
    }

    public static IEnumerable<(string Path, string Name, string Type)> ListAvailableSensors()
    {
        var sensors = new List<(string, string, string)>();
        try
        {
            var hwmonDirs = Directory.GetDirectories("/sys/class/hwmon");
            foreach (var hwmon in hwmonDirs)
            {
                var name = "unknown";
                var namePath = Path.Combine(hwmon, "name");
                if (File.Exists(namePath)) name = File.ReadAllText(namePath).Trim();

                foreach (var tempFile in Directory.GetFiles(hwmon, "temp*_input"))
                    sensors.Add((tempFile, name, "temperature"));

                foreach (var fanFile in Directory.GetFiles(hwmon, "fan*_input"))
                    sensors.Add((fanFile, name, "fan"));
            }

            if (Directory.Exists("/sys/class/thermal"))
            {
                foreach (var zone in Directory.GetDirectories("/sys/class/thermal", "thermal_zone*"))
                {
                    var tempPath = Path.Combine(zone, "temp");
                    if (File.Exists(tempPath))
                    {
                        var type = "thermal_zone";
                        var typePath = Path.Combine(zone, "type");
                        if (File.Exists(typePath)) type = File.ReadAllText(typePath).Trim();
                        sensors.Add((tempPath, type, "temperature"));
                    }
                }
            }
        }
        catch { }
        return sensors;
    }
}
