/*
 * Andrew Arace
 * 2010
 * http://code.google.com/p/flex-code-generator/
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace FlexCodeGenerator {

	class Program {
		static bool _deleteConfirmed = false;
		static bool _defaultStrings = false;
		static bool _defaultNumbers = false;
		static bool _bindable = false;
		static Type _fluorineClass = null;
		static string _flexPackage = null;

		private static List<string> Classes;
		private static void PrintUsage() {
			PrintUsage(null);
		}


		private static void PrintUsage(string error) {
			if (!string.IsNullOrEmpty(error)) {
				Console.WriteLine(error);
			}
			Console.WriteLine("Generates code for Flex ActionScript classes based on .NET assemblies.\n");
			Console.Write("fcg <dll names-comma separated> [outputdir] [options]");
			Console.WriteLine("\n\nOptions");
			Console.Write("[-p] <packagename> \t\tOverride actionscript package with <packagename>.\n");
			Console.Write("[-e]\t\tInclude unknown types (non .net primitive).\n");
			Console.Write("\t\t\teg.  public var MyAddress:AddressObject;\n");
			Console.Write("[-m]\t\tGenerate Model Loader Class.\n\n");
			Console.Write("[-y]\t\tAutomatically delete any *.as files in output dir.\n\n");
			Console.Write("[-ds]\t\tDefault all strings to \"\".\n\n");
			Console.Write("[-di]\t\tDefault all numbers to -1.\n\n");
			Console.Write("[-b]\t\tAdd [Bindable] to class.\n\n");
		}


		static void Main(string[] args) {
			if (args == null || args.Length < 2) {
				PrintUsage();
				return;
			}

			string assemblyPath = args[0];
			string[] assemblyPaths = assemblyPath.Split(",".ToCharArray());
			
			string outputDir = string.Empty;
			bool includeUnk = false;
			bool includeModelLoader = false;
			outputDir = Directory.GetCurrentDirectory();

			foreach(string a in assemblyPaths) {
				if (!File.Exists(a)) {
					PrintUsage(string.Format("Could not find assembly {0}", a));
					return;
				}
			}

			for (int i = 1; i < args.Length; i++) {
				string item = args[i];
				if (item.StartsWith("-")) {
					switch (item.ToLower()) {
						case ("-e"):
							includeUnk = true;
							break;
						case ("-m"):
							includeModelLoader = true;
							break;
						case ("-y"):
							_deleteConfirmed = true;
							break;
						case ("-ds"):
							_defaultStrings = true;
							break;
						case ("-di"):
							_defaultNumbers = true;
							break;
						case ("-b"):
							_bindable = true;
							break;
						case ("-p"):
							if (!args[i + 1].StartsWith("-")) {
								_flexPackage = args[i + 1];
								i++;
								Console.WriteLine("Overrideing actionscript package with {0}", _flexPackage);
							}
							else {
								PrintUsage("Incorrect switch -p");
								return;
							}
							break;
						default:
							Console.WriteLine("Unrecognized switch: {0}, exiting...", item);
							return;
					}
				}
				else {
					//output dir param
					outputDir = args[1];
				}
			}

			#region Output Dir Checking

			string[] existingFilees = Directory.GetFiles(outputDir, "*.as");
			if (existingFilees != null && existingFilees.Length > 0) {
				string answer;
				if (!_deleteConfirmed) {
					Console.Write("All .as files in output directory will be removed. Continue? [Y/N] ");
					answer = Console.ReadLine();
					if (answer.ToUpper() != "Y") {
						Console.WriteLine("Exiting...");
						return;
					}
				}
				Console.WriteLine("Deleting all *.as files in output...");
				foreach (string f in existingFilees) {
					try {
						File.Delete(f);
					}
					catch (IOException ioex) {
						Console.WriteLine(string.Format("Failed to delete {0}, {1}. Exiting...", f, ioex.Message));
						return;
					}
				}
			}
			#endregion
			
			//for building the modelloader
			Classes = new List<string>();
			foreach (string a in assemblyPaths) {
				Console.Out.WriteLine("Generating Flex/ActionScript Classes...");
				Assembly asm = System.Reflection.Assembly.LoadFrom(a);
				Type[] types = asm.GetExportedTypes();

				foreach (Type t in types) {
					object[] attrs = t.GetCustomAttributes(false);
					if (attrs != null && attrs.Length > 0) {
						bool foundFx = false;
						foreach (object o in attrs) {
							if (o.GetType().Name == "RemotingServiceAttribute") {
								//we have found the Fluorine remoting class, we need to disect it later
								_fluorineClass = t;
								foundFx = true;
								break;
							}
						}
						if (foundFx) {
							continue;
						}
					}

					if (!(t.BaseType == typeof(MulticastDelegate))) {
						WriteTypeAS(t, outputDir, includeUnk);
					}
					else {
						Console.Out.WriteLine(String.Format("Skipped {0}, non Object.", t.Name));
					}
				}

				if (includeModelLoader) {
					Console.Out.WriteLine();
					Console.Out.WriteLine("Generating ModelLoader...");
					GenerateModelLoader(outputDir);
				}

				if (_fluorineClass != null) {
					Console.Out.WriteLine("Generating FluorineFX service handler...");
					GenerateFXHandler(outputDir);
				}
			}

			Console.Out.WriteLine();
			Console.Out.WriteLine("Finished exporting.");
#if DEBUG
			Console.WriteLine("Press <enter> to quit...");
			Console.In.ReadLine();
#endif
		}


		private static void AddFileHeader(StringBuilder sb) {
			sb.AppendLine("// THIS CLASS WAS GENERATED FROM A .NET ASSEMBLY BY THE GEONETICS FLEX CODE GENERATOR");
			sb.AppendLine("// KEEP IN MIND THAT ANY CUSTOM CODE ADDED TO THIS CLASS WILL BE OVERWRITTEN");
			sb.AppendLine("// IF THE CODE IS REGENERATED INTO YOUR FLEX PROJECT BY THE FCG");
			sb.AppendLine();
		}


		private static void GenerateModelLoader(string outputDir) {
			if (Classes == null || Classes.Count == 0)
				return;
			string pkg = null;
			if (!string.IsNullOrEmpty(_flexPackage)) {
				pkg = _flexPackage;
			}
			else {
				pkg = "Model";
			}

			StringBuilder sb = new StringBuilder();
			AddFileHeader(sb);
			sb.AppendFormat("package {0} {{\n", pkg);
			sb.AppendLine("\timport flash.events.EventDispatcher;");
			sb.AppendLine("\timport flash.events.IEventDispatcher;");
			sb.AppendLine();
			sb.AppendLine("\tpublic class ModelLoader extends EventDispatcher {");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate static var _instance:ModelLoader;");
			sb.AppendLine("\t\tpublic static function get Instance() : ModelLoader {");
			sb.AppendLine("\t\t\tif(_instance == null) {");
			sb.AppendLine("\t\t\t\t_instance = new ModelLoader();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\treturn _instance;");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t\tpublic function ModelLoader(target:IEventDispatcher=null) {");
			sb.AppendLine("\t\t\tsuper(target);");
			foreach (string s in Classes) {
				sb.AppendFormat("\t\t\tnew {0}();\n", s);
			}
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");
			sb.AppendLine();
			sb.AppendLine("}");

			File.WriteAllText(Path.Combine(outputDir, "ModelLoader.as"), sb.ToString());
		}


		private static void GenerateFXHandler(string outputDir) {
			MethodInfo[] minfos = _fluorineClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			List<ServiceMethodData> serviceMethods = new List<ServiceMethodData>();

			foreach (MethodInfo minfo in minfos) {
				if (minfo.ReturnParameter.ParameterType.Name != "Void") {
					ServiceMethodData smd = new ServiceMethodData(minfo);
					serviceMethods.Add(smd);
					smd.ReturnData = ReturnTypeRegistry.RegisterReturnType(minfo.ReturnParameter);
				}
			}

			StringBuilder sb = new StringBuilder();

			string pkg = null;
			if (!string.IsNullOrEmpty(_flexPackage)) {
				pkg = _flexPackage;
			}
			else {
				pkg = _fluorineClass.Namespace;
			}

			AddFileHeader(sb);
			sb.AppendFormat("package {0} {{\n", pkg);
			sb.AppendLine("\timport mx.core.FlexGlobals;");
			sb.AppendLine("\timport flash.events.Event;");
			sb.AppendLine("\timport flash.events.EventDispatcher;");
			sb.AppendLine("\timport flash.events.IEventDispatcher;");
			sb.AppendLine("\timport mx.rpc.remoting.RemoteObject;");
			sb.AppendLine("\timport mx.rpc.events.FaultEvent;");
			sb.AppendLine("\timport mx.rpc.events.ResultEvent;");
			sb.AppendLine("\timport mx.rpc.AbstractOperation;");
			sb.AppendLine();
			foreach (ServiceMethodData smd in serviceMethods) {
				sb.AppendLine("\t" + smd.EventFromAPIDeclaration);
			}
			sb.AppendLine("\tpublic class ModelAPI extends EventDispatcher {");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate static var _instance:ModelAPI;");
			sb.AppendLine("\t\tpublic static function get Instance() : ModelAPI {");
			sb.AppendLine("\t\t\tif(_instance == null) {");
			sb.AppendLine("\t\t\t\t_instance = new ModelAPI();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\treturn _instance;");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic static var GatewayURL:String = FlexGlobals.topLevelApplication.parameters.gatewayURI;");
			sb.AppendLine("\t\tprivate var _service:RemoteObject;");

			sb.AppendLine();
			sb.AppendLine("\t\tpublic function ModelAPI(target:IEventDispatcher=null) {");
			sb.AppendLine("\t\t\tsuper(target);");
			sb.AppendLine("\t\t\t_service = new RemoteObject(\"fluorine\");");
			sb.AppendLine("\t\t\tif (GatewayURL == null || GatewayURL == \"\") {");
			sb.AppendLine("\t\t\tGatewayURL = \"http://localhost/gateway.aspx\";");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\t_service.endpoint = GatewayURL;");
			sb.AppendLine("\t\t\t_service.showBusyCursor = true;");
			sb.AppendLine("\t\t\t_service.makeObjectsBindable = true;");
			sb.AppendLine("\t\t\t_service.source = \"InspectionFluorineLibrary.InspectionFxService\";");
			sb.AppendLine("\t\t\t_service.addEventListener(FaultEvent.FAULT, handleFault);");
			sb.AppendLine("\t\t}");

			sb.AppendLine();
			sb.AppendLine("\t\tprivate function handleFault(event:FaultEvent) : void {");
			sb.AppendLine("\t\t\ttrace(event.message[\"faultString\"]);");
			sb.AppendLine("\t\t}");

			sb.AppendLine();
			sb.AppendLine("\t\tprivate function handleGenericCall(functionName:String, ");
			sb.AppendLine("\t\t\tparameters:Array, ");
			sb.AppendLine("\t\t\tresultEvent:Event, ");
			sb.AppendLine("\t\t\tresultProperty:String) : void {");
			sb.AppendLine("\t\t\tvar serviceFunction:AbstractOperation = _service.getOperation(functionName);");
			sb.AppendLine("\t\t\tvar funcHandleResult:Function = function handleResult(revent:ResultEvent) : void {");
			sb.AppendLine("\t\t\t\tserviceFunction.removeEventListener(ResultEvent.RESULT, funcHandleResult);");
			sb.AppendLine("\t\t\t\tvar resultObject:* = revent.result;");
			sb.AppendLine("\t\t\t\tresultEvent[resultProperty] = resultObject;");
			sb.AppendLine("\t\t\t\tdispatchEvent(resultEvent);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\tserviceFunction.addEventListener(ResultEvent.RESULT, funcHandleResult);");
			sb.AppendLine("\t\t\tserviceFunction.arguments = parameters;");
			sb.AppendLine("\t\t\tserviceFunction.send();");
			sb.AppendLine("\t\t}");

			
			foreach (ServiceMethodData smd in serviceMethods) {
				sb.AppendLine(smd.MethodCallDefinition);
			}

			WriteServiceEventClass(outputDir, serviceMethods);

			sb.AppendLine("\t}"); //class
			sb.AppendLine("}"); //namespace
			File.WriteAllText(Path.Combine(outputDir, "ModelAPI.as"), sb.ToString());
		}
		

		private static void WriteServiceEventClass(string outputDir, List<ServiceMethodData> methods) {
			StringBuilder eventsb = new StringBuilder();

			eventsb.Append("package services.events {\n");
			eventsb.AppendLine("\timport flash.events.Event;");
			eventsb.AppendLine("\tdynamic public class APIEvent extends Event {");
			foreach (ServiceMethodData smd in methods) {
				eventsb.AppendLine(smd.EventDefinitionConst);
			}
			eventsb.AppendLine();
			List<string> usedReferences = new List<string>();
			foreach (ServiceMethodData smd in methods) {
				if (!usedReferences.Contains(smd.ReturnData.EventPropertyDefinition)) {
					eventsb.AppendLine("\t\t" + smd.ReturnData.EventPropertyDefinition);
					usedReferences.Add(smd.ReturnData.EventPropertyDefinition);
				}
			}

			eventsb.AppendLine("\t\tpublic function APIEvent(type:String, bubbles:Boolean=false, cancelable:Boolean=false) {");
			eventsb.AppendLine("\t\t\ttrace(\"APIEvent.\" + type);");
			eventsb.AppendLine("\t\t\tsuper(type, bubbles, cancelable);");
			eventsb.AppendLine("\t\t}");

			eventsb.AppendLine("\t}"); //class
			eventsb.AppendLine("}"); //namespace

			/*
				package services.events {
	
					import Model.TransactionResult;
					import flash.events.Event;
					import mx.collections.ArrayCollection;
	
					dynamic public class APIEvent extends Event {
						public static const INSPECTION_RESULTS:String = "INSPECTION_RESULTS";
						public static const SAVE_RESULT:String = "SAVE_RESULT";
		
						public var Inspections:ArrayCollection;
						public var SaveResult:TransactionResult;
		
						public function APIEvent(type:String, bubbles:Boolean=false, cancelable:Boolean=false) {
							trace("APIEvent." + type);
							super(type, bubbles, cancelable);
						}
		
					}
				}
			 */
			File.WriteAllText(Path.Combine(outputDir, "APIEvent.as"), eventsb.ToString());
		}


		private static void WriteTypeAS(Type t, string outputDir, bool includeUnknown) {
			PropertyInfo[] props = t.GetProperties();
			MemberInfo[] mi = t.GetMembers();
			FieldInfo[] fis = t.GetFields();
			string pkg = null;
			if (!string.IsNullOrEmpty(_flexPackage)) {
				pkg = _flexPackage;
			}
			else {
				pkg = t.Namespace;
			}
			
			if (((props != null && props.Length > 0) || (fis != null && fis.Length > 0)) && !t.IsEnum) {
				Console.Out.WriteLine("\t{0}.as", t.Name);
				Classes.Add(t.Name);

				StringBuilder sb = new StringBuilder();
				AddFileHeader(sb);
				sb.AppendFormat("package {0} {{\n", pkg);
				sb.AppendLine();
				sb.AppendLine("\timport mx.collections.ArrayCollection;");
				sb.AppendLine();
				sb.AppendFormat("\t[RemoteClass(alias=\"{0}\")]\n", t.FullName);
				sb.AppendLine("\t[Bindable]");
				sb.AppendFormat("\tpublic class {0} {{\n", t.Name);

				foreach (PropertyInfo pi in props) {
					if (pi.CanRead) {
						string propName = pi.Name;
						string returnName = pi.PropertyType.Name;
						WriteTypeDeclaration(includeUnknown, sb, propName, returnName, pi.PropertyType);
					}
				}

				foreach (FieldInfo fi in fis) {
					string propName = fi.Name;
					string returnName = fi.FieldType.Name;
					WriteTypeDeclaration(includeUnknown, sb, propName, returnName, fi.FieldType);
				}
				sb.AppendLine("\t}");
				sb.AppendLine();
				sb.AppendLine("}");

				File.WriteAllText(Path.Combine(outputDir, t.Name + ".as"), sb.ToString());
			}
			else {
				if (t.IsEnum) {
					StringBuilder sb = new StringBuilder();
					AddFileHeader(sb);
					sb.AppendFormat("package {0} {{\n", pkg);
					sb.AppendLine();
					if (_bindable) {
							sb.AppendLine("\t[Bindable]");
					}
					sb.AppendFormat("\tpublic class {0} {{\n", t.Name);
					foreach (object value in t.GetEnumValues()) {
						sb.AppendFormat("\t\tpublic static const {0}:Number = {1};\n", t.GetEnumName(value),  (int)value);
					}
					sb.AppendLine("");
					sb.AppendLine("\t\tpublic static function NumberToString(n:Number) : String {");
					sb.AppendLine("\t\t\tswitch(n) {");
					foreach (object value in t.GetEnumValues()) {
						sb.AppendFormat("\t\t\t\tcase({0}):\n", (int)value);
						sb.AppendFormat("\t\t\t\t\treturn \"{0}\";\n", t.GetEnumName(value));
					}
					sb.AppendLine("\t\t\t}");
					sb.AppendLine("\t\t\treturn \"UNKNOWN\";");
					sb.AppendLine("\t\t}");
					sb.AppendLine("");
					sb.AppendLine("\t\tpublic static function DataSourceForBinding(includeBlank:Boolean) : Array {");
					sb.AppendLine("\t\t\tvar a:Array = new Array();");
					sb.AppendLine("\t\t\tvar o:Object;");
					sb.AppendLine("\t\t\tif(includeBlank) {");
					sb.AppendLine("\t\t\t\to = new Object();");
					sb.AppendLine("\t\t\t\to.Name = \"\";");
					sb.AppendLine("\t\t\t\to.Value = -1;");
					sb.AppendLine("\t\t\t\ta.push(o);");
					sb.AppendLine("\t\t\t}");
					sb.AppendFormat("\t\t\tfor(var i:Number = 1; i <= {0}; i++) {{\n", t.GetEnumValues().Length);
					sb.AppendLine("\t\t\t\to = new Object();");
					sb.AppendLine("\t\t\t\to.Value = i;");
					sb.AppendLine("\t\t\t\to.Name = NumberToString(i);");
					sb.AppendLine("\t\t\t\ta.push(o);");
					sb.AppendLine("\t\t\t}");
					sb.AppendLine("\t\t\treturn a;");
					sb.AppendLine("\t\t}");
					sb.AppendLine("");

					sb.AppendLine("\t\tpublic static function StringToNumber(s:String) : Number {");
					sb.AppendLine("\t\t\tswitch(s) {");
					foreach (object value in t.GetEnumValues()) {
						sb.AppendFormat("\t\t\t\tcase(\"{0}\"):\n", t.GetEnumName(value));
						sb.AppendFormat("\t\t\t\t\treturn {0}.{1};\n", t.Name, t.GetEnumName(value));
					}
					sb.AppendLine("\t\t\t}");
					sb.AppendLine("\t\t\treturn -1;");
					sb.AppendLine("\t\t}");

					sb.AppendLine("\t}");

					sb.AppendLine("}");

					File.WriteAllText(Path.Combine(outputDir, t.Name + ".as"), sb.ToString());
				}
			}
		}


		private static void WriteTypeDeclaration(bool includeUnknown, StringBuilder sb, string propName, string returnName, Type returnType) {
			
			switch (returnName) {
				case ("String"):
					if (_defaultStrings) {
						sb.AppendFormat("\t\tpublic var {0}:{1} = \"\";\n", propName, "String");
					}
					else {
						sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "String");
					}
					break;
				case ("Int32"):
				case ("Double"):
				case ("Decimal"):
				case ("float"):
				case ("Single"):
					if (_defaultNumbers) {
						sb.AppendFormat("\t\tpublic var {0}:{1} = -1;\n", propName, "Number");
					}
					else {
						sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "Number");
					}
					break;
				case ("DateTime"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "Date");
					break;
				case ("Boolean"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "Boolean");
					break;
				case ("List`1"):
				case ("ArrayList"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "ArrayCollection");
					break;
				case ("Dictionary"):
				case ("Dictionary`2"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "Object");
					break;
				case ("Object"):
				case("Hashtable"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "Object");
					break;
				case ("Array"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "Array");
					break;
				case ("Byte[]"):
					sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, "ByteArray");
					break;
				case("Nullable`1"):
					//look at the nullable underlying type
					Type t = Nullable.GetUnderlyingType(returnType);
					WriteTypeDeclaration(includeUnknown, sb, propName, t.Name, t);
					break;
				default:
					if (includeUnknown) {
						if (returnType.IsEnum) {
							sb.AppendFormat("\t\tpublic var {0}:{1}; //Enum {2}\n", propName, "Number", returnName);
						}
						else {
							sb.AppendFormat("\t\tpublic var {0}:{1};\n", propName, returnName);
						}
					}
					else {
						Console.Out.WriteLine(" Skipped type {0}", returnName);
					}
					break;
			}
		}

	}
}
