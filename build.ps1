
$cmd = $args[0];

if (-Not $cmd)
{
	$cmd = "debug"
}

$ExcludeHotReload = (Test-Path -Path .\exclude-hot-reload.txt)

function Vsix
{
	Write-Host "Creating VSIX..."
	& ./node_modules/.bin/vsce package
	Write-Host "Done."
}
function Build
{
	Write-Host "Building...(ExcludeHotReload: $ExcludeHotReload)"

	& msbuild /r /p:Configuration=Debug /p:nugetInteractive=true /p:ExcludeHotReload=$ExcludeHotReload ./src/xamarin-debug/xamarin-debug.csproj

	& tsc -p ./src/typescript

	Write-Host "Done (ExcludeHotReload: $ExcludeHotReload)."
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