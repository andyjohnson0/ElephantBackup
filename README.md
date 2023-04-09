# Elephant Backup

Elephant Backup is a simple console-based backup application for Windows. You can use it to back up one or more directory
hierarchies to a target such as a removable device or local directory. Files are stored as exact copies, making it suitable
for long-term storage of data.

Use of this free software is at your own risk. You should accept responsibility for evaluating its reliability and
suitability for your needs.

## Getting Started

1. Download the pre-built binary from the [releases](https://github.com/andyjohnson0/ElephantBackup/releases)
page, or clone the repo and build it yourself.

2. Add eb.exe to your path

3. In a command box run `eb /createconfig` to create a eb.config file in your home directory, then edit this
   to specify the backup you want.

4. Run `eb` to do a backup.

## Prerequisites

.net 6 or later

## Author

Andrew Johnson | [github.com/andyjohnson0](https://github.com/andyjohnson0) | https://andyjohnson.uk

## Licence

This project is licensed under the terms of the MIT license. See the [licence file](LICENSE.md) for more information.
