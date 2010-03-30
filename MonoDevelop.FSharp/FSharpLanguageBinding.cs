// 
// FSharpBindingCompilerManager.cs
//  
// Author:
//       Vasili I Galchin <vigalchin@gmail.com> but heavily based on CSharp work by Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;
// WNH using Microsoft.CSharp;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;

using MonoDevelop.FSharp.Project;


namespace MonoDevelop.FSharp
{
	public class FSharpLanguageBinding : IDotNetLanguageBinding
	{
		FSharpEnhancedCodeProvider provider;
		
		// Keep the platforms combo of CodeGenerationPanelWidget in sync with this list
		public static IList<string> SupportedPlatforms = new string[] { "anycpu", "x86", "x64", "itanium" };
	
		public string Language {
			get {
LoggingService.LogInfo("F# Language");
				return "F#";
			}
		}
		
		public string ProjectStockIcon {
			get { 
LoggingService.LogInfo("F# ProjectStockIcon");
				return "md-fsharp-project";
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
LoggingService.LogInfo("F# IsSourceCodeFile");
			return string.Compare (Path.GetExtension (fileName), ".fs", true) == 0;
		}
		
		public BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, IProgressMonitor monitor)
		{
LoggingService.LogInfo(">>>>>>> F# Compile");
			return FSharpBindingCompilerManager.Compile (projectItems, configuration, configSelector, monitor);
		}
		
		public ConfigurationParameters CreateCompilationParameters (XmlElement projectOptions)
		{
			FSharpCompilerParameters pars = new FSharpCompilerParameters ();
			if (projectOptions != null) {
				string platform = projectOptions.GetAttribute ("Platform");
				if (SupportedPlatforms.Contains (platform))
					pars.PlatformTarget = platform;
				string debugAtt = projectOptions.GetAttribute ("DefineDebug");
				if (string.Compare ("True", debugAtt, true) == 0)
					pars.DefineSymbols = "DEBUG";
			}
LoggingService.LogInfo("F# CreateCompliationParameters");
			return pars;
		}
	
		public ProjectParameters CreateProjectParameters (XmlElement projectOptions)
		{
LoggingService.LogInfo("F# CreateProjectParameters");
			return new FSharpProjectParameters ();
		}
		
		public string SingleLineCommentTag { get { return "//"; } }
		public string BlockCommentStartTag { get { return "/*"; } }
		public string BlockCommentEndTag { get { return "*/"; } }
		
		public CodeDomProvider GetCodeDomProvider ()
		{
//			if (provider == null)
//				provider = new FSharpEnhancedCodeProvider ();
			
LoggingService.LogInfo("F# GetCodeDomProvider");
return null;
//return provider;       WNH FIXME!!!!!!!!!!!!
		}
		
		public string GetFileName (string baseName)
		{
LoggingService.LogInfo("F# GetFileName");
			return baseName + ".fs";
		}
		
		public IParser Parser {
			get { 
LoggingService.LogInfo("F# Parser");
				return null; 
			}
		}
		
//FSharpRefactorer refactorer = new FSharpRefactorer ();
		public IRefactorer Refactorer {
			get { 
			//	return refactorer;
LoggingService.LogInfo("F# Refactorer");
				return null;
			}
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
LoggingService.LogInfo("F# GetSupportedClrVersions");
			return new ClrVersion[] { 
				ClrVersion.Net_1_1, 
				ClrVersion.Net_2_0, 
				ClrVersion.Clr_2_1,
				ClrVersion.Net_4_0
			};
		}
	}
	

	internal static class Counters
	{
		public static Counter ResolveTime = InstrumentationService.CreateCounter ("Resolve Time", "Timing");
	}

}
