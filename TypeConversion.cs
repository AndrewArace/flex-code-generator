/*
 * Andrew Arace
 * 2010
 * http://code.google.com/p/flex-code-generator/
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlexCodeGenerator {
    internal class TypeConversion {

        internal static string NETTypeToAS(string returnName, Type returnType) {
            
            switch (returnName) {
                case ("String"):
                    return "String";
                case ("Int32"):
                case ("Double"):
                case ("Decimal"):
                case ("float"):
                case ("Single"):
                    return "Number";
                case ("DateTime"):
                    return "Date";
                case ("Boolean"):
                    return "Boolean";
                case ("List`1"):
                case ("ArrayList"):
                    return "ArrayCollection";
                case ("Dictionary"):
                case ("Dictionary`2"):
                case ("Object"):
                case("Hashtable"):
                    return "Object";
                case ("Array"):
                    return "Array";
                case ("Byte[]"):
                    return "ByteArray";
                case("Nullable`1"):
                    //look at the nullable underlying type
                    Type t = Nullable.GetUnderlyingType(returnType);
                    return NETTypeToAS(t.Name, t);
                default:
                    return returnName;
            }
        }

    }
}
