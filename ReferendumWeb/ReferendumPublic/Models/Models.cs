using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReferendumPublic.Models
{
    public class BallotPaperInputModel
    {
        public string Trial_ID { get; set; }
        public string MotionOne { get; set; }
        public string MotionTwo { get; set; }
    }

    public class StepOneInputModel
    {        
        public string BIRPart1 { get; set; }
        public string BIRPart1CD { get; set; }

        public string BIRPart1Whole { get { return BIRPart1 + BIRPart1CD; } }

        public string BIRPart2 { get; set; }
        public string BIRPart3 { get; set; }

        public string Phone { get; set; }
    }

    public class StepTwoInputModel
    {
        public string Trial_ID { get; set; }
        public string SMS_Code { get; set; }
    }

    public class MessageViewModel
    {
        public bool SuggestedAction_GoHome { get; set; }
        public enum MessageType { Info, Warning, Error }
        public MessageType Message_Type { get; set; }
        public string Message_Title { get; set; }
        public string Message { get; set; }
    }
}