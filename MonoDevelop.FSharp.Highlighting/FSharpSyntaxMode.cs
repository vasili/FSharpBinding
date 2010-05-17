// 
// SyntaxMode.cs
//  
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.FSharp.Project;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.FSharp.Highlighting
{
	public class FSharpSyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode
	{
		public bool DisableConditionalHighlighting {
			get;
			set;
		}
		
		static FSharpSyntaxMode ()
		{
			MonoDevelop.Debugger.DebuggingService.DisableConditionalCompilation += (EventHandler<MonoDevelop.Ide.Gui.DocumentEventArgs>)MonoDevelop.Core.Gui.DispatchService.GuiDispatch (new EventHandler<MonoDevelop.Ide.Gui.DocumentEventArgs> (OnDisableConditionalCompilation));
		}
		
		static void OnDisableConditionalCompilation (object s, MonoDevelop.Ide.Gui.DocumentEventArgs e)
		{
			ITextEditorDataProvider provider = e.Document.GetContent<ITextEditorDataProvider> ();
			if (provider == null)
				return;
			FSharpSyntaxMode mode = provider.GetTextEditorData ().Document.SyntaxMode as FSharpSyntaxMode;
			if (mode == null)
				return;
			mode.DisableConditionalHighlighting = true;
			provider.GetTextEditorData ().Document.CommitUpdateAll ();
		}
		
		public FSharpSyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, "FSharpSyntaxMode.xml");
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = baseMode.Spans;
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.table = baseMode.Table;
				this.properties = baseMode.Properties;
			}
			
			AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("String", new HighlightUrlSemanticRule ("string"));
			AddSemanticRule (new HighlightFSharpSemanticRule ());
		}
		
		public override SpanParser CreateSpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
		{
			return new FSharpSpanParser (doc, mode, line, spanStack);
		}
		
		class IfBlockSpan : Span
		{
			public bool IsValid {
				get;
				private set;
			}
			
			public IfBlockSpan (bool isValid)
			{
				this.IsValid = isValid;
				TagColor = "text.preprocessor";
				if (!isValid) {
					Color = "comment.block";
					Rule = "String";
				} else {
					Color = "text";
					Rule = "<root>";
				}
				StopAtEol = false;
			}
			public override string ToString ()
			{
				return string.Format("[IfBlockSpan: IsValid={0}, Color={1}, Rule={2}]", IsValid, Color, Rule);
			}
		}
		
		class ElseIfBlockSpan : Span
		{
			public bool IsValid {
				get;
				private set;
			}
			
			public ElseIfBlockSpan (bool isValid)
			{
				this.IsValid = isValid;
				TagColor = "text.preprocessor";
				if (!isValid) {
					Color = "comment.block";
					Rule = "String";
				} else {
					Color = "text";
					Rule = "<root>";
				}
				StopAtEol = false;
			}
			public override string ToString ()
			{
				return string.Format("[ElseIfBlockSpan: IsValid={0}, Color={1}, Rule={2}]", IsValid, Color, Rule);
			}
		}
		
		class ElseBlockSpan : Span
		{
			public bool IsValid {
				get;
				private set;
			}
			
			public ElseBlockSpan (bool isValid)
			{
				this.IsValid = isValid;
				TagColor = "text.preprocessor";
				if (!isValid) {
					Color = "comment.block";
					Rule = "String";
				} else {
					Color = "text";
					Rule = "<root>";
				}
				StopAtEol = false;
			}
			
			public override string ToString ()
			{
				return string.Format("[ElseBlockSpan: IsValid={0}, Color={1}, Rule={2}]", IsValid, Color, Rule);
			}
		}
		
		protected class FSharpSpanParser : SpanParser
		{
			FSharpSyntaxMode FSharpSyntaxMode {
				get {
					return (FSharpSyntaxMode)mode;
				}
			}
			class ConditinalExpressionEvaluator : IFSharpCode.NRefactory.Visitors.AbstractAstVisitor
			{
				HashSet<string> symbols = new HashSet<string> ();
				
				public ConditinalExpressionEvaluator (Document doc)
				{
					var project = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.CurrentSelectedProject;
					if (project != null) {
						DotNetProjectConfiguration configuration = project.GetConfiguration (project.ParentSolution.DefaultConfigurationSelector) as DotNetProjectConfiguration;
						if (configuration != null) {
							FSharpCompilerParameters cparams = configuration.CompilationParameters as FSharpCompilerParameters;
							if (cparams != null) {
								string[] syms = cparams.DefineSymbols.Split (';', ',');
								foreach (string s in syms) {
									string ss = s.Trim ();
									if (ss.Length > 0 && !symbols.Contains (ss))
										symbols.Add (ss);
								}
							}
						}
					}
					
					ProjectDom dom = ProjectDomService.GetProjectDom (project);
					ParsedDocument parsedDocument = ProjectDomService.GetParsedDocument (dom, doc.FileName);
					if (parsedDocument == null)
						parsedDocument = ProjectDomService.ParseFile (dom, doc.FileName ?? "a.cs", delegate { return doc.Text; });
					if (parsedDocument != null) {
						foreach (PreProcessorDefine define in parsedDocument.Defines) {
							symbols.Add (define.Define);
						}
					}
				}
				
				public override object VisitIdentifierExpression (IFSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
				{
					return symbols.Contains (identifierExpression.Identifier);
				}
				
				public override object VisitUnaryOperatorExpression (IFSharpCode.NRefactory.Ast.UnaryOperatorExpression unaryOperatorExpression, object data)
				{
					bool result = (bool)(unaryOperatorExpression.Expression.AcceptVisitor (this, data) ?? (object)false);
					if (unaryOperatorExpression.Op == IFSharpCode.NRefactory.Ast.UnaryOperatorType.Not)
						return !result;
					return result;
				}
				
				public override object VisitPrimitiveExpression (IFSharpCode.NRefactory.Ast.PrimitiveExpression primitiveExpression, object data)
				{
					return (bool)primitiveExpression.Value;
				}

				public override object VisitBinaryOperatorExpression (IFSharpCode.NRefactory.Ast.BinaryOperatorExpression binaryOperatorExpression, object data)
				{
					bool left  = (bool)(binaryOperatorExpression.Left.AcceptVisitor (this, data) ?? (object)false);
					bool right = (bool)(binaryOperatorExpression.Right.AcceptVisitor (this, data) ?? (object)false);
					
					switch (binaryOperatorExpression.Op) {
					case IFSharpCode.NRefactory.Ast.BinaryOperatorType.InEquality:
						return left != right;
					case IFSharpCode.NRefactory.Ast.BinaryOperatorType.Equality:
						return left == right;
					case IFSharpCode.NRefactory.Ast.BinaryOperatorType.LogicalOr:
						return left || right;
					case IFSharpCode.NRefactory.Ast.BinaryOperatorType.LogicalAnd:
						return left && right;
					}
					
					Console.WriteLine ("Unknown operator:" + binaryOperatorExpression.Op);
					return left;
				}
			}
			
			protected override void ScanSpan (ref int i)
			{
				if (FSharpSyntaxMode.DisableConditionalHighlighting) {
					base.ScanSpan (ref i);
					return;
				}
				if (i + 5 < doc.Length && doc.GetTextAt (i, 5) == "#else") {
					LineSegment line = doc.GetLineByOffset (i);
					
					bool previousResult = false;
					
					foreach (Span span in spanStack.ToArray ().Reverse ()) {
						if (span is IfBlockSpan) {
							previousResult = ((IfBlockSpan)span).IsValid;
						}
						if (span is ElseIfBlockSpan) {
							previousResult |= ((ElseIfBlockSpan)span).IsValid;
						}
					}
					
					
					int length = line.Offset + line.EditableLength - i;
					while (spanStack.Count > 0 && !(CurSpan is IfBlockSpan)) {
						spanStack.Pop ();
					}
//					IfBlockSpan ifBlock = (IfBlockSpan)CurSpan;
					ElseBlockSpan elseBlockSpan = new ElseBlockSpan (!previousResult);
					OnFoundSpanBegin (elseBlockSpan, i, 0);
					
					spanStack.Push (elseBlockSpan);
					ruleStack.Push (GetRule (elseBlockSpan));
					
					// put pre processor eol span on stack, so that '#else' gets the correct highlight
					OnFoundSpanBegin (preprocessorSpan, i, 1);
					spanStack.Push (preprocessorSpan);
					ruleStack.Push (preprocessorRule);
					i += length - 1;
					return;
				}
				
				if (CurRule.Name == "text.preprocessor" && i >= 3 && doc.GetTextAt (i - 3, 3) == "#if") {
					LineSegment line = doc.GetLineByOffset (i);
					int length = line.Offset + line.EditableLength - i;
					string parameter = doc.GetTextAt (i, length);
					IFSharpCode.NRefactory.Parser.FSharp.Lexer lexer = new IFSharpCode.NRefactory.Parser.FSharp.Lexer (new System.IO.StringReader (parameter));
					IFSharpCode.NRefactory.Ast.Expression expr = lexer.PPExpression ();
					bool result = false;
					if (expr != null && !expr.IsNull) {
						object o = expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc), null);
						if (o is bool)
							result = (bool)o;
					}
					IfBlockSpan ifBlockSpan = new IfBlockSpan (result);
					OnFoundSpanBegin (ifBlockSpan, i, length);
					i += length - 1;
					spanStack.Push (ifBlockSpan);
					ruleStack.Push (GetRule (ifBlockSpan));
					return;
				}
				if (i + 5 < doc.Length && doc.GetTextAt (i, 5) == "#elif" && spanStack.Any (span => span is IfBlockSpan)) {
					LineSegment line = doc.GetLineByOffset (i);
					int length = line.Offset + line.EditableLength - i;
					string parameter = doc.GetTextAt (i + 5, length - 5);
					
					IFSharpCode.NRefactory.Parser.FSharp.Lexer lexer = new IFSharpCode.NRefactory.Parser.FSharp.Lexer (new System.IO.StringReader (parameter));
					IFSharpCode.NRefactory.Ast.Expression expr = lexer.PPExpression ();
				
					bool result = !expr.IsNull ? (bool)expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc), null) : false;
					
					if (result) {
						bool previousResult = false;
						foreach (Span span in spanStack.ToArray ().Reverse ()) {
							if (span is IfBlockSpan) {
								previousResult = ((IfBlockSpan)span).IsValid;
							}
							if (span is ElseIfBlockSpan) {
								previousResult |= ((ElseIfBlockSpan)span).IsValid;
							}
						}
						
						result = !previousResult;
					}
					
					ElseIfBlockSpan elseIfBlockSpan = new ElseIfBlockSpan (result);
					OnFoundSpanBegin (elseIfBlockSpan, i, 0);
					
					spanStack.Push (elseIfBlockSpan);
					ruleStack.Push (GetRule (elseIfBlockSpan));
					
					// put pre processor eol span on stack, so that '#elif' gets the correct highlight
					OnFoundSpanBegin (preprocessorSpan, i, 1);
					spanStack.Push (preprocessorSpan);
					ruleStack.Push (preprocessorRule);
					//i += length - 1;
					return;
				}
				base.ScanSpan (ref i);
			}
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, int i)
			{
				if (cur is IfBlockSpan || cur is ElseIfBlockSpan || cur is ElseBlockSpan) {
					bool end = i + 6 < doc.Length && doc.GetTextAt (i, 6) == "#endif";
					if (end) {
						OnFoundSpanEnd (cur, i, 0); // put empty end tag in
						while (spanStack.Count > 0 && !(spanStack.Peek () is IfBlockSpan)) {
							spanStack.Pop ();
							if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
								ruleStack.Pop ();
						}
						if (spanStack.Count > 0)
							spanStack.Pop ();
						if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
							ruleStack.Pop ();
						// put pre processor eol span on stack, so that '#endif' gets the correct highlight
						foreach (Span span in mode.Spans) {
							if (span.Rule == "text.preprocessor") {
								OnFoundSpanBegin (span, i, 1);
								spanStack.Push (span);
								ruleStack.Push (GetRule (span));
								break;
							}
						}
					}
					return end;
				}
				return base.ScanSpanEnd (cur, i);
			}
			
			Span preprocessorSpan;
			Rule preprocessorRule;
			
			public FSharpSpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack) : base (doc, mode, line, spanStack)
			{
				foreach (Span span in mode.Spans) {
					if (span.Rule == "text.preprocessor") {
						preprocessorSpan = span;
						preprocessorRule = GetRule (span);
					}
				}
			}
		}
	}
}
 
