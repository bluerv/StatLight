using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using NUnit.Framework;
using StatLight.Core.WebServer.XapInspection;

namespace StatLight.Core.Tests.WebServer.XapInspection
{
    [TestFixture]
    public class XapRewriterTests : FixtureBase
    {
        private XapRewriter _xapRewriter;
        private ZipFile _originalXapHost;
        private ZipFile _originalXapUnderTest;
        private ZipFile _expectedXapHost;

        protected override void Before_all_tests()
        {
            base.Before_all_tests();

            _xapRewriter = new XapRewriter(base.TestLogger);


            var appManifest = @"<Deployment xmlns=""http://schemas.microsoft.com/client/2007/deployment"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" EntryPointAssembly=""StatLight.Client.Harness"" EntryPointType=""StatLight.Client.Harness.App"" RuntimeVersion=""4.0.50826.0"">
  <Deployment.Parts>
    <AssemblyPart x:Name=""StatLight.Client.Harness"" Source=""StatLight.Client.Harness.dll"" />
    <AssemblyPart x:Name=""System.Windows.Controls"" Source=""System.Windows.Controls.dll"" />
    <AssemblyPart x:Name=""System.Xml.Linq"" Source=""System.Xml.Linq.dll"" />
    <AssemblyPart x:Name=""System.Xml.Serialization"" Source=""System.Xml.Serialization.dll"" />
    <AssemblyPart x:Name=""Microsoft.Silverlight.Testing"" Source=""Microsoft.Silverlight.Testing.dll"" />
    <AssemblyPart x:Name=""Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight"" Source=""Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll"" />
  </Deployment.Parts>
</Deployment>
";

            _originalXapHost = new ZipFile();
            _originalXapHost.AddEntry("AppManifest.xaml", "/", appManifest);
            _originalXapHost.AddEntry("StatLight.Client.Harness.dll", "/", new byte[] { 1, 2 });
            _originalXapHost.AddEntry("StatLight.Client.Harness.MSTest.dll", "/", new byte[] { 1, 2 });
            _originalXapHost.AddEntry("System.Windows.Controls.dll", "/", new byte[] { 1, 2 });
            _originalXapHost.AddEntry("System.Xml.Linq.dll", "/", new byte[] { 1, 2 });
            _originalXapHost.AddEntry("System.Xml.Serialization.dll", "/", new byte[] { 1, 2 });
            _originalXapHost.AddEntry("Microsoft.Silverlight.Testing.dll", "/", new byte[] { 1, 2 });
            _originalXapHost.AddEntry("Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll", "/", new byte[] { 1, 2 });
            string originalXapHostFileName = Path.GetTempFileName();
            _originalXapHost.Save(originalXapHostFileName);


            var appManifest2 = @"<Deployment xmlns=""http://schemas.microsoft.com/client/2007/deployment"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" EntryPointAssembly=""StatLight.IntegrationTests.Silverlight.MSTest"" EntryPointType=""StatLight.IntegrationTests.Silverlight.App"" RuntimeVersion=""4.0.50826.0"">
  <Deployment.Parts>
    <AssemblyPart x:Name=""StatLight.IntegrationTests.Silverlight.MSTest"" Source=""StatLight.IntegrationTests.Silverlight.MSTest.dll"" />
    <AssemblyPart x:Name=""Microsoft.Silverlight.Testing"" Source=""Microsoft.Silverlight.Testing.dll"" />
    <AssemblyPart x:Name=""Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight"" Source=""Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll"" />
  </Deployment.Parts>
</Deployment>
";

            _originalXapUnderTest = new ZipFile();
            _originalXapUnderTest.AddEntry("AppManifest.xaml", "/", appManifest2);
            _originalXapUnderTest.AddEntry("StatLight.IntegrationTests.Silverlight.MSTest.dll", "/", new byte[] { 1, 2 });
            _originalXapUnderTest.AddEntry("Microsoft.Silverlight.Testing.dll", "/", new byte[] { 1, 2 });
            _originalXapUnderTest.AddEntry("Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll", "/", new byte[] { 1, 2 });
            _originalXapUnderTest.AddEntry("Test/Test/Test.xml", "/", "Hello");
            string originalXapUnderTestFileName = Path.GetTempFileName();
            _originalXapUnderTest.Save(originalXapUnderTestFileName);

            var expectedAppManifest = @"<Deployment xmlns=""http://schemas.microsoft.com/client/2007/deployment"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" EntryPointAssembly=""StatLight.Client.Harness"" EntryPointType=""StatLight.Client.Harness.App"" RuntimeVersion=""4.0.50826.0"">
  <Deployment.Parts>
    <AssemblyPart x:Name=""StatLight.Client.Harness"" Source=""StatLight.Client.Harness.dll"" />
    <AssemblyPart x:Name=""System.Windows.Controls"" Source=""System.Windows.Controls.dll"" />
    <AssemblyPart x:Name=""System.Xml.Linq"" Source=""System.Xml.Linq.dll"" />
    <AssemblyPart x:Name=""System.Xml.Serialization"" Source=""System.Xml.Serialization.dll"" />
    <AssemblyPart x:Name=""Microsoft.Silverlight.Testing"" Source=""Microsoft.Silverlight.Testing.dll"" />
    <AssemblyPart x:Name=""Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight"" Source=""Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll"" />
    <AssemblyPart x:Name=""StatLight.IntegrationTests.Silverlight.MSTest"" Source=""StatLight.IntegrationTests.Silverlight.MSTest.dll"" />
  </Deployment.Parts>
</Deployment>
";
            _expectedXapHost = new ZipFile();
            _expectedXapHost.AddEntry("StatLight.Client.Harness.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("StatLight.Client.Harness.MSTest.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("System.Windows.Controls.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("System.Xml.Linq.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("System.Xml.Serialization.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("Microsoft.Silverlight.Testing.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("Microsoft.VisualStudio.QualityTools.UnitTesting.Silverlight.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("StatLight.IntegrationTests.Silverlight.MSTest.dll", "/", new byte[] { 1, 2 });
            _expectedXapHost.AddEntry("Test/Test/Test.xml", "/", "Hello");
            _expectedXapHost.AddEntry("AppManifest.xaml", "/", expectedAppManifest);
            string expectedXapHostFileName = Path.GetTempFileName();
            _expectedXapHost.Save(expectedXapHostFileName);

        }

        [Test]
        public void Should_serialize_zip_bytes_correctly()
        {
            _originalXapHost.ToByteArray().ShouldEqual(_originalXapHost.ToByteArray());
        }

        [Test]
        public void Should_rewrite_correctly()
        {
            var newFiles = new List<ITestFile>
                               {
                                   new TempTestFile("StatLight.IntegrationTests.Silverlight.MSTest.dll"),
                                   new TempTestFile("Test/Test/Test.xml"),
                               };

            ZipFile actualXapHost = _xapRewriter.RewriteZipHostWithFiles(_originalXapHost.ToByteArray(), newFiles);
            string actualXapHostFileName = Path.GetTempFileName();
            actualXapHost.Save(actualXapHostFileName);
            AssertZipsEqual(_expectedXapHost, actualXapHost);

        }

        private static void AssertZipsEqual(ZipFile expected, ZipFile actual)
        {
            actual.Count.ShouldEqual(expected.Count, "zip files contain different counts");

            for (int i = 0; i < expected.Count; i++)
            {
                var expectedFile = expected[i];
                var actualFile = actual[i];

                actualFile.FileName.ShouldEqual(expectedFile.FileName);
                //actualFile.ToByteArray().ShouldEqual(expectedFile.ToByteArray(), "File [{0}] bytes not same.".FormatWith(actualFile.FileName));
            }

        }

        private class TempTestFile : ITestFile
        {
            private readonly string _fileName;

            public TempTestFile(string fileName)
            {
                _fileName = fileName;
            }

            public string FileName
            {
                get { return _fileName; }
            }

            public byte[] File
            {
                get { return new byte[] { 1, 2, 3 }; }
            }
        }
    }
}