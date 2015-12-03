using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clues.Library;
using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;


namespace Clues
{
    [Description("Illustrates the functionning of the PI Data Pipe, to get changes from PI Tags as changes occurs"),
    AdditionalDescription("Don't forget to stop the DataPipe gracefully by hitting a key before stopping the application.  Otherwise you may leave unwanted signups on the server."),
    UsageExample("PIDataPipeListener -p SRV-PI -t sinudoid,cdt158")
    ]
    public class PIDataPipeListener : AppletBase
    {

        //Command line options
        [Option('s', "PIServer", HelpText = "PI Data Archive Server to connect to", Required = true, MutuallyExclusiveSet = "PIDataPipe")]
        public string PIServerName { get; set; }

        [OptionList('t', "tags", HelpText = "List of tags to subscribe to.  Delimited by ',' ", Separator = ',', Required = true, MutuallyExclusiveSet = "PIDataPipe")]
        public List<string> TagList { get; set; }

        [Option('i', "interval", HelpText = "Time interval at which the DataPipe will check if new data is present on the Server, in seconds", DefaultValue = 5, Required = false)]
        public int Interval { get; set; }



        private ManualResetEvent _terminateRequest = new ManualResetEvent(false);

        /// <summary>  
        /// This examples shows how an AF data pipe works  
        /// We create one afDataPipe and two piDataPipes  
        /// </summary>  
        public override void Run()
        {
            if (Interval <= 0)
                throw new InvalidParameterException("Interval must be greater than 0");

            Task[] tasks = new[]
            {
                Task.Run(() => MonitorPITags())
            };

            Console.WriteLine("IMPORTANT: Press a key when you want to stop monitoring the values to stop listening gracefully...");
            Console.ReadKey();

            _terminateRequest.Set();

            Task.WaitAll(tasks);

        }


        private void MonitorPITags()
        {
            DataPipeHandler archiveDataPipeHandler = null;
            DataPipeHandler snapshotDataPipeHandler = null;

            try
            {
                
                if (string.IsNullOrEmpty(PIServerName))
                {
                    throw new PIServerNotFoundException();
                }

                else
                {

                    PIServer piserver;
                    var piConnectionManager = PiConnectionMgr.ConnectAndGetServer(PIServerName, out piserver);

                    // get the tag we want to monitor  
                    var pointList = PIPoint.FindPIPoints(piserver, TagList).ToList();

                    // event pipe for archive modifications
                    var archive = AFDataPipeType.Archive;

                    archiveDataPipeHandler = new DataPipeHandler(new PIConsoleDataObserver(archive), archive);
                    archiveDataPipeHandler.AddSignupsWithInitEvents(pointList);

                    // event pipe for snpshot modifications
                    var snapshot = AFDataPipeType.Snapshot;

                    snapshotDataPipeHandler = new DataPipeHandler(new PIConsoleDataObserver(snapshot), snapshot);
                    snapshotDataPipeHandler.AddSignupsWithInitEvents(pointList);


                    // archive data pipe is for demonstrative use
                    // you may only need the snapshot in your application, this depends on your use case
                    archiveDataPipeHandler.StartListening(TimeSpan.FromSeconds(Interval));
                    
                    snapshotDataPipeHandler.StartListening(TimeSpan.FromSeconds(Interval));

                    Logger.InfoFormat("Listening for data changes started. Checking every {0}s", Interval);

                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }


            finally
            {
                // here the method will wait until _terminateRequest.Set() before terminating
                _terminateRequest.WaitOne();

                // in case you don't know this is called null propagation
                // its equivalent to if x!=null x.Dispose()
                archiveDataPipeHandler?.Dispose();
                snapshotDataPipeHandler?.Dispose();
            }

        }


    }


}








