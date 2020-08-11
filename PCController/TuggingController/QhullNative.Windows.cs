using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Qhull.Native {
    internal static class NativeMethods {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPWStr)] string lpProcName);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
    }

    public static class LibQhull_r {
        public static string qh_version {
            get {
                var Dll = NativeMethods.GetModuleHandle(@"qhull_r.dll");
                var strAddr = NativeMethods.GetProcAddress(Dll, "qh_version");
                
                return Marshal.PtrToStringAnsi(strAddr);
            }
        }

        public static string qh_version2 {
            get {
                var Dll = NativeMethods.GetModuleHandle(@"qhull_r.dll");
                var strAddr = NativeMethods.GetProcAddress(Dll, "qh_version");

                return Marshal.PtrToStringAnsi(strAddr);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Qh {
            [MarshalAs(UnmanagedType.Bool)] bool ALLpoints;
            [MarshalAs(UnmanagedType.Bool)] bool ALLOWshort;
            [MarshalAs(UnmanagedType.Bool)] bool ALLOWwarning;
            [MarshalAs(UnmanagedType.Bool)] bool ALLPWwide;
            [MarshalAs(UnmanagedType.Bool)] bool ANGLEmerge;
            [MarshalAs(UnmanagedType.Bool)] bool APPROXhull;
            double MINoutside;
            [MarshalAs(UnmanagedType.Bool)] bool ANNOTATEoutput;
            [MarshalAs(UnmanagedType.Bool)] bool ATinfinity;

            [MarshalAs(UnmanagedType.Bool)] bool AVOIDold;
            [MarshalAs(UnmanagedType.Bool)] bool BESToutside;
            [MarshalAs(UnmanagedType.Bool)] bool CDDinput;
            [MarshalAs(UnmanagedType.Bool)] bool CDDoutput;
            [MarshalAs(UnmanagedType.Bool)] bool CHECKduplicates;
            [MarshalAs(UnmanagedType.Bool)] bool CHECKfrequently;
            double permerge_cos;
            double postmerge_cos;
            [MarshalAs(UnmanagedType.Bool)] bool DELAUNAY;
            [MarshalAs(UnmanagedType.Bool)] bool DOintersections;
            int DROPdim;
            [MarshalAs(UnmanagedType.Bool)] bool FLUSHprint;
            [MarshalAs(UnmanagedType.Bool)] bool FORCEoutput;
            int GOODpoint;
            IntPtr GOODppointp;
            [MarshalAs(UnmanagedType.Bool)] bool GOODthreshold;
            int GOODvertex;
            IntPtr GOODvertexp;
            bool HALFspace;
            bool ISqhullQh;
            int IStracing;
            int KEEParea;
            bool KEEPcoplanar;
            bool KEEPinside;
            int KEEPmerge;
            double KEEPminArea;
            double MAXcoplanar;
            int MAXwide;
            bool MERGEexact;
            bool MERGEindependent;
            bool MERGING;
            double premerge_centrum;
            double postmerge_centrum;
            bool MERGEpinched;
            bool MERGEvertices;
            double MINvisible;
            bool NOnarrow;
            bool Nonearinside;
            bool NOpremerge;
            bool ONLYgood;
            bool ONLYmax;
            bool PICKfurthest;
            bool POSTmerge;
            bool PREmerge;
            bool PRINTcentrums;
            bool PRINTcoplanar;
            int PRINTdim;
            bool PRINTdots;
            bool PRINTgood;
            bool PRINTinner;
            bool PRINTneighbors;
            bool PRINTnoplanes;
            bool PRINToptions1st;
            bool PRINTouter;
            bool PRINTprecision;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 29)]
            int[] PRINTout;
            bool PRINTridge;
            bool PRINTspheres;
            bool PRINTstatistics;
            bool PRINTsummary;
            bool PRINTtransparent;
            bool PROJECTdelaunay;
            int PROJECTinput;
            bool RANDOMdist;
            double RANDOMfactor;
            double RANDOMa;
            double RANDOMb;
            bool RANDOMoutside;
            int REPORTfreq;
            int REPORTfreq2;
            int RERUN;
            int ROTATErandom;
            bool SCALEinput;
            bool SCALElast;
            bool SETroundoff;
            bool SKIPcheckmax;
            bool SKIPconvex;
            bool SPLITthresholds;
            int STOPadd;
            int STOPcone;
            int STOPpoint;
            int TESTpoints;
            bool TESTvneighbors;
            int TRACElevel;
            int TRACElastrun;
            int TRACEpoint;
            double TRACEdist;
            int TRACEmerge;
            bool TRIangulate;
            bool TRInormals;
            bool UPPERdelaunay;
            bool USEstdout;
            bool VERIFYoutput;
            bool VIRTUALmemory;
            bool VORONOI;

            double AREAfactor;
            bool DOcheckmax;
            
            [MarshalAs(UnmanagedType.LPStr)] string feasible_string;
            IntPtr feasible_point;
            
            bool GETarea;
            bool KEEPnearinside;
            int hull_dim;
            int input_dim;
            int num_points;
            IntPtr first_point;
            bool POINTSmalloc;
            IntPtr input_points;
            bool input_malloc;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            string qhull_command;
            
            int qhull_commandsiz2;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            string rbox_command;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            string qhull_options;

            int qhull_optionlen;
            int qhull_optionsiz;
            int qhull_optionsiz2;
            int run_id;
            bool VERTEXneighbors;
            bool ZEROcentrum;
            IntPtr upper_threshold;
            IntPtr lower_threshold;
            IntPtr upper_bound;
            IntPtr lower_bound;
            
            //
            double ANGLEround;
            double centrum_radius;
            double cos_max;
            double DISTround;
            double MAXabs_coord;
            double MAXlastcoord;
            double MAXoutside;
            double MAXsumcoord;
            double MAXwidth;
            double MINdenom_1;
            double MINdenom;
            double MINdenom_1_2;
            double MINdenom_2;
            double MINlastcoord;
            IntPtr NEARzero;
            double NEARinside;
            double ONEmerge;
            double outside_err;
            double WIDEfacet;
            bool NARROWhull;

            //
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
            string qhull;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            _SETJMP_FLOAT128[] errexit;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            string jmpXtra;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            _SETJMP_FLOAT128[] restartexit;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            string jmpXtra2;

            IntPtr fin;
            IntPtr fout;
            IntPtr ferr;

            IntPtr interior_point;
            int normal_size;
            int center_size;
            int TEMPsize;

            //
            IntPtr facet_list;
            IntPtr facet_tail;
            IntPtr facet_next;
            IntPtr newfacet_list;
            IntPtr visible_list;
            int num_visible;
            uint tracefacet_id;
            IntPtr tracefacet;
            uint traceridge_id;
            IntPtr traceridge;
            uint tracevertex_id;
            IntPtr tracevertex;
            IntPtr vertex_list;
            IntPtr vertex_tail;
            IntPtr newvertex_list;
            int num_facets;
            int num_vertices;
            int num_outside;
            int num_good;
            uint facet_id;
            uint ridge_id;
            uint vertex_id;
            uint first_newfacet;

            //
            ulong hulltime;
            bool ALLOWrestart;
            int build_cnt;
            int CENTERtype;
            int furthest_id;
            int last_errcode;
            IntPtr GOODclosest;
            IntPtr coplanar_apex;
            bool hasAreaVolume;
            bool hasTriangulation;
            bool isRenameVertex;
            double JOGGLEmax;
            bool maxoutdone;
            double max_outside;
            double max_vertex;
            double min_vertex;
            bool NEWfacets;
            bool NEWtentative;
            bool findbestnew;
            bool finbest_notsharp;
            bool NOerrexit;
            double PRINTcradius;
            double PRINTradius;
            bool POSTmerging;
            int printoutvar;
            int printoutnum;
            uint repart_facetid;
            int retry_addpoint;
            bool QHULLfinished;
            double totarea;
            double totvol;
            uint visit_id;
            uint vertex_visit;

               










        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _SETJMP_FLOAT128 {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            ulong[] Part;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct facetT {
            public double furthestdist;
            public double maxoutside;
            public double offset;
            public IntPtr normal;

            [StructLayout(LayoutKind.Explicit)]
            public struct f {
                [FieldOffset(0)]
                double area;
                [FieldOffset(0)]
                IntPtr replace;
                [FieldOffset(0)]
                IntPtr samecycle;
                [FieldOffset(0)]
                IntPtr newcycle;
                [FieldOffset(0)]
                IntPtr trivisible;
                [FieldOffset(0)]
                IntPtr triowner;
            };

            IntPtr center;
            IntPtr previous;
            IntPtr next;
            IntPtr vertices;
            IntPtr ridges;
            IntPtr neighbors;
            IntPtr outsideset;
            IntPtr coplanarset;
            uint visitid;
            uint id;

            uint raw;

            uint nummerge {
                get {
                    return (raw >> 9) & 0x1FF;
                }
            }

            bool tricoplanar { get => ((raw >> 1) & 0x01) != 0; }
            bool newfacet { get => ((raw >> 1) & 0x01) != 0; } 
            bool visible { get => ((raw >> 1) & 0x01) != 0; }
            bool toporient { get => ((raw >> 1) & 0x01) != 0; }

            bool simplicial { get => ((raw >> 1) & 0x01) != 0; }
            bool seen { get => ((raw >> 1) & 0x01) != 0; }
            bool seen2 { get => ((raw >> 1) & 0x01) != 0; }
            bool flipped { get => ((raw >> 1) & 0x01) != 0; }
            bool upperdelaunay { get => ((raw >> 1) & 0x01) != 0; }
            bool notfurthest { get => ((raw >> 1) & 0x01) != 0; }
            bool good { get => ((raw >> 1) & 0x01) != 0; }
            bool isarea { get => ((raw >> 1) & 0x01) != 0; }

            bool dupridge { get => ((raw >> 1) & 0x01) != 0; }
            bool mergeridge { get => ((raw >> 1) & 0x01) != 0; }
            bool mergeridge2 { get => ((raw >> 1) & 0x01) != 0; }
            bool coplanarhorizon { get => ((raw >> 1) & 0x01) != 0; }
            bool cycledone { get => ((raw >> 1) & 0x01) != 0; }
            bool tested { get => ((raw >> 1) & 0x01) != 0; }
            bool keepcentrum { get => ((raw >> 1) & 0x01) != 0; }

            bool newmerge { get => ((raw >> 1) & 0x01) != 0; }
            bool degenerate { get => ((raw >> 1) & 0x01) != 0; }
            bool redundant { get => ((raw >> 1) & 0x01) != 0; }

        }

        [DllImport("qhull_r.dll", EntryPoint = "qh_qhull")]
        private static extern void QHullNative();
        public static void QHull() => QHullNative();

        //boolT qh_addpoint(pointT* furthest, facetT* facet, boolT checkdist);
        [DllImport("qhull_r.dll", EntryPoint = "qh_addpoint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddPointNative(ref double furthest, ref Facet facet, [MarshalAs(UnmanagedType.Bool)] bool checkdist);
        public static bool AddPoint(ref double furthest, ref Facet facet, bool checkdist) => AddPointNative(ref furthest, ref facet, checkdist);
    }
}