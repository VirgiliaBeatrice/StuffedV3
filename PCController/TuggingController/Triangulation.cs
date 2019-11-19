using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using NLog;
using SkiaSharp;
using System.Numerics;

namespace TuggingController {

    public class Mapping {
        public struct BarycentricCoordinate {
            public float U;
            public float V;
            public float W;
        }

        public class ConfigurationObject {
            public Vector2 C1 { get; set; }
            public Vector2 C2 { get; set; }
            public Vector2 C3 { get; set; }
            public Vector2 C4 { get; set; }

            public ConfigurationObject() { }
            public ConfigurationObject(SKPoint[] points) {
                this.C1 = new Vector2 { X = points[0].X, Y = points[0].Y };
                this.C2 = new Vector2 { X = points[1].X, Y = points[1].Y };
                this.C3 = new Vector2 { X = points[2].X, Y = points[2].Y };
                this.C4 = new Vector2 { X = points[3].X, Y = points[3].Y };
            }
            public void Multiply(float c) {
                this.C1 *= c;
                this.C2 *= c;
                this.C3 *= c;
                this.C4 *= c;
            }

            public SKPoint[] ToSKPoint() {
                return new SKPoint[] {
                    new SKPoint(this.C1.X, this.C1.Y),
                    new SKPoint(this.C2.X, this.C2.Y),
                    new SKPoint(this.C3.X, this.C3.Y),
                    new SKPoint(this.C4.X, this.C4.Y),
                };
            }
        }

        public class ConfigurationSpace : List<ConfigurationObject> {
            public ConfigurationObject X1 {
                get {
                    return this[0];
                }
            }
            public ConfigurationObject X2 {
                get {
                    return this[1];
                }
            }
            public ConfigurationObject X3 {
                get {
                    return this[2];
                }
            }

        }
        public class StateSpace : List<SKPoint> {
            public SKPoint X1 {
                get {
                    return this[0];
                }
            }
            public SKPoint X2 {
                get {
                    return this[1];
                }
            }
            public SKPoint X3 {
                get {
                    return this[2];
                }
            }
        }

        public ConfigurationSpace Configurations { get; set; } = new ConfigurationSpace();
        public StateSpace Controls { get; set; } = new StateSpace();
        public Mapping() { }

        public ConfigurationObject GetInterpolatedConfiguration(SKPoint target) {
            BarycentricCoordinate coor = this.GetBarycentricCoordinate(target);

            return new ConfigurationObject() {
                C1 = this.Configurations.X1.C1 * coor.U + this.Configurations.X2.C1 * coor.V + this.Configurations.X3.C1 * coor.W,
                C2 = this.Configurations.X1.C2 * coor.U + this.Configurations.X2.C2 * coor.V + this.Configurations.X3.C2 * coor.W,
                C3 = this.Configurations.X1.C3 * coor.U + this.Configurations.X2.C3 * coor.V + this.Configurations.X3.C3 * coor.W,
                C4 = this.Configurations.X1.C4 * coor.U + this.Configurations.X2.C4 * coor.V + this.Configurations.X3.C4 * coor.W,
            };
        }
        public void CreatePair(SKPoint control, SKPoint[] configuration) {
            this.Controls.Add(control);
            this.Configurations.Add(new ConfigurationObject(configuration));

            //if (this.Configurations.Count == 3) {
                
            //}
        }

        // https://blog.csdn.net/silangquan/article/details/21990713
        public BarycentricCoordinate GetBarycentricCoordinate(SKPoint target) {
            var a1 = this.GetTriangleArea(new SKPoint[] {
                Controls.X2, Controls.X3, target
            });
            var a2 = this.GetTriangleArea(new SKPoint[] {
                Controls.X1, Controls.X3, target
            });
            var a3 = this.GetTriangleArea(new SKPoint[] {
                Controls.X1, Controls.X2, target
            });

            var a = this.GetTriangleArea(new SKPoint[] {
                Controls.X1, Controls.X2, Controls.X3
            });

            var u = a1 / a;
            var v = a2 / a;
            var w = a3 / a;

            return new BarycentricCoordinate { 
                U = u,
                V = v,
                W = w 
            };
        }

        // https://en.wikipedia.org/wiki/Shoelace_formula
        private float GetTriangleArea(SKPoint[] vertices) {
            TriangleMath tri = new TriangleMath() {
                A = vertices[0],
                B = vertices[1],
                C = vertices[2]
            };
            return Math.Abs(tri.A.X * (tri.B.Y - tri.C.Y) + tri.B.X * (tri.C.Y - tri.A.Y) + tri.C.X * (tri.A.Y - tri.B.Y)) / 2;
        }
    }

    public class TriangleMath {
        public SKPoint A { get; set; }
        public SKPoint B { get; set; }
        public SKPoint C { get; set; }

        public TriangleMath() { }
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
