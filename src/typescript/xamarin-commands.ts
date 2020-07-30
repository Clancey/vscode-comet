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

function execFileAsync(file: string, args?: string[]): Thenable<Error> {
    
    return new Promise((resolve) => {
        child.execFile(file, args, (error) => {
            resolve(error);
        });
    });
}

async function createProject(templateName: string, templateKind: string, folder: vscode.Uri) {

    const createProject = ["new", templateName, "-o", folder.fsPath, "-k", templateKind];
    var extPath = vscode.extensions.getExtension('ms-vscode.xamarin').extensionPath;
    const installTemplates = ["new", "--install", path.join(extPath, "templates", "Xamarin.Templates.Multiplatform.0.0.1.nupkg")];

    var error = await execFileAsync("dotnet", createProject);
    if (error) {
        error = await execFileAsync("dotnet", installTemplates);
        if (!error) {
            error = await execFileAsync("dotnet", createProject);
        }
    }

    if (error) {
        var message = error.message;
        if ((error as any).code == "ENOENT") {
            // Normally the error message is very informative. If dotnet can't be located the error message needs fixing.
            message = "Error! Make sure you have the dotnet cli tool installed.";
        }
        vscode.window.showErrorMessage(message);
    } else {
        vscode.window.showInformationMessage(`Created a ${templateKind}!`);
        await vscode.commands.executeCommand("vscode.openFolder", folder);
    }
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
