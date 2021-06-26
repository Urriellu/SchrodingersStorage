using Newtonsoft.Json;
using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace SchrodingersStorage
{
    public class SchrodingersFile<T>
    {
        public readonly string PathFilePrimary;
        public readonly string PathFileSecondary;

        public string PathParentDirectoryPrimary => Path.GetDirectoryName(PathFilePrimary);
        public string PathParentDirectorySecondary => Path.GetDirectoryName(PathFileSecondary);

        public bool Exists => Primary.Exists || Secondary.Exists;

        readonly FileInfo Primary;
        readonly FileInfo Secondary;

        public SchrodingersFile(string pathFilePrimary, string pathFileSecondary)
        {
            if (string.IsNullOrEmpty(pathFilePrimary)) throw new ArgumentNullException(nameof(pathFilePrimary));
            if (string.IsNullOrEmpty(pathFileSecondary)) throw new ArgumentNullException(nameof(pathFileSecondary));

            this.PathFilePrimary = pathFilePrimary;
            this.PathFileSecondary = pathFileSecondary;

            Primary = new FileInfo(PathFilePrimary);
            Secondary = new FileInfo(PathFileSecondary);
        }

        public void MoveToSecondary() => File.Move(PathFilePrimary, PathFileSecondary);

        public void Write(T content)
        {
            string json = JsonConvert.SerializeObject(content);
            WriteAsString(json);
        }

        internal void WriteAsString(string content)
        {
            if (Secondary.Exists)
            {
                // if it has been moved to secondary, update its contents in secondary location
                File.WriteAllText(PathFileSecondary, content);
                File.Delete(PathFilePrimary);
            }
            else
            {
                File.WriteAllText(PathFilePrimary, content); // if it hasn't been moved to secondary, or it doesn't exist anywhere, write to primary
                if (Secondary.Exists) File.Move(PathFilePrimary, PathFileSecondary); // if another thread or process has created the file in secondary while we were writing to the primary, move from primary to secondary
            }
        }

        internal string ReadAsString()
        {
            string content;
            try { content = File.ReadAllText(PathFilePrimary); }
            catch
            {
                try { content = File.ReadAllText(PathFileSecondary); }
                catch
                {
                    throw new Exception($"Unable to read from either primary nor secondary location.");
                }
            }
            return content;
        }

        public T Read()
        {
            string json = ReadAsString();
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }
    }
}
