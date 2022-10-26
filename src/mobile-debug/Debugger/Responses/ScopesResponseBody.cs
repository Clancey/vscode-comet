/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Collections.Generic;
using System.Linq;
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class ScopesResponseBody : ResponseBody
{
    public Scope[] scopes { get; }

    public ScopesResponseBody(List<Scope> scps)
    {
        scopes = scps.ToArray<Scope>();
    }
}
