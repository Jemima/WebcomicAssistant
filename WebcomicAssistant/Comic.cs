using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebcomicAssistant
{
    public class Comic
    {
        public string Name
        {
            get;
        }

        public DateTime LastVisited
        {
            get;
        }

        public Uri Upto
        {
            get;
        }

        string Error;
        Regex Pattern;
        string SourcePath;

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
                    Upto = new Uri(stream.ReadLine());
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
            File.Replace(SourcePath + ".tmp", SourcePath, null);
        }
    }
}
