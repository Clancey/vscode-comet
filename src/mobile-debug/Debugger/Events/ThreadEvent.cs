/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class ThreadEvent : Event
{
    public ThreadEvent(string reasn, int tid)
        : base("thread", new
        {
            reason = reasn,
            threadId = tid
        })
    { }
}
