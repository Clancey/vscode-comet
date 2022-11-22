$netVersion = "net7.0";

$cmd = $args[0];

if (-Not $cmd)
{
	$cmd = "debug"
}

function Vsix
{
	Write-Host "Creating VSIX..."
	& ./node_modules/.bin/vsce package -o ./vscode-comet.vsix
	if ($LASTEXITCODE) { Throw "Creating VSIX failed." }
	Write-Host "Done."
}
function BuildNet
{
	Write-Host "Building .NET Projects..."

	& dotnet build /r /p:Configuration=Debug ./src/mobile-debug/mobile-debug.csproj
	if ($LASTEXITCODE) { Throw "Building mobile-debug failed." }

	& dotnet build /r /p:Configuration=Debug ./src/DotNetWorkspaceAnalyzer/DotNetWorkspaceAnalyzer.csproj
	if ($LASTEXITCODE) { Throw "Building DotNetWorkspaceAnalyzer failed." }

	Write-Host "Done .NET Projects."

	Write-Host "Building Reloadify 3000 "

	& dotnet build /p:Configuration=Debug ./external/Reloadify3000/Reloadify.CommandLine/Reloadify.CommandLine.csproj
	if ($LASTEXITCODE) { Throw "Building Reloadify3000 failed." }

	$reloadifyDest = "./src/mobile-debug/bin/Debug/$netVersion/Reloadify/"

	Write-Host "Copying Reloadify 3000 output"
	if (Test-Path $reloadifyDest) {
		Remove-Item $reloadifyDest -Force -Recurse
	}
	New-Item -ItemType Directory -Force -Path $reloadifyDest
	Copy-Item  -Path "./external/Reloadify3000/Reloadify.CommandLine/bin/Debug/$netVersion/*"  -Destination $reloadifyDest -Recurse
}

function BuildTypeScript
{
	Write-Host "Building TypeScript..."

	& npm run webpack
	if ($LASTEXITCODE) { Throw "Building TypeScript failed." }

	Write-Host "Done TypeScript."
}


switch ($cmd) {
	"all" {
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
