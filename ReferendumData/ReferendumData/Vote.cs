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
    
    public partial class Vote
    {
        public Vote()
        {
            this.Voters = new HashSet<Voter>();
            this.Operator_Operation = new HashSet<Operator_Operation>();
        }
    
        public int Vote_ID { get; set; }
        public System.DateTime DateTime { get; set; }
        public string Motion_One { get; set; }
        public string Motion_Two { get; set; }
        public string Category { get; set; }
        public bool In_Person { get; set; }
        public string Remark { get; set; }
    
        public virtual ICollection<Voter> Voters { get; set; }
        public virtual ICollection<Operator_Operation> Operator_Operation { get; set; }
    }
}
