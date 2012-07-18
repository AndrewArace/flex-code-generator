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
	class ServiceMethodData {
		private System.Reflection.MethodInfo minfo;
		public ReturnData ReturnData { get; set; }

		public string EventName {
			get {
				return minfo.Name.ToUpper() + "_RESULT";
			}
		}


		public string EventValue {
			get {
				return minfo.Name.ToLower() + "Result";
			}
		}


		public string EventFromAPIDeclaration {
			get {
				//[Event(name="getuserResult", type="services.events.APIEvent")]
				return String.Format("[Event(name=\"{0}\", type=\"services.events.APIEvent\")]", EventValue);
			}
		}


		public string EventDefinitionConst {
			get {
				return String.Format("\t\tpublic static const {0}:String = \"{1}\";", EventName, EventValue);
			}
		}


		public string MethodCallDefinition {
			get {
				/*
					public function CallGetAllInspections():void{
						var params:Array = new Array();
						params.push(API_KEY);
						handleGenericCall("GetAllInspections"
							, params
							, new APIEvent(APIEvent.INSPECTION_RESULTS)
							, "Inspections");
					}
				 */
				ParameterInfo[] pis = minfo.GetParameters();
				string paramDeclaractions = string.Empty;
				foreach (ParameterInfo p in pis) {
					if (paramDeclaractions.Length > 0) {
						paramDeclaractions += ",";
					}
					paramDeclaractions = paramDeclaractions + string.Format("{0}:{1}", p.Name, TypeConversion.NETTypeToAS(p.ParameterType.Name, p.ParameterType));
				}
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("\t\tpublic function Call{0}({1}):void{{\n", minfo.Name, paramDeclaractions);
				sb.AppendLine("\t\t\tvar params:Array = new Array();");
				foreach (ParameterInfo p in pis) {
					sb.AppendFormat("\t\t\tparams.push({0});\n", p.Name);
				}
				sb.AppendFormat("\t\t\thandleGenericCall(\"{0}\"\n", minfo.Name);
				sb.AppendLine("\t\t\t\t, params");
				sb.AppendFormat("\t\t\t\t, new APIEvent(APIEvent.{0})\n", EventName);
				sb.AppendFormat("\t\t\t\t, \"{0}\");\n", ReturnData.EventPropertyName);
				sb.AppendLine("\t\t}\n");
				return sb.ToString();
			}
		}


		public ServiceMethodData(System.Reflection.MethodInfo minfo) {
			this.minfo = minfo;
		}


	}
}
