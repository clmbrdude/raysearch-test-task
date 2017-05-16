using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static NodeRunner Start(string jsFile)
        {
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
