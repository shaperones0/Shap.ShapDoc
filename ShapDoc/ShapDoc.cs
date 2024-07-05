using System.Diagnostics;

namespace Shap.ShapDoc
{
    public class ShapDoc(ShapDoc.Node root)
    {
        [DebuggerDisplay("<{Name} args[{Args.Count}] children[{Children.Count}]>")]
        public class Node(string name, Node? parent,
            Dictionary<string, string?>? args = null, List<Node>? children = null)
        {
            public readonly string Name = name;
            public readonly Dictionary<string, string?> Args = args ?? [];
            public readonly List<Node> Children = children ?? [];
            public readonly Node? Parent = parent;

            public IEnumerable<Node> EachChildWithTag(string name)
            {
                foreach (Node child in Children)
                {
                    if (child.Name == name)
                        yield return child;
                }
            }

            public Node? FindChild(string name)
            {
                IEnumerable<Node> children = EachChildWithTag(name);
                if (children.Any()) return children.First();
                else return null;
            }

            public string? GetInnerText()
            {
                if (Children.Count == 1 && Children[0].Name == "t")
                    return Children[0].Args["text"]!;
                return null;
            }

            public string GetInnerTextOrDefault(string defaultStr)
            {
                if (Children.Count == 1 && Children[0].Name == "t")
                    return Children[0].Args["text"]!;
                return defaultStr;
            }

            public string? GetInnerProperty(string name)
            {
                Node? childNode;
                if ((childNode = FindChild(name)) != null)
                    return childNode.GetInnerText();
                return null;
            }

            public string GetInnerPropertyOrDefault(string name, string defaultStr)
            {
                Node? childNode;
                if ((childNode = FindChild(name)) != null)
                    return childNode.GetInnerTextOrDefault(defaultStr);
                return defaultStr;
            }
        }

        public readonly Node Root = root;
    }
}
