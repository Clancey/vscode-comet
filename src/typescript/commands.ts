import * as vscode from 'vscode';
import { EmulatorItem, MobileEmulatorProvider } from "./sidebar"
import * as child from 'child_process';

// Initiates the flow of creating a new project.  Is opinionated.

function execFileAsync(file: string, args?: string[]): Thenable<Error> {
    
    return new Promise((resolve) => {
        child.execFile(file, args, (error) => {
            resolve(error);
        });
    });
}


export async function selectEmulatorTreeView(evt: vscode.TreeViewSelectionChangeEvent<EmulatorItem>, treeViewProvider: MobileEmulatorProvider) {

    if (evt.selection.length !== 1) {
        vscode.window.showErrorMessage(`There are ${evt.selection.length} emulators selected!`);
        return;
    }

    evt.selection.forEach(element => {
        treeViewProvider.CURRENT_EMULATOR = element;   
        treeViewProvider.refresh(element);  
    });
}
