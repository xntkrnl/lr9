using Microsoft.Win32;
using System.IO;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace lr9.Code
{
    public class FileManager
    {
        static private string currentFilePath = "";

        public static void Load(MainWindow mainWindow)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SQL файлы (*.sql)|*.sql|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                currentFilePath = openFileDialog.FileName;
                mainWindow.CommandBox.Text = File.ReadAllText(currentFilePath);
                mainWindow.CurrentFileText.Text = Path.GetFileName(currentFilePath);
            }
        }
        public static void SaveAs(MainWindow mainWindow)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SQL файлы (*.sql)|*.sql|Все файлы (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                currentFilePath = saveFileDialog.FileName;
                File.WriteAllText(currentFilePath, mainWindow.CommandBox.Text);
                mainWindow.CurrentFileText.Text = Path.GetFileName(currentFilePath);
            }
        }

        public static void Save(MainWindow mainWindow)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAs(mainWindow);
                return;
            }

            File.WriteAllText(currentFilePath, mainWindow.CommandBox.Text);
        }
    }
}
