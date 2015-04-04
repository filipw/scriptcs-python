using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using ScriptCs.Contracts;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace ScriptCs.Engine.PythonCs
{
    public class PythonEngine : IScriptEngine
    {
        private readonly IScriptHostFactory _scriptHostFactory;
        private readonly ILog _logger;

        public PythonEngine(IScriptHostFactory scriptHostFactory, ILog logger)
        {
            _scriptHostFactory = scriptHostFactory;
            _logger = logger;
        }

        public ScriptResult Execute(string code, string[] scriptArgs, AssemblyReferences references,
            IEnumerable<string> namespaces,
            ScriptPackSession scriptPackSession)
        {
            if (scriptPackSession == null)
            {
                throw new ArgumentNullException("scriptPackSession");
            }

            if (references == null)
            {
                throw new ArgumentNullException("references");
            }

            var executionReferences = new AssemblyReferences(references.Assemblies, references.Paths);
            executionReferences.Union(scriptPackSession.References);

            var allNamespaces = namespaces.Union(scriptPackSession.Namespaces).Distinct();
            var host = _scriptHostFactory.CreateScriptHost(new ScriptPackManager(scriptPackSession.Contexts), scriptArgs) as ScriptHost;

            var runtimeSetup = new ScriptRuntimeSetup();
            runtimeSetup.LanguageSetups.Add(Python.CreateLanguageSetup(null));

            var runtime = new ScriptRuntime(runtimeSetup);
            var python = Python.GetEngine(runtime);
            python.Runtime.LoadAssembly(typeof(IScriptHost).Assembly);
            python.Runtime.LoadAssembly(typeof(object).Assembly);
            python.Runtime.LoadAssembly(typeof(ExpandoObject).Assembly);

            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic);

            foreach (var reference in executionReferences.Paths)
            {
                var loadedAssembly =
                    currentAssemblies.FirstOrDefault(
                        x => String.Equals(x.Location, reference, StringComparison.InvariantCultureIgnoreCase));
                if (loadedAssembly != null)
                {
                    python.Runtime.LoadAssembly(loadedAssembly);
                }
                else
                {
                    try
                    {
                        var assembly = reference.ToLowerInvariant().EndsWith("dll") ? Assembly.LoadFrom(reference) : Assembly.LoadWithPartialName(reference);
                        if (assembly != null)
                        {
                            python.Runtime.LoadAssembly(assembly);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Warn("Invalid path reference: " + reference);
                    }
                }
            }

            foreach (var assembly in executionReferences.Assemblies)
            {
                python.Runtime.LoadAssembly(assembly);
            }

            foreach (var @namespace in allNamespaces)
            {
                code = "from " + @namespace + " import *" + Environment.NewLine + code;
            }

            var scope = python.CreateScope();
            scope.SetVariable("scriptcs", host);
                
            try
            {
                var result = python.Execute(code, scope);
                return new ScriptResult(result);
            }
            catch (NotSupportedException e)
            {
                return new ScriptResult(compilationException: e);
            }
            catch (Exception e)
            {
                return new ScriptResult(executionException: e);
            }
        }

        public string BaseDirectory { get; set; }

        public string CacheDirectory { get; set; }

        public string FileName { get; set; }
    }
}
