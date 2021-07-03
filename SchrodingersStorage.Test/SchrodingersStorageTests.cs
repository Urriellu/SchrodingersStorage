using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace SchrodingersStorage.Test
{
    [TestClass]
    public class SchrodingersStorageTests
    {
        [TestMethod]
        public void T01_Files()
        {
            string filenamecommon = $"{nameof(SchrodingersFile)}-{nameof(T01_Files)}-{DateTime.Now:yyyyMMdd-HHmmssfff}";
            string pathFilePrimary = Path.Combine(Path.GetTempPath(), $"{filenamecommon}-1.txt");
            string pathFileSecondary = Path.Combine(Path.GetTempPath(), $"{filenamecommon}-2.txt");

            SchrodingersFile f = new SchrodingersFile(pathFilePrimary, pathFileSecondary);
            Assert.IsFalse(f.Exists);

            const string content1 = "Sample file content.";

            f.Write(content1);
            Assert.IsTrue(f.Exists);
            Assert.IsTrue(File.Exists(f.PathFilePrimary));
            Assert.IsFalse(File.Exists(f.PathFileSecondary));
            Assert.IsTrue(f.Size.Bytes == content1.Length);

            string readback = f.Read<string>();
            Assert.AreEqual(content1, readback);

            Assert.IsTrue(f.IsInPrimary);
            Assert.IsFalse(f.IsOnlyInSecondary);
            Assert.IsTrue(f.Exists);
            f.MoveToSecondary();
            Assert.IsFalse(f.IsInPrimary);
            Assert.IsTrue(f.IsOnlyInSecondary);
            Assert.IsTrue(f.Exists);
            Assert.IsFalse(File.Exists(f.PathFilePrimary));
            Assert.IsTrue(File.Exists(f.PathFileSecondary));
        }


        [TestMethod]
        public void T02_Directories()
        {
            string instanceid = $"{nameof(SchrodingersFile)}-{nameof(T02_Directories)}-{DateTime.Now:yyyyMMdd-HHmmssfff}";
            string pathDirPrimary = Path.Combine(Path.GetTempPath(), $"{instanceid}-primary", "d1");
            string pathDirSecondary = Path.Combine(Path.GetTempPath(), $"{instanceid}-secondary", "d1");

            SchrodingersDirectory d = new SchrodingersDirectory(pathDirPrimary, pathDirSecondary);

            const string content1 = "Sample file content.";
            SchrodingersFile f1 = d.CreateFile("f1.txt", content1);

            Assert.IsTrue(f1.Exists);
            Assert.IsTrue(File.Exists(Path.Combine(pathDirPrimary, f1.FileName)));
            Assert.IsFalse(File.Exists(Path.Combine(pathDirSecondary, f1.FileName)));

            var filesInSDir = d.Files;
            Assert.AreEqual(1, filesInSDir.Count());
            Assert.IsTrue(filesInSDir.First().FileName == f1.FileName);

            Assert.IsTrue(f1.IsInPrimary);
            Assert.IsFalse(f1.IsOnlyInSecondary);
            d.MoveToSecondary();
            Assert.IsFalse(f1.IsInPrimary);
            Assert.IsTrue(f1.IsOnlyInSecondary);
        }
    }
}
