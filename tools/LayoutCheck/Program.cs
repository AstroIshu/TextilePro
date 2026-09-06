using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var app = new TextilePro.UI.App();
        app.InitializeComponent();
        System.Reflection.Assembly.Load("MaterialDesignThemes.Wpf");
        var root = Path.GetFullPath("TextilePro.UI/Views");
        Directory.CreateDirectory("artifacts/layout");
        XNamespace w = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        foreach (var file in Directory.GetFiles(root, "*.xaml"))
        {
            var xml = XDocument.Load(file);
            xml.Root.Attribute(x + "Class")?.Remove();
            foreach (var attr in xml.Root!.DescendantsAndSelf().Attributes()
                         .Where(a => a.Name.LocalName is "Click" or "PreviewTextInput" or "Pasting" or "DataObject.Pasting").ToList())
                attr.Remove();
            foreach (var attr in xml.Root.DescendantsAndSelf().Attributes().Where(a => a.Value.StartsWith("clr-namespace:TextilePro.UI") && !a.Value.Contains("assembly=")).ToList())
                attr.Value += ";assembly=TextilePro";
            foreach (var view in xml.Descendants().Where(e => e.Name.NamespaceName == "clr-namespace:TextilePro.UI.Views").ToList())
                view.ReplaceWith(new XElement(w + "TextBlock", new XAttribute("Text", view.Name.LocalName + " workspace")));
            foreach (var size in new[] { new Size(1180,720), new Size(1440,900) })
            {
                var markup = xml.ToString().Replace("clr-namespace:TextilePro.UI.Converters\"", "clr-namespace:TextilePro.UI.Converters;assembly=TextilePro\"");
                var element = (FrameworkElement)XamlReader.Parse(markup);
                if (Path.GetFileName(file) == "SupplierView.xaml")
                {
                    element.DataContext = new SupplierPreview
                    {
                        Contacts = new[]
                        {
                            new TextilePro.Core.Models.SupplierContact
                            {
                                ContactName = "Aarav Sharma",
                                Email = "aarav.sharma@example.com",
                                CountryCode = "+91",
                                LocalPhone = "9876543210",
                                Website = "https://example-supplier.com"
                            }
                        },
                        SelectedSupplier = new TextilePro.Core.Models.Supplier
                        {
                            Name = "Example supplier",
                            Country = "India",
                            Address = "Long address used to verify that supplier details remain fully visible without overlapping adjacent controls."
                        }
                    };
                }
                if (Path.GetFileName(file) == "EvaluationView.xaml")
                {
                    element.DataContext = new EvaluationPreview
                    {
                        EvaluationRecords =
                        [
                            new EvaluationRecordPreview { SupplierName = "Aarav Textiles Private Limited", EvaluationDate = "06-09-2026", Score = 19, Class = "A" },
                            new EvaluationRecordPreview { SupplierName = "Sustainable Fabric Works", EvaluationDate = "29-08-2026", Score = 14, Class = "B" }
                        ],
                        Documents =
                        [
                            new TextilePro.Core.Models.EvaluationDocument { FileName = "supplier-self-declaration.pdf" }
                        ]
                    };
                }
                if (element is Window window)
                {
                    var content = (FrameworkElement)window.Content;
                    window.Content = null;
                    content.Resources = window.Resources;
                    element = content;
                }
                var available = Path.GetFileName(file) == "MainWindow.xaml" ? size : new Size(size.Width-260,size.Height-110);
                element.Measure(available); element.Arrange(new Rect(available)); element.UpdateLayout();
                element.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                element.Measure(available); element.Arrange(new Rect(available)); element.UpdateLayout();
                var bmp = new RenderTargetBitmap((int)available.Width,(int)available.Height,96,96,PixelFormats.Pbgra32);
                bmp.Render(element);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp));
                using var output = File.Create($"artifacts/layout/{Path.GetFileNameWithoutExtension(file)}-{size.Width}.png");
                encoder.Save(output);
                Console.WriteLine($"Rendered {Path.GetFileName(file)} at {available}");
            }
        }
    }

    public class SupplierPreview
    {
        public TextilePro.Core.Models.SupplierContact[] Contacts { get; set; } = Array.Empty<TextilePro.Core.Models.SupplierContact>();
        public TextilePro.Core.Models.Supplier SelectedSupplier { get; set; } = new();
    }

    public class EvaluationPreview
    {
        public object? SelectedSupplier { get; set; }
        public object[] Suppliers { get; set; } = Array.Empty<object>();
        public EvaluationRecordPreview[] EvaluationRecords { get; set; } = Array.Empty<EvaluationRecordPreview>();
        public TextilePro.Core.Models.EvaluationDocument[] Documents { get; set; } = Array.Empty<TextilePro.Core.Models.EvaluationDocument>();
    }

    public class EvaluationRecordPreview
    {
        public string SupplierName { get; set; } = string.Empty;
        public string EvaluationDate { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Class { get; set; } = string.Empty;
    }
}
