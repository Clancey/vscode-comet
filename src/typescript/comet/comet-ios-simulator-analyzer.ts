import * as _ from "lodash";

import { parseString, selectPropertyPathItems } from "./xml-parsing";

export interface ISimulatorVersion
{
    name:string;
    id:string;
    versions:{name:string, id:string}[];
}
export class CometiOSSimulatorAnalyzer
{
    
  private xml: string;
  private parsedXml: any;
    public async ParseSumulators() : Promise<ISimulatorVersion[]>{
        

        this.parsedXml = await parseString(this.xml);

        return undefined;
    }

}