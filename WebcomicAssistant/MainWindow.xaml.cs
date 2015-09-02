using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace WebcomicAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Comic> LoadComics()
        {
            List<Comic> comics = new List<Comic>();
            DirectoryInfo sites;
            DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while(current != null && Directory.Exists(current.FullName + "/sites") == false)
            {
                current = current.Parent;
            }
            if(current == null)
            {
                sites = Directory.CreateDirectory("sites");
            }
            else
            {
                sites = new DirectoryInfo(current.FullName + "/sites");
            }

            // Search for any backup files from saving, and restore them if we find any.
            foreach(FileInfo file in sites.EnumerateFiles("*.tmp", SearchOption.AllDirectories))
            {
                string target = file.FullName.Substring(0, file.FullName.Length - 4);
                if(!File.Exists(target))
                {
                    File.Move(file.FullName, target);
                }
                else
                {
                    if(File.GetLastWriteTime(target) < File.GetLastWriteTime(file.FullName))
                    {
                        File.Delete(target);
                        File.Move(file.FullName, target);
                    }
                    else
                    {
                        // Current file looks newer than the backup. Rename the backup so we don't try to process it in the future,
                        // keep it around in case, and then go on with our lives.
                        for(int n = 0; n < 4096; n++)
                        {
                            if(!File.Exists(target + n.ToString() + ".bak"))
                            {
                                File.Move(file.FullName, target + n.ToString() + ".bak");
                                break;
                            }
                        }
                    }
                }
            }
            foreach(FileInfo file in sites.EnumerateFiles("*.ini", SearchOption.AllDirectories))
            {
                comics.Add(new Comic(file.FullName));
            }
            return comics;
        }
        public MainWindow()
        {
            InitializeComponent();

            foreach(Comic c in LoadComics())
            {
                lvSites.Items.Add(c);
            }
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
