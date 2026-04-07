using ICSharpCode.AvalonEdit.Highlighting;
using lr9.Code;
using Npgsql;
using System.Data;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace lr9
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CommandBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("SQL");
            SqlHighlighManager.EnableCommentHighlighting(CommandBox);

            CommandBox.TextArea.KeyDown += CommandBox_KeyDown;
        }

        private async void Execute_Click(object sender, RoutedEventArgs e)
        {

            MainTabControl.IsEnabled = false;
            ResultTabControl.IsEnabled = false;
            ExecuteButton.IsEnabled = false;

            string query = GetCurrentQuery();
            await DBCommandManager.Instance.ExecuteCommands(query, this);

            MainTabControl.IsEnabled = true;
            ResultTabControl.IsEnabled = true;
            ExecuteButton.IsEnabled = true;
        }

        private void CommandBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Execute_Click(this, e);
            }
        }

        public void CreateNewResultTab(DataTable table)
        {
            TabItem newTab = new TabItem();
            newTab.Header = $"Результат {ResultTabControl.Items.Count}";

            DataGrid dg = new DataGrid();
            dg.AutoGenerateColumns = true;
            dg.ItemsSource = table.DefaultView;
            dg.Margin = new Thickness(10);

            newTab.Content = dg;

            ResultTabControl.Items.Add(newTab);
        }

        public void SelectResultTab()
        {
            MainTabControl.SelectedItem = ResultTab;
            ResultTabControl.SelectedItem = ResultTabControl.Items[0];
        }

        private string GetCurrentQuery()
        {
            if (!string.IsNullOrWhiteSpace(CommandBox.SelectedText))
                return CommandBox.SelectedText;

            string text = CommandBox.Text;
            int caret = CommandBox.CaretOffset;

            int start = text.LastIndexOf(';', Math.Max(0, caret - 1));
            if (start == -1)
                start = 0;
            else
                start += 1;

            int end = text.IndexOf(';', caret);
            if (end == -1)
                end = text.Length;

            return text.Substring(start, end - start).Trim();
        }

        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            FileManager.Load(this);
        }

        private void SaveAsFile(object sender, RoutedEventArgs e)
        {
            FileManager.SaveAs(this);
        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            FileManager.Save(this);
        }
    }
}