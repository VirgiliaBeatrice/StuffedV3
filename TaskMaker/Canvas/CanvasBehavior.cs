using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNetExtension;
using TaskMaker.Mapping;

namespace TaskMaker {
    public partial class Canvas {

    }

    public partial class Layer {
        private readonly Triangulation _triHandler = Services.TriHandler;

        public void Triangulate() {
            var selectedEntities = Entities.Where(e => e.IsSelected).ToList();

            // Clear any simlicial complex which includes any selected entity
            foreach(var e in selectedEntities) {
                foreach(var c in new List<SimplicialComplex>(Complexes)) {
                    if (c.ContainsEntity(e)) {
                        var idx = Complexes.IndexOf(c);

                        Complexes.RemoveAt(idx);
                        Exteriors.RemoveAt(idx);
                    }
                }
            }

            Triangulate(selectedEntities);
        }

        private void Triangulate(List<Entity> entities) {
            var complex = new SimplicialComplex();

            Complexes.Add(complex);

            // Case: amount less than 3
            if (entities.Count() < 3) {
                return;
            }
            else if (entities.Count() == 3) {
                var tri = entities.ToArray();

                complex.Add(new Simplex(tri));

                var a = tri[0].Location;
                var b = tri[1].Location;
                var c = tri[2].Location;

                var centroid = (a + b + c).DivideBy(3.0f);
                var theta0 = Math.Asin((a - centroid).Cross(b - centroid) / ((a - centroid).Length * (b - centroid).Length));
                var theta1 = Math.Asin((a - centroid).Cross(c - centroid) / ((a - centroid).Length * (c - centroid).Length));

                var ccw = theta0 > theta1 ? new Entity[] { tri[0], tri[2], tri[1] } : new Entity[] { tri[0], tri[1], tri[2] };

                foreach (var e in ccw) {
                    complex.AddExtreme(e);
                }

                Exteriors.Add(complex.CreateExterior());
                //CreateExterior();
            }
            else {
                // Case: amount larger than 3
                var vectors = entities.Select(e => new double[] { e.Location.X, e.Location.Y });
                var flattern = new List<double>();

                foreach (var e in vectors) {
                    flattern.AddRange(e);
                }

                var input = flattern.ToArray();
                var output = _triHandler.RunDelaunay_v1(2, input.Length / 2, ref input);

                var outputConvexList = _triHandler.RunConvexHull_v1(2, input.Length / 2, ref input);
                // cw => ccw
                outputConvexList.Reverse();
                var outputConvex = new LinkedList<int>(outputConvexList);

                foreach (var triIndices in output) {
                    var arrSelectedEntities = entities.ToArray();
                    var tri = new Entity[] {
                            arrSelectedEntities[triIndices[0]],
                            arrSelectedEntities[triIndices[1]],
                            arrSelectedEntities[triIndices[2]]
                        };

                    complex.Add(new Simplex(tri));
                }

                // Get all edges of convex hull.
                for (var it = outputConvex.First; it != null; it = it.Next) {
                    Entity e1;
                    var arrSelectedEntities = entities.ToArray();

                    var e0 = arrSelectedEntities[it.Value];

                    complex.AddExtreme(e0);

                    if (it == outputConvex.Last) {
                        e1 = arrSelectedEntities[outputConvex.First.Value];
                    }
                    else {
                        e1 = arrSelectedEntities[it.Next.Value];
                    }

                    var edge = complex.GetAllEdges().Where(e => e.Contains(e0) & e.Contains(e1));

                    complex.AddComplexEdge(edge.First());
                }

                Exteriors.Add(complex.CreateExterior());
                //CreateExterior();
            }

            // Reset entities' states
            Invalidate();
        }

        public void Reset() {
            Complexes.ForEach(c => c.Reset());
        }
    }
}
