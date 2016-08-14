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
                doc.PageWidth = 800;
                doc.PagePadding = new Thickness(50);
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
            doc.PageWidth = 800;
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
            }
            else
            {
                var splitresult = uri.OriginalString.Split(new char[] { '#' }, 2);
                string path = splitresult[0].Trim();
                string bookmark = null;
                if (splitresult.Count() > 1) bookmark = splitresult[1].Trim();

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

                documentViewer.Document

            }
            /*
            else
            {
                if (uri.Contains('#'))
                {
                    var uriSplit = uri.Split('#');
                    var pathUri = uriSplit[0];
                    var bookmarkName = uriSplit[1];

                    //ShowDocument(pathUri);

                    using (FileStream fs = File.Open(pathUri, FileMode.Open, FileAccess.Read))
                    {
                        var content = new TextRange(mainDocument.ContentStart, mainDocument.ContentEnd);

                        if (content.CanLoad(DataFormats.Rtf))
                        {
                            content.Load(fs, DataFormats.Rtf);
                        }

                        foreach (Block block in mainDocument.Blocks)
                        {
                            if (block is Paragraph)
                            {
                                Paragraph paragraph = (Paragraph)block;
                                Search(paragraph.Inlines, bookmarkName, paragraph);
                            }
                            else
                            {
                                
                            }
                            //        mainDocument.
                            //        if (block.)
                            //            block.BringIntoView
                            //   }
                        }

                        //paragraph.BringIntoView();
                    }
                }
                else
                {
                    ShowDocument(uri);
                }
            }
            */
            
        }

        private void Search(InlineCollection inlines, string bookmarkName, Paragraph paragraph)
        {
            foreach (Inline inline in inlines)
            {
                if (inline is Run)
                {
                    Run run = (Run)inline;
                    if (run.Text.Contains("#"))
                    {
                        paragraph.BringIntoView();
                    }
                }
                else
                {
                    if (inline is Span)
                    {
                        Span span = (Span)inline;
                        
                        Search(span.Inlines, bookmarkName, paragraph);
                    }
                }
            }
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
