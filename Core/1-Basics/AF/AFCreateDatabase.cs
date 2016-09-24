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
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using Clues.Library;

namespace Clues
{

    [Description("Creates a new AF Database")]
    [UsageExample("Applet -s SRV01")]
    public class AFCreateDatabase : AppletBase
    {
        //Command line Options
        [Option('s', "server", HelpText = "AF Server name to connect to")]
        public string Server { get; set; }

        [Option('d', "database", HelpText = "The name of the new database that will be created")]
        public string Database { get; set; }


        public override void Run()
        {
            try
            {
                PISystems piSystems = new PISystems();
                PISystem piSystem = piSystems[Server];               
                piSystem.Connect(true, null);
                Logger.InfoFormat("Connected to AF Server {0}", Server);

                Logger.InfoFormat("Creating the new database {0} ...", Database);

                if(piSystem.Databases.Contains(Database))
                    throw new Exception("The database already exists. Cannot create the new database.");

                piSystem.Databases.Add(Database);

                Logger.InfoFormat("Database creation completed");

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }
    }
}
