# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2024-01-06

### Added
- Initial release of BOREAS Linux Driver
- Support for GAMDIAS BOREAS P2-62D digital display (USB ID `1B80:B53A`)
- Temperature display with automatic CPU sensor detection
- Fan speed display with RPM monitoring
- Display rotation support (0°, 90°, 180°, 270°)
- JSON configuration file support
- Systemd service integration
- Udev rules for non-root device access
- Command-line interface with multiple options:
  - `--help` - Show usage information
  - `--list-sensors` - List available hwmon sensors
  - `--generate-config` - Generate sample configuration
  - `--test` - Test mode without daemon
- Auto-detection of CPU temperature sensors (coretemp, k10temp, zenpower)
- Auto-detection of CPU fan sensors
- Graceful shutdown handling (SIGTERM, Ctrl+C)
- USB HID protocol implementation for device communication

### Technical Details
- Built on .NET 8.0 Runtime
- Uses HidApi.Net for USB HID communication
- Reads sensors from Linux hwmon subsystem
- Update interval configurable (default: 1000ms)

[Unreleased]: https://github.com/RiaanAspeling/gamdias-boreas-P2-62D-linux/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/RiaanAspeling/gamdias-boreas-P2-62D-linux/releases/tag/v1.0.0
