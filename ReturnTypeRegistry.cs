/*
 * Andrew Arace
 * 2010
 * http://code.google.com/p/flex-code-generator/
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FlexCodeGenerator {
    class ReturnTypeRegistry {

        static List<ReturnData> _returns = new List<ReturnData>();

        internal static ReturnData RegisterReturnType(ParameterInfo pi) {
            if (pi.ParameterType.Name != "Void") {

                string eventName;
                string eventDef;

                if (pi.ParameterType.IsGenericType) {
                    //a generic type, proably a generic list
                    Type[] genDef = pi.ParameterType.GetGenericArguments();
                    if (genDef.Length != 1) {
                        throw new NotImplementedException("Can not handle multi-argument generics.");
                    }
                    eventName = "my" + genDef[0].Name + "s";
                    eventDef = string.Format("public var {0}:ArrayCollection;", eventName);
                }
                else {
                    eventName = "my" + pi.ParameterType.Name;
                    eventDef = string.Format("public var {0}:{1};", eventName, pi.ParameterType.Name);
                }

                ReturnData ret = new ReturnData(eventName, eventDef);
                if (!_returns.Contains(ret)) {
                    _returns.Add(ret);
                }
                return ret;
            }
            else {
                System.Diagnostics.Debug.WriteLine(pi.ToString() + " void parameter skipped.");
            }
            return null;
        }
    }
}
