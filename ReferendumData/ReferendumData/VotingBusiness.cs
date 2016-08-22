using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferendumData
{
    using Newtonsoft.Json;

    public class VotingBusiness
    {
        public DateTime VotingOpenFrom = new DateTime(2014, 8, 24, 0, 0, 0);
        public DateTime VotingOpenTill = new DateTime(2014, 8, 31, 12, 0, 0);

        public bool IsNowDuringVotingPeriod { get { if ((DateTime.Now >= VotingOpenFrom)&&(DateTime.Now <= VotingOpenTill)) {return true;}  return false; /* return true;*/ } }

        private ReferendumEntities _db = null;
        public ReferendumEntities db { get { if (_db == null) _db = new ReferendumEntities(); return _db; } }

        private DateTime AdultDOB = new DateTime(1996, 8, 31);
        private DateTime Teenager = new DateTime(1998, 8, 31);

        public enum MayVoteResult { MayVote, MayOverridePreviousVote, VotedAlready, Underaged, DeniedForOtherReasons };

        public class VoteResult {
            public bool Voted { get; set; }
            public Exception Ex { get; set; }
        }

        public Code GetCode (string CodeName, string CodeCategory)
        {
            var query = db.Codes.Where(c => c.Name == CodeName && c.Category == CodeCategory);

            if (query.Count() > 0)
                return query.First();

            return null;
        }

        public string GenerateOnlineAttemptID()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            while (true)
            {
                var stringChars = new char[16];                
                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }
                var codeInString = new String(stringChars);

                if (!db.Attempts.Any(a => a.Attempt_ID == codeInString))
                    return codeInString;
            }
        }

        public VoteResult Vote(string IDNumber, DateTime DOB, string Phone, string MotionOne, string MotionTwo, bool InPerson = false, bool IDHasedAlready = false)
        {
            try
            {
                var hashedID = (IDHasedAlready) ? IDNumber : DataUtilities.GetSaltedHash(IDNumber);
                var mayVoteResult = CheckMayVote(hashedID, DOB, Phone, true);

                if (mayVoteResult == MayVoteResult.MayVote)
                {
                    Vote vote = new Vote() { Motion_One = MotionOne[0].ToString(), Motion_Two = MotionTwo[0].ToString(), Category = (DOB <= AdultDOB) ? "A" : "T", In_Person = InPerson, DateTime = DateTime.Now };
                    Voter voter = new Voter() { ID_Number = hashedID, Date_of_Birth = DOB, Phone = Phone, Vote = vote };
                    db.Voters.Add(voter);
                    db.Votes.Add(vote);                                        
                    db.SaveChanges();

                    return new VoteResult() { Voted = true };
                }
                else if ((mayVoteResult == MayVoteResult.MayOverridePreviousVote) && (InPerson))
                {
                    var voter = db.Voters.First(v => v.ID_Number == hashedID);
                    string remark = JsonConvert.SerializeObject(voter.Vote, Formatting.Indented,new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    voter.Date_of_Birth = DOB;
                    voter.Vote.Motion_One = MotionOne[0].ToString();
                    voter.Vote.Motion_Two = MotionTwo[0].ToString();
                    voter.Vote.In_Person = InPerson;
                    voter.Vote.DateTime = DateTime.Now;
                    voter.Vote.Category = (DOB <= AdultDOB) ? "A" : "T";
                    voter.Vote.Remark += remark;

                    db.SaveChanges();

                    return new VoteResult() { Voted = true };
                }

            }catch(Exception ex)
            {
                return new VoteResult() { Voted = false, Ex = ex };
            }

            return new VoteResult() { Voted = false };
        }

        public MayVoteResult CheckMayVote(string IDNumber, DateTime DOB, string Phone, bool IDHasedAlready = false)
        {
            var hashedID = (IDHasedAlready) ? IDNumber : DataUtilities.GetSaltedHash(IDNumber);

            if (DOB > Teenager)
                return MayVoteResult.Underaged;

            if (!string.IsNullOrEmpty(Phone))
            {
                if (db.Voters.Any(v => v.Phone == Phone))
                    return MayVoteResult.VotedAlready;
            }

            if (db.Voters.Any(v => v.ID_Number == hashedID))
            {
                var voter = db.Voters.First(v => v.ID_Number == hashedID);
                
                if (voter.Vote.In_Person)
                {
                    return MayVoteResult.VotedAlready;
                }
                else
                {
                    return MayVoteResult.MayOverridePreviousVote;
                }
            }
            else
            {
                return MayVoteResult.MayVote;
            }
        }
    }
}
