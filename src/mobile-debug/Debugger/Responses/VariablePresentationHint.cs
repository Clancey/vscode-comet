/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace VSCodeDebug.Debugger;

public class VariablePresentationHint
{
    public VariablePresentationHint(string kind, string[] attrs)
    {
        this.kind = kind;
        attributes = attrs;
    }

    public string[] attributes { get; }

    public string kind { get; }
}
