# GAMDIAS BOREAS P2-62D Linux Driver

A Linux daemon for the GAMDIAS BOREAS P2-62D digital display (USB ID `1B80:B53A`). This device is typically bundled with GAMDIAS CPU coolers and only comes with Windows software (ZEUS CAST).

This project provides a native Linux solution to display CPU temperature and fan speed on the device.

## Features

- Display CPU temperature in Celsius or Fahrenheit
- Display CPU fan speed in RPM
- Rotate between multiple display modes on a configurable timer
- Auto-detection of CPU temperature and fan sensors
- Runs as a systemd daemon
- JSON configuration file

## Hardware

- **Protocol**: USB HID
- **Display**: 4-digit 7-segment with temperature/fan icons

### Supported Devices

| Device | USB ID | Status |
|--------|--------|--------|
| GAMDIAS BOREAS P2-62D | `1B80:B53A` | Tested |
| GAMDIAS BOREAS M2-51D (also sold as Rosewill M2-51D) | `1B80:B554` | Untested — see [Related Projects](#related-projects) |

GAMDIAS ships several sibling displays on nearby product IDs (`B533`, `B534`,
`B538`, `B53C`), which this driver does **not** currently claim. If you own one
and it responds to this protocol, a pull request adding its ID is welcome — see
`SupportedProducts` in `P2-62D/BoreasDevice.cs` and `install/99-boreas.rules`.

## Prerequisites

- .NET 8.0 Runtime
- `hidapi` library
- Linux kernel with hwmon support

### Install Dependencies

```bash
# Ubuntu/Debian
sudo apt install dotnet-runtime-8.0 libhidapi-hidraw0

# Fedora
sudo dnf install dotnet-runtime-8.0 hidapi

# Arch Linux
sudo pacman -S dotnet-runtime aspnet-runtime hidapi
```

## Building

```bash
dotnet build -c Release
```

## Installation

All commands below are run from the repository root.

### 1. Build and Install Binary

```bash
dotnet publish P2-62D/Boreas.csproj -c Release -o publish
sudo mkdir -p /usr/local/lib/boreas
sudo cp -r publish/* /usr/local/lib/boreas/
sudo ln -sf /usr/local/lib/boreas/boreas /usr/local/bin/boreas
```

### 2. Install udev Rules (for non-root access)

```bash
sudo cp install/99-boreas.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules
sudo udevadm trigger
```

The rule grants access to the `plugdev` group, so add yourself to it and log
out and back in:

```bash
sudo usermod -aG plugdev $USER
```

### 3. Install Configuration

```bash
sudo mkdir -p /etc/boreas
sudo cp P2-62D/config.json /etc/boreas/
```

### 4. Install systemd Service

```bash
sudo cp install/boreas.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now boreas
```

## Configuration

Edit `/etc/boreas/config.json`:

```json
{
  "UpdateIntervalMs": 1000,
  "Sensors": {
    "CpuTempPath": null,
    "CpuFanPath": null
  },
  "Display": [
    { "Mode": "CpuTempCelsius", "DurationSeconds": 10 },
    { "Mode": "CpuFanSpeed", "DurationSeconds": 5 }
  ]
}
```

### Options

| Field | Description |
|-------|-------------|
| `UpdateIntervalMs` | How often to update the display (milliseconds) |
| `CpuTempPath` | Path to temperature sensor, or `null` for auto-detect |
| `CpuFanPath` | Path to fan sensor, or `null` for auto-detect |
| `Display` | List of display modes to rotate through |

### Display Modes

- `CpuTempCelsius` - CPU temperature in °C
- `CpuTempFahrenheit` - CPU temperature in °F
- `CpuFanSpeed` - CPU fan speed in RPM

## Usage

```bash
# Show help
boreas --help

# Run with default config (/etc/boreas/config.json)
boreas

# Run with custom config
boreas /path/to/config.json

# List available sensors
boreas --list-sensors

# Generate sample config
boreas --generate-config

# Run display test
boreas --test
```

## Fan Speed Monitoring

The device itself does not read fan speed - it only displays what you send it. Fan speed is read from Linux hwmon sensors.

### Finding Your Fan Sensor

Many motherboards require a specific kernel module to expose fan sensors:

```bash
# List available sensors
boreas --list-sensors

# Check loaded hwmon drivers
ls /sys/class/hwmon/*/name
```

### MSI Motherboards (NCT6687)

MSI motherboards often use the NCT6687 Super I/O chip which requires a third-party driver:

```bash
# Install build dependencies
sudo apt install build-essential linux-headers-$(uname -r) dkms git

# Clone and install the driver
git clone https://github.com/Fred78290/nct6687d.git
cd nct6687d
sudo make dkms/install

# Load the module
sudo modprobe nct6687

# Make it load on boot
echo "nct6687" | sudo tee /etc/modules-load.d/nct6687.conf

# Verify fan sensors appear
cat /sys/class/hwmon/hwmon*/name | grep nct6687
```

### Other Motherboards

Run `sudo sensors-detect` from the `lm-sensors` package to identify and load the correct driver for your hardware.

## Protocol Documentation

The USB HID protocol was reverse-engineered from the Windows ZEUS CAST application.

### Packet Structure

Every packet is a **64-byte HID output report**. Only the first 13 bytes carry
data; the remainder is zero padding and must still be sent.

| Byte | Field | Description |
|------|-------|-------------|
| 0 | Header | `0x3A` |
| 1 | Header | `0xB5` |
| 2 | Command | `0x01` = display, `0x20` = init |
| 3-6 | Digits | Display digits, leftmost first (0-9, or `0x20` to blank) |
| 7 | Decimal | `0x01` = show decimal point |
| 8 | Unit | `0x01` = Celsius, `0x00` = Fahrenheit |
| 9 | CPU Icon | `0x01` = show CPU icon |
| 10 | Display Mode | `0x00` = temperature, `0x01` = fan |
| 11 | Flashing | `0x01` = flash display |
| 12 | Checksum | Sum of bytes 0-11 & 0xFF |

An init packet (command `0x20`, all other data bytes zero) is sent once after
connecting, before any display packet.

Byte 9 controls the CPU icon only — it is independent of the display mode in
byte 10. Temperature packets set it, fan packets clear it.

### Packet Types

**Temperature** (byte 10 = `0x00`):
- Digits represent value × 10 (e.g., 457 = 45.7°)
- Decimal point shown between digit 3 and 4
- Shows °C or °F icon based on byte 8
- Up to 2 leading zeros blanked (so 5.0° sends `0x20 0x20 5 0`)

**Fan** (byte 10 = `0x01`):
- Digits represent RPM directly (e.g., 1234 = 1234 RPM)
- No decimal point
- Shows fan icon
- Up to 3 leading zeros blanked (so 900 RPM sends `0x20 9 0 0`)

In both cases the value is clamped to the range 0-9999 before the digits are
extracted.

## Troubleshooting

### Device not found

1. Check the device is connected: `lsusb | grep 1b80`
2. Check udev rules are installed: `ls /etc/udev/rules.d/99-boreas.rules`
3. Reload udev: `sudo udevadm control --reload-rules && sudo udevadm trigger`

### No fan sensor detected

1. Run `boreas --list-sensors` to see available sensors
2. Install appropriate hwmon driver for your motherboard
3. Manually specify the fan path in config.json

### Permission denied

Ensure udev rules are installed and you're in the `plugdev` group:
```bash
sudo usermod -aG plugdev $USER
# Log out and back in
```

## Related Projects

- [gamdias-boreas-cpu-air-cooler-linux-driver](https://github.com/yanfuzhou/gamdias-boreas-cpu-air-cooler-linux-driver)
  by [@yanfuzhou](https://github.com/yanfuzhou) — a Rust implementation of the same
  protocol, also GPL-3.0. It needs no .NET runtime and ships prebuilt binaries, so
  it may be the better choice if you would rather not install a runtime. The
  M2-51D product ID listed above was identified by that project.

## License

This project is licensed under the GNU General Public License v3.0 - see the LICENSE file for details.

## Acknowledgments

- Protocol reverse-engineered from GAMDIAS ZEUS CAST Windows application
- Uses [HidApi.Net](https://github.com/badcel/HidApi.Net) for USB HID communication
- NCT6687 driver by [Fred78290](https://github.com/Fred78290/nct6687d)
