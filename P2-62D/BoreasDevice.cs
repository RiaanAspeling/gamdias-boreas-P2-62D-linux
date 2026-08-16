// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2025 BOREAS Linux Project Contributors

using HidApi;

namespace Boreas;

public sealed class BoreasDevice : IDisposable
{
    private const ushort VendorId = 0x1B80;

    private static readonly (ushort ProductId, string Name)[] SupportedProducts =
    {
        // Original implementation support
        (0xB53A, "BOREAS P2-62D"),
        // B554 (M2-51D, also sold as the Rosewill M2-51D) was identified by the 
        // Rust implementation at https://github.com/yanfuzhou/gamdias-boreas-cpu-air-cooler-linux-driver 
        // and is untested here.
        (0xB554, "BOREAS M2-51D"),
    };

    private Device? _device;
    private bool _disposed;

    public bool IsConnected => _device != null;

    public string? ConnectedProduct { get; private set; }

    public bool Connect()
    {
        if (_device != null) return true;

        try
        {
            Hid.Init();
            foreach (var (productId, name) in SupportedProducts)
            {
                foreach (var deviceInfo in Hid.Enumerate(VendorId, productId))
                {
                    if (deviceInfo.InterfaceNumber == 0)
                    {
                        _device = new Device(deviceInfo.Path);
                        ConnectedProduct = name;
                        return true;
                    }
                }
            }
            return false;
        }
        catch { return false; }
    }

    public void Disconnect()
    {
        _device?.Dispose();
        _device = null;
        ConnectedProduct = null;
    }

    public bool SendPacket(byte[] packet)
    {
        if (_device == null) return false;
        try { _device.Write(packet); return true; }
        catch { return false; }
    }

    public bool Initialize() => SendPacket(BoreasProtocol.BuildInitPacket());
    public bool DisplayTemperature(double temperature, bool celsius = true, bool flashing = false) => SendPacket(BoreasProtocol.BuildTemperaturePacket(temperature, celsius, flashing));
    public bool DisplayFanSpeed(int rpm, bool flashing = false) => SendPacket(BoreasProtocol.BuildFanPacket(rpm, flashing));

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            Hid.Exit();
            _disposed = true;
        }
    }
}
