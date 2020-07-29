import * as vscode from 'vscode';
import { EmulatorItem, XamarinEmulatorProvider } from "./sidebar"
import { create } from 'lodash';

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

    
    var templateName: string = "forms-app"
    var status = createProject(templateName, folder.path);

    if (status) {
        vscode.window.showInformationMessage(`Created! template=${templateName}`);
        vscode.workspace.updateWorkspaceFolders(0, 1, { uri: folder });
    }

    // TODO: Move focus to Explorer somehow to make it obvious!
}   


function createProject(templateName: string, folderPath: string, recalled: boolean = false) : boolean {
    
    const cprocess = require('child_process');

    cprocess.exec(`dotnet new ${templateName} -o ${folderPath}`, (err, stdout, stderr) => {
        console.log('stdout 1: ' + stdout);
        console.log('stderr 1: ' + stderr);

        if (recalled)
            return err == 0;

        // Don't have the template installed
        if (err) {
            console.log(`error 1: ${err}`)
            vscode.window.showErrorMessage("Template not found...adding.");

            var thisExtension = vscode.extensions.getExtension('ms-vscode.xamarin');
            var extPath = thisExtension.extensionPath;
            cprocess.exec(`dotnet new --install ${extPath}/templates/Xamarin.Templates.Multiplatform.0.0.1.nupkg > /dev/null`, (err, sout, serr) => {
                
                console.log('stdout 2: ' + sout);
                console.log('stderr 2: ' + serr);
                
                if (err) {
                    console.log(`error 2: ${err}`)
                    vscode.window.showErrorMessage("Could not add template :(");  
                    return false;
                }

                // Success
                return createProject(templateName, folderPath, true);
                
            });
        }
        return true;
    });
    return true
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
