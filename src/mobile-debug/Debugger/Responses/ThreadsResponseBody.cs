/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Collections.Generic;
using System.Linq;
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class ThreadsResponseBody : ResponseBody
{
    public Thread[] threads { get; }

    public ThreadsResponseBody(List<Thread> ths)
    {
        threads = ths.ToArray<Thread>();
    }
}
