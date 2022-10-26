import { DeviceData } from "./util";
import { ProjectInfo } from "./ProjectInfo";
import { TargetFrameworkInfo } from "./TargetFrameworkInfo";


export class MobileSessionStartupInfo {
	constructor() {
		this.Project = undefined;
		this.Configuration = undefined;
		this.Platform = undefined;
		this.TargetFramework = undefined;
		this.DebugPort = 55559;
		this.Device = undefined;
	}

	Project: ProjectInfo;
	Configuration: string;
	Platform: string;
	TargetFramework: TargetFrameworkInfo;
	Device: DeviceData;
	DebugPort: number = 55559;
}
