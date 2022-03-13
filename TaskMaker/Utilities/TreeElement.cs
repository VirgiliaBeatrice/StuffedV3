using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TaskMaker.View;
using TaskMaker.View.Widgets;

namespace TaskMaker.Utilities {
    public partial class TreeElement {
        static public TreeElement Create<T>(string name) where T : new() {
            return new TreeElement {
                Name = name,
                Data = new T()
            };
        }
    }

    public partial class TreeElement {
        public string Name { get; set; } = "";
        public TreeElement Parent { get; set; } = null;
        protected virtual List<TreeElement> Children { get; set; } = new List<TreeElement>();

        public object Data { get; set; }

        public TreeElement() { }

        public TreeElement(TreeElement other) {
            Name = other.Name;
            Data = other.Data;
        }

        public void AddChild(TreeElement child) {
            Children.Add(child);
            child.Parent = this;
        }

        public TreeElement AddChild<T>(string name) where T : new() {
            var newChild = Create<T>(name);

            AddChild(newChild);

            return newChild;
        }

        public bool RemoveChild(TreeElement child) {
            return Children.Remove(child);
        }

        public List<TreeElement> GetAllChild() {
            return Children;
        }

        public string PrintAllChild() {
            var content = new StringBuilder();
            content.AppendLine($"|-- {Name}");

            foreach (var child in Children) {
                var output = child.PrintAllChild();

                using (var stream = new MemoryStream()) {

                    var writer = new StreamWriter(stream);

                    writer.Write(output);
                    writer.Flush();
                    stream.Position = 0;

                    var sr = new StreamReader(stream);
                    var line = sr.ReadLine();

                    content.Append(' ', 4);
                    content.AppendLine(line);

                    while (!sr.EndOfStream) {
                        line = sr.ReadLine();

                        content.Append(' ', 4);
                        content.AppendLine(line);
                    }
                }
            }

            return content.ToString();
        }

        static public TreeElement Clone(TreeElement node) {
            var newNode = new TreeElement(node);

            foreach (var child in node.Children) {
                var newChildNode = Clone(child);

                newNode.AddChild(newChildNode);
            }

            return newNode;
        }

        public TreeElement Clone() {
            return Clone(this);
        }
    }
}
