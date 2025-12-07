// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2024 BOREAS Linux Project Contributors

namespace Boreas;

/// <summary>
/// USB HID protocol for GAMDIAS BOREAS displays.
///
/// Packet structure (64 bytes, only first 13 used):
///   [0]  Header byte 1 (0x3A)
///   [1]  Header byte 2 (0xB5)
///   [2]  Command (0x01 for display, 0x20 for init)
///   [3]  Digit 1 (thousands / leftmost)
///   [4]  Digit 2 (hundreds)
///   [5]  Digit 3 (tens)
///   [6]  Digit 4 (ones / rightmost)
///   [7]  Decimal point (0x01 = show between digit 3 and 4)
///   [8]  Temperature unit (0x01 = Celsius, 0x00 = Fahrenheit)
///   [9]  CPU indicator (0x01 = show CPU icon)
///   [10] Display mode (0x00 = temperature, 0x01 = fan)
///   [11] Flashing (0x01 = flash display)
///   [12] Checksum (sum of bytes 0-11, masked with 0xFF)
///
/// Digit values: 0-9 for numbers, 0x20 for blank (leading zero suppression)
/// </summary>
public static class BoreasProtocol
{
    private const byte Header1 = 0x3A;
    private const byte Header2 = 0xB5;
    private const byte CommandDisplay = 0x01;
    private const byte CommandInit = 0x20;
    private const byte ModeTemperature = 0x00;
    private const byte ModeFan = 0x01;
    private const byte UnitCelsius = 0x01;
    private const byte UnitFahrenheit = 0x00;
    private const byte BlankDigit = 0x20;

    public const int PacketSize = 64;

    public static byte[] BuildTemperaturePacket(double temperature, bool celsius = true, bool flashing = false)
    {
        int value = Math.Clamp((int)(temperature * 10), 0, 9999);
        var (d1, d2, d3, d4) = ExtractDigits(value, blankLeadingZeros: 2);
        return BuildPacket(d1, d2, d3, d4, hasDecimal: true, celsius ? UnitCelsius : UnitFahrenheit, showCpuIcon: true, ModeTemperature, flashing);
    }

    public static byte[] BuildFanPacket(int rpm, bool flashing = false)
    {
        rpm = Math.Clamp(rpm, 0, 9999);
        var (d1, d2, d3, d4) = ExtractDigits(rpm, blankLeadingZeros: 3);
        return BuildPacket(d1, d2, d3, d4, hasDecimal: false, unit: 0x00, showCpuIcon: false, ModeFan, flashing);
    }

    public static byte[] BuildInitPacket()
    {
        var packet = new byte[PacketSize];
        packet[0] = Header1;
        packet[1] = Header2;
        packet[2] = CommandInit;
        packet[12] = CalculateChecksum(packet);
        return packet;
    }

    public static double CelsiusToFahrenheit(double celsius) => celsius * 9.0 / 5.0 + 32.0;

    private static byte[] BuildPacket(byte d1, byte d2, byte d3, byte d4,
        bool hasDecimal, byte unit, bool showCpuIcon, byte mode, bool flashing)
    {
        var packet = new byte[PacketSize];
        packet[0] = Header1;
        packet[1] = Header2;
        packet[2] = CommandDisplay;
        packet[3] = d1;
        packet[4] = d2;
        packet[5] = d3;
        packet[6] = d4;
        packet[7] = (byte)(hasDecimal ? 0x01 : 0x00);
        packet[8] = unit;
        packet[9] = (byte)(showCpuIcon ? 0x01 : 0x00);
        packet[10] = mode;
        packet[11] = (byte)(flashing ? 0x01 : 0x00);
        packet[12] = CalculateChecksum(packet);
        return packet;
    }

    private static (byte d1, byte d2, byte d3, byte d4) ExtractDigits(int value, int blankLeadingZeros)
    {
        byte d1 = (byte)(value / 1000);
        byte d2 = (byte)((value / 100) % 10);
        byte d3 = (byte)((value / 10) % 10);
        byte d4 = (byte)(value % 10);

        if (blankLeadingZeros >= 1 && d1 == 0) { d1 = BlankDigit;
        if (blankLeadingZeros >= 2 && d2 == 0) { d2 = BlankDigit;
        if (blankLeadingZeros >= 3 && d3 == 0) { d3 = BlankDigit; }}}

        return (d1, d2, d3, d4);
    }

    private static byte CalculateChecksum(byte[] packet)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++) sum += packet[i];
        return (byte)(sum & 0xFF);
    }
}
