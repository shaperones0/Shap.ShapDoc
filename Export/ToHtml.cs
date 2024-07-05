namespace Shap.ShapDoc.Exporter
{
    public class ShapDocToHtml
    {
        public class HtmlException(string? message, Exception? innerException = null)
            : Exception(message, innerException)
        { }

        internal readonly struct HeadInfo(string? title = null, string? subtitle = null, string? version = null, string? author = null)
        {
            public readonly string title = title ?? "ShapDoc Document";
            public readonly string subtitle = subtitle ?? "";
            public readonly string version = version ?? "0.0.1";
            public readonly string author = author ?? "shaperones";
        }

        static internal HeadInfo GatherHeadInfo(ShapDoc.Node headNode)
        {
            return new HeadInfo(
                title: headNode.GetInnerProperty("title"),
                subtitle: headNode.GetInnerProperty("subtitle"),
                author: headNode.GetInnerProperty("author"),
                version: headNode.GetInnerProperty("version")
            );
        }

        static internal MyLinkedList<string> ProcessTextNode(ShapDoc.Node node)
        {
            MyLinkedList<string> rezult;
            switch (node.Name)
            {
                case "t":
                    //text node cannot contain anything other than text
                    return new([node.Args["text"]!]);
                case "b":
                    //inside any other node there might be other nodes
                    rezult = new(["<strong>"]);
                    foreach (ShapDoc.Node childNode in node.Children)
                    {
                        rezult.AttachLast(ProcessTextNode(childNode));
                    }
                    rezult.AddLast("</strong>");
                    return rezult;
                case "h1":
                    rezult = new(["<h1>"]);
                    foreach (ShapDoc.Node childNode in node.Children)
                    {
                        rezult.AttachLast(ProcessTextNode(childNode));
                    }
                    rezult.AddLast("</h1>");
                    return rezult;
                case "img":
                    string src = node.Args["src"]!;
                    return new([$"<img src=\"{src}\">"]);
                case "br":
                    return new(["<br>"]);
                default:
                    throw new HtmlException($"Conversion of tag to HTML is unsupported atm.");
            }
        }

        static internal List<string> ProcessPreformattedCodeNode(ShapDoc.Node nodePCode)
        {
            List<string> rezult = ["<div class=\"code\"><div>"];
            foreach (ShapDoc.Node node in nodePCode.Children)
            {
                switch (node.Name)
                {
                    case "pcn":
                        string text = node.Args.GetValueOrDefault("text") ?? "";
                        if (node.Args.TryGetValue("type", out string? type))
                        {
                            
                            rezult.Add($"<span class=\"code-{type!}\">{text}</span>");
                        }
                        else
                        {
                            rezult.Add($"<span>{text}</span>");
                        }
                        break;
                    case "pcbr":
                        rezult.Add("&nbsp;</div><div>");
                        break;
                    default:
                        throw new HtmlException("Invalid syntax of pcode");
                }
            }
            rezult.Add("</div></div>");

            return rezult;
        }

        public string Convert(ShapDoc doc)
        {
            ShapDoc.Node? headNode = doc.Root.FindChild("head");
            HeadInfo headInfo = headNode != null ? GatherHeadInfo(headNode) : new();
            string htmlBegin = @$"<html><head><title>{headInfo.title}</title><style>@font-face{{font-family:CMUSerif;src:url(data/fnt/cmunrm-msofix.ttf)}}@font-face{{font-family:CMUSerif;font-weight:700;src:url(data/fnt/cmunbx-msofix.ttf)}}@font-face{{font-family:Consolas;src:url(data/fnt/consola.ttf)}}:root{{--code_space:#3b3b3b;--code_keyword1:#af00db;--code_keyword2:#0000ff;--code_operator:#000000;--code_number:#098658;--code_variable:#001080;--code_string:#a31515;--code_function:#795e26;--code_class:#267f99;--code_comment:#008000}}body{{font-family:""Times New Roman"",Times,serif;width:850px;margin-left:auto;margin-right:auto;font-size:20;margin-bottom:300px}}h1,h2{{font-family:CMUSerif,serif}}img{{width:850px;object-fit:contain}}div.code{{border-style:solid;border-width:1px;padding:5px 5px 5px 5px;font-family:Consolas,Consolas,monospace;font-size:14;background-color:#f0f0f0}}span.code-Space{{color:var(--code_space)}}span.code-Keyword1{{color:var(--code_keyword1)}}span.code-Keyword2{{color:var(--code_keyword2)}}span.code-Operator{{color:var(--code_operator)}}span.code-Number{{color:var(--code_number)}}span.code-Variable{{color:var(--code_variable)}}span.code-String{{color:var(--code_string)}}span.code-Function{{color:var(--code_function)}}span.code-Class{{color:var(--code_class)}}span.code-Comment{{color:var(--code_comment)}}</style></head><body>";
            string htmlEnd = "</body></html>";
            List<string> htmlInner = [];

            foreach (ShapDoc.Node topLevelNode in doc.Root.Children)
            {
                switch (topLevelNode.Name)
                {
                    case "head": break; //skip this node
                    case "t":
                    case "b":
                    case "h1":
                    case "img":
                    case "br":
                        htmlInner.AddRange(ProcessTextNode(topLevelNode).AsEnumerable());
                        break;
                    //case "code":
                    //    //TODO implement a better syntax highlighter
                    //    break;
                    case "pcode":
                        htmlInner.AddRange(ProcessPreformattedCodeNode(topLevelNode));
                        break;
                    default:
                        throw new HtmlException($"Conversion of tag to HTML is unsupported atm.");
                }
            }

            return htmlBegin + string.Join("", htmlInner) + htmlEnd;
        }
    }

    internal class MyLinkedList<T>
    {
        public class Node(T value)
        {
            public T Value = value;
            public Node? Previous { get; set; } = null;
            public Node? Next { get; set; } = null;

            public void AttachNext(Node node)
            {
                Next = node;
                node.Previous = this;
            }

            public Node DetachNext()
            {
                if (Next == null) throw new Exception();
                Node nextNode = Next;
                nextNode.Previous = null;
                Next = null;
                return nextNode;
            }
        }
        public Node? First { get; private set; } = null;
        public Node? Last { get; private set; } = null;
        public int Count { get; private set; } = 0;

        public MyLinkedList(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                AddLast(item);
            }
        }

        public void AddLast(T item)
        {
            if (Last == null)
            {
                First = new(item);
                Last = First;
            }
            else
            {
                Node newNode = new(item);
                Last.AttachNext(newNode);
                Last = newNode;
            }
            Count++;
        }

        public void AttachLast(MyLinkedList<T> list)
        {
            if (list.First == null) return;
            list.First.Previous = Last;
            if (Last != null) Last.Next = list.First;
            Last = list.Last;
            Count += list.Count;
        }

        public IEnumerable<T> AsEnumerable()
        {
            if (First != null)
            {
                Node? curNode = First;
                while (curNode != null)
                {
                    yield return curNode.Value;
                    curNode = curNode.Next;
                }
            }
        }
    }
}
