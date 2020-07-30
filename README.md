# Xamarin.Forms team hackathon 2020 project to create VS Code tooling prototype

Project Goals:

* Create a dedicated Xamarin extension that adds an emulator view, XAML IntelliSense, create new project and debugging experience in VS Code
* Create an extension pack that bundles together multiple extensions to create the full experience (C#, new Xamarin extension, etc.)
* Create a landing page experience for new Xamarin projects that provides helpful links and UI instructional overlay

All the above experiences should work in VS Code on both Windows and Mac

**Debug experience**

* Build and launch the app on the emulator
* Locals, Stacks, breakpoints, etc.
* Support both Android & iOS


## Run from Source

### Prereqs
- [Powershell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7)
- [Node (npm)](https://nodejs.org/en/download/)
- On mac: [Mono](https://www.mono-project.com/download/stable/)
- dotnet [dotnet core + cli](https://dotnet.microsoft.com/download/dotnet-core/3.1)

0. For now we'll expect you have the Xamarin SDK, etc present via a Visual Studio (proper) install
1. Clone repo recursively `git clone --recursive git@github.com:joshspicer/vscode-xamarin-ext.git`
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
