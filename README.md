# Comet Extension

Build .NET 6 Mobile Apps with MVU style UI with Comet

**Debug experience**

* Build and launch the app on the emulator
* Locals, Stacks, breakpoints, etc.
* Supports Android and iOS/MacCatalyst (macOS only)

## Run from Source

### Prerequisites
- [Powershell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7)
- [Node (npm)](https://nodejs.org/en/download/)
- Typescript: `npm install -g typescript`
- VSCE (to package VSIX): `npm install -g vsce`
- .NET 6 Preview with Android/iOS/Catalyst workloads (Install `dotnet tool install -g redth.net.maui.check` and run `maui-check` to ensure your environment is setup correctly)
- Mono (mac only)
- VSCode on Mac must have the `"omnisharp.useGlobalMono": "never"` setting to properly load net6.0 projects!

1. Clone repo recursively `git clone --recursive git@github.com:clancey/vscode-comet.git`
2. `pwsh build.ps1 build`
3. `npm i`
4. Debug the extension (there's an _Extension_ launch profile already setup in launch.json), and open up a .NET 6 project in the new VS Code instance being debugged.

## Debugging a .NET Mobile app with the Extension

1. Create a `.vscode/launch.json` file (The Debug tab has a "Create launch .json file" button which can help - choose 'Comet for .NET Mobile' from the list):
```json
{
	"version": "0.2.0",
	"configurations": [
		{
			"name": "Debug",
			"type": "comet",
			"request": "launch",
			"preLaunchTask": "comet: Build"
		}
	]
}
```
3. Choose a _Startup Project_ from the status bar menu.
4. Choose a _Device_ from the status bar menu.
5. Start debugging!


## Debugging the mobile-debug.exe process

1. Open the `./src/mobile-debug/mobile-debug.sln` in Visual Studio
2. Set the startup / debug args to `--server` (this tells the mobile-debug.exe process to listen on 4711 socket instead of expecting stdin/out for communication with VSCode extension)
3. Start debugging mobile-debug.exe
4. Open vscode-comet in VSCode and debug the _Extension_ launch profile which already exists in launch.json.  This will open a new instance of VSCode with the Comet extension loaded (being debugged by the first VSCode instance).
6. Load a valid project (ie: `dotnet new maui`) in the second instance of VSCode
7. Follow the steps in the section above (_Debugging a .NET Mobile app with the Extension_) to setup a `launch.json` for the project.
8. Before you start debugging, add the following setting to your launch.json: `"debugServer": 4711,` (this tells VSCode to connect to the 4711 socket which your mobile-debug.exe process is listening on now, as opposed to launching mobile-debug.exe directly itself and using stdio).
9. Start debugging your app from the second VSCode instance.
10. You should be able to hit breakpoints and see the VSCode instance connect to the mobile-debug.exe running process which you are debugging from Visual Studio.
