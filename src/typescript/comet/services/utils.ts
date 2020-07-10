import * as child_process from "child_process";
import * as fs from "fs";
import * as path from "path";
import { DebugProtocol } from "vscode-debugprotocol";

export const isWin = /^win/.test(process.platform);

const toolEnv = Object.create(process.env);
toolEnv.PUB_ENVIRONMENT = (toolEnv.PUB_ENVIRONMENT ? `${toolEnv.PUB_ENVIRONMENT}:` : "") + "vscode.comet";


export function safeSpawn(workingDirectory: string, binPath: string, args: string[]): child_process.ChildProcess {
	// Spawning processes on Windows with funny symbols in the path requires quoting. However if you quote an
	// executable with a space in its path and an argument also has a space, you have to then quote all of the
	// arguments too!
	// Tragic.
	// https://github.com/nodejs/node/issues/7367
	return child_process.spawn(`"${binPath}"`, args.map((a) => `"${a}"`), { cwd: workingDirectory, env: toolEnv, shell: true });
}
