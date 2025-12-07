// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2024 BOREAS Linux Project Contributors

using System.Runtime.InteropServices;
using Boreas;

class Program
{
    private static volatile bool _running = true;

    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
                case "--list-sensors":
                    ListSensors();
                    return 0;
                case "--generate-config":
                    GenerateConfig(args.Length > 1 ? args[1] : "config.json");
                    return 0;
                case "--test":
                    return RunTest();
            }
        }

        var configFile = args.Length > 0 ? args[0] : "/etc/boreas/config.json";
        if (!File.Exists(configFile))
        {
            var localConfig = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(localConfig))
                configFile = localConfig;
            else
            {
                Console.Error.WriteLine($"Configuration file not found: {configFile}");
                Console.Error.WriteLine("Run with --generate-config to create a sample configuration.");
                return 1;
            }
        }

        BoreasConfig config;
        try
        {
            config = BoreasConfig.Load(configFile);
            Console.WriteLine($"Loaded configuration from: {configFile}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load configuration: {ex.Message}");
            return 1;
        }

        Console.CancelKeyPress += (_, e) => { e.Cancel = true; _running = false; Console.WriteLine("Shutdown requested..."); };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _running = false;
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => { _running = false; Console.WriteLine("SIGTERM received, shutting down..."); });

        return RunDaemon(config);
    }

    static int RunDaemon(BoreasConfig config)
    {
        var sensors = new SensorReader(config.Sensors.CpuTempPath, config.Sensors.CpuFanPath);
        Console.WriteLine($"CPU Temp sensor: {sensors.CpuTempPath ?? "not found"}");
        Console.WriteLine($"CPU Fan sensor: {sensors.CpuFanPath ?? "not found"}");

        using var device = new BoreasDevice();
        Console.WriteLine("Connecting to BOREAS display...");

        int retryCount = 0;
        while (!device.Connect() && _running)
        {
            if (++retryCount % 10 == 1) Console.WriteLine("Device not found, waiting...");
            Thread.Sleep(1000);
        }

        if (!_running) return 0;

        Console.WriteLine("Connected to BOREAS display");
        device.Initialize();

        int currentDisplayIndex = 0;
        DateTime lastRotation = DateTime.UtcNow;
        Console.WriteLine("Starting display loop...");

        while (_running)
        {
            try
            {
                if (config.Display.Count > 1)
                {
                    var elapsed = DateTime.UtcNow - lastRotation;
                    if (elapsed.TotalSeconds >= config.Display[currentDisplayIndex].DurationSeconds)
                    {
                        currentDisplayIndex = (currentDisplayIndex + 1) % config.Display.Count;
                        lastRotation = DateTime.UtcNow;
                    }
                }

                var displayItem = config.Display.Count > 0 ? config.Display[currentDisplayIndex] : new DisplayItem();

                switch (displayItem.Mode)
                {
                    case DisplayMode.CpuTempCelsius:
                        var tempC = sensors.ReadCpuTemperature();
                        if (tempC.HasValue) device.DisplayTemperature(tempC.Value, celsius: true);
                        break;
                    case DisplayMode.CpuTempFahrenheit:
                        var tempF = sensors.ReadCpuTemperature();
                        if (tempF.HasValue) device.DisplayTemperature(BoreasProtocol.CelsiusToFahrenheit(tempF.Value), celsius: false);
                        break;
                    case DisplayMode.CpuFanSpeed:
                        var rpm = sensors.ReadCpuFanSpeed();
                        if (rpm.HasValue) device.DisplayFanSpeed(rpm.Value);
                        break;
                }

                Thread.Sleep(config.UpdateIntervalMs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Thread.Sleep(1000);
                if (!device.IsConnected) { Console.WriteLine("Reconnecting..."); device.Connect(); }
            }
        }

        Console.WriteLine("Daemon stopped.");
        return 0;
    }

    static void PrintHelp()
    {
        Console.WriteLine(@"BOREAS Display Daemon - Linux driver for GAMDIAS BOREAS P2-62D

Usage: boreas [OPTIONS] [CONFIG_FILE]

Arguments:
  CONFIG_FILE          Path to config file (default: /etc/boreas/config.json)

Options:
  -h, --help           Show this help message
  --list-sensors       List available sensors on this system
  --generate-config    Generate a sample configuration file
  --test               Run a quick display test

Examples:
  boreas                           Run with default config
  boreas ./config.json             Run with custom config
  boreas --generate-config         Create sample config.json
  boreas --list-sensors            Show available sensors");
    }

    static void ListSensors()
    {
        Console.WriteLine("Available sensors:\n");
        foreach (var (path, name, type) in SensorReader.ListAvailableSensors())
        {
            string value = "?";
            try
            {
                var content = File.ReadAllText(path).Trim();
                if (int.TryParse(content, out int raw))
                    value = type == "temperature" ? $"{raw / 1000.0:F1}°C" : $"{raw} RPM";
            }
            catch { }
            Console.WriteLine($"  [{type,-12}] {name,-15} {path}");
            Console.WriteLine($"               Current value: {value}");
        }
    }

    static void GenerateConfig(string path)
    {
        if (File.Exists(path))
        {
            Console.Write($"File {path} exists. Overwrite? [y/N] ");
            if (Console.ReadLine()?.ToLower() != "y") { Console.WriteLine("Cancelled."); return; }
        }
        BoreasConfig.SaveSample(path);
        Console.WriteLine($"Sample configuration written to: {path}");
    }

    static int RunTest()
    {
        Console.WriteLine("BOREAS Display Test\n===================");
        using var device = new BoreasDevice();

        if (!device.Connect()) { Console.Error.WriteLine("Failed to connect to device."); return 1; }

        Console.WriteLine("Connected!");
        device.Initialize();

        Console.WriteLine("Test 1: Celsius 42.5°C");
        device.DisplayTemperature(42.5, celsius: true);
        Thread.Sleep(2000);

        Console.WriteLine("Test 2: Fahrenheit 98.6°F");
        device.DisplayTemperature(98.6, celsius: false);
        Thread.Sleep(2000);

        Console.WriteLine("Test 3: Fan 1234 RPM");
        device.DisplayFanSpeed(1234);
        Thread.Sleep(2000);

        Console.WriteLine("Test 4: Live sensors...");
        var sensors = new SensorReader();

        var temp = sensors.ReadCpuTemperature();
        if (temp.HasValue) { Console.WriteLine($"  CPU Temp: {temp.Value:F1}°C"); device.DisplayTemperature(temp.Value); Thread.Sleep(2000); }

        var fan = sensors.ReadCpuFanSpeed();
        if (fan.HasValue) { Console.WriteLine($"  CPU Fan: {fan.Value} RPM"); device.DisplayFanSpeed(fan.Value); Thread.Sleep(2000); }

        Console.WriteLine("Test complete!");
        return 0;
    }
}
