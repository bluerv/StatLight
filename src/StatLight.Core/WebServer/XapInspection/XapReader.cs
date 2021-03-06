
using System.Collections;
using System.IO.Packaging;
using System.Xml;
using Ionic.Zip;

namespace StatLight.Core.WebServer.XapInspection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Xml.Linq;
    using StatLight.Core.Common;

    public class XapReader
    {
        private readonly ILogger _logger;
        public XapReader(ILogger logger)
        {
            _logger = logger;
        }
        public TestFileCollection LoadXapUnderTest(string archiveFileName)
        {
            var files = new List<ITestFile>();
            string testAssemblyFullName = null;

            var fileStream = FileReader.ReadAllBytes(archiveFileName);

            using (IXapZipArchive archive = XapZipArchiveFactory.Create(fileStream))
            {
                var appManifest = LoadAppManifest(archive);

                if (appManifest != null)
                {
                    string testAssemblyName = GetTestAssemblyNameFromAppManifest(appManifest);

                    AssemblyName assemblyName = GetAssemblyName(archive, testAssemblyName);
                    if (assemblyName != null)
                    {
                        testAssemblyFullName = assemblyName.ToString();
                    }
                }

                files.AddRange(archive.ToList());

                foreach (var item in files)
                    _logger.Debug("XapItems.FilesContainedWithinXap = {0}".FormatWith(item.FileName));
            }


            var xapItems = new TestFileCollection(_logger, testAssemblyFullName, files);


            return xapItems;
        }

        private static AssemblyName GetAssemblyName(IXapZipArchive zip1, string testAssemblyName)
        {
            string tempFileName = Path.GetTempFileName();
            var fileData = zip1.ReadFileIntoBytes(testAssemblyName);
            AssemblyName assemblyName = null;
            if (fileData != null)
            {
                File.WriteAllBytes(tempFileName, fileData);
                assemblyName = AssemblyName.GetAssemblyName(tempFileName);
            }

            File.Delete(tempFileName);

            return assemblyName;
        }

        private static string GetTestAssemblyNameFromAppManifest(string appManifest)
        {
            var root = XElement.Parse(appManifest);

            var entryPointAssemblyNode = root.Attribute("EntryPointAssembly");

            if (entryPointAssemblyNode == null)
                throw new StatLightException("Cannot find the EntryPointAssembly attribute in the AppManifest.xaml");

            return entryPointAssemblyNode.Value + ".dll";
        }

        private static string LoadAppManifest(IXapZipArchive zip1)
        {
            var fileData = zip1.ReadFileIntoBytes("AppManifest.xaml");
            if (fileData != null)
            {
                string xaml = Encoding.UTF8.GetString(fileData);
                if (xaml[0] == '<')
                    return xaml;
                return xaml.Substring(1);
            }

            return null;
        }

        public static string GetRuntimeVersion(string xapPath)
        {
            using (var archive = XapZipArchiveFactory.Read(xapPath))
            {
                var appManifestEntry = archive["AppManifest.xaml"];
                if (appManifestEntry == null)
                    return null;

                var xAppManifest = XElement.Load(appManifestEntry.ToStream());

                var runtimeVersion = xAppManifest.Attribute("RuntimeVersion");
                return runtimeVersion != null ? runtimeVersion.Value : null;
            }
        }
    }

    public static class FileReader
    {
        public static byte[] ReadAllBytes(string path)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch (IOException ex)
                {
                    if (ex.Message.Contains("because it is being used by another process"))
                    {
                        Thread.Sleep(500);
                    }
                    else
                    {
                        throw;
                    }
                }

                // Don't wait on file forever... fail if it's locked for 15 seconds or more.
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(15))
                {
                    throw new StatLightException("Could not seem read the file [{0}] as it appears to be locked by another process.".FormatWith(path));
                }

            }
        }
    }
}

