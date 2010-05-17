//
// FSharpFormattingProperties.cs
//
// Author: Jeffrey Stedfast <fejj@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using MonoDevelop.Core;

namespace MonoDevelop.FSharp.Formatting
{
	public enum GotoLabelIndentStyle {
		// Place goto labels in the leftmost column
		LeftJustify,
		
		// Place goto labels one indent less than current
		OneLess,
		
		// Indent goto labels normally
		Normal
	}
	
	public class FormattingProperties {
		static MonoDevelop.Core.Properties properties;
		
		static FormattingProperties ()
		{
			properties = PropertyService.Get ("FSharpBinding.FormattingProperties", new MonoDevelop.Core.Properties ());
		}
		
		public static bool IndentCaseLabels {
			get {
				return properties.Get ("IndentCaseLabels", false);
			}
			set {
				properties.Set ("IndentCaseLabels", value);
			}
		}
		
		public static GotoLabelIndentStyle GotoLabelIndentStyle {
			get {
				return properties.Get ("GotoLabelIndentStyle", GotoLabelIndentStyle.OneLess);
			}
			set {
				properties.Set ("GotoLabelIndentStyle", value);
			}
		}
	}
}