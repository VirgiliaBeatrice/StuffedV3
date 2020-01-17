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

    public class SimplicialComplex {
        public class BarycentricCoordinate {
            protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

            public float U { get; set; }
            public float V { get; set; }
            public float W { get; set; }

            public BarycentricCoordinate() { }
            // TODO: Precision Bug
            public bool IsInside {
                get {
                    //return (U <= 1 & U >= 0) & (V <= 1 & V >= 0) & (W <= 1 & W >= 0) & (U + V + W == 1);
                    //Logger.Debug("UVW: {0} {1} {2}", U, V, W);
                    return (U <= 1.0f & U >= 0.0f) & (V <= 1.0f & V >= 0.0f) & (W <= 1.0f & W >= 0.0f) & (Math.Round(U + V + W, 2) == 1.0f);
                }
            }
        }
        public struct BarycentricCoordinateI {
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
            protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

        [Serializable]
        public class ConfigurationSpace_v1 : Dictionary<string, ConfigurationObject> {
            protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

            public ConfigurationObject X1 {
                get {
                    return this["A"];
                }
            }
            public ConfigurationObject X2 {
                get {
                    return this["B"];
                }
            }
            public ConfigurationObject X3 {
                get {
                    return this["C"];
                }
            }

            public ConfigurationSpace_v1() {
                this["A"] = null;
                this["B"] = null;
                this["C"] = null;
            }

            protected ConfigurationSpace_v1(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) {
            }
        }

        public struct Simplex3I {
            public SKPoint V1;
            public SKPoint V2;
            public SKPoint V3;
        }
        public class Simplex3 : List<SKPoint> {
            public SKPoint V1 {
                get {
                    return this[0];
                }
            }
            public SKPoint V2 {
                get {
                    return this[1];
                }
            }
            public SKPoint V3 {
                get {
                    return this[2];
                }
            }
        }

        [Serializable]
        public class Simplex3_v1 : Dictionary<string, Entry> {
            public SKPoint V1 {
                get {
                    return this["A"].Value;
                }
            }
            public SKPoint V2 {
                get {
                    return this["B"].Value;
                }
            }
            public SKPoint V3 {
                get {
                    return this["C"].Value;
                }
            }
            public Simplex3_v1() {
                this["A"] = null;
                this["B"] = null;
                this["C"] = null;
            }
            protected Simplex3_v1(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        }

        public ConfigurationSpace Configurations { get; set; } = new ConfigurationSpace();
        public ConfigurationSpace_v1 Configurations_v1 { get; set; } = new ConfigurationSpace_v1();
        public Simplex3 States { get; set; } = new Simplex3();
        public Simplex3_v1 States_v1 { get; set; } = new Simplex3_v1();
        public bool IsSet { get; set; } = false;
        public SimplicialComplex() { }

        public ConfigurationObject GetInterpolatedConfiguration(SKPoint target) {
            BarycentricCoordinate coor = this.GetBarycentricCoordinate(target);

            return new ConfigurationObject() {
                C1 = this.Configurations.X1.C1 * coor.U + this.Configurations.X2.C1 * coor.V + this.Configurations.X3.C1 * coor.W,
                C2 = this.Configurations.X1.C2 * coor.U + this.Configurations.X2.C2 * coor.V + this.Configurations.X3.C2 * coor.W,
                C3 = this.Configurations.X1.C3 * coor.U + this.Configurations.X2.C3 * coor.V + this.Configurations.X3.C3 * coor.W,
                C4 = this.Configurations.X1.C4 * coor.U + this.Configurations.X2.C4 * coor.V + this.Configurations.X3.C4 * coor.W,
            };
        }

        public ConfigurationObject GetInterpolatedConfiguration_v1(SKPoint target) {
            if (this.IsSet) {
                BarycentricCoordinate coor = this.GetBarycentricCoordinate_v1(target);

                return new ConfigurationObject() {
                    C1 = this.Configurations_v1.X1.C1 * coor.U + this.Configurations_v1.X2.C1 * coor.V + this.Configurations_v1.X3.C1 * coor.W,
                    C2 = this.Configurations_v1.X1.C2 * coor.U + this.Configurations_v1.X2.C2 * coor.V + this.Configurations_v1.X3.C2 * coor.W,
                    C3 = this.Configurations_v1.X1.C3 * coor.U + this.Configurations_v1.X2.C3 * coor.V + this.Configurations_v1.X3.C3 * coor.W,
                    C4 = this.Configurations_v1.X1.C4 * coor.U + this.Configurations_v1.X2.C4 * coor.V + this.Configurations_v1.X3.C4 * coor.W,
                };
            }
            else {
                return null;
            }
        }

        public void CreateSimplex(SKPoint[] vertices) {
            foreach(SKPoint v in vertices) {
                this.States.Add(v);
                // Create empty ConfigurationObject
                this.Configurations.Add(new ConfigurationObject());
            }
        }

        public void SetConfig(int idx, SKPoint[] config) {
            this.Configurations[idx] = new ConfigurationObject(config);
        }
        public void CreatePair(SKPoint control, SKPoint[] configuration) {
            //this.States_v1.Add()
            this.States.Add(control);
            this.Configurations.Add(new ConfigurationObject(configuration));
        }

        public void CreatePair_v1(Entry entry, string vertexName) {
            this.States_v1[vertexName] = entry;
            this.Configurations_v1[vertexName] = new ConfigurationObject(entry.PairedConfig);
            //this.Configurations.Add(new ConfigurationObject(entry.PairedConfig));

            if (this.States_v1.Values.Any(e => e == null)) {
                this.IsSet = false;
            }
            else {
                this.IsSet = true;
            }
        }

        public static BarycentricCoordinate GetBarycentricCoordinate(SKPoint target, Simplex3I simplex) {
            var a1 = GetTriangleArea(new SKPoint[] {
                simplex.V2, simplex.V3, target
            }, true);
            var a2 = GetTriangleArea(new SKPoint[] {
                simplex.V3, simplex.V1, target
            }, true);
            var a3 = GetTriangleArea(new SKPoint[] {
                simplex.V1, simplex.V2, target
            }, true);

            var a = GetTriangleArea(new SKPoint[] {
                simplex.V1, simplex.V2, simplex.V3
            }, true);

            var u = a1 / a;
            var v = a2 / a;
            var w = a3 / a;

            //Logger logger = LogManager.GetCurrentClassLogger();
            //logger.Debug("UVW: {0} {1} {2}", u, v, w);
            //logger.Debug("A: {0}", a);

            //var u = a1 / a > 1.0f ? 0.0f : a1 / a;
            //var v = a2 / a > 1.0f ? 0.0f : a2 / a;
            //var w = a3 / a > 1.0f ? 0.0f : a3 / a;

            return new BarycentricCoordinate {
                U = u,
                V = v,
                W = w
            };
        }

        // https://blog.csdn.net/silangquan/article/details/21990713
        public BarycentricCoordinate GetBarycentricCoordinate(SKPoint target) {
            var a1 = GetTriangleArea(new SKPoint[] {
                States.V2, States.V3, target
            }, true);
            var a2 = GetTriangleArea(new SKPoint[] {
                States.V3, States.V1, target
            }, true);
            var a3 = GetTriangleArea(new SKPoint[] {
                States.V1, States.V2, target
            }, true);

            var a = GetTriangleArea(new SKPoint[] {
                States.V1, States.V2, States.V3
            }, true);

            var u = a1 / a;
            var v = a2 / a;
            var w = a3 / a;

            //var u = a1 / a > 1.0f ? 0.0f : a1 / a;
            //var v = a2 / a > 1.0f ? 0.0f : a2 / a;
            //var w = a3 / a > 1.0f ? 0.0f : a3 / a;


            return new BarycentricCoordinate { 
                U = u,
                V = v,
                W = w 
            };
        }

        public BarycentricCoordinate GetBarycentricCoordinate_v1(SKPoint target) {
            var a1 = GetTriangleArea(new SKPoint[] {
                States_v1.V2, States_v1.V3, target
            }, true);
            var a2 = GetTriangleArea(new SKPoint[] {
                States_v1.V3, States_v1.V1, target
            }, true);
            var a3 = GetTriangleArea(new SKPoint[] {
                States_v1.V1, States_v1.V2, target
            }, true);

            var a = GetTriangleArea(new SKPoint[] {
                States_v1.V1, States_v1.V2, States_v1.V3
            }, true);

            var u = a1 / a;
            var v = a2 / a;
            var w = a3 / a;

            //var u = a1 / a > 1.0f ? 0.0f : a1 / a;
            //var v = a2 / a > 1.0f ? 0.0f : a2 / a;
            //var w = a3 / a > 1.0f ? 0.0f : a3 / a;


            return new BarycentricCoordinate {
                U = u,
                V = v,
                W = w
            };
        }

        // https://en.wikipedia.org/wiki/Shoelace_formula
        private static float GetTriangleArea(SKPoint[] vertices, bool isSigned) {
            Logger logger = LogManager.GetCurrentClassLogger();

            TriangleMath tri = new TriangleMath() {
                A = vertices[0],
                B = vertices[1],
                C = vertices[2]
            };

            var orientation = GetNormalOfTriangle(vertices);
            //logger.Debug("Orient: {0}", orientation == 1 ? "CCW" : "CW");
            var ret = Math.Abs(tri.A.X * (tri.B.Y - tri.C.Y) + tri.B.X * (tri.C.Y - tri.A.Y) + tri.C.X * (tri.A.Y - tri.B.Y)) / 2.0f;

            //return (tri.A.X * (tri.B.Y - tri.C.Y) + tri.B.X * (tri.C.Y - tri.A.Y) + tri.C.X * (tri.A.Y - tri.B.Y)) / 2.0f;

            return isSigned ? ret * orientation : ret;
        }

        private static float GetNormalOfTriangle(SKPoint[] vertices) {
            Vector3 v0, v1, v2;

            v0 = new Vector3(vertices[0].X, vertices[0].Y, 0);
            v1 = new Vector3(vertices[1].X, vertices[1].Y, 0);
            v2 = new Vector3(vertices[2].X, vertices[2].Y, 0);

            Vector3 normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

            return normal.Z;
        }
    }

    public class ComplexCollection : List<SimplicialComplex> {
        public ComplexCollection() : base() { }
    }
    //public class Complices : List<SimplicialComplex> {
    //    public Complices() : base() { }
    //}

    public struct Triangle2I {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
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
        public DataReceivedHandler DataReceived;
        public string ReceivedData = "";
        public string TAG = "";

        public Triangulation() { }

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

        //TODO: Command process
        public void RunConvexHull() {
            var taskInfo = new ProcessStartInfo {
                FileName = "qconvex",
                Arguments = "QJ Fx TI data.txt",
                CreateNoWindow = true,
                //RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };


            Logger.Debug("Start a new process for computing Convex Hull.");
            this.Task = new Process();
            this.Task.StartInfo = taskInfo;
            this.Task.OutputDataReceived += CMD_DataReceived;
            this.Task.ErrorDataReceived += CMD_ErrorReceived;
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

        public void RunDelaunayNeighbor() {
            //Logger.Debug(input);
            var taskInfo = new ProcessStartInfo {
                FileName = "qdelaunay",
                Arguments = "QJ TI data.txt Fx",
                CreateNoWindow = true,
                //RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };


            Logger.Debug("Start a new process for test delaunay neighbor.");
            this.Task = new Process();
            this.Task.StartInfo = taskInfo;
            this.Task.OutputDataReceived += CMD_DataReceived;
            this.Task.ErrorDataReceived += CMD_ErrorReceived;
            this.Task.EnableRaisingEvents = true;
            this.Task.Exited += CMD_ProcessExited;
            this.TAG = "N";

        }

        public void CMD_DataReceived(object sender, DataReceivedEventArgs e) {
            //Console.WriteLine("Output from other process.");
            //Console.WriteLine(e.Data);
            ReceivedData += e.Data + "\r\n";
        }

        public void CMD_ErrorReceived(object sender, DataReceivedEventArgs e) {
            Console.WriteLine("Error: " + e.Data);
        }

        public void StartTask() {
            this.Task.Start();
            Console.WriteLine("Started process ID = 0x{0:X}.", this.Task.Id);
            this.Task.BeginOutputReadLine();
            //Console.WriteLine(this.Task.StandardOutput.ReadToEnd());
            //this.Task.WaitForExit();
        }

        public void CMD_ProcessExited(object sender, EventArgs e) {
            this.DataReceived(this.Task.StartInfo.FileName + this.TAG, this.ReceivedData);
            ReceivedData = "";
            Console.WriteLine("Stopped process ID = 0x{0:X}.", this.Task.Id);
            this.Task = null;
            this.TAG = "";
        }
    }

    //public class TrianglulationReceivedEventArgs : EventArgs {
    //    public string Data { get; set; }

    //    }
}
