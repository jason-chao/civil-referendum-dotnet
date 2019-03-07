using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

///<summary>
/// The view models for the MVC framework
///</summary>
namespace ReferendumStatistics.Models
{
    public class DummyInputModel { }

    public class MotionResult
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int Blank { get; set; }
    }

    public class TurnoutViewModel
    {
        public List<TimeSlot> TimeSlotList { get; set; }

        public bool MotionOneOut { get; set; }
        public MotionResult MotionOneAdult { get; set; }
        public MotionResult MotionOneTeen { get; set; }
        public MotionResult MotionOneTotal { get; set; }

        public bool MotionTwoOut { get; set; }
        public MotionResult MotionTwoAdult { get; set; }
        public MotionResult MotionTwoTeen { get; set; }
        public MotionResult MotionTwoTotal { get; set; }
    }

    public class TimeSlot
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Number { get; set; }
        public int CumulativeNumber { get; set; }
    }
}