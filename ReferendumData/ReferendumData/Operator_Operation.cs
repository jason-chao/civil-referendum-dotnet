//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReferendumData
{
    using System;
    using System.Collections.Generic;
    
    public partial class Operator_Operation
    {
        public long Operator_Operation_ID { get; set; }
        public int Operator_ID { get; set; }
        public Nullable<long> Operator_Session_ID { get; set; }
        public System.DateTime DateTime { get; set; }
        public string Description { get; set; }
        public string IP_Address { get; set; }
        public string User_Agent { get; set; }
        public Nullable<int> Voter_ID { get; set; }
        public Nullable<int> Vote_ID { get; set; }
    
        public virtual Operator Operator { get; set; }
        public virtual Operator_Session Operator_Session { get; set; }
        public virtual Vote Vote { get; set; }
        public virtual Voter Voter { get; set; }
    }
}