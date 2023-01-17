# XMADownloader
**This application is in active development, some features are expected to not work as intended or not work at all**
This application is designed for downloading FFXIV mods posted on XIV Mod Archive.

IMPORTANT: You need a valid account to download NSFW content. You will be prompted to login on the first launch.

## Usage
#### Download all available files from user
XMADownloader.App.exe --url #page url#. Page url should follow one of the following patterns:
* https://xivmodarchive.com/user/#numbers#
#### Download all available files from user into custom directory and save all possible data (mod description contents, raw html responses)
XMADownloader.App.exe --url #page url# --download-directory c:\downloads --descriptions --html
#### Show available commands and their descriptions
XMADownloader.App.exe --help

## Build instructions
See docs\BUILDING.md

## Supported features
* Tested under Windows and Linux. Should work on any platform supported by .NET Core and Chromium browser.
* Downloading files from mods
* Saving contents of mod description
* Saving website responses (mostly for troubleshooting purposes)
* External links extraction from mod page
	* C# plugin support (see below)
	* Limited/dumb direct link support (XMADownloader will attempt to download any file with valid extension if no suitable plugin is installed)
	* Dropbox support
	* Blacklist (configured in settings.json)
* Plugins (via C#)
	* Custom downloaders for adding download support for websites which need custom download logic
	* XMADownloader comes with the following plugins by default: Google Drive, Mega.nz, Patreon (not logged in mode only)

## License
All files in this repository are licensed under the license listed in LICENSE.md file unless stated otherwise.
