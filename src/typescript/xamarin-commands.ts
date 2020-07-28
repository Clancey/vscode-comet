import * as vscode from 'vscode';
import { EmulatorItem, XamarinEmulatorProvider } from "./sidebar"

// Initiates the flow of creating a new project.  Is opinionated.
export async function newProject() {
    var inputOpts : vscode.InputBoxOptions = {
        prompt: "Choose a name for your Xamarin Project",
        placeHolder: "MyCoolApp"
    }
    var project_name = await vscode.window.showInputBox(inputOpts)

    var dialogOpts: vscode.OpenDialogOptions = {
        canSelectFiles: false,
        canSelectFolders: true
    };
   var folder = await vscode.window.showOpenDialog(dialogOpts);

    // TEMP
    // Requires that you have added `forms-app` to your env via https://github.com/xamarin/xamarin-templates#creating-a-new-template
    const cprocess = require('child_process')
    cprocess.exec(`dotnet new forms-app -o ${folder[0].path}/${project_name}`, (err, stdout, stderr) => {
        vscode.window.showInformationMessage(`Creating ${project_name} at path ${folder[0].path}...`);
    console.log('stdout: ' + stdout);
    console.log('stderr: ' + stderr);
    if (err) {
        console.log('error: ' + err);
        vscode.window.showErrorMessage("Uh oh!");
    }
});
} 

export async function selectEmulator(evt: vscode.TreeViewSelectionChangeEvent<EmulatorItem>, treeViewProvider: XamarinEmulatorProvider) {

    if (evt.selection.length !== 1) {
        vscode.window.showErrorMessage(`There are ${evt.selection.length} emulators selected!`);
        return;
    }

    evt.selection.forEach(element => {
        treeViewProvider.CURRENT_EMULATOR = element;     
        vscode.commands.executeCommand('xamarinEmulator.refresh');   
    });
}