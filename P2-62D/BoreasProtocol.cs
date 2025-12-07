// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2024 BOREAS Linux Project Contributors

namespace Boreas;

public static class BoreasProtocol
{
    private const byte Header1 = 0x3A;
    private const byte Header2 = 0xB5;
    private const byte Command = 0x01;
    public const int PacketSize = 64;
    private const byte ModeTemperature = 0x00;
    private const byte ModeFan = 0x01;
    private const byte UnitCelsius = 0x01;
    private const byte UnitFahrenheit = 0x00;
    private const byte BlankDigit = 0x20;

    public static byte[] BuildCelsiusPacket(double temperature, bool flashing = false)
    {
        var packet = new byte[PacketSize];
        int value = Math.Clamp((int)(temperature * 10), 0, 9999);

        byte digit1 = (byte)(value / 1000);
        byte digit2 = (byte)((value / 100) % 10);
        byte digit3 = (byte)((value / 10) % 10);
        byte digit4 = (byte)(value % 10);

        if (digit1 == 0) { digit1 = BlankDigit; if (digit2 == 0) digit2 = BlankDigit; }

        packet[0] = Header1;
        packet[1] = Header2;
        packet[2] = Command;
        packet[3] = digit1;
        packet[4] = digit2;
        packet[5] = digit3;
        packet[6] = digit4;
        packet[7] = 0x01;
        packet[8] = UnitCelsius;
        packet[9] = 0x01;
        packet[10] = ModeTemperature;
        packet[11] = (byte)(flashing ? 0x01 : 0x00);
        packet[12] = CalculateChecksum(packet);

        return packet;
    }

    public static byte[] BuildFahrenheitPacket(double temperature, bool flashing = false)
    {
        var packet = new byte[PacketSize];
        int value = Math.Clamp((int)(temperature * 10), 0, 9999);

        byte digit1 = (byte)(value / 1000);
        byte digit2 = (byte)((value / 100) % 10);
        byte digit3 = (byte)((value / 10) % 10);
        byte digit4 = (byte)(value % 10);

        if (digit1 == 0) { digit1 = BlankDigit; if (digit2 == 0) digit2 = BlankDigit; }

        packet[0] = Header1;
        packet[1] = Header2;
        packet[2] = Command;
        packet[3] = digit1;
        packet[4] = digit2;
        packet[5] = digit3;
        packet[6] = digit4;
        packet[7] = 0x01;
        packet[8] = UnitFahrenheit;
        packet[9] = 0x01;
        packet[10] = ModeTemperature;
        packet[11] = (byte)(flashing ? 0x01 : 0x00);
        packet[12] = CalculateChecksum(packet);

        return packet;
    }

    public static byte[] BuildFanPacket(int rpm, bool flashing = false)
    {
        var packet = new byte[PacketSize];
        rpm = Math.Clamp(rpm, 0, 9999);

        byte digit1 = (byte)(rpm / 1000);
        byte digit2 = (byte)((rpm / 100) % 10);
        byte digit3 = (byte)((rpm / 10) % 10);
        byte digit4 = (byte)(rpm % 10);

        if (digit1 == 0) { digit1 = BlankDigit; if (digit2 == 0) { digit2 = BlankDigit; if (digit3 == 0) digit3 = BlankDigit; } }

        packet[0] = Header1;
        packet[1] = Header2;
        packet[2] = Command;
        packet[3] = digit1;
        packet[4] = digit2;
        packet[5] = digit3;
        packet[6] = digit4;
        packet[7] = 0x00;
        packet[8] = 0x00;
        packet[9] = 0x00;
        packet[10] = ModeFan;
        packet[11] = (byte)(flashing ? 0x01 : 0x00);
        packet[12] = CalculateChecksum(packet);

        return packet;
    }

    public static byte[] BuildInitPacket()
    {
        var packet = new byte[PacketSize];
        packet[0] = Header1;
        packet[1] = Header2;
        packet[2] = 0x20;
        packet[12] = CalculateChecksum(packet);
        return packet;
    }

    private static byte CalculateChecksum(byte[] packet)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++) sum += packet[i];
        return (byte)(sum & 0xFF);
    }

    public static double CelsiusToFahrenheit(double celsius) => celsius * 9.0 / 5.0 + 32.0;
}
