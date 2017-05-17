using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayCareTestTask
{
    class NodeRunner : IDisposable
    {
        private Process _proc;
        private string _jsFile;

        public NodeRunner(string jsFile)
        {
            this._jsFile = jsFile;
        }

        public static NodeRunner Start()
        {
            var jsFile = ConfigurationManager.AppSettings["jsFile"];
            if (! File.Exists(jsFile))
            {

                string exceptionMessage = "Specify path to the app.js in RayCareTestTask.dll.config file. The property is named jsFile\n" + 
                                        "E.g. <add key=\"jsFile\" value=" +
                                        @"""C: \Users\dalovenv\Documents\github\raysearch\server\node_modules\frontend_test\build\app.js""/>";
                throw new FileNotFoundException(exceptionMessage);
            }
            var instance = new NodeRunner(jsFile);
            instance.StartNode();
            return instance;
        }
        private void StartNode()
        {
            _proc = Process.Start("node", _jsFile);
        }

        public void StopNode()
        {
            _proc.Kill();
        }
        public void Dispose()
        {
            if(_proc != null)
            {
                StopNode();
                _proc.Dispose();
                _proc = null;
            }
        }
    }
}
