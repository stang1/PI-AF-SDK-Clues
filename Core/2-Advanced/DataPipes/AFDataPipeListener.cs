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
    /// <summary>
    /// <example>
    /// AFDataPipeListener -s SRV-PI -d AFDatabase -a "Motors\Motor1|Temperature"
    /// </example>
    /// </summary>
    [
    Description("Illustrates the functionning of the AF Data Pipe, to get changes from AFAttributes as changes occurs"),
    AdditionalDescription("Don't forget to stop the DataPipe gracefully by hitting a key before stopping the application.  Otherwise you may leave unwanted signups on the server."),
    UsageExample("AFDataPipeListener -s SRV-PI -d AFDatabase -a \"Motors\\Motor1|Temperature\"")
    ]
    public class AFDataPipeListener : AppletBase
    {

        //Command line options
        [Option('s', "AFServer", HelpText = "AF Server to connect to", Required = true)]
        public string AFServerName { get; set; }

        [Option('d', "database", HelpText = "AF Database to connect to", Required = true)]
        public string AFDatabaseName { get; set; }

        [OptionList('a', "attributes", HelpText = "list of elements to subscribe to.  Delimited by ',' ", Separator = ',', Required = true)]
        public List<string> AttributesList { get; set; }

        [Option('i', "interval", HelpText = "Time interval at which the DataPipe will check if new data is present on the Server, in seconds", DefaultValue = 5, Required = false)]
        public int Interval { get; set; }

        /// <summary>
        /// Private variables
        /// </summary>

        // the manual reset event serves to transmit information to the MonitorAFAttribute task
        private ManualResetEvent _terminateRequest = new ManualResetEvent(false);

        /// <summary>  
        /// This examples shows how an AF data pipe works  
        /// We create one afDataPipe and two piDataPipes  
        /// </summary>  
        public override void Run()
        {

            if(Interval<=0)
                throw new InvalidParameterException("Interval must be greater than 0");
                

            Task[] tasks = new[]
            {
                Task.Run(() => MonitorAFAttributes()),
            };

            Console.WriteLine("IMPORTANT: Press a key when you want to stop monitoring the values to stop listening gracefully...");
            Console.ReadKey();

            _terminateRequest.Set();

            Task.WaitAll(tasks);

        }

        private void MonitorAFAttributes()
        {

            DataPipeHandler afDataPipeHandler = null;

            try
            {

                //connect to AF Server  
                if (string.IsNullOrEmpty(AFServerName))
                {
                    throw new AFServerNotFoundException();
                }

                else
                {

                    AFDatabase database;
                    var _afConnectionManager = AfConnectionMgr.ConnectAndGetDatabase(AFServerName, AFDatabaseName,
                        out database);

                    // get the attributes that will be monitored
                    IDictionary<string, string> findAttributesErrors;
                    var attributes = AFAttribute.FindAttributesByPath(AttributesList, database, out findAttributesErrors);

                    // in case there was errors in the search we display them
                    if (findAttributesErrors != null && findAttributesErrors.Count > 0)
                        findAttributesErrors.ToList().ForEach(e => Logger.ErrorFormat("{0},{1}", e.Key, e.Value));


                    afDataPipeHandler = new DataPipeHandler(new AFConsoleDataObserver());
                    afDataPipeHandler.AddSignupsWithInitEvents(attributes);

                    afDataPipeHandler.StartListening(TimeSpan.FromSeconds(Interval));

                    Logger.InfoFormat("Listening for data changes started. Checking every {0}s", Interval);

                }

            }


            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            finally
            {
                _terminateRequest.WaitOne();

                // null propagation operator, this is same as x!=null x.Dispose()
                afDataPipeHandler?.Dispose();
            }

        }


    }

}






