import * as vscode from 'vscode';
import { EmulatorItem, XamarinEmulatorProvider } from "./sidebar"
import { create } from 'lodash';
import * as child from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

// Initiates the flow of creating a new project.  Is opinionated.

function execFileAsync(file: string, args?: string[]): Thenable<Error> {
    
    return new Promise((resolve) => {
        child.execFile(file, args, (error) => {
            resolve(error);
        });
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
