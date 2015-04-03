using ScriptCs.Contracts;

namespace ScriptCs.Engine.PythonCs
{
    [Module("python", Extensions = ".py")]
    public class PythonModule : IModule
    {
        public void Initialize(IModuleConfiguration config)
        {
            config.ScriptEngine<PythonEngine>().LineProcessor<PythonNamespaceLineProcessor>();
        }
    }
}