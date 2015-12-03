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
using System.Runtime.CompilerServices;
using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using Clues.Library;
using OSIsoft.AF.Analysis;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Modeling;
using OSIsoft.AF.Time;


namespace Clues
{

    [Description("To Create or Edit an Eventframe")]
    [AdditionalDescription("")]
    [UsageExample("AFEventFrameCreate -s SRV01 -d afdatabase -n \"2015-12-03 15.35.00 EventABC\"" )]
    public class AFEventFrameCreate : AppletBase
    {
       


        //Command line Options
        [Option('s', "AFServer", HelpText = "AF Server to connect to", Required = true)]
        public string Server { get; set; }

        [Option('d', "database", HelpText = "Name of the AF database use", Required = true)]
        public string Database { get; set; }

        [Option('n', "name", HelpText = "Name of the EF To Create", Required = true)]
        public string EFName { get; set; }

        [Option("startTime", HelpText = "Start Time of the Event Frame", Required = false)]
        public string StartTime { get; set; }

        [Option("endTime", HelpText = "End Time of the Event Frame", Required = false)]
        public string EndTime { get; set; }

        [Option('t', "template", HelpText = "To create the event frame based on a template.  Name of the template.", Required = false)]
        public string Template { get; set; }


        public override void Run()
        {

            // connects to AF
            AFDatabase afDatabase;
            var afConnectionHelper = AfConnectionHelper.ConnectAndGetDatabase(Server, Database, out afDatabase);

             CreateEventFrame(afDatabase,EFName,StartTime,EndTime,Template);
            

        }



        private void CreateEventFrame(AFDatabase afDatabase,string name, string startTime=null, string endTime=null, string template = null)
        {
            
            // look to get the template, if template=null then aftemplate will be null as well
            var afTemplate = GetEventFrameTemplate(afDatabase,template);

            var ef = new AFEventFrame(afDatabase, name, afTemplate);
            if(!string.IsNullOrEmpty(startTime)) ef.SetStartTime(startTime);
            if(!string.IsNullOrEmpty(endTime)) ef.SetEndTime(endTime);


            // for demonstration purpose, we add some attributes to the event frame created.
            AddAttributes(afDatabase,ef);


            Logger.InfoFormat("Checking in EF: name: {0} GUID: {1}", ef.Name, ef.ID);
            // the checkin writes the event frame to the database
            ef.CheckIn();

            Logger.InfoFormat("EventFrameCreated: name: {0} GUID: {1}",ef.Name,ef.ID);

        }

        private AFElementTemplate GetEventFrameTemplate(AFDatabase afDatabase, string template)
        {
            if (string.IsNullOrEmpty(template))
                return null;

            if (afDatabase.ElementTemplates.Contains(template))
            {
                return afDatabase.ElementTemplates[template];
            }
            else
            {
                // if template does not exist, we stop right there by throwing an exception
                throw new AFEventFrameTemplateNotFoundException();
            }

        }

        private void CreateEventFrameTemplate()
        {

        }


        private void AddAttributes(AFDatabase afDatabase,AFEventFrame eventFrame)
        {
            // random number generator, to create different attributes for each event frames
            var randomGen=new Random(DateTime.Now.Millisecond);

            // creates an int attribute on the event frame
            var attribute = eventFrame.Attributes.Add("Value1");
            attribute.Type = typeof(int);
            attribute.SetValue(new AFValue(randomGen.Next(10, 1000)));

            // double attribute
            attribute = eventFrame.Attributes.Add("Value2");
            attribute.Type = typeof(double);
            attribute.SetValue(new AFValue(randomGen.Next(10,1000)));

            // here we are adding a tag that is normally present on all PI Data Archive Servers.
            // to show how to add a PIPoint data reference attribute
            attribute = eventFrame.Attributes.Add("Value3");
            attribute.DataReferencePlugIn = AFDataReference.GetPIPointDataReference(afDatabase.PISystem);
            attribute.ConfigString = @"\\optimus\sinusoid";
        }


    }
}
