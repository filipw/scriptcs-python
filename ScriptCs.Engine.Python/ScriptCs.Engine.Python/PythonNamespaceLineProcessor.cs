using System;
using ScriptCs.Contracts;

namespace ScriptCs.Engine.PythonCs
{
    public class PythonNamespaceLineProcessor : IUsingLineProcessor
    {
        private const string NamespaceString = "import ";

        public bool ProcessLine(IFileParser parser, FileParserContext context, string line, bool isBeforeCode)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (!IsUsingLine(line))
            {
                return false;
            }

            var @namespace = GetNamespace(line);
            if (!context.Namespaces.Contains(@namespace))
            {
                context.Namespaces.Add(@namespace);
            }

            return true;
        }

        private static bool IsUsingLine(string line)
        {
            return line.Trim(' ').StartsWith(NamespaceString);
        }

        private static string GetNamespace(string line)
        {
            return line.Trim(' ')
                .Replace(NamespaceString, string.Empty);
        }
    }
}