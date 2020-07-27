import * as vscode from 'vscode';
import { timeStamp } from 'console';
import { DeviceData } from "./xamarinutil"
import { XamarinUtil } from "./xamarinutil"

class XamarinEmulatorProvider implements vscode.TreeDataProvider<EmulatorItem> {

    constructor(private workspaceRoot: string) {}

    onDidChangeTreeData?: vscode.Event<void | EmulatorItem>;

    getTreeItem(element: EmulatorItem): vscode.TreeItem | Thenable<vscode.TreeItem> {
        return element;
    }

    getChildren(element?: EmulatorItem): vscode.ProviderResult<EmulatorItem[]> {

        vscode.window.showInformationMessage("Starting getChildren()!");

        // if (!this.workspaceRoot) {
        //     vscode.window.showInformationMessage("Not in workspace root!")
        //     return 
        // }

        return Promise.resolve(this.getEmulatorsAndDevices());
    }

    getParent?(element: EmulatorItem): vscode.ProviderResult<EmulatorItem> {
        throw new Error("Method not implemented.");
    }

    async getEmulatorsAndDevices() : Promise<EmulatorItem[]> {
        var util = new XamarinUtil();

        var results = new Array<EmulatorItem>();
        var devices = await util.GetAndroidDevices();

        for (var device of devices) {
            results.push(new EmulatorItem(device.name, "android", device.serial, device.isEmulator, device.isRunning));
        }

        return results;
    }
}

class EmulatorItem extends vscode.TreeItem {

    constructor(
        public readonly name: string,
        serial: string,
        platform: string,
        isEmulator: boolean = false,
        isRunning: boolean = false,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState = vscode.TreeItemCollapsibleState.None) 
    {
        super(name, collapsibleState);

        this.serial = serial;
        this.isEmulator = isEmulator;
        this.isRunning = isRunning;
    }

    get tooltip(): string {
		return `${this.label}-Android emulator`;
    }
    
    serial: string;
    isEmulator: boolean;
    isRunning: boolean;
    platform: string;

    // description()
    // contextValue = 

}

 export default XamarinEmulatorProvider;