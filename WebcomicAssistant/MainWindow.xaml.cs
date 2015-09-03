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
using CefSharp;
using System.Collections.ObjectModel;

namespace WebcomicAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Comic Current = null;
        ObservableCollection<Comic> Comics;

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
            cefBrowser.FrameLoadEnd += OnPageLoad;
            Comics = new ObservableCollection<Comic>(LoadComics());
            lvSites.ItemsSource = Comics;
        }

        private void OnPageLoad(object sender, FrameLoadEndEventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(OnPageLoadUIThread));
        }

        /// <summary>
        ///  The actual OnPageLoad handler, we want to run in the context of the UI thread so we can update UI elements.
        /// </summary>
        private void OnPageLoadUIThread()
        {
            txtUrl.Text = cefBrowser.Address;
            if (Current != null && Current.MatchAgainstUrl(cefBrowser.Address))
            {
                Current.Update(cefBrowser.Address);
            }
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void NavGo(object sender, RoutedEventArgs e)
        {
            cefBrowser.Load(txtUrl.Text);
        }

        private void NavForwards(object sender, RoutedEventArgs e)
        {
            cefBrowser.Forward();
        }

        private void NavBack(object sender, RoutedEventArgs e)
        {
            cefBrowser.Back();
        }

        private void NavKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                NavGo(sender, null);
            }
        }

        private void LoadComic(object sender, RoutedEventArgs e)
        {
            if(Current != null)
            {
                Current.SaveChanges();
            }
            Current = this.lvSites.SelectedItem as Comic;
            cefBrowser.Load(Current.Upto.ToString());
        }
    }
}
