/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class ConsoleOutputEvent : Event
{
    public ConsoleOutputEvent(string outpt)
        : base("output", new
        {
            category = "console",
            output = outpt.Trim() + Environment.NewLine
        })
    { }
}
