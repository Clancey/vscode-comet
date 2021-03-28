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

1. Clone repo recursively `git clone --recursive git@github.com:clancey/vscode-comet.git`
2. `pwsh build.ps1 build`
3. `npm i`
4. Press F5 to debug 

## Debugging a Xamarin app with the Extension

1. Create a `launch.json` file (vscode can help you do this or you can create it with the text below:)
```json
{
	"version": "0.2.0",
	"configurations": [
		{
			"name": "Debug",
			"type": "xamarin",
			"request": "launch",
			"preLaunchTask": "xamarin: Build"
		}
	]
}
```
3. Choose a _Startup Project_ from the status bar menu.
4. Choose a _Device_ from the status bar menu.
5. Start debugging!
