﻿using MathNet.Numerics.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xamarin.Forms.Internals;

namespace TuggingController {


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

    public class QhullCSharp {
        private static IntPtr Qhull { get; set; }

        [DllImport("qhull_csharp.dll", EntryPoint = "qhull_run_delaunay", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr qhull_run_delaunay(int pointDimension, int pointCount, [In, Out] double[] flatPoints);
        public static void QhullRunDelaunay(int pointDimension, int pointCount, ref double[] flatPoints) {
            Qhull = qhull_run_delaunay(pointDimension, pointCount, flatPoints);
        }

        [DllImport("qhull_csharp.dll", EntryPoint = "qhull_run_convex", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr qhull_run_convex(int pointDimension, int pointCount, [In, Out] double[] flatPoints);
        public static void QhullRunConvex(int pointDimension, int pointCount, ref double[] flatPoints) {
            Qhull = qhull_run_convex(pointDimension, pointCount, flatPoints);
        }

        [DllImport("qhull_csharp.dll", EntryPoint = "qhull_delete", CallingConvention = CallingConvention.Cdecl)]
        private static extern int qhull_delete(IntPtr qhPtr);
        public static void QhullReset() {
            qhull_delete(Qhull);
        }

        //[DllImport("qhull_csharp.dll", EntryPoint = "qhull_get_facets", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //private static extern void qhull_get_facets(IntPtr qhPtr, StringBuilder facetsInfo);
        //public static void QhullGetFacets(ref StringBuilder facetsInfo) => qhull_get_facets(Qhull, facetsInfo);

        [DllImport("qhull_csharp.dll", EntryPoint = "qhull_get_good_facets", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern void qhull_get_good_facets(IntPtr qhPtr, StringBuilder goodFacetsInfo);
        private static List<int[]> ParseGoodFacetsInfo(string info) {
            Console.WriteLine(info);

            List<int[]> facetVerticesCollection = new List<int[]>();

            foreach (var line in info.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
                int[] vertexIndices = line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(v => Convert.ToInt32(v)).ToArray();

                facetVerticesCollection.Add(vertexIndices);
            }

            return facetVerticesCollection;
        }
        public static List<int[]> QhullGetGoodFacets() {
            var goodFacetsInfo = new StringBuilder(1000);

            qhull_get_good_facets(Qhull, goodFacetsInfo);

            return ParseGoodFacetsInfo(goodFacetsInfo.ToString());
        }

        [DllImport("qhull_csharp.dll", EntryPoint = "qhull_get_convex_hull", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern void qhull_get_convex_hull(IntPtr qhPtr, StringBuilder convexHullInfo);

        [DllImport("qhull_csharp.dll", EntryPoint = "qhull_get_convex_hull_2d", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern void qhull_get_convex_hull_2d(IntPtr qhPtr, StringBuilder convexHullInfo);

        private static List<int> ParseInfo(string info) {
            //Console.WriteLine(info);

            List<int> collection = new List<int>();

            foreach (var line in info.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
                collection.Add(Convert.ToInt32(line));
            }

            return collection;
        }

        public static List<int> QhullGetConvexHull() {
            var convexHullInfo = new StringBuilder(1000);

            qhull_get_convex_hull_2d(Qhull, convexHullInfo);

            return ParseInfo(convexHullInfo.ToString());
        }
    }
}
