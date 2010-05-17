// FSharpAmbience.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using System.CodeDom;

namespace MonoDevelop.FSharp.Dom
{
	public class FSharpAmbience : Ambience, IDomVisitor<OutputSettings, string>
	{
		const string nullString = "Null";
		static Dictionary<string, string> netToFSharpTypes = new Dictionary<string, string> ();
		static FSharpAmbience ()
		{
			netToFSharpTypes["System.Void"]    = "void";
			netToFSharpTypes["System.Object"]  = "object";
			netToFSharpTypes["System.Boolean"] = "bool";
			netToFSharpTypes["System.Byte"]    = "byte";
			netToFSharpTypes["System.SByte"]   = "sbyte";
			netToFSharpTypes["System.Char"]    = "char";
			netToFSharpTypes["System.Enum"]    = "enum";
			netToFSharpTypes["System.Int16"]   = "short";
			netToFSharpTypes["System.Int32"]   = "int";
			netToFSharpTypes["System.Int64"]   = "long";
			netToFSharpTypes["System.UInt16"]  = "ushort";
			netToFSharpTypes["System.UInt32"]  = "uint";
			netToFSharpTypes["System.UInt64"]  = "ulong";
			netToFSharpTypes["System.Single"]  = "float";
			netToFSharpTypes["System.Double"]  = "double";
			netToFSharpTypes["System.Decimal"] = "decimal";
			netToFSharpTypes["System.String"]  = "string";
		}
		
		public override string ConvertTypeName (string typeName)
		{
			return NetToFSharpTypeName (typeName);
		}

		public static string NetToFSharpTypeName (string netTypeName)
		{
			if (netToFSharpTypes.ContainsKey (netTypeName)) 
				return netToFSharpTypes[netTypeName];
			return netTypeName;
		}

		protected override IDomVisitor<OutputSettings, string> OutputVisitor {
			get {
				return this;
			}
		}
		
		public FSharpAmbience () : base ("F#", "text/x-fsharp")
		{
			classTypes[ClassType.Class]     = "class";
			classTypes[ClassType.Enum]      = "enum";
			classTypes[ClassType.Interface] = "interface";
			classTypes[ClassType.Struct]    = "struct";
			classTypes[ClassType.Delegate]  = "delegate";
			
			parameterModifiers[ParameterModifiers.In]       = "";
			parameterModifiers[ParameterModifiers.Out]      = "out";
			parameterModifiers[ParameterModifiers.Ref]      = "ref";
			parameterModifiers[ParameterModifiers.Params]   = "params";
			parameterModifiers[ParameterModifiers.Optional] = "";
			
			modifiers[Modifiers.Private]              = "private";
			modifiers[Modifiers.Internal]             = "internal";
			modifiers[Modifiers.Protected]            = "protected";
			modifiers[Modifiers.Public]               = "public";
			modifiers[Modifiers.Abstract]             = "abstract";
			modifiers[Modifiers.Virtual]              = "virtual";
			modifiers[Modifiers.Sealed]               = "sealed";
			modifiers[Modifiers.Static]               = "static";
			modifiers[Modifiers.Override]             = "override";
			modifiers[Modifiers.Readonly]             = "readonly";
			modifiers[Modifiers.Const]                = "const";
			modifiers[Modifiers.Partial]              = "partial";
			modifiers[Modifiers.Extern]               = "extern";
			modifiers[Modifiers.Volatile]             = "volatile";
			modifiers[Modifiers.Unsafe]               = "unsafe";
			modifiers[Modifiers.Overloads]            = "";
			modifiers[Modifiers.WithEvents]           = "";
			modifiers[Modifiers.Default]              = "";
			modifiers[Modifiers.Fixed]                = "";
			modifiers[Modifiers.ProtectedAndInternal] = "protected internal";
			modifiers[Modifiers.ProtectedOrInternal]  = "internal protected";
			
		}
		
		public static string NormalizeTypeName (string typeName)
		{
			int idx = typeName.IndexOf ('`');
			if (idx > 0) 
				return typeName.Substring (0, idx);
			return typeName;
		}
		
		public override bool IsValidFor (string fileName)
		{
			if (fileName == null)
				return false;
			return fileName.EndsWith (".fs");
		}
		
		public override string SingleLineComment (string text)
		{
			return "// " + text;
		}

		public override string GetString (string nameSpace, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitKeyword ("namespace"));
			result.Append (Format (nameSpace));
			return result.ToString ();
		}
		
		public string Visit (ICompilationUnit unit, OutputSettings settings)
		{
			return "TODO";
		}
		
		public string Visit (IUsing u, OutputSettings settings)
		{
			return "TODO";
		}
		
		public string Visit (IProperty property, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (property.Modifiers)));
			if (settings.IncludeReturnType) {
				result.Append (GetString (property.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			AppendExplicitInterfaces(result, property, settings);
			result.Append (settings.EmitName (property, Format (property.Name)));
			if (settings.IncludeParameters && property.Parameters.Count > 0) {
				result.Append (settings.Markup ("["));
				bool first = true;
				foreach (IParameter parameter in property.Parameters) {
					if (!first)
						result.Append (settings.Markup (", "));
					AppendParameter (settings, result, parameter);
					first = false;
				}
				result.Append (settings.Markup ("]"));
			}
			return result.ToString ();
		}
		
		void AppendParameter (OutputSettings settings, StringBuilder result, IParameter parameter)
		{
			if (parameter.IsOut) {
				result.Append (settings.Markup ("out"));
				result.Append (settings.Markup (" "));
			} else if (parameter.IsRef) {
				result.Append (settings.Markup ("ref"));
				result.Append (settings.Markup (" "));
			} else if (parameter.IsParams) {
				result.Append (settings.Markup ("params"));
				result.Append (settings.Markup (" "));
			}
			result.Append (GetString (parameter, settings));
		}
		
		public string Visit (IField field, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (field.Modifiers)));
			
			if (settings.IncludeReturnType && !(field.IsLiteral && field.DeclaringType != null && field.DeclaringType.ClassType == ClassType.Enum)) {
				result.Append (GetString (field.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			result.Append (settings.EmitName (field, Format (field.Name)));
			
			return result.ToString ();
		}
		
		public string Visit (IReturnType returnType, OutputSettings settings)
		{
			if (returnType.Type is AnonymousType)
				return returnType.Type.AcceptVisitor (this, settings);
			StringBuilder result = new StringBuilder ();
			if (!settings.UseNETTypeNames && netToFSharpTypes.ContainsKey (returnType.FullName)) {
				result.Append (settings.EmitName (returnType, netToFSharpTypes[returnType.FullName]));
			} else {
				if (settings.UseFullName) 
					result.Append (settings.EmitName (returnType, Format (NormalizeTypeName (returnType.Namespace))));
				
				foreach (ReturnTypePart part in returnType.Parts) {
					if (result.Length > 0)
						result.Append (settings.EmitName (returnType, "."));
					result.Append (settings.EmitName (returnType, Format (NormalizeTypeName (part.Name))));
					if (settings.IncludeGenerics && part.GenericArguments.Count > 0) {
						result.Append (settings.Markup ("<"));
						if (!settings.HideGenericParameterNames) {
							bool hideArrays = settings.HideArrayBrackets;
							settings.OutputFlags &= ~OutputFlags.HideArrayBrackets;
							for (int i = 0; i < part.GenericArguments.Count; i++) {
								if (i > 0)
									result.Append (settings.Markup (", "));
								result.Append (GetString (part.GenericArguments[i], settings));
							}
							if (hideArrays)
								settings.OutputFlags |= OutputFlags.HideArrayBrackets;
						}
						result.Append (settings.Markup (">"));
					}
				}
			}
			
			if (!settings.HideArrayBrackets && returnType.ArrayDimensions > 0) {
				for (int i = 0; i < returnType.ArrayDimensions; i++) {
					result.Append (settings.Markup ("["));
					int dimension = returnType.GetDimension (i);
					if (dimension > 0)
						result.Append (settings.Markup (new string (',', dimension)));
					result.Append (settings.Markup ("]"));
				}
			}
			return result.ToString ();
		}
		
		void AppendExplicitInterfaces(StringBuilder sb, IMember member, OutputSettings settings)
		{
			foreach (IReturnType explicitInterface in member.ExplicitInterfaces) {
				sb.Append (Visit (explicitInterface, settings));
				sb.Append ('.');
			}
		}
		
		public string Visit (IMethod method, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (method.Modifiers)));
			
			if (settings.IncludeReturnType && !method.IsConstructor && !method.IsFinalizer) {
				result.Append (GetString (method.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			AppendExplicitInterfaces (result, method, settings);
			
			if (method.IsConstructor) {
				result.Append (settings.EmitName (method, Format (method.DeclaringType.Name)));
			} else if (method.IsFinalizer) {
				result.Append (settings.EmitName (method, settings.Markup ("~") + Format (method.DeclaringType.Name)));
			} else {
				result.Append (settings.EmitName (method, Format (method.Name)));
			}
			
			if (settings.IncludeGenerics) {
				if (method.TypeParameters.Count > 0) {
					result.Append (settings.Markup ("<"));
					
					if (!settings.HideGenericParameterNames) {
						InstantiatedMethod instantiatedMethod = method as InstantiatedMethod;
						
						for (int i = 0; i < method.TypeParameters.Count; i++) {
							if (i > 0)
								result.Append (settings.Markup (", "));
							if (instantiatedMethod != null) {
								result.Append (this.GetString (instantiatedMethod.GenericParameters[i], settings));
							} else {
								result.Append (NetToFSharpTypeName (method.TypeParameters[i].Name));
							}
						}
					}
					result.Append (settings.Markup (">"));
				}
			}
			
			if (settings.IncludeParameters) {
				result.Append (settings.Markup ("("));
				bool first = true;
				
				if (method.Parameters != null) {
					foreach (IParameter parameter in method.Parameters) {
						if (settings.HideExtensionsParameter && method.IsExtension && parameter == method.Parameters[0])
							continue;
						if (!first)
							result.Append (settings.Markup (", "));
						AppendParameter (settings, result, parameter);
						first = false;
					}
				}
				result.Append (settings.Markup (")"));
			}
			
			return result.ToString ();
		}
		
		public string Visit (IParameter parameter, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			if (settings.IncludeParameterName) {
				if (settings.IncludeModifiers) {
					if (parameter.IsOut) {
						result.Append (settings.EmitKeyword ("out"));
					}
					if (parameter.IsRef) {
						result.Append (settings.EmitKeyword ("ref"));
					}
					if (parameter.IsParams) {
						result.Append (settings.EmitKeyword ("params"));
					}
				}
				
				if (settings.IncludeReturnType) {
					result.Append (GetString (parameter.ReturnType, settings));
					result.Append (" ");
				}
				
				if (settings.HighlightName) {
					result.Append (settings.EmitName (parameter, settings.Highlight (Format (parameter.Name))));
				} else {
					result.Append (settings.EmitName (parameter, Format (parameter.Name)));
				}
			} else {
				result.Append (GetString (parameter.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		public string Visit (IType type, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			if (type is AnonymousType) {
				result.Append ("new {");
				foreach (IProperty property in type.Properties) {
					result.AppendLine ();
					result.Append ("\t");
					result.Append (property.ReturnType.AcceptVisitor (this, settings));
					result.Append (" ");
					result.Append (property.Name);
					result.Append (";");
				}
				result.AppendLine ();
				result.Append ("}");
				return result.ToString ();
			}
			
			InstantiatedType instantiatedType = type as InstantiatedType;
			string modStr = base.GetString (type.ClassType == ClassType.Enum ? (type.Modifiers & ~Modifiers.Sealed) :  type.Modifiers);
			string modifiers = !String.IsNullOrEmpty (modStr) ? settings.EmitModifiers (modStr) : "";
			string keyword = settings.EmitKeyword (GetString (type.ClassType));
			
			string name = null;
			if (type.Name.EndsWith("[]")) {
				List<IMember> member = type.SearchMember ("Item", true);
				if (member != null && member.Count >0)
					name = Visit (member[0].ReturnType, settings);
				if (settings.IncludeGenerics)
					name += "[]";
			} 
			if (name == null) {
				if (settings.UseFullName && type.DeclaringType == null) {
					name = Format (instantiatedType == null ? type.FullName : instantiatedType.UninstantiatedType.FullName);
				} else {
					IType realType = instantiatedType == null ? type : instantiatedType.UninstantiatedType;
					name = Format (NormalizeTypeName ((settings.UseFullInnerTypeName && realType.DeclaringType != null) ? GetString (realType.DeclaringType, OutputFlags.UseFullInnerTypeName) + "." + realType.Name : realType.Name));
				}
			}
			int parameterCount = type.TypeParameters.Count;
			if (instantiatedType != null) 
				parameterCount = instantiatedType.GenericParameters.Count;
			
			
			if (modifiers.Length == 0 && keyword.Length == 0 && (!settings.IncludeGenerics || parameterCount == 0) && (!settings.IncludeBaseTypes || !type.BaseTypes.Any ()))
				return settings.UseFullName && type.DeclaringType != null ? GetString (type.DeclaringType, OutputFlags.UseFullName) + "." + name : name;
			
			result.Append (modifiers);
			result.Append (keyword);
			if (result.Length > 0 && !result.ToString ().EndsWith (" "))
				result.Append (settings.Markup (" "));
			if (settings.UseFullName && type.DeclaringType != null) {
				result.Append (GetString (type.DeclaringType, OutputFlags.UseFullName));
				result.Append (settings.Markup ("."));
			}
			
			result.Append (settings.EmitName (type, name));
			
			if (settings.IncludeGenerics && parameterCount > 0) {
				result.Append (settings.Markup ("<"));
				if (!settings.HideGenericParameterNames) {
					for (int i = 0; i < parameterCount; i++) {
						if (i > 0)
							result.Append (settings.Markup (", "));
						if (instantiatedType != null) {
							result.Append (this.GetString (instantiatedType.GenericParameters[i], settings));
						} else {
							result.Append (NetToFSharpTypeName (type.TypeParameters[i].Name));
						}
					}
				}
				result.Append (settings.Markup (">"));
			}
			
			if (settings.IncludeBaseTypes && type.BaseTypes.Any ()) {
				bool first = true;
				foreach (IReturnType baseType in type.BaseTypes) {
					if (baseType.FullName == "System.Object" || baseType.FullName == "System.Enum")
						continue;
					result.Append (settings.Markup (first ? " : " : ", "));
					first = false;
					result.Append (this.GetString (baseType, settings));	
				}
				
			}
			return result.ToString ();
		}
		
		static void PrintObject (StringBuilder result, OutputSettings settings, object o)
		{
			if (o is string) {
				result.Append (settings.Markup ("\""));
				result.Append (o);
				result.Append (settings.Markup ("\""));
			} else if (o is char) {
				result.Append (settings.Markup ("'"));
				result.Append (o);
				result.Append (settings.Markup ("'"));
			} else if (o is bool) {
				result.Append (((bool)o) ? "true" : "false");
			} else if (o is CodePrimitiveExpression) {
				CodePrimitiveExpression cpe = (CodePrimitiveExpression)o;
				PrintObject (result, settings, cpe.Value);
			} else 
				result.Append (o);
		}
		public string Visit (IAttribute attribute, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.Markup ("["));
			string attrName = GetString (attribute.AttributeType, settings);
			if (attrName.EndsWith ("Attribute"))
				attrName = attrName.Substring (0, attrName.Length - "Attribute".Length);
			result.Append (attrName);
			result.Append (settings.Markup ("("));
			bool first = true;
			if (attribute.PositionalArguments != null) {
				foreach (object o in attribute.PositionalArguments) {
					if (!first)
						result.Append (settings.Markup (", "));
					first = false;
					PrintObject (result, settings, o);
				}
			}
			result.Append (settings.Markup (")]"));
			return result.ToString ();
		}
		
		public string Visit (Namespace ns, OutputSettings settings)
		{
			return settings.EmitKeyword ("namespace") + settings.EmitName (ns, ns.Name);
		}
		
		public string Visit (LocalVariable var, OutputSettings settings)
		{
			return settings.EmitName (var, var.Name);
		}
		
		public string Visit (IEvent evt, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (evt.Modifiers)));
			result.Append (settings.EmitKeyword ("event"));
			if (settings.IncludeReturnType) {
				result.Append (GetString (evt.ReturnType, settings));
				result.Append (settings.Markup (" "));
			}
			
			AppendExplicitInterfaces(result, evt, settings);
			result.Append (settings.EmitName (evt, Format (evt.Name)));
			
			return result.ToString ();
		}
	}
}
