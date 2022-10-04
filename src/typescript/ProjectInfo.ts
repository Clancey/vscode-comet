import { TargetFrameworkInfo } from "./TargetFrameworkInfo";

export interface IDictionary {
    [index:string]: string;
}

export class ProjectInfo {

	Name: string;
	Path: string;
	AssemblyName: string;
	TargetFrameworks: TargetFrameworkInfo[];
	IsExe: boolean;
	Configurations: string[];
	Platforms: string[];
	OutputPath: string;
	Properties: IDictionary;
}
