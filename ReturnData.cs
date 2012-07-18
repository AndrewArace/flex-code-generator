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
    class ReturnData {

        public string EventPropertyName;
        public string EventPropertyDefinition;

        public ReturnData(string eventName, string eventDef) {
            this.EventPropertyName = eventName;
            this.EventPropertyDefinition = eventDef;
        }


        public override bool Equals(object obj) {
            ReturnData r2 = obj as ReturnData;
            return (r2.EventPropertyDefinition == this.EventPropertyDefinition && r2.EventPropertyName == this.EventPropertyName);
        }

    }
}
