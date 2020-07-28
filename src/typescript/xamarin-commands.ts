import * as vscode from 'vscode';
import { EmulatorItem, XamarinEmulatorProvider } from "./sidebar"

// Initiates the flow of creating a new project.  Is opinionated.
export async function newProject() {

    var dialogOpts: vscode.OpenDialogOptions = {
        canSelectFiles: false,
        canSelectFolders: true,
        openLabel: "Create Xamarin Project"
    };
    var folderURI: vscode.Uri[] = await vscode.window.showOpenDialog(dialogOpts);
    var folder: vscode.Uri = folderURI[0];

    vscode.window.showInformationMessage(`Creating project at ${folder.path}...`);

    // TEMP
    // Requires that you have added `forms-app` to your env via https://github.com/xamarin/xamarin-templates#creating-a-new-template
    const cprocess = require('child_process')
    cprocess.exec(`dotnet new forms-app -o ${folder.path}`, (err, stdout, stderr) => {
    console.log('stdout: ' + stdout);
    console.log('stderr: ' + stderr);
    vscode.window.showInformationMessage("Success!");

    if (err) {
        console.log('error: ' + err);
        vscode.window.showErrorMessage("Uh oh!");
        return;
    }

    vscode.workspace.updateWorkspaceFolders(0, 1, { uri: folder });

    // TODO: Move focus to Explorer somehow to make it obvious!

    });
}   

export async function selectEmulatorTreeView(evt: vscode.TreeViewSelectionChangeEvent<EmulatorItem>, treeViewProvider: XamarinEmulatorProvider) {

    if (evt.selection.length !== 1) {
        vscode.window.showErrorMessage(`There are ${evt.selection.length} emulators selected!`);
        return;
    }

    evt.selection.forEach(element => {
        treeViewProvider.CURRENT_EMULATOR = element;   
        treeViewProvider.refresh(element);  
    });
}
