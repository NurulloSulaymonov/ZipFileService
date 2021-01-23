using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipFileService
{
    public class LogRangeRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var test = SendLogFile(new LogRangeRequest()
            {
                StartDate = new DateTime(2021, 01, 20),
                EndDate = new DateTime(2021, 01, 22)
            }, 1);
        }

        /// <summary>
        /// Get Logs Files and send it to server 
        /// </summary>
        /// <param name="range"></param>
        /// <param name="reqestId"></param>
        /// <returns></returns>
        public static async Task SendLogFile(LogRangeRequest range, int reqestId)
        {
            var fileList = new List<string>();
            byte[] zip = null;
            //get Core Archives 
            var coreArchiveLogs = GetMatchedFilesFromDirectory(@"\logs\core\archives\", range.StartDate, range.EndDate);
            fileList.AddRange(coreArchiveLogs);
            //get Core files
            var coreLogs = GetMatchedFilesFromDirectory(@"\logs\core\", range.StartDate, range.EndDate);
            fileList.AddRange(coreLogs);
            // get payments from archive 
            var paymentArchiveLogs = GetMatchedFilesFromDirectory(@"\logs\payments\archives\", range.StartDate, range.EndDate);
            fileList.AddRange(paymentArchiveLogs);
            //get payments
            var payments = GetMatchedFilesFromDirectory(@"\logs\payments\", range.StartDate, range.EndDate);
            fileList.AddRange(payments);
            try
            {
                if (fileList.Count > 0)
                {
                    //convert files to zip with a help of DotnetZip
                    zip = ConvertZipFile(fileList);
                    if (zip != null)
                    {
                        using (var fw = File.OpenWrite(Directory.GetCurrentDirectory() + "/test.zip"))
                        {
                            var memZip = new MemoryStream(zip);
                            memZip.CopyTo(fw);
                        }
                        return;
                    };
                }
            }
            catch (Exception ex)
            {

                throw;
            }


            var info = zip == null ? "NO logs found" : "";

        }


        /// <summary>
        /// Generic Method to get files from directory
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetMatchedFilesFromDirectory(string directory, DateTime startDate, DateTime endDate)
        {
            try
            {
                string currentDir = Directory.GetCurrentDirectory();
                var archivePath = currentDir + directory;

                IEnumerable<string> files = Directory.GetFiles(archivePath);
                var selectedFiles = new List<string>();
                foreach (var file in files)
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    var regex = new Regex(@"(\d{1,4}([.\-/])\d{1,2}([.\-/])\d{1,4})");
                    if (regex.IsMatch(filename))
                    {
                        var reg = regex.Match(filename).Value;
                        DateTime result;
                        if (DateTime.TryParse(reg, out result))
                        {
                            if (result >= startDate && result <= endDate)
                            {
                                selectedFiles.Add(file);
                            }
                        }
                    }
                }
                return selectedFiles;
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<string>();

            }
        }


        /// <summary>
        /// Convert files to zip
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static byte[] ConvertZipFile(IEnumerable<string> files)
        {
            try
            {
                using (var mStream = new MemoryStream())
                {
                    using (var zipFile = new ZipArchive(mStream, ZipArchiveMode.Create, false))
                    {
                        foreach (string file in files)
                        {
                            var ext = Path.GetExtension(file);
                            if (ext == ".log")
                            {
                                var stream = File.ReadAllBytes(file);
                                var entry = zipFile.CreateEntry(Path.GetFileName(file), CompressionLevel.NoCompression).Open();
                                entry.Write(stream, 0, stream.Length);
                                entry.Close();
                            }
                            else
                            {
                                string fileName;
                                var fileContent = GetZipContent(file, out fileName);
                                if (fileContent == null)
                                {
                                    continue;
                                }
                                var entry = zipFile.CreateEntry(fileName, CompressionLevel.Fastest);
                                var fileStream = entry.Open();
                                fileStream.Write(fileContent, 0, fileContent.Length);
                                fileStream.Close();
                            }

                        }
                    }
                    return mStream.ToArray();
                }
            }
            catch (Exception ex)
            {

                return null;
            }

        }

        /// <summary>
        /// Get single file from given zip and return it 
        /// </summary>
        /// <param name="str"></param>
        /// <returns>byte[]</returns>
        private static byte[] GetZipContent(string str, out string name)
        {
            //var stream = new MemoryStream();
            //var zip = System.IO.Compression.ZipFile.OpenRead(str);
            //var file = zip.Entries.First();
            //name = file.Name;

            //return stream.ToArray();

            using (ZipArchive zip = ZipFile.Open(str, ZipArchiveMode.Read))
            {
                var stream = new MemoryStream();
                var file = zip.Entries.First();
                name = file.Name;
                file.Open().CopyTo(stream);
                return stream.ToArray();
            }
        }

    }
}
