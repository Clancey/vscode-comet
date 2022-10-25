/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class TerminatedEvent : Event
{
    public TerminatedEvent()
        : base("terminated") { }
}
