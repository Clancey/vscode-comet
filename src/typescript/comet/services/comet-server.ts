import * as vs from "vscode";
import { StdIOService, UnknownNotification, UnknownResponse } from "./stdio_service";
import * as c from "./comet-types"

export class FlutterDaemon extends StdIOService<UnknownNotification> {
	//public deviceManager: FlutterDeviceManager;

	constructor(flutterBinPath: string, projectFolder: string) {
		super(() => "comet-temp.log", true);

		this.createProcess(projectFolder, flutterBinPath, ["daemon"]);

		//this.deviceManager = new FlutterDeviceManager(this);

		// Enable device polling.
		this.deviceEnable();
	}

	public dispose() {
		//this.deviceManager.dispose();
		super.dispose();
	}

	protected sendMessage<T>(json: string) {
		try {
			super.sendMessage(json);
		} catch (e) {
			console.error(e);
			//reloadExtension("The Comet Daemon has terminated.");
			throw e;
		}
	}

	protected shouldHandleMessage(message: string): boolean {
		// Everything in comet is wrapped in [] so we can tell what to handle.
		return message.startsWith("[") && message.endsWith("]");
	}

	
	protected processUnhandledMessage(message: string): void {
		vs.window.showWarningMessage(message);
	}

	// TODO: Can we code-gen all this like the analysis server?

	protected handleNotification(evt: UnknownNotification) {
		switch (evt.event) {
			case "device.added":
				this.notify(this.deviceAddedSubscriptions, evt.params as c.Device);
				break;
			case "device.removed":
				this.notify(this.deviceRemovedSubscriptions, evt.params as c.Device);
				break;
		}
	}

	// Subscription lists.

	private deviceAddedSubscriptions: Array<(notification: c.Device) => void> = [];
	private deviceRemovedSubscriptions: Array<(notification: c.Device) => void> = [];

	// Request methods.

	public deviceEnable(): Thenable<UnknownResponse> {
		return this.sendRequest("device.enable");
	}

	public getEmulators(): Thenable<Array<{ id: string, name: string }>> {
		return this.sendRequest("emulator.getEmulators");
	}

	public launchEmulator(emulatorId: string): Thenable<void> {
		return this.sendRequest("emulator.launch", { emulatorId });
	}

	// Subscription methods.

	public registerForDeviceAdded(subscriber: (notification: c.Device) => void): vs.Disposable {
		return this.subscribe(this.deviceAddedSubscriptions, subscriber);
	}

	public registerForDeviceRemoved(subscriber: (notification: c.Device) => void): vs.Disposable {
		return this.subscribe(this.deviceRemovedSubscriptions, subscriber);
	}
}
