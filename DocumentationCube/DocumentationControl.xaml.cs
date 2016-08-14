using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DocumentationCube
{
    /// <summary>
    /// Логика взаимодействия для DocumentationControl.xaml
    /// </summary>
    public partial class DocumentationControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private CategoryNode _docs = null;
        private Node _selectedNode;
        private Dictionary<string, Block> _bookmarkedBlocks = new Dictionary<string, Block>();
        private Stack<Node> previousNode = new Stack<Node>();
        private Stack<Node> nextNode = new Stack<Node>();
        #endregion

        public DocumentationControl()
        {
            InitializeComponent();
            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(OnNavigationRequest));
            PropertyChanged += OnPropertyChanged;
            DataContext = this;
        }

        #region Properties

        public CategoryNode Docs
        {
            get { return _docs; }
            set { _docs = value; RaisePropertyChanged(nameof(Docs)); }
        }


        public Node SelectedNode
        {
            get
            {
                return _selectedNode;
            }
            set
            {
                _selectedNode = value;
                RaisePropertyChanged(nameof(SelectedNode));
            }
        }

        #endregion

        #region Methods

        private static CategoryNode LoadDirectoryNode(DirectoryInfo directoryInfo)
        {
            var node = new CategoryNode();
            node.Text = directoryInfo.Name;
            node.Path = directoryInfo.FullName;

            foreach (var directory in directoryInfo.GetDirectories())
                node.Nodes.Add(LoadDirectoryNode(directory));

            foreach (var file in directoryInfo.GetFiles())
            {
                if (System.IO.Path.GetExtension(file.Name).ToLower() != ".rtf") continue;
                var fnode = new DocumentNode();
                fnode.Text = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                fnode.Path = file.FullName;
                node.Nodes.Add(fnode);
            }

            return node;
        }


        private void LoadDocumentation(string path)
        {
            if (!Directory.Exists(path)) path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), path);
            if (!Directory.Exists(path)) return;

            previousNode.Clear();
            nextNode.Clear();

            Docs = LoadDirectoryNode(new DirectoryInfo(path));
            SelectedNode = null;
        }


        private Node LookUpFullPath(Node node, string path)
        {
            if (node.Path == path) return node;
            if (node is CategoryNode)
            {
                foreach(Node n in (node as CategoryNode).Nodes)
                {
                    Node resultnode = LookUpFullPath(n, path);
                    if (resultnode != null) return resultnode;
                }
            }
            return null;
        }


        private Node LookUpFile(Node node, string file)
        {
            if (System.IO.Path.GetFileName(node.Path) == file) return node;
            if (node is CategoryNode)
            {
                foreach (Node n in (node as CategoryNode).Nodes.OrderBy( n => (n is CategoryNode)? 1 : 0 ))
                {
                    Node resultnode = LookUpFile(n, file);
                    if (resultnode != null) return resultnode;
                }
            }
            return null;
        }


        private FlowDocument GenerateDocument(Node node)
        {
            _bookmarkedBlocks.Clear();
            if (node == null) return SimpleDocument("Выберите документ для отображения.");

            if (!(node is DocumentNode)) return SimpleDocument("Выберите документ для отображения.");
            
            if (!File.Exists(node.Path)) return SimpleDocument("Файл не найден.");

            try
            {
                var doc = new FlowDocument();
                using (FileStream fs = File.Open(node.Path, FileMode.Open, FileAccess.Read))
                {
                    var content = new TextRange(doc.ContentStart, doc.ContentEnd);
                    content.Load(fs, DataFormats.Rtf);
                }
                doc.MaxPageWidth = 800;
                doc.ColumnWidth = 800;
                doc.PagePadding = new Thickness(50);
                _bookmarkedBlocks = ProccessBookmarksInDocument(doc);
                return doc;
            }
            catch
            {
                return SimpleDocument("Файл поврежден и не может быть отображен.");
            }
        }


        private FlowDocument SimpleDocument(string text)
        {
            var paragraph = new Paragraph(new Run(text));
            paragraph.TextAlignment = TextAlignment.Center;
            var doc = new FlowDocument(paragraph);
            doc.MaxPageWidth = 800;
            doc.ColumnWidth = 800;
            doc.PagePadding = new Thickness(50);
            return doc;
        }


        private void SelectNewNode(Node node)
        {
            nextNode.Clear();
            if (SelectedNode is DocumentNode) previousNode.Push(SelectedNode);
            SelectedNode = node;
        }

        #endregion

        #region Event handlers

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedNode))
            {
                documentViewer.Document = GenerateDocument(SelectedNode);

                var item = contentsTreeView.ItemContainerGenerator.ContainerFromItem(contentsTreeView.SelectedItem) as TreeViewItem;
                if (item != null) item.IsSelected = false;
                
                if (SelectedNode != null && SelectedNode != contentsTreeView.SelectedItem)
                {
                    item = contentsTreeView.ItemContainerGenerator.ContainerFromItem(SelectedNode) as TreeViewItem;
                    if (item != null) item.IsSelected = true;
                }
            }
        }


        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (contentsTreeView.SelectedItem == null) return;
            if (SelectedNode == contentsTreeView.SelectedItem) return;
            SelectNewNode(contentsTreeView.SelectedItem as Node);
        }


        private void OnNavigationRequest(object sender, RoutedEventArgs e)
        {
            
            Hyperlink link = e.OriginalSource as Hyperlink;
            Uri uri = link.NavigateUri;

            if (uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                Process.Start(uri.OriginalString);
                return;
            }

            var splitresult = uri.OriginalString.Split(new char[] { '#' }, 2);
            string path = splitresult[0].Trim();
            string bookmark = null;
            if (splitresult.Count() > 1) bookmark = splitresult[1].Trim();
            if (bookmark != null) bookmark = "#" + bookmark;

            if (!string.IsNullOrEmpty(path))
            {
                string fullpath = System.IO.Path.Combine(Docs.Path, path.Replace('/', '\\'));
                Node node = LookUpFullPath(Docs, fullpath);
                if (node == null) node = LookUpFile(Docs, path);
                if (node != null)
                {
                    SelectNewNode(node);
                }
            }

            if (!string.IsNullOrEmpty(bookmark))
            {
                /*
                Paragraph p = GetBookmarkFromDocument(documentViewer.Document, bookmark);
                if (p != null)
                {
                    if (p.NextBlock != null) p.NextBlock.BringIntoView();
                    //p.NextBlock.BringIntoView();
                    //MessageBox.Show("found " + bookmark);
                }
                */
                if (_bookmarkedBlocks.ContainsKey(bookmark)) _bookmarkedBlocks[bookmark].BringIntoView();
            }
            
        }


        // не используется
        private Paragraph GetBookmarkFromDocument(FlowDocument document, string bookmark)
        {
            foreach (Paragraph p in document.Blocks.OfType<Paragraph>())
            {
                Run run;
                if (p.Inlines.FirstInline is Run)
                {
                    run = p.Inlines.FirstInline as Run;
                }
                else if (p.Inlines.FirstInline is Span)
                {
                    Span span = p.Inlines.FirstInline as Span;
                    if (span.Inlines.FirstInline is Run) run = span.Inlines.FirstInline as Run;
                    else continue;
                }
                else
                {
                    continue;
                }
                if (run.Text == bookmark) return p;
            }
            return null;
        }


        private Dictionary<string, Block> ProccessBookmarksInDocument(FlowDocument document)
        {
            var list = new List<Block>();
            var dic = new Dictionary<string, Block>();

            foreach (Paragraph p in document.Blocks.OfType<Paragraph>())
            {
                Run run;
                if (p.Inlines.FirstInline is Run)
                {
                    run = p.Inlines.FirstInline as Run;
                }
                else if (p.Inlines.FirstInline is Span)
                {
                    Span span = p.Inlines.FirstInline as Span;
                    if (span.Inlines.FirstInline is Run) run = span.Inlines.FirstInline as Run;
                    else continue;
                }
                else
                {
                    continue;
                }
                if (run.Text.StartsWith("#") && p.NextBlock != null)
                {
                    dic.Add(run.Text, p.NextBlock);
                    list.Add(p);
                }
            }

            while (list.Count > 0 )
            {
                document.Blocks.Remove(list[list.Count - 1]);
                list.RemoveAt(list.Count - 1);
            }

            return dic;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (previousNode.Any())
            {
                nextNode.Push(SelectedNode);
                SelectedNode = previousNode.Pop();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (nextNode.Any())
            {
                previousNode.Push(SelectedNode);
                SelectedNode = nextNode.Pop();
            }
        }
        #endregion

        #region INotifyPropertyChanded
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Dependancy Properties
        
        public static DependencyProperty PathProperty =
            DependencyProperty.Register("Path", typeof(string), typeof(DocumentationControl),
            new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnPathPropertyChanged)));

        private static void OnPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as DocumentationControl).LoadDocumentation((string)e.NewValue);
        }

        public string Path
        {
            get
            {
                return (string)GetValue(PathProperty);
            }
            set
            {
                SetValue(PathProperty, value);
            }
        }

        #endregion
    }
}
