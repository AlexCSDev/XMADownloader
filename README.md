# XMADownloader
**This application is in active development, some features are expected to not work as intended or not work at all. Suboptimal performance is to be expected.**
**No prebuilt binaries will be provided until I am satistfied with the current implementation.**

This application is designed for downloading Final Fantasy XIV mods posted on XIV Mod Archive (https://xivmodarchive.com).

IMPORTANT: You need a valid XMA account to use this application. Login window will be shown on first launch.

## Usage
#### Download all available mods from user with all referenced mods
XMADownloader.App.exe --url #page url#. Page url should follow one of the following patterns:
* https://xivmodarchive.com/user/#numbers#
#### Download all available files from user into custom directory and save all possible data (mod description contents, raw html responses, crawl results json file)
XMADownloader.App.exe --url #page url# --download-directory c:\downloads --descriptions --html --export-results-json
#### Show available commands and their descriptions
XMADownloader.App.exe --help

## Recommendations
If you download a lot of files for the first time it is recommended to disable remote file size checking with `--disable-remote-file-size-check` to limit the possibility of triggering rate limiting on the server. Please be advised that this command will also disable validation of downloaded files.

## Build instructions
See docs\BUILDING.md

## Supported features
* Tested under Windows and Linux. Should work on any platform supported by .NET Core and Chromium browser.
* Downloading files from mods including referenced mod files
* Saving contents of mod description
* Saving website responses (mostly for troubleshooting purposes)
* External links extraction from mod page
	* C# plugin support (see below)
	* Limited/dumb direct link support (XMADownloader will attempt to download any file with valid extension if no suitable plugin is installed)
	* Dropbox support
	* Blacklist (configured in settings.json)
* Plugins (via C#)
	* Custom downloaders for adding download support for websites which need custom download logic
	* XMADownloader comes with the following plugins by default: Google Drive, Mega.nz, Patreon (public posts only)

## License
All files in this repository are licensed under the license listed in LICENSE.md file unless stated otherwise.
