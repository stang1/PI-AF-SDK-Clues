#region Copyright
//  Copyright 2015 OSIsoft, LLC
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using Clues.Library;
using Clues._3_Utility.PITestDataUtil;
using OSIsoft.AF.Time;

namespace Clues
{

    [Description("Utility to create tags for testing purpose.  You can also generate data into the tags for the specified time period.")]
    [AdditionalDescription("All the tags created will get a prefix \"clues.gen\"; tag names will get added a number at the end. i.g. clues.gen.[suffix].00000")]
    [UsageExample("PIGenDataUtil -s server01 --suffix A --numStart 0 --numEnd 10 :: will generate 11 tags with name clues.gen.A.00000\n" + 
                   "or to generate 30d of 5m data data at the same time: \n" +
                    "PIGenDataUtil -s server01 --suffix A --numStart 0 --numEnd 10 --DataSt *-30d --dataIntervall 300")]
    public class PIGenDataUtil : AppletBase
    {
        //Command line Options
        [Option('s', "server", HelpText = "Name of the PI Data Archive server to connect to.")]
        public string Server { get; set; }

        //Command line Options - Tags
        [Option("suffix", HelpText = "Name of the suffix that will used in the tag name creation. i.g. clues.gen.[suffix].00000 ", Required = true)]
        public string NameSuffix { get; set; }

        [Option("numStart", HelpText = "Starting number to generate tag names.", Required = true)]
        public int NumStart { get; set; }

        [Option("numEnd", HelpText = "Ending number to generate tag names.", Required = true)]
        public int NumEnd { get; set; }

        [Option("delete", HelpText = "When specified, instead of creating the tags, it will delete the tags.", Required = false)]
        public bool Delete { get; set; }


        //Command line Options - Data generation
        [Option("dataSt", HelpText = "StartTime for the data generation. If not specified, no data is generated.", Required = false)]
        public string DataSt { get; set; }

        [Option("dataEt", HelpText = "Endtime for the data generation. Id not specified, and DataSt is specified, then current time is used.", Required = false)]
        public string DataEt { get; set; }

        [Option("dataInterval", HelpText = "Time interval at which the data will be generated. In seconds.", DefaultValue = 600, Required = false)]
        public int DataInterval { get; set; }
        

        private AFTime _dataEndTime;
        private AFTime _dataStartTime;
        private TimeSpan _interval;


        private void ValidateParameters()
        {
            // limit days intervalls to 30 days
            if (NumStart < 0 || NumEnd < 0)
                throw new PITestDataUtilInvalidParameterException("NumStart and NumEnd must be greater than 0");

            if (NumStart > NumEnd)
                throw new PITestDataUtilInvalidParameterException("NumEnd must be greater than NumStart");

            if (DataSt != null)
            {
                // AFTime will throw an error in case the string is not valid
                _dataStartTime = new AFTime(DataSt);

                _dataEndTime = DataEt != null ? new AFTime(DataEt) : new AFTime("*");

                _interval = TimeSpan.FromSeconds(DataInterval);
            }
                

        }


        public override void Run()
        {
            PIServer server = null;

            try
            {
                ValidateParameters();

                PiConnectionHelper.ConnectAndGetServer(Server, out server);

                if (Delete)
                {
                    DeleteTags(server);

                }

                else
                {
                    var newPoints = CreatePoints(server);

                    if (DataSt != null)
                        GenerateData(newPoints);
                }


            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        private IList<PIPoint> CreatePoints(PIServer server)
        {
            var tagNames = GeneratePointNames(NameSuffix, NumStart, NumEnd);

            Logger.Info("Creating the PI Points on the server");
            var result = server.CreatePIPoints(tagNames, GetPointAttributes());

            if (result != null && result.HasErrors)
                result.Errors.ToList().ForEach(e => Logger.Error("Error with: " + e.Key, e.Value));

            Logger.Info("operation completed.");

            // if results are not nul returns the results, otherwise, returns null
            return result?.Results;
        }

        private void GenerateData(IList<PIPoint> newPoints)
        {

            Parallel.ForEach(newPoints, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, point =>
            {
                var dg = new DataGenerator();
                var values = dg.GetRandomValues(point, _dataStartTime, _dataEndTime, _interval);
                point.Server.UpdateValues(values,AFUpdateOption.Replace);
                Logger.InfoFormat("Inserted {0} values for PI Point {1}",values.Count,point.Name);

            });


        }

        private void DeleteTags(PIServer server)
        {
            var tagNames = GeneratePointNames(NameSuffix, NumStart, NumEnd);

            Logger.Warn("Are you sure you want to delete the PI Points? PRESS 'y' to continue. Any other key to abort.");
            var input = Console.ReadKey();
            if (input.KeyChar == 'y' || input.KeyChar == 'Y')
            {
                Logger.Warn("Deleting tags");
                var result = server.DeletePIPoints(tagNames);

                if (result != null && result.HasErrors)
                    result.Errors.ToList().ForEach(e => Logger.Error("Error with: " + e.Key, e.Value));

                Logger.Info("operation completed.");
                return;
            }

            Logger.Info("operation aborted.");
        }


        private List<string> GeneratePointNames(string suffix, int numStart, int numEnd)
        {
            Logger.Info("Generating tag names");
            var tagNames = new List<string>();

            for (int i = numStart; i <= numEnd; i++)
            {
                var tagName = string.Format("clues.gen.{0}.{1:00000}", suffix, i);
                tagNames.Add(tagName);
            }

            return tagNames;
        }


        private Dictionary<string, object> GetPointAttributes()
        {

            var attributes = new Dictionary<string, object>();
            attributes.Add(PICommonPointAttributes.PointClassName, "classic");
            // currently we just use default: Float32
           //  attributes.Add(PICommonPointAttributes.PointType, PIPointType.Float32);
            attributes.Add(PICommonPointAttributes.Descriptor, "This is a tag generated by clues PITestDataUtil.  See https://github.com/osisoft/PI-AF-SDK-Clues for more information ");
            attributes.Add(PICommonPointAttributes.PointSource, "CLUES");

            return attributes;

        }



        private bool tagIsValid(string tagName)
        {
            var regex = new Regex(@"^[A-Z,a-z,0-9,_,%][^\*'\?;\{\}\[\]|\\`']+");

            return regex.IsMatch(tagName);


        }
    }

    public class PITestDataUtilInvalidParameterException : Exception
    {
        public PITestDataUtilInvalidParameterException(string message = "") : base("The passed parameter is not valid. " + message) { }
    }
}
