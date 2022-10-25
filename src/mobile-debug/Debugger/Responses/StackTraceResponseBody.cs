/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Collections.Generic;
using System.Linq;
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class StackTraceResponseBody : ResponseBody
{
    public StackFrame[] stackFrames { get; }
    public int totalFrames { get; }

    public StackTraceResponseBody(List<StackFrame> frames, int total)
    {
        stackFrames = frames.ToArray<StackFrame>();
        totalFrames = total;
    }
}
