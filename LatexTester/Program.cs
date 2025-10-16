using System.Diagnostics;
using System.IO;
using System;

Console.WriteLine("--- LaTeX Compilation Test Started ---");

var latexCode = @"
\documentclass{article}
\begin{document}
Hello from the C# Console App!
\end{document}
";

var tempId = Guid.NewGuid().ToString();
var tempDir = Path.Combine(Path.GetTempPath(), tempId);
var texFilePath = Path.Combine(tempDir, "test.tex");
var pdfFilePath = Path.Combine(tempDir, "test.pdf");

try
{
    Directory.CreateDirectory(tempDir);
    File.WriteAllText(texFilePath, latexCode);
    Console.WriteLine($"Temporary directory created at: {tempDir}");

    var processStartInfo = new ProcessStartInfo
    {
        FileName = "pdflatex",
        Arguments = $"-output-directory=\"{tempDir}\" -interaction=nonstopmode \"{texFilePath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    Console.WriteLine("Starting pdflatex process...");
    using (var process = Process.Start(processStartInfo))
    {
        if (process == null)
        {
            Console.WriteLine("ERROR: Failed to start pdflatex process. Is it in your system PATH?");
            return;
        }

        // Read output and error streams
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit(30000);

        Console.WriteLine("\n--- Standard Output ---");
        Console.WriteLine(output);
        Console.WriteLine("--- End of Standard Output ---\n");

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine("--- Standard Error ---");
            Console.WriteLine(error);
            Console.WriteLine("--- End of Standard Error ---\n");
        }

        if (File.Exists(pdfFilePath))
        {
            Console.WriteLine("SUCCESS: PDF file was created successfully!");
        }
        else
        {
            Console.WriteLine("FAILURE: PDF file was NOT created.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nCRITICAL ERROR: An exception occurred.");
    Console.WriteLine(ex.ToString());
}
finally
{
    if (Directory.Exists(tempDir))
    {
        // Directory.Delete(tempDir, true); // We keep the folder for inspection
        Console.WriteLine("Cleanup skipped. Please check the temporary folder for files.");
    }
    Console.WriteLine("\n--- Test Finished. Press any key to exit. ---");
    Console.ReadKey();
}