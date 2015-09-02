using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace WebcomicAssistant
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // For an unknown reason WPF ignores the user's region and defaults to en-US,
            // resulting in dates, etc, being displayed incorrectly for most of the world.
            // Manually set the language to the user's current.
            // Taken from http://weblog.west-wind.com/posts/2009/Jun/14/WPF-Bindings-and-CurrentCulture-Formatting.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(
                        CultureInfo.CurrentCulture.IetfLanguageTag
                    )
                )
            );
        }
    }
}
