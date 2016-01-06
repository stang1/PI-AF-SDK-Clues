using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;

namespace Clues._3_Utility.PITestDataUtil
{
    public class DataGenerator
    {
        private Random _random=new Random();

        public List<AFValue> GetRandomValues(PIPoint point, AFTime startTime, AFTime endTime, TimeSpan interval)
        {
            var newValues=new List<AFValue>();

            var currentTime = startTime.LocalTime;
            while (currentTime <= endTime.LocalTime)
            {
                
                newValues.Add(new AFValue()
                {
                    PIPoint = point,
                    Timestamp = currentTime,
                    Value = 1000*_random.NextDouble()
                });

                currentTime = currentTime.Add(interval);
            }

            return newValues;

        }


    }
}
