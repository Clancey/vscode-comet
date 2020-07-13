switch ($args[0]) {
	"all" {
		Write-Host "vsix created"
	}
	"vsix" {
		& ./node_modules/.bin/vsce package
	}
	"publish" {
		& ./node_modules/.bin/vsce publish
	}
	"build" {
		& msbuild /p:Configuration=Debug mono-debug.sln

		& npx webpack --mode production
		& node_modules/.bin/tsc -p ./src/typescript

		Write-Host "build finished"
	}
	"debug" {
		& msbuild /p:Configuration=Debug mono-debug.sln

		& node_modules/.bin/tsc -p ./src/typescript
	}
}
