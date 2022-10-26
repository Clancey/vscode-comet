/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

// ---- Response -------------------------------------------------------------------------

public class Capabilities : ResponseBody
{

    public bool supportsConfigurationDoneRequest;
    public bool supportsFunctionBreakpoints;
    public bool supportsConditionalBreakpoints;
    public bool supportsEvaluateForHovers;
    public dynamic[] exceptionBreakpointFilters;

    public bool supportsStepBack = false;
    public bool supportsExceptionInfoRequest = true;
    public bool supportsSetExpression = false;
    public bool supportsSetVariable = false;
    public bool supportsDisassembleRequest = false;
    public bool supportsReadMemoryRequest = false;
    public bool supportsWriteMemoryRequest = false;
    public bool supportSuspendDebuggee = false;
    public bool supportTerminateDebuggee = false;
}
