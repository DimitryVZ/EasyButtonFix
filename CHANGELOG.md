# Changelog

All notable changes to EasyButtonFix are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.0] - 2026-11-25

First public release.

### Added
- Blocks ghost clicks of bouncing mouse buttons: Back, Forward, Left, Right and Middle, each with its own block time (20–300 ms).
- **Auto-calibration**: press a button 10 times and get a recommended block time.
- Live activity list showing every press, its timing and whether it was blocked.
- Counter of blocked clicks per button and in total.
- Tray icon with pause and exit. Closing the window keeps protection running.
- Start with Windows (starts hidden in the tray).
- German and English UI, following the Windows language or chosen in settings.
- Windows 11 design (WPF-UI) with dark/light mode and Mica.
- Installer (Inno Setup, no admin rights) and portable ZIP.

[Unreleased]: https://github.com/DimitryVZ/EasyButtonFix/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/DimitryVZ/EasyButtonFix/releases/tag/v1.0.0
