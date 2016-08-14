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

            Docs = LoadDirectoryNode(new DirectoryInfo(path));
        }

        private FlowDocument GenerateDocument(Node node)
        {
            if (!(node is DocumentNode))
            {
                var paragraph = new Paragraph(new Run("Выберите документ для отображения."));
                paragraph.TextAlignment = TextAlignment.Center;
                return new FlowDocument(paragraph);
            }
            
            if (!File.Exists(node.Path))
            {
                var paragraph = new Paragraph(new Run("Файл не найден."));
                paragraph.TextAlignment = TextAlignment.Center;
                return new FlowDocument(paragraph);
            }

            try
            {
                var doc = new FlowDocument();
                using (FileStream fs = File.Open(node.Path, FileMode.Open, FileAccess.Read))
                {
                    var content = new TextRange(doc.ContentStart, doc.ContentEnd);
                    content.Load(fs, DataFormats.Rtf);
                }
                doc.PageWidth = 800;
                return doc;
            }
            catch
            {
                var paragraph = new Paragraph(new Run("Файл поврежден и не может быть отображен."));
                paragraph.TextAlignment = TextAlignment.Center;
                return new FlowDocument(paragraph);
            }
        }

        #endregion

        #region Event handlers

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedNode))
            {
                documentViewer.Document = GenerateDocument(SelectedNode);
                if (SelectedNode != contentsTreeView.SelectedItem)
                {
                    (contentsTreeView.ItemContainerGenerator.ContainerFromItem(SelectedNode) as TreeViewItem).IsSelected = true;
                }
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedNode = contentsTreeView.SelectedItem as Node;
        }

        private void OnNavigationRequest(object sender, RoutedEventArgs e)
        {
            /*
            Hyperlink link = e.OriginalSource as Hyperlink; 
            string uri = link.NavigateUri.ToString();

            if (uri.Contains("http"))
            {
                Process.Start(uri);
            }
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
            /*
            if (previousNode.Any())
            {
                nextNode.Push(_activeDocument);
                ShowDocument(previousNode.Pop());
                previousNode.Pop();
            }
            */
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (nextNode.Any())
            {
                ShowDocument(nextNode.Pop());
            }
            */
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
