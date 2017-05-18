using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RayCareTestTask
{
    class NodeRunner : IDisposable
    {
        private Process proc;
        private string jsFile;
        private bool exitOrdered = false;
        private ManualResetEventSlim gotListeningString;

        public NodeRunner(string jsFile)
        {
            this.jsFile = jsFile;
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
            gotListeningString = new ManualResetEventSlim();
            proc = new Process();
            proc.StartInfo.FileName = @"\program files\nodejs\node";
            proc.StartInfo.Arguments = jsFile;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.EnableRaisingEvents = true;
            proc.Exited += ProcessExitedHandler;
            proc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            proc.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            // Wait for listening string
            if (! gotListeningString.Wait(TimeSpan.FromSeconds(3)))
            {
                throw new ApplicationException("Node server failed to start");
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.Print("Received {0}", e.Data);
            if(!String.IsNullOrEmpty(e.Data) && e.Data.Contains("Server listening on port"))
            {
                gotListeningString.Set();
            }
        }

        private void ProcessExitedHandler(object sender, EventArgs e)
        {
            if(!exitOrdered)
            {
                throw new ApplicationException("Node server exited (maybe another instance of node is running)");
            }
        }
        public void StopNode()
        {
            exitOrdered = true;
            if (!proc.HasExited)
            {
                proc.Kill();
            }
        }
        public void Dispose()
        {
            if(proc != null)
            {
                StopNode();
                proc.Dispose();
                proc = null;
            }
        }
    }
}
