// EventCreationCompletionData.cs
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
using System.Text;
using System.Linq;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.FSharp.Formatting;
using MonoDevelop.FSharp.Parser;
using Mono.TextEditor;

namespace MonoDevelop.FSharp.Completion
{
	public class EventCreationCompletionData : CompletionData, IActionCompletionData
	{
		string parameterList;
		IMember callingMember;
		TextEditorData editor;
		int initialOffset;
		
		public EventCreationCompletionData (TextEditorData editor, string varName, IType delegateType, IEvent evt, string parameterList, IMember callingMember, IType declaringType) : base (null)
		{
			if (string.IsNullOrEmpty (varName))
				varName = "handle";
			this.DisplayText   = "Handle" + Char.ToUpper (varName[0]) + varName.Substring (1) + evt.Name;
			
			if (declaringType.SearchMember (this.DisplayText, true).Count > 0) {
				for (int i = 1; i < 10000; i++) {
					if (declaringType.SearchMember (this.DisplayText + i.ToString (), true).Count == 0) {
						this.DisplayText = this.DisplayText + i.ToString ();
						break;
					}
				}
			}
			this.editor        = editor;
			this.parameterList = parameterList;
			this.callingMember = callingMember;
			this.Icon          = "md-newmethod";
			this.initialOffset = editor.Caret.Offset;
		}
		
		public void InsertCompletionText (ICompletionWidget widget, CodeCompletionContext context)
		{
			// insert add/remove event handler code after +=/-=
			editor.Replace (initialOffset, editor.Caret.Offset - initialOffset, this.DisplayText + ";");
			
			// Search opening bracket of member
			int pos = editor.Document.LocationToOffset (callingMember.BodyRegion.Start.Line - 1, callingMember.BodyRegion.Start.Column - 1);
			while (pos < editor.Document.Length && editor.Document.GetCharAt (pos) != '{') {
				pos++;
			}
			
			// Search closing bracket of member
			pos = editor.Document.GetMatchingBracketOffset (pos) + 1;
			
			pos = Math.Max (0, Math.Min (pos, editor.Document.Length - 1));
			
			// Insert new event handler after closing bracket
			string indent = editor.Document.GetLine (callingMember.Location.Line).GetIndentation (editor.Document);
			
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ();
			sb.AppendLine ();
			sb.Append (indent);
			if (callingMember.IsStatic)
				sb.Append ("static ");
			sb.Append ("void ");sb.Append (this.DisplayText);sb.Append (' ');sb.Append (this.parameterList);sb.AppendLine ();
			sb.Append (indent);sb.Append ("{");sb.AppendLine ();
			sb.Append (indent);sb.Append (TextEditorProperties.IndentString);
			int cursorPos = pos + sb.Length;
			sb.AppendLine ();
			sb.Append (indent);sb.Append ("}");
			editor.Insert (pos, sb.ToString ());
			editor.Caret.Offset = cursorPos;
		}
		
	}
}
