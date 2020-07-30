
$cmd = $args[0];

if (-Not $cmd)
{
	$cmd = "debug"
}

function Vsix
{
	& ./node_modules/.bin/vsce package
	Write-Host "vsix created"
}
function Build
{
	& msbuild /r /p:Configuration=Debug /p:nugetInteractive=true ./src/xamarin-debug/xamarin-debug.csproj

	& node_modules/.bin/tsc -p ./src/typescript

	Write-Host "build finished"
}

switch ($cmd) {
	"all" {
		Debug
		Build
		Vsix
	}
	"vsix" {
		Build
		Vsix
	}
	"build" {
		Build
	}
	"debug" {
		Build
	}
}