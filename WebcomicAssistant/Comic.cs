using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebcomicAssistant
{
    public class Comic /*: INotifyPropertyChanged*/
    {
        public string Name
        {
            get;
        }

        public DateTime LastVisited
        {
            get;
        }

        public string Upto
        {
            get; private set;
        }

        string Error;
        Regex Pattern;
        public string SourcePath
        {
            get; private set;
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Instantiates a site object from a file describing the comic.
        /// </summary>
        /// <remarks>
        /// File format:
        /// Line 1: Name - Friendly name of the website,
        /// Line 2: Pattern - A Regex which websites containing this comic match,
        /// Line 3: Upto - The URL of the current comic.
        /// </remarks>
        /// <param name="file">Path to the site file</param>
        public Comic(string file)
        {
            SourcePath = file;
            Name = null;
            try {
                using (StreamReader stream = new StreamReader(file))
                {
                    Name = stream.ReadLine();
                    Pattern = new Regex(stream.ReadLine(), RegexOptions.Compiled);
                    Upto = stream.ReadLine();
                    LastVisited = File.GetLastWriteTime(file);
                }
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }
        }

        public void SaveChanges()
        {
            using(StreamWriter stream = new StreamWriter(SourcePath + ".tmp"))
            {
                stream.WriteLine(Name);
                stream.WriteLine(Pattern.ToString());
                stream.WriteLine(Upto.ToString());
            }
            try {
                File.Replace(SourcePath + ".tmp", SourcePath, null);
            } catch (IOException)
            {
                // Keep the tmp file around, we'll recover it later.
            }
        }

        /// <summary>
        /// Tests the URL to see if it's this comic or not.
        /// </summary>
        /// <returns>true if the url looks like it's for this webcomic, false otherwise</returns>
        public bool MatchAgainstUrl(string Url)
        {
            return Pattern.IsMatch(Url);
        }

        public void Update(string Url)
        {
            Upto = Url;
            SaveChanges();
        }
    }
}
