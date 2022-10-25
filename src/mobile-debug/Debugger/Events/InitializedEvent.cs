/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

// ---- Events -------------------------------------------------------------------------

public class InitializedEvent : Event
{
    public InitializedEvent()
        : base("initialized") { }
}
