using System;
using System.IO;
using System.Text;

class Analyze {
    static void Main() {
        string file = @"C:\Users\Snkii\Documents\CROWNs_Albion_Fable_III\src\Dialogs.cs";
        string content = File.ReadAllText(file, Encoding.UTF8);
        content = content.Replace("\r\n", "\n");
        
        int idx = content.IndexOf("public void OpenQueueManager()");
        if (idx < 0) { Console.WriteLine("Method not found"); return; }
        
        string method = content.Substring(idx);
        int pos = method.IndexOf("ShowPopup(d);");
        while (pos >= 0) {
            int start = Math.Max(0, pos - 150);
            int len = Math.Min(300, method.Length - start);
            string ctx = method.Substring(start, len);
            Console.WriteLine("FOUND at pos " + pos + ":");
            Console.WriteLine(ctx);
            Console.WriteLine("---");
            pos = method.IndexOf("ShowPopup(d);", pos + 1);
        }
    }
}