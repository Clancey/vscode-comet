
$cmd = $args[0];

if (-Not $cmd)
{
	$cmd = "debug"
}

function Vsix
{
	Write-Host "Creating VSIX..."
	& ./node_modules/.bin/vsce package
	Write-Host "Done."
}
function BuildNet
{
	Write-Host "Building .NET Project..."

	& msbuild /r /p:Configuration=Debug ./src/mobile-debug/mobile-debug.csproj

	Write-Host "Done .NET Project."

	Write-Host "Building Reloadify 3000 "

	& dotnet build /p:Configuration=Debug ./external/Reloadify3000/Reloadify.CommandLine/Reloadify.CommandLine.csproj

	Write-Host "Copying Reloadify 3000 output"
	& Copy-Item  ./external/Reloadify3000/Reloadify.CommandLine/bin/Debug/net6.0/*  ./src/mobile-debug/bin/Debug/net472/Reloadify -Recurse -force
}

function BuildTypeScript
{
	Write-Host "Building TypeScript..."

	& npm run webpack

	Write-Host "Done TypeScript."
}


switch ($cmd) {
	"all" {
		Debug
		BuildNet
		BuildTypeScript
		Vsix
	}
	"vsix" {
		BuildNet
		BuildTypeScript
		Vsix
	}
	"build" {
		BuildNet
		BuildTypeScript
	}
	"debug" {
		BuildNet
		BuildTypeScript
	}
	"ts" {
		BuildTypeScript
	}
	"net" {
		BuildNet
	}
}