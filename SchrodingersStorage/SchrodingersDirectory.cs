using Humanizer.Bytes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SchrodingersStorage
{
    public class SchrodingersDirectory
    {
        public string Name => Primary.Name;

        public readonly string PathDirectoryPrimary;
        public readonly string PathDirectorySecondary;

        public string PathParentDirectoryPrimary => Path.GetDirectoryName(PathDirectoryPrimary);
        public string PathParentDirectorySecondary => Path.GetDirectoryName(PathDirectorySecondary);

        public bool Exists => Primary.Exists || Secondary.Exists;

        DirectoryInfo Primary => new DirectoryInfo(PathDirectoryPrimary);
        DirectoryInfo Secondary => new DirectoryInfo(PathDirectorySecondary);

        public SchrodingersFile GetFile(string filename) => new SchrodingersFile(Path.Combine(PathDirectoryPrimary, filename), Path.Combine(PathDirectorySecondary, filename));

        public SchrodingersDirectory(SchrodingersDirectory parent, string name) : this(Path.Combine(parent.PathDirectoryPrimary, name), Path.Combine(parent.PathDirectorySecondary, name)) { }

        public SchrodingersDirectory(string pathDirPrimary, string pathDirSecondary)
        {
            if (string.IsNullOrEmpty(pathDirPrimary)) throw new ArgumentNullException(nameof(pathDirPrimary));
            if (string.IsNullOrEmpty(pathDirSecondary)) throw new ArgumentNullException(nameof(pathDirSecondary));
            if (pathDirPrimary == pathDirSecondary) throw new ArgumentException($"Primary and secondary paths must be different.");
            if (Path.GetFileName(pathDirPrimary) != Path.GetFileName(pathDirSecondary)) throw new ArgumentException($"Both directory must have the same name, but located in different parent directories.");

            PathDirectoryPrimary = pathDirPrimary;
            PathDirectorySecondary = pathDirSecondary;
        }

        public IEnumerable<SchrodingersDirectory> Directories
        {
            get
            {
                var primarySubDirsNames = Primary.GetDirectories().Select(d => d.Name);
                var secondarySubDirsNames = Secondary.GetDirectories().Select(d => d.Name);
                var allSubDirsNames = primarySubDirsNames.Union(secondarySubDirsNames);
                List<SchrodingersDirectory> sdirs = new List<SchrodingersDirectory>();
                foreach (string subdirname in allSubDirsNames) sdirs.Add(new SchrodingersDirectory(Path.Combine(PathDirectoryPrimary, subdirname), Path.Combine(PathDirectorySecondary, subdirname)));
                return sdirs;
            }
        }

        public SchrodingersFile CreateFile<T>(string filename, T content)
        {
            if (Path.GetInvalidFileNameChars().Any(c => filename.Contains(c))) throw new ArgumentException($"File name '{filename}' is invalid. It must NOT contain a path, and it cannot contain any invalid characters ({Path.GetInvalidFileNameChars()}).");
            SchrodingersFile f = new SchrodingersFile(Path.Combine(PathDirectoryPrimary, filename), Path.Combine(PathDirectorySecondary, filename));
            f.Write(content);
            return f;
        }

        public SchrodingersDirectory CreateDirectory(string dirname)
        {
            if (Path.GetInvalidFileNameChars().Any(c => dirname.Contains(c))) throw new ArgumentException($"Directory name '{dirname}' is invalid. It must NOT contain a path, and it cannot contain any invalid characters ({Path.GetInvalidFileNameChars()}).");
            SchrodingersDirectory d = new SchrodingersDirectory(Path.Combine(PathDirectoryPrimary, dirname), Path.Combine(PathDirectorySecondary, dirname));
            return d;
        }

        public void Delete()
        {
            Secondary.Delete(true);
            Primary.Delete(true);
        }

        public IEnumerable<SchrodingersFile> Files
        {
            get
            {
                string[] primaryFileNames = new string[0];
                try { primaryFileNames = Primary.GetFiles().Select(d => d.Name).ToArray(); } catch { }
                string[] secondaryFileNames = new string[0];
                try { secondaryFileNames = Secondary.GetFiles().Select(d => d.Name).ToArray(); } catch { }
                var allFileNames = primaryFileNames.Union(secondaryFileNames);
                List<SchrodingersFile> files = new List<SchrodingersFile>();
                foreach (string filename in allFileNames) files.Add(new SchrodingersFile(Path.Combine(PathDirectoryPrimary, filename), Path.Combine(PathDirectorySecondary, filename)));
                return files;
            }
        }

        public ByteSize Size
        {
            get
            {
                ByteSize s = ByteSize.FromBytes(0);
                foreach (SchrodingersDirectory dir in Directories) s += dir.Size;
                foreach (SchrodingersFile file in Files) s += file.Size;
                return s;
            }
        }

        public bool IsInPrimary => Files.All(f => f.IsInPrimary) && Directories.All(d => d.IsInPrimary);

        public bool IsOnlyInSecondary => Files.All(f => f.IsOnlyInSecondary) && Directories.All(d => d.IsOnlyInSecondary);

        public void MoveToPrimary()
        {
            if (!Directory.Exists(PathDirectoryPrimary)) Directory.CreateDirectory(PathDirectoryPrimary);
            foreach (SchrodingersDirectory dir in Directories) dir.MoveToPrimary();
            foreach (SchrodingersFile file in Files) file.MoveToPrimary();
            Directory.Delete(PathDirectorySecondary, recursive: false);
        }

        public void MoveToSecondary()
        {
            if (!Directory.Exists(PathDirectorySecondary)) Directory.CreateDirectory(PathDirectorySecondary);
            foreach (SchrodingersDirectory dir in Directories) dir.MoveToSecondary();
            foreach (SchrodingersFile file in Files) file.MoveToSecondary();
            Directory.Delete(PathDirectoryPrimary, recursive: false);
        }
    }
}
