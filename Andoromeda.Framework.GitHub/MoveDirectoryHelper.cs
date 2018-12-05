﻿using System.Collections.Generic;
using System.IO;

namespace Andoromeda.Framework.GitHub
{
    internal static class MoveDirectoryHelper
    {
        public static void MoveDirectory(string source, string target)
        {
            var stack = new Stack<Folders>();
            stack.Push(new Folders(source, target));

            while (stack.Count > 0)
            {
                var folders = stack.Pop();

                if(!Directory.Exists(folders.Target))
                {
                    Directory.CreateDirectory(folders.Target);
                }

                foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                {
                    string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }

            try
            {
                Directory.Delete(source, true);
            }
            catch(IOException)
            { }
        }

        public class Folders
        {
            public string Source { get; private set; }

            public string Target { get; private set; }

            public Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }
    }
}
