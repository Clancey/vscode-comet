import * as xml2js from "xml2js";
import * as _ from "lodash";

export function parseString(xml: string): Promise<any> {
  return new Promise<any>((resolve, reject) => {
    xml2js.parseString(xml, (err, result) => {
      if (err) {
        reject(err);
      } else {
        resolve(result);
      }
    });
  });
}

// Selects items that match the property path. This function work like a 
// simple XPath selector.
export function selectPropertyPathItems<T>(obj: any, propertyPath: string[]): T[] {
  let result: T[] = [obj];

  for (let i = 0; i < propertyPath.length; i++) {
    let propertyName = propertyPath[i];
    result = _.flatMap(result, o => {
      if (o === undefined || o == null) {
        return [];
      }
      let value = o[propertyName];
      if (value instanceof Array) {
        return value;
      } else if (value === undefined || value === null) {
        return [];
      } else {
        return [value];
      }
    }) as T[];
  }

  return result;
}
