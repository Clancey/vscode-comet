/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

public class EvaluateResponseBody : ResponseBody
{
    public string result { get; }
    public string type { get; }

    public int variablesReference { get; }

    public VariablePresentationHint variablePresentationHint { get; }

    public EvaluateResponseBody(string value, int reff = 0, string type = null, VariablePresentationHint presentationHint = null)
    {
        result = value;
        variablesReference = reff;
        this.type = type;
        variablePresentationHint = presentationHint;
    }
}
