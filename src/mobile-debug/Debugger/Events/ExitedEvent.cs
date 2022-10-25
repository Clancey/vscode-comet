/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class ExitedEvent : Event
{
    public ExitedEvent(int exCode)
        : base("exited", new { exitCode = exCode }) { }
}
