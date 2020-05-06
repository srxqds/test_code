using Microsoft.Build.Locator;
using RoslynAnalyzer.CLI.Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynAnalyzer.CLI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Attempt to set the version of MSBuild.
            VisualStudioInstance[] visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            VisualStudioInstance instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : (VisualStudioInstance)SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);


            try
            {
                if (args.Length <= 0)
                {
                    return;
                }

                var startTime = DateTime.Now;

                var fileName = args[0];
                var fileInfo = new FileInfo(fileName);

                //NOTE: This could be configurable via the CLI at some point
                var report = new AnalyzerReport();
                report.AddExporter(new ConsoleAnalyzerExporter());
                report.AddExporter(new JsonAnalyzerExporter());


                report.InitializeReport(fileInfo);

                var tasks = new List<Task>();
                if (fileInfo.Exists)
                {
                    var solutionAnalyzer = new SolutionAnalyzer();
                    var analyzeTask = solutionAnalyzer.LoadAnadAnalyzeProject(fileInfo, report);
                    tasks.Add(analyzeTask);
                }

                Task.WaitAll(tasks.ToArray());

                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                report.FinalizeReport(duration);

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception generalException)
            {
                
                Console.WriteLine("There was an exception running the analysis");
                Console.WriteLine(generalException.ToString());
            }



        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }
            // 默认选择第一个，vs2019有问题
            return visualStudioInstances[0];
            //while (true)
            //{
            //    var userResponse = Console.ReadLine();
            //    if (int.TryParse(userResponse, out int instanceNumber) &&
            //        instanceNumber > 0 &&
            //        instanceNumber <= visualStudioInstances.Length)
            //    {
            //        return visualStudioInstances[instanceNumber - 1];
            //    }
            //    Console.WriteLine("Input not accepted, try again.");
            //}
        }

    }
}
