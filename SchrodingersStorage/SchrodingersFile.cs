using Humanizer.Bytes;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.NG;
using System.Linq;

namespace SchrodingersStorage
{
    public class SchrodingersFile
    {
        public string FileName => Primary.Name;
        public IOPriorityClass IOPriority;
        public readonly string PathFilePrimary;
        public readonly string PathFileSecondary;
        public string PathParentDirectoryPrimary => Path.GetDirectoryName(PathFilePrimary);
        public string PathParentDirectorySecondary => Path.GetDirectoryName(PathFileSecondary);

        public bool Exists => Primary.Exists || Secondary.Exists;

        public ByteSize Size
        {
            get
            {
                if (Primary.Exists)
                {
                    try { return ByteSize.FromBytes(Primary.Length); }
                    catch { }
                }
                try { return ByteSize.FromBytes(Secondary.Length); }
                catch { return ByteSize.FromBytes(0); }
            }
        }

        public bool IsInPrimary => Primary.Exists(iopriority: IOPriority);

        public bool IsOnlyInSecondary => Secondary.Exists(iopriority: IOPriority) && !Primary.Exists(iopriority: IOPriority);

        public string CurrentPath
        {
            get
            {
                if (Primary.Exists) return Primary.FullName;
                if (Secondary.Exists) return Secondary.FullName;
                throw new FileNotFoundException(FileName);
            }
        }

        FileInfo Primary => new FileInfo(PathFilePrimary);
        FileInfo Secondary => new FileInfo(PathFileSecondary);

        public SchrodingersFile(string pathFilePrimary, string pathFileSecondary, IOPriorityClass ioPriorityClass = IOPriorityClass.L02_NormalEffort)
        {
            if (string.IsNullOrEmpty(pathFilePrimary)) throw new ArgumentNullException(nameof(pathFilePrimary));
            if (string.IsNullOrEmpty(pathFileSecondary)) throw new ArgumentNullException(nameof(pathFileSecondary));
            if (Path.GetDirectoryName(pathFilePrimary) == Path.GetDirectoryName(pathFileSecondary)) throw new ArgumentException($"Primary and secondary paths must be different.");
            if (Path.GetFileName(pathFilePrimary) != Path.GetFileName(pathFileSecondary)) throw new ArgumentException($"Both paths must have the same file name, but in different directories.");

            PathFilePrimary = pathFilePrimary;
            PathFileSecondary = pathFileSecondary;
            this.IOPriority = ioPriorityClass;
        }

        public void MoveToPrimary()
        {
            if (Secondary.Exists) FileNG.Move(PathFileSecondary, PathFilePrimary, overwrite: true, iopriority: IOPriority);
        }

        public void MoveToSecondary()
        {
            if (Primary.Exists) FileNG.Move(PathFilePrimary, PathFileSecondary, overwrite: true, iopriority: IOPriority);
        }

        Type[] typesNotFormattedAsJson = new Type[]
        {
            typeof(Boolean),
            typeof(SByte),
            typeof(Byte),
            typeof(Char),
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(String)
        };

        static T ChangeType<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(T));
        }


        public void Write<T>(T content)
        {
            string txt;
            if (typesNotFormattedAsJson.Contains(typeof(T))) txt = content.ToString();
            else txt = JsonConvert.SerializeObject(content);
            WriteAsString(txt);
        }

        internal void WriteAsString(string content)
        {
            if (Secondary.Exists)
            {
                // if it has been moved to secondary, update its contents in secondary location
                FileNG.WriteAllText(PathFileSecondary, content, iopriority: IOPriority);
                FileNG.Delete(PathFilePrimary, iopriority: IOPriority);
            }
            else
            {
                FileNG.WriteAllText(PathFilePrimary, content, iopriority: IOPriority); // if it hasn't been moved to secondary, or it doesn't exist anywhere, write to primary
                if (Secondary.Exists) FileNG.Move(PathFilePrimary, PathFileSecondary); // if another thread or process has created the file in secondary while we were writing to the primary, move from primary to secondary
            }
        }

        internal string ReadAsString()
        {
            string content;
            try { content = FileNG.ReadAllText(PathFilePrimary, iopriority: IOPriority); }
            catch
            {
                try { content = FileNG.ReadAllText(PathFileSecondary, iopriority: IOPriority); }
                catch { throw new Exception($"Unable to read from either primary nor secondary location."); }
            }
            return content;
        }

        public T Read<T>()
        {
            if (typesNotFormattedAsJson.Contains(typeof(T)))
            {
                string str = ReadAsString();
                if (typeof(T) == typeof(string)) return (T)(object)str;
                else return (T)(object)ChangeType<T>(str);
            }
            string json = ReadAsString();
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }

        public void Delete()
        {
            bool deletedFromPrimary = true;
            bool deletedFromSecondary = true;
            try { FileNG.Delete(PathFilePrimary, IOPriority); }
            catch { deletedFromPrimary = false; }
            try { FileNG.Delete(PathFileSecondary, IOPriority); }
            catch { deletedFromSecondary = false; }
            if (!deletedFromPrimary && !deletedFromSecondary) throw new FileNotFoundException($"{PathFilePrimary} or {PathFileSecondary}");
        }
    }
}
