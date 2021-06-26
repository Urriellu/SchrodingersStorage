using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace SchrodingersStorage.Test
{
    [TestClass]
    public class SchrodingersStorageTests
    {
        [TestMethod]
        public void T01()
        {
            string filenamecommon = $"{nameof(SchrodingersFile)}-{nameof(T01)}-{DateTime.Now:yyyyMMdd-HHmmssfff}";
            string pathFilePrimary = Path.Combine(Path.GetTempPath(), $"{filenamecommon}-1.txt");
            string pathFileSecondary = Path.Combine(Path.GetTempPath(), $"{filenamecommon}-2.txt");

            SchrodingersFile f = new SchrodingersFile(pathFilePrimary, pathFileSecondary);
            Assert.IsFalse(f.Exists);

            const string content1 = "Sample file content.";

            f.Write(content1);
            Assert.IsTrue(f.Exists);
            Assert.IsTrue(File.Exists(f.PathFilePrimary));
            Assert.IsFalse(File.Exists(f.PathFileSecondary));

            string readback = f.Read();
            Assert.AreEqual(content1, readback);

            f.MoveToSecondary();
            Assert.IsTrue(f.Exists);
            Assert.IsFalse(File.Exists(f.PathFilePrimary));
            Assert.IsTrue(File.Exists(f.PathFileSecondary));
        }
    }
}
