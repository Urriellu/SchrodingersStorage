using Humanizer.Bytes;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.NG;
using System.Linq;
using System.Text;

namespace SchrodingersStorage
{
    public class SchrodingersDirectory
    {
        public string Name => Primary.Name;
        public IOPriorityClass IOPriority;

        public readonly string PathDirectoryPrimary;
        public readonly string PathDirectorySecondary;

        public string PathParentDirectoryPrimary => Path.GetDirectoryName(PathDirectoryPrimary);
        public string PathParentDirectorySecondary => Path.GetDirectoryName(PathDirectorySecondary);

        public bool Exists => Primary.Exists || Secondary.Exists;

        DirectoryInfo Primary => new DirectoryInfo(PathDirectoryPrimary);
        DirectoryInfo Secondary => new DirectoryInfo(PathDirectorySecondary);

        public SchrodingersFile GetFile(string filename, IOPriorityClass ioPriorityClass = IOPriorityClass.L02_NormalEffort)
        {
            if (Path.IsPathRooted(filename)) throw new ArgumentException($"Filename '{filename}' must be only a file name, without a path.");
            SchrodingersFile sf = new SchrodingersFile(Path.Combine(PathDirectoryPrimary, filename), Path.Combine(PathDirectorySecondary, filename), ioPriorityClass);
            return sf;
        }

        public SchrodingersDirectory GetDirectory(string directoryName, IOPriorityClass ioPriorityClass = IOPriorityClass.L02_NormalEffort)
        {
            if (Path.IsPathRooted(directoryName)) throw new ArgumentException($"Directory name '{directoryName}' must be only a name, without a path.");
            SchrodingersDirectory sf = new SchrodingersDirectory(Path.Combine(PathDirectoryPrimary, directoryName), Path.Combine(PathDirectorySecondary, directoryName), ioPriorityClass);
            return sf;
        }

        public SchrodingersDirectory(SchrodingersDirectory parent, string name, IOPriorityClass ioPriorityClass = IOPriorityClass.L02_NormalEffort) : this(Path.Combine(parent.PathDirectoryPrimary, name), Path.Combine(parent.PathDirectorySecondary, name), ioPriorityClass) { }

        public SchrodingersDirectory(string pathDirPrimary, string pathDirSecondary, IOPriorityClass ioPriorityClass = IOPriorityClass.L02_NormalEffort)
        {
            if (string.IsNullOrEmpty(pathDirPrimary)) throw new ArgumentNullException(nameof(pathDirPrimary));
            if (string.IsNullOrEmpty(pathDirSecondary)) throw new ArgumentNullException(nameof(pathDirSecondary));
            if (pathDirPrimary == pathDirSecondary) throw new ArgumentException($"Primary and secondary paths must be different.");
            if (Path.GetFileName(pathDirPrimary) != Path.GetFileName(pathDirSecondary)) throw new ArgumentException($"Both directory must have the same name, but located in different parent directories.");

            PathDirectoryPrimary = pathDirPrimary;
            PathDirectorySecondary = pathDirSecondary;
            this.IOPriority = ioPriorityClass;
        }

        public virtual IEnumerable<SchrodingersDirectory> Directories => GetDirectories<SchrodingersDirectory>();

        protected IEnumerable<TDirectories> GetDirectories<TDirectories>(Func<string, string, TDirectories> constructor = null) where TDirectories : SchrodingersDirectory
        {
            var result = new List<TDirectories>();
            string[] primarySubDirsNames = new string[0];
            try { primarySubDirsNames = Primary.GetDirectories(iopriority: IOPriority).Select(d => d.Name).ToArray(); }
            catch { }
            string[] secondarySubDirsNames = new string[0];
            try { secondarySubDirsNames = Secondary.GetDirectories(iopriority: IOPriority).Select(d => d.Name).ToArray(); }
            catch { }
            var allSubDirsNames = primarySubDirsNames.Union(secondarySubDirsNames);
            foreach (string subdirname in allSubDirsNames)
            {
                string pathDirPrim = Path.Combine(PathDirectoryPrimary, subdirname);
                string pathDirSec = Path.Combine(PathDirectorySecondary, subdirname);
                TDirectories newobj;
                if (constructor != null) newobj = constructor(pathDirPrim, pathDirSec);
                else newobj = (TDirectories)Activator.CreateInstance(typeof(TDirectories), pathDirPrim, pathDirSec, IOPriority);
                result.Add(newobj);
            }
            return result;
        }

        public SchrodingersFile CreateFile<T>(string filename, T content)
        {
            if (Path.GetInvalidFileNameChars().Any(c => filename.Contains(c))) throw new ArgumentException($"File name '{filename}' is invalid. It must NOT contain a path, and it cannot contain any invalid characters ({Path.GetInvalidFileNameChars()}).");
            SchrodingersFile f = new SchrodingersFile(Path.Combine(PathDirectoryPrimary, filename), Path.Combine(PathDirectorySecondary, filename), IOPriority);
            f.Write(content);
            return f;
        }

        public SchrodingersDirectory CreateDirectory(string dirname)
        {
            if (Path.GetInvalidFileNameChars().Any(c => dirname.Contains(c))) throw new ArgumentException($"Directory name '{dirname}' is invalid. It must NOT contain a path, and it cannot contain any invalid characters ({Path.GetInvalidFileNameChars()}).");
            SchrodingersDirectory d = new SchrodingersDirectory(Path.Combine(PathDirectoryPrimary, dirname), Path.Combine(PathDirectorySecondary, dirname), IOPriority);
            return d;
        }

        public void Delete()
        {
            Secondary.Delete(true, iopriority: IOPriority);
            Primary.Delete(true, iopriority: IOPriority);
        }

        /// <summary>List of <see cref="SchrodingersFile"/> in this <see cref="SchrodingersDirectory"/>.</summary>
        public IEnumerable<SchrodingersFile> Files
        {
            get
            {
                string[] primaryFileNames = new string[0];
                try { primaryFileNames = Primary.GetFiles().Select(d => d.Name).ToArray(); }
                catch { }
                string[] secondaryFileNames = new string[0];
                try { secondaryFileNames = Secondary.GetFiles().Select(d => d.Name).ToArray(); }
                catch { }
                var allFileNames = primaryFileNames.Union(secondaryFileNames);
                foreach (string filename in allFileNames) yield return new SchrodingersFile(Path.Combine(PathDirectoryPrimary, filename), Path.Combine(PathDirectorySecondary, filename), IOPriority);
            }
        }

        /// <summary>List of all <see cref="SchrodingersFile"/> in this <see cref="SchrodingersDirectory"/> and all subdirectories.</summary>
        public IEnumerable<SchrodingersFile> AllFiles => Files.Concat(Directories.SelectMany(d => d.AllFiles));

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

        public bool IsInPrimary
        {
            get
            {
                if (!Primary.Exists) return false;
                if (Files.Any(f => f.IsOnlyInSecondary)) return false;
                if (Directories.Any(d => d.IsOnlyInSecondary)) return false;
                return true;
            }
        }

        public bool IsOnlyInSecondary => !Primary.Exists && Secondary.Exists && !Files.Any(f => f.IsOnlyInSecondary) && !Directories.Any(d => !d.IsOnlyInSecondary);

        public void MoveToPrimary()
        {
            if (!DirectoryNG.Exists(PathDirectoryPrimary, iopriority: IOPriority)) DirectoryNG.CreateDirectory(PathDirectoryPrimary, iopriority: IOPriority);
            foreach (SchrodingersDirectory dir in Directories) dir.MoveToPrimary();
            foreach (SchrodingersFile file in Files) file.MoveToPrimary();
            if (Secondary.Exists(iopriority: IOPriority)) Secondary.Delete(recursive: false, iopriority: IOPriority);
        }

        public void MoveToSecondary()
        {
            if (!DirectoryNG.Exists(PathDirectorySecondary, iopriority: IOPriority)) DirectoryNG.CreateDirectory(PathDirectorySecondary, IOPriority);
            foreach (SchrodingersDirectory dir in Directories) dir.MoveToSecondary();
            foreach (SchrodingersFile file in Files) file.MoveToSecondary();
            if (Primary.Exists(iopriority: IOPriority)) Primary.Delete(recursive: false, iopriority: IOPriority);
        }
    }

    /// <summary>
    /// Represents a <see cref="SchrodingersDirectory"/> whose subdirectories (which would also be <see cref="SchrodingersDirectory"/>) are custom types derived from <see cref="SchrodingersDirectory"/>.
    /// </summary>
    /// <typeparam name="TDirectories">Type of its subdirectories.</typeparam>
    public class SchrodingersDirectory<TDirectories> : SchrodingersDirectory where TDirectories : SchrodingersDirectory
    {
        readonly Func<string, string, TDirectories> Constructor;

        /// <summary>
        /// Created a new <see cref="SchrodingersDirectory"/> whose subdirectories (which would also be <see cref="SchrodingersDirectory"/>) are custom types derived from <see cref="SchrodingersDirectory"/>.
        /// </summary>
        /// <param name="pathDirPrimary">Path to primary directory location.</param>
        /// <param name="pathDirSecondary">Path to secondary directory location.</param>
        /// <param name="constructor">Constructor used to create objects representing each subdirectory. If undefined, the default constructor is used instead.</param>
        public SchrodingersDirectory(string pathDirPrimary, string pathDirSecondary, IOPriorityClass ioPriorityClass, Func<string, string, TDirectories> constructor = null) : base(pathDirPrimary, pathDirSecondary, ioPriorityClass)
        {
            this.Constructor = constructor;
        }

        public virtual new IEnumerable<TDirectories> Directories => GetDirectories<TDirectories>(Constructor);
    }
}
