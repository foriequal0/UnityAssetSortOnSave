using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AssetSortOnSave
{
    public class AssetSortOnSave : UnityEditor.AssetModificationProcessor
    {
        public static void OnWillSaveAssets(string[] files)
        {
            var unityFiles = files.Where(x =>
            {
                var lower = x.ToLower();
                return lower.EndsWith(".unity") || lower.EndsWith(".prefab");
            });

            foreach (var unityFile in unityFiles)
            {
                var hook = new HookAfterSave(unityFile);
                hook.Enable();
            }
        }
    }

    internal class HookAfterSave : IDisposable
    {
        private string path;

        FileSystemWatcher watcher = null;

        public HookAfterSave(string path)
        {
            this.path = path;
        }

        public void Enable()
        {
            var dirname = Path.GetDirectoryName(path);
            var filename = Path.GetFileName(path);
            var watcher = new FileSystemWatcher(dirname, filename);

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

            this.watcher = watcher;
        }

        private void Disable()
        {
            watcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }

        private void OnChanged(object source, FileSystemEventArgs args)
        {
            this.Disable();

            string content;
            using (var reader = new StreamReader(File.OpenRead(path)))
            {
                content = reader.ReadToEnd();
            }

            using (var writer = new StreamWriter(File.Open(path, FileMode.Create)))
            {
                var sorter = new YamlDocumentSorter(content);
                writer.Write(sorter.GetSortedYaml());
            }

            EditorApplication.update += new EditorApplication.CallbackFunction(OnReload);
        }

        private void OnReload()
        {
            EditorApplication.update -= new EditorApplication.CallbackFunction(OnReload);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    internal class YamlDocumentSorter
    {
        internal class Document
        {
            public string StartAndComment { get; internal set; }
            public string Body { get; internal set; }

            public Document(string startAndComment, string body)
            {
                this.StartAndComment = startAndComment;
                this.Body = body;
            }
        }

        private string unsorted;
        private bool sorted = false;
        private string sortedDocument = null;

        public YamlDocumentSorter(string content)
        {
            unsorted = content;
            sorted = false;
        }

        public string GetSortedYaml()
        {
            if (!sorted)
            {
                sortedDocument = SortDocument(ToLineEmuerable(unsorted));
                unsorted = null;
                sorted = true;
            }
            return sortedDocument;
        }

        private static IEnumerable<string> ToLineEmuerable(string content)
        {
            var reader = new StringReader(content);
            string line;
            while (true)
            {
                line = reader.ReadLine();
                if (line != null)
                    yield return line;
                else
                    yield break;
            }
        }

        private string SortDocument(IEnumerable<string> lines)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> withoutComment;
            var comment = ConsumeStartWith(lines, "#", out withoutComment);
            sb.Append(comment);

            IEnumerable<string> withoutDirective;
            var directive = ConsumeStartWith(withoutComment, "%", out withoutDirective);
            sb.Append(directive);

            var documents = ConsumeExplicitDocument(withoutDirective);
            var sorted = documents.OrderBy(x => x.StartAndComment);
            foreach (var doc in sorted)
            {
                sb.Append(doc.StartAndComment);
                sb.Append(doc.Body);
            }
            return sb.ToString();
        }

        private string ConsumeStartWith(IEnumerable<string> lines, string startsWith, out IEnumerable<string> unconsumed)
        {
            StringBuilder sb = new StringBuilder();
            int consumedLine = 0;
            foreach (var line in lines)
            {
                if (line.TrimStart(new char[] { ' ' }).StartsWith(startsWith))
                {
                    consumedLine++;
                    sb.AppendLine(line);
                }
                else
                {
                    break;
                }
            }

            unconsumed = lines.Skip(consumedLine);
            return sb.ToString();
        }

        private IEnumerable<Document> ConsumeExplicitDocument(IEnumerable<string> lines)
        {
            string startAndComment = null;
            StringBuilder sb = null;
            bool firstEncounter = true;

            foreach (var line in lines)
            {
                var documentStart = line.TrimStart(new char[] { ' ' }).StartsWith("---");
                if (documentStart)
                {
                    if (firstEncounter == false)
                    {
                        yield return new Document(startAndComment, sb.ToString());
                    }
                    startAndComment = line + System.Environment.NewLine;
                    sb = new StringBuilder();
                    firstEncounter = false;
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            yield return new Document(startAndComment, sb.ToString());
        }
    }
}