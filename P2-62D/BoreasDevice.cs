// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2024 BOREAS Linux Project Contributors

using HidApi;

namespace Boreas;

public sealed class BoreasDevice : IDisposable
{
    private const ushort VendorId = 0x1B80;
    private const ushort ProductId = 0xB53A;

    private Device? _device;
    private bool _disposed;

    public bool IsConnected => _device != null;

    public bool Connect()
    {
        if (_device != null) return true;

        try
        {
            Hid.Init();
            foreach (var deviceInfo in Hid.Enumerate(VendorId, ProductId))
            {
                if (deviceInfo.InterfaceNumber == 0)
                {
                    _device = new Device(deviceInfo.Path);
                    return true;
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
    }

    public bool SendPacket(byte[] packet)
    {
        if (_device == null) return false;
        try { _device.Write(packet); return true; }
        catch { return false; }
    }

    public bool Initialize() => SendPacket(BoreasProtocol.BuildInitPacket());
    public bool DisplayCelsius(double temperature, bool flashing = false) => SendPacket(BoreasProtocol.BuildCelsiusPacket(temperature, flashing));
    public bool DisplayFahrenheit(double temperature, bool flashing = false) => SendPacket(BoreasProtocol.BuildFahrenheitPacket(temperature, flashing));
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
