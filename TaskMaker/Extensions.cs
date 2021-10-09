using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaker {
    public class Extensions {
        public static T[] Concat<T>(T[] a, T[] b) {
            var val = new T[a.Length + b.Length];

            a.CopyTo(val, 0);
            b.CopyTo(val, a.Length);

            return val;
        }

        //public static T[] Concat<T>(this T[] a, T[] b) {
        //    return Concat<T>(this, b);
        //}
    }
}
