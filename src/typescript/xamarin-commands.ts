import * as vscode from 'vscode';
import { EmulatorItem, XamarinEmulatorProvider } from "./sidebar"
import { create } from 'lodash';
import * as child from 'child_process';
import * as path from 'path';

// Initiates the flow of creating a new project.  Is opinionated.
export async function newProject() {

    var dialogOpts: vscode.OpenDialogOptions = {
        canSelectFiles: false,
        canSelectFolders: true,
        openLabel: "Create Xamarin Project"
    };
    var folderURI: vscode.Uri[] = await vscode.window.showOpenDialog(dialogOpts);
    var folder: vscode.Uri = folderURI[0];

    var templateName: string = "forms-app";
    var templateKinds = ["blank", "master-detail", "tabbed", "shell"];

    var quickpicks = await vscode.window.showQuickPick(templateKinds, {
        canPickMany: false,
        placeHolder: "Choose a Xamarin Forms template"
    });
    var templateKind: string = quickpicks[0];

    vscode.window.showInformationMessage(`Creating project....`);

    await createProject(templateName, templateKind, folder);
}   

async function createProject(templateName: string, templateKind: string, folder: vscode.Uri, haveTriedInstallingTemplate: boolean = false) {

    const cmd1 = ["new", templateName, "-o", folder.fsPath, "-k", templateKind];
    var extPath = vscode.extensions.getExtension('ms-vscode.xamarin').extensionPath;
    const cmd2 = ["new", "--install", path.join(extPath, "templates", "Xamarin.Templates.Multiplatform.0.0.1.nupkg")];
    child.execFile("dotnet", cmd1, async (err1) => {

        // Error expected if user doesn't have templates installed
        if (err1) {
            if (!haveTriedInstallingTemplate) {
                // Install embedded Xamarin Forms templates
                child.execFile("dotnet", cmd2, (err2) => {
                    if (!err2) {
                        return createProject(templateName, templateKind, folder, true);
                    } else {
                        vscode.window.showErrorMessage(err2.message);
                    }
                });
            } else {
                vscode.window.showErrorMessage(err1.message);
            }
            return;
        }

        // Success Messages
        vscode.window.showInformationMessage(`Created a ${templateKind}!`);
        await vscode.commands.executeCommand("vscode.openFolder", folder);
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
