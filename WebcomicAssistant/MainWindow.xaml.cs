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
using System.Diagnostics;

namespace WebcomicAssistant
{
    public class SiteFilter : IRequestHandler
    {
        private MainWindow window;

        public SiteFilter(MainWindow window)
        {
            this.window = window;
        }

        public bool GetAuthCredentials(IWebBrowser browser, bool isProxy, string host, int port, string realm, string scheme, ref string username, ref string password)
        {
            return false;
        }

        public bool OnBeforeBrowse(IWebBrowser browser, IRequest request, bool isRedirect, bool isMainFrame)
        {
            if (window.Current == null ||
                isMainFrame == false   ||
                window.Current.MatchAgainstUrl(request.Url))
            {
                // subframe, no current comic or page matches the current comic. Continue to load the page.
                return false;
            }
            else if (window.DuringUserNavigation)
            {
                // The user has manually navigated somewhere (typing in URL bar, back button, etc).
                // Search to see if it's a different comic and set it active if so.
                foreach(Comic c in window.Comics)
                {
                    if (c.MatchAgainstUrl(request.Url))
                    {
                        // Active comic switched, continue as normal.
                        window.Current.SaveChanges();
                        window.Current = c;
                        window.DuringUserNavigation = false;
                        return false;
                    }
                }
                // Not a comic, but the user chose to navigate to it, so we unset the current comic
                // and go back to being a basic web browser.
                window.Current.SaveChanges();
                window.Current = null;
                window.DuringUserNavigation = false;
                return false;
            }
            else
            {
                // Navigating outside our webcomic, open in the user's default browser
                System.Diagnostics.Process.Start(request.Url);
                return true;
            }
        }

        public bool OnBeforePluginLoad(IWebBrowser browser, string url, string policyUrl, WebPluginInfo info)
        {
            return false;
        }

        private bool UriCompare(Uri a, Uri b)
        {
            string sa = a.Host;
            string sb = b.Host;
            if(sa.Contains(sb) || sb.Contains(sa))
            {
                // One host is a subset of the other e.g. files.explosm.net and explosm.net
                return true;
            }
            // TODO: Handle cases like www.comic.com vs images.comic.com
            // when I run into a webcomic which is affected
            return false;
        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser browser, IRequest request, bool isMainFrame)
        {
            if(UriCompare(new Uri(request.Url), new Uri(window.Current.Upto)))
            {
                return CefReturnValue.Continue;
            }
            else
            {
                return CefReturnValue.Cancel;
            }
        }

        public bool OnCertificateError(IWebBrowser browser, CefErrorCode errorCode, string requestUrl)
        {
            return false;
        }

        public void OnPluginCrashed(IWebBrowser browser, string pluginPath)
        {
        }

        public void OnRenderProcessTerminated(IWebBrowser browser, CefTerminationStatus status)
        {
        }

        public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
        {
            return false;
        }

        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
        {
        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            return CefReturnValue.Continue;
        }

        public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            return false;
        }

        public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
        {
        }

        public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
        {
            return false;
        }

        public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, ref string newUrl)
        {
        }

        public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
        {
            return false;
        }

        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {
        }

        public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return false;
        }

        public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return null;
        }

        public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
        }

        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            return false;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Comic Current;
        public bool DuringUserNavigation;
        public ObservableCollection<Comic> Comics;

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
            cefBrowser.RequestHandler = new SiteFilter(this);
            Comics = new ObservableCollection<Comic>(LoadComics());
            lvSites.ItemsSource = Comics;
            Comic last = Comics[0];
            foreach(Comic comic in Comics)
            {
                if(comic.LastVisited > last.LastVisited)
                {
                    last = comic;
                }
            }
            Current = last;
            cefBrowser.Load(Current.Upto);
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

            // If we haven't opened the first comic yet the user can do whatever.
            if (Current != null &&
                Current.MatchAgainstUrl(cefBrowser.Address))
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
            DuringUserNavigation = true;
            cefBrowser.Load(txtUrl.Text);
        }

        private void NavForwards(object sender, RoutedEventArgs e)
        {
            DuringUserNavigation = true;
            cefBrowser.Forward();
        }

        private void NavBack(object sender, RoutedEventArgs e)
        {
            DuringUserNavigation = true;
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
            cefBrowser.Load(Current.Upto);
        }

        private void OpenSitesFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(Directory.GetParent(Current.SourcePath).FullName);
        }
    }
}
