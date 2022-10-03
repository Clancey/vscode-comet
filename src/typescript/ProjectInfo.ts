import { TargetFrameworkInfo } from "./TargetFrameworkInfo";


export class ProjectInfo {

	Name: string;
	Path: string;
	AssemblyName: string;
	TargetFrameworks: TargetFrameworkInfo[];
	IsExe: boolean;
	Configurations: string[];
	Platforms: string[];
	OutputPath: string;
}
