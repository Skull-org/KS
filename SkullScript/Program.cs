using System;
using System.IO;
using Jsbeautifier;
using System.Text.RegularExpressions;

namespace SharpBasic
{
	class Program
	{
		static void Main(string[] args)
		{
			bool multilinecomment = false;

			if (args.Length != 1) {
				Console.WriteLine("Usage: Skull Script File <file.ks>");
				return;
			}

			string sbFilePath = args[0];
			if (!File.Exists(sbFilePath)) {
				Console.WriteLine("File not found: " + sbFilePath);
				return;
			}

			string jsFilePath = Path.ChangeExtension(sbFilePath, ".js");
			string[] sbLines = File.ReadAllLines(sbFilePath);

			StringWriter jsCode = new StringWriter();

			// Regular expressions for keywords that need opening braces '{'
			Regex ifRegex = new Regex(@"^\s*if\s+(.*)", RegexOptions.IgnoreCase);
			Regex whileRegex = new Regex(@"^\s*while\s+(.*)", RegexOptions.IgnoreCase);
			Regex forRegex = new Regex(@"^\s*for\s+(.*)", RegexOptions.IgnoreCase);
			Regex functionRegex = new Regex(@"^\s*function\s+(.*)", RegexOptions.IgnoreCase);
			Regex subRegex = new Regex(@"^\s*Sub\s+(.*)", RegexOptions.IgnoreCase);
			Regex defRegex = new Regex(@"^\s*def\s+(.*)", RegexOptions.IgnoreCase);
			Regex funcRegex = new Regex(@"^\s*func\s+(.*)", RegexOptions.IgnoreCase);
			Regex elseifRegex = new Regex(@"^\s*elseif\s+(.*)", RegexOptions.IgnoreCase);
			Regex endRegex = new Regex(@"\bend\b", RegexOptions.IgnoreCase);
			Regex elseRegex = new Regex(@"\belse\b", RegexOptions.IgnoreCase);
			Regex repeatRegex = new Regex(@"\brepeat\b", RegexOptions.IgnoreCase);
			Regex untilRegex = new Regex(@"^\s*until\s+(.*)", RegexOptions.IgnoreCase);
			// Regex for class and constructor
			Regex classRegex = new Regex(@"^\s*class\s+(\w+)", RegexOptions.IgnoreCase);
			Regex constructorRegex = new Regex(@"^\s*constructor\s*\(?(.*)\)?", RegexOptions.IgnoreCase);

			Regex tryRegex = new Regex(@"^\s*try\s*", RegexOptions.IgnoreCase);
			Regex catchRegex = new Regex(@"^\s*catch\s*\(?(.*)\)?", RegexOptions.IgnoreCase);
			Regex finallyRegex = new Regex(@"^\s*finally\s*", RegexOptions.IgnoreCase);

			// Regex for switch, case, default, break, and continue
			Regex switchRegex = new Regex(@"^\s*switch\s+(.*)", RegexOptions.IgnoreCase);
			Regex caseRegex = new Regex(@"^\s*case\s+(.*)", RegexOptions.IgnoreCase);
			Regex defaultRegex = new Regex(@"^\s*default\s*", RegexOptions.IgnoreCase);
			Regex breakRegex = new Regex(@"\bbreak\b", RegexOptions.IgnoreCase);
			Regex continueRegex = new Regex(@"\bcontinue\b", RegexOptions.IgnoreCase);

			// To track if a statement is incomplete and requires multiline ASI handling
			bool isIncompleteStatement = false;

			foreach (string line in sbLines) {
				if (multilinecomment) {
					jsCode.WriteLine(line);
					if (line.Contains("*/")) {
						multilinecomment = false;
					}
					continue;
				}

				// Check for the start of a multi-line comment
				if (line.Contains("/*")) {
					multilinecomment = true;
					jsCode.WriteLine(line);
					continue;
				}

				// Process single-line comments
				string[] cl = line.Split(new[] { "//" }, StringSplitOptions.None);
				string currentLine = cl[0].Trim(); // Process only the code part

				// If the line is empty, just output it (with the comment if any)
				if (string.IsNullOrWhiteSpace(currentLine)) {
					jsCode.WriteLine(line); // Keep the whole line as it is, including the comment
					continue;
				}

				// Transpile keywords that require opening brace '{'
				currentLine = ifRegex.Replace(currentLine, m => "if (" + m.Groups[1].Value.Trim() + ") {");
				currentLine = whileRegex.Replace(currentLine, m => "while (" + m.Groups[1].Value.Trim() + ") {");
				currentLine = forRegex.Replace(currentLine, m => "for (" + m.Groups[1].Value.Trim() + ") {");
				currentLine = functionRegex.Replace(currentLine, m => "function " + m.Groups[1].Value.Trim() + " {");
				currentLine = subRegex.Replace(currentLine, m => "function " + m.Groups[1].Value.Trim() + " {");
				currentLine = defRegex.Replace(currentLine, m => "function " + m.Groups[1].Value.Trim() + " {");
				currentLine = funcRegex.Replace(currentLine, m => "function " + m.Groups[1].Value.Trim() + " {");
				currentLine = elseRegex.Replace(currentLine, "} else {");
				currentLine = elseifRegex.Replace(currentLine, m => "} else if ( " + m.Groups[1].Value.Trim() + ") {");
				currentLine = repeatRegex.Replace(currentLine, "do {");
				currentLine = untilRegex.Replace(currentLine, m => "} while ( " + m.Groups[1].Value.Trim() + ");");
				currentLine = classRegex.Replace(currentLine, m => "class " + m.Groups[1].Value.Trim() + " {");
				currentLine = constructorRegex.Replace(currentLine, m => "constructor(" + m.Groups[1].Value.Trim() + ") {");

				currentLine = tryRegex.Replace(currentLine, "try {");
				currentLine = catchRegex.Replace(currentLine, m => "} catch (" + m.Groups[1].Value.Trim() + ") {");
				currentLine = finallyRegex.Replace(currentLine, "} finally {");
				
				// Transpile switch-case statements
				currentLine = switchRegex.Replace(currentLine, m => "switch (" + m.Groups[1].Value.Trim() + ") {");
				currentLine = caseRegex.Replace(currentLine, m => "case " + m.Groups[1].Value.Trim() + ":");
				currentLine = defaultRegex.Replace(currentLine, "default:");
				currentLine = breakRegex.Replace(currentLine, "break;");
				currentLine = continueRegex.Replace(currentLine, "continue;");

				// Replace "End" with "}"
				currentLine = endRegex.Replace(currentLine, "}");

				// Handle multiline statements (ASI)
				if (isIncompleteStatement ||
				!Regex.IsMatch(currentLine.TrimEnd(), @"[{}:;)]$") && // No closing brace, semicolon, or parenthesis at end
				!currentLine.Trim().StartsWith("if ") &&
				!currentLine.Trim().StartsWith("while ") &&
				!currentLine.Trim().StartsWith("for ") &&
				!currentLine.Trim().StartsWith("function ") &&
				!currentLine.Trim().StartsWith("class ") &&
				!currentLine.Trim().StartsWith("switch ") &&
				!currentLine.Trim().StartsWith("else") &&
				!currentLine.Trim().StartsWith("catch") &&
				!currentLine.Trim().StartsWith("finally")) {
					// If the statement is incomplete, don't add a semicolon yet
					isIncompleteStatement = true;
				} else {
					// Complete statement, ensure it ends with a semicolon
					currentLine = currentLine.TrimEnd() + ";";
					isIncompleteStatement = false;
				}


				// Check if there's a comment part and append it properly
				if (cl.Length == 2) {
					jsCode.WriteLine(currentLine + " //" + cl[1].Trim());  // Add comment back
				} else {
					jsCode.WriteLine(currentLine);  // No comment, just output the transpiled code
				}
			}

			string unformattedJs = jsCode.ToString();

			// Initialize Jsbeautifier with default settings
			var beautifier = new Beautifier();
			var options = new BeautifierOptions() {
				IndentSize = 4, // Set indentation size to 4 spaces
				PreserveNewlines = true
			};

			// Beautify the JavaScript code
			string beautifiedJs = beautifier.Beautify(unformattedJs, options);

			File.WriteAllText(jsFilePath, beautifiedJs);
			Console.WriteLine("Transpilation complete! Output written to " + jsFilePath);
		}
	}
}
