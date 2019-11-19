using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using NLog;
using SkiaSharp;

namespace TuggingController {

    public class Mapping {
        public List<SKPoint[]> Configurations { get; set; } = new List<SKPoint[]>();
        public List<SKPoint> Controls { get; set; } = new List<SKPoint>();
        public Mapping() { }

        public void CreatePair(SKPoint control, SKPoint[] configuration) {
            this.Controls.Add(control);
            this.Configurations.Add(configuration);
        }
    }

    class Triangulation {
        public Process Task;
        public delegate void DataReceivedHandler(string name, string data);
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public DataReceivedHandler OnDataReceived;
        public string ReceivedData = "";

        public Triangulation() {
            //Logger.Debug("CWD: {0}", System.IO.Directory.GetCurrentDirectory());
            //var taskInfo = new ProcessStartInfo {
            //    FileName = "rbox.exe",
            //    Arguments = "10 D2",
            //    CreateNoWindow = true,
            //    RedirectStandardInput = true,
            //    RedirectStandardError = true,
            //    RedirectStandardOutput = true,
            //    UseShellExecute = false
            //};


            //Logger.Debug("Start a new process for test purpose.");
            //this.Task = new Process();
            //this.Task.StartInfo = taskInfo;
            //this.Task.OutputDataReceived += CMD_DataReceived;
            //this.Task.EnableRaisingEvents = true;
            //this.Task.Exited += CMD_ProcessExited;

        }

        public void RunRbox() {
            var taskInfo = new ProcessStartInfo {
                FileName = "rbox.exe",
                Arguments = "4 D2",
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };


            Logger.Debug("Start a new process for test rbox.");
            this.Task = new Process();
            this.Task.StartInfo = taskInfo;
            this.Task.OutputDataReceived += CMD_DataReceived;
            this.Task.EnableRaisingEvents = true;
            this.Task.Exited += CMD_ProcessExited;
        }

        public void RunDelaunay() {
            //Logger.Debug(input);
            var taskInfo = new ProcessStartInfo {
                FileName = "qdelaunay",
                Arguments = "QJ i TI data.txt",
                CreateNoWindow = true,
                //RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };


            Logger.Debug("Start a new process for test delaunay.");
            this.Task = new Process();
            this.Task.StartInfo = taskInfo;
            this.Task.OutputDataReceived += CMD_DataReceived;
            this.Task.ErrorDataReceived += CMD_ErrorReceived;
            this.Task.EnableRaisingEvents = true;
            this.Task.Exited += CMD_ProcessExited;
        }

        public void CMD_DataReceived(object sender, DataReceivedEventArgs e) {
            //Console.WriteLine("Output from other process.");
            //Console.WriteLine(e.Data);
            ReceivedData += e.Data + "\r\n";
        }

        public void CMD_ErrorReceived(object sender, DataReceivedEventArgs e) {
            Console.WriteLine(e.Data);
        }

        public void StartTask() {
            this.Task.Start();
            this.Task.BeginOutputReadLine();
            //Console.WriteLine(this.Task.StandardOutput.ReadToEnd());
            //this.Task.WaitForExit();
        }

        public void CMD_ProcessExited(object sender, EventArgs e) {
            this.OnDataReceived(this.Task.StartInfo.FileName, this.ReceivedData);
            ReceivedData = "";
            this.Task = null;
        }
    }

    //public class TrianglulationReceivedEventArgs : EventArgs {
    //    public string Data { get; set; }

    //    }
}
