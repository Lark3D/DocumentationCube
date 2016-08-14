using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentationCube
{
    public class Node
    {
        public string Text { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public class CategoryNode : Node
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
    }

    public class DocumentNode : Node
    {

    }


}
