using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferendumData
{
    using Newtonsoft.Json;

    /// <summary>
    /// Core business logic for the civil referendum - recording the votes and the voters
    /// </summary>
    public class VotingBusiness
    {
        /// <summary>
        /// The opening time of the 2014 Civil Referendum
        /// </summary>
        public DateTime VotingOpenFrom = new DateTime(2014, 8, 24, 0, 0, 0);

        /// <summary>
        /// The closing time of the 2014 Civil Referendum
        /// </summary>
        public DateTime VotingOpenTill = new DateTime(2014, 8, 31, 12, 0, 0);

        /// <summary>
        /// Whether or not the vote is open now
        /// </summary>
        public bool IsNowDuringVotingPeriod { get { if ((DateTime.Now >= VotingOpenFrom)&&(DateTime.Now <= VotingOpenTill)) {return true;}  return false; /* return true;*/ } }

        // The database access object
        private ReferendumEntities _db = null;
        public ReferendumEntities db { get { if (_db == null) _db = new ReferendumEntities(); return _db; } }

        /// <summary>
        /// Minimum Date of Birth for the adult (18+) group
        /// </summary>
        private DateTime AdultDOB = new DateTime(1996, 8, 31);

        /// <summary>
        /// Minimum Date of Birth for the teenager (16+) group
        /// </summary>
        private DateTime Teenager = new DateTime(1998, 8, 31);

        /// <summary>
        /// Types of result of voter eligibility validation
        /// </summary>
        public enum MayVoteResult { MayVote, MayOverridePreviousVote, VotedAlready, Underaged, DeniedForOtherReasons };

        /// <summary>
        /// Types of result of a Voting Attempt
        /// </summary>
        public class VoteResult {
            public bool Voted { get; set; }
            public Exception Ex { get; set; }
        }

        /// <summary>
        /// Get the status code for a Voting Attempt
        /// </summary>
        public Code GetCode (string CodeName, string CodeCategory)
        {
            var query = db.Codes.Where(c => c.Name == CodeName && c.Category == CodeCategory);

            if (query.Count() > 0)
                return query.First();

            return null;
        }

        /// <summary>
        /// Generate an ID for a Voing Attempt
        /// </summary>
        public string GenerateOnlineAttemptID()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            while (true)
            {
                // Length of Voting Attempt ID is 16
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

        /// <summary>
        /// Record a vote
        /// </summary>
        public VoteResult Vote(string IDNumber, DateTime DOB, string Phone, string MotionOne, string MotionTwo, bool InPerson = false, bool IDHasedAlready = false)
        {
            try
            {
                var hashedID = (IDHasedAlready) ? IDNumber : DataUtilities.GetSaltedHash(IDNumber);

                // Check whether the voter has not cast a vote and is eligible to vote
                var mayVoteResult = CheckMayVote(hashedID, DOB, Phone, true);

                // Create a record for a new vote, if the voter has not voted
                if (mayVoteResult == MayVoteResult.MayVote)
                {
                    Vote vote = new Vote() { Motion_One = MotionOne[0].ToString(), Motion_Two = MotionTwo[0].ToString(), Category = (DOB <= AdultDOB) ? "A" : "T", In_Person = InPerson, DateTime = DateTime.Now };
                    Voter voter = new Voter() { ID_Number = hashedID, Date_of_Birth = DOB, Phone = Phone, Vote = vote };
                    db.Voters.Add(voter);
                    db.Votes.Add(vote);                                        
                    db.SaveChanges();

                    return new VoteResult() { Voted = true };
                }
                // Override the voter's previous online vote, if the voter has voted online and now wishes to vote in-person at a ballot place
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
            }catch(Exception ex) // Refuse to record a vote in all other cases
            {
                return new VoteResult() { Voted = false, Ex = ex };
            }

            return new VoteResult() { Voted = false };
        }
        
        /// <summary>
        /// Check whether a vote has not cast a vote and is eligible to vote
        /// </summary>
        public MayVoteResult CheckMayVote(string IDNumber, DateTime DOB, string Phone, bool IDHasedAlready = false)
        {
            var hashedID = (IDHasedAlready) ? IDNumber : DataUtilities.GetSaltedHash(IDNumber);

            // Refused the voter if aged below 16
            if (DOB > Teenager)
                return MayVoteResult.Underaged;

            // Refuse the voter if the phone number has been used
            if (!string.IsNullOrEmpty(Phone))
            {
                if (db.Voters.Any(v => v.Phone == Phone))
                    return MayVoteResult.VotedAlready;
            }

            // If the vote has voted
            if (db.Voters.Any(v => v.ID_Number == hashedID))
            {
                var voter = db.Voters.First(v => v.ID_Number == hashedID);
                
                // If the previous vote was cast in person, refuse the voter
                if (voter.Vote.In_Person)
                {
                    return MayVoteResult.VotedAlready;
                }
                else // If the previous vote was cast online, allow the voter to vote iff (if and only if) the voter is voting in-person at a voting place
                {
                    return MayVoteResult.MayOverridePreviousVote;
                }
            }
            else // If the vote has not voted, allow the voter to vote
            {
                return MayVoteResult.MayVote;
            }
        }
    }
}
