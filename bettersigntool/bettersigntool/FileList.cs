using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bettersigntool
{
    public static class FileList
    {
        public static bool IsFileList(string filename)
        {
            return filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
        }

        public static List<string> Load(string filename)
        {
            string fileLocation = Directory.GetParent(filename).FullName;

            string[] files = File.ReadAllLines(filename);

            // Skip empties and attach the full directory path to them
            //
            return files
                .Where(f => !String.IsNullOrEmpty(f))
                .Select(f => Path.Combine(fileLocation, f))
                .ToList();
        } 
    }
}
