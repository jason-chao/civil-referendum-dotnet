﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ReferendumEntities : DbContext
    {
        public ReferendumEntities()
            : base("name=ReferendumEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Attempt> Attempts { get; set; }
        public virtual DbSet<Attempt_Record> Attempt_Record { get; set; }
        public virtual DbSet<Code> Codes { get; set; }
        public virtual DbSet<Vote> Votes { get; set; }
        public virtual DbSet<Voter> Voters { get; set; }
        public virtual DbSet<Operator> Operators { get; set; }
        public virtual DbSet<Operator_Operation> Operator_Operation { get; set; }
        public virtual DbSet<Operator_Session> Operator_Session { get; set; }
    }
}
