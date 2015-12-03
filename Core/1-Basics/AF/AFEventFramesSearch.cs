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
using System.ComponentModel;
using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using Clues.Library;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Time;

namespace Clues
{

    [Description("Searches event frames")]
    [AdditionalDescription("")]
    [UsageExample("AFEventFramesSearch -s SRV01 -d afdatabase -t *-30d")]
    public class AFEventFramesSearch : AppletBase
    {
        //Command line Options
        [Option('s', "AFServer", HelpText = "AF Server to connect to", Required = true)]
        public string Server { get; set; }

        [Option('d', "database", HelpText = "Name of the AF database use", Required = true)]
        public string Database { get; set; }
        
        [Option('t', "searchTime", HelpText = "Specifies the times at which the event frames search will be performed.", Required = true)]
        public string SearchTime { get; set; }



        public override void Run()
        {

            // connects to AF
            AFDatabase afDatabase;
            var afConnectionHelper = AfConnectionHelper.ConnectAndGetDatabase(Server, Database, out afDatabase);

            SearchEventFrames(afDatabase, SearchTime);
            
        }
        

        /// <summary>
        /// Searches event frames that where ACTIVE at the specified time
        /// </summary>
        /// <remarks>There are a lot of ways to search event frames, this is just one way of doing it</remarks>
        /// <param name="afDatabase"></param>
        /// <param name="time"></param>
        /// Search mode is the key to understand how EFs are found, in this scenario we look for overlapped:
        /// <see cref="https://techsupport.osisoft.com/Documentation/PI-AF-SDK/html/T_OSIsoft_AF_Asset_AFSearchMode.htm"/>
        private void SearchEventFrames(AFDatabase afDatabase, string time)
        {
            try
            {

                var searchTime = new AFTime(time);

                Logger.InfoFormat("Searching for event frames in {0} database at time {1}",afDatabase.Name, searchTime);

                var eventFrames = AFEventFrame.FindEventFrames(
                    database: afDatabase,
                    searchRoot: null,
                    searchMode: AFSearchMode.Overlapped,
                    startTime: searchTime,
                    endTime: searchTime,
                    nameFilter: string.Empty,
                    referencedElementNameFilter: string.Empty,
                    eventFrameCategory: null,
                    eventFrameTemplate: null,
                    referencedElementTemplate: null,
                    durationQuery: null,
                    searchFullHierarchy: false,
                    sortField: AFSortField.Name,
                    sortOrder: AFSortOrder.Ascending,
                    startIndex: 0,
                    maxCount: 1000
                );

                Logger.InfoFormat("Found {0} event frames: ", eventFrames.Count);
                foreach (var ef in eventFrames)
                {
                    Logger.InfoFormat("{0}-ST:{1}-ET:{2}", ef.Name, ef.StartTime, ef.EndTime);
                }

            }

            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }


       

    }
}
