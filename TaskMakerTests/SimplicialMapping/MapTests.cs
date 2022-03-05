using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskMaker.SimplicialMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Numpy;

namespace TaskMaker.SimplicialMapping.Tests {
    [TestClass()]
    public class MapTests {
        private readonly NLinearMap _multiBary;

        public MapTests() {
            _multiBary = new NLinearMap();

            var layer = new ControlUIWidget("Test");
            layer.Entities = GetEntities();

            layer.Entities.ForEach(e => e.IsSelected = true);
            
            layer.Complex = new SimplicialComplex();
            layer.Triangulate();

            //_multiBary.AddBary(
            //    layer.Entities.ToArray(),
            //    layer.Complex.ToArray(),
            //    layer.Exterior);
        }

        private List<Entity> GetEntities() {
            var p0 = new SKPoint() { X = 100, Y = 100 };
            var p1 = new SKPoint() { X = 200, Y = 200 };
            var p2 = new SKPoint() { X = 300, Y = 300 };
            var e0 = new Entity(p0);
            var e1 = new Entity(p1);
            var e2 = new Entity(p2);

            return new List<Entity>() { e0, e1, e2 };
        }

        private void SetTensor() {
            while(_multiBary.SetComponent(np.arange(100.0f).reshape(1, 2).GetData<float>())){

            }
        }


        [TestMethod()]
        public void MultiBaryTest() {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddBaryTest() {
            Assert.Fail();
        }

        [TestMethod()]
        public void ConcatTest() {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetIndicesTest() {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetTensorTest() {
            Assert.Fail();
        }

        [TestMethod()]
        public void CalculateTest() {
            Assert.Fail();
        }
    }
}