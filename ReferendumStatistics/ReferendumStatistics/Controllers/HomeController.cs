using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CaptchaMvc.Attributes;

using ReferendumData;
using ReferendumStatistics.Models;
using Newtonsoft.Json;
using System.IO;

namespace ReferendumStatistics.Controllers
{
    /// <summary>
    /// The web controller for the page of voter turnout and results of the 2014 Civil Referendum
    /// </summary>
    public class HomeController : Controller
    {
        VotingBusiness vb = new VotingBusiness();
        /// <summary>
        /// Time for announcing the result of motion 1
        /// </summary>
        DateTime announcementOne = new DateTime(2014, 08, 31, 12, 0, 0);
        /// <summary>
        /// Time for announcing the result of motion 2 (in observance of the restriction on the release of survey results imposed by Macau election laws)
        /// </summary>
        DateTime announcementTwo = new DateTime(2014, 09, 02, 0, 0, 0);

        /// <summary>
        /// Return the static landing page with a Captcha
        /// </summary>
        [HttpGet]
        public ActionResult Index()
        {
            return View(new TurnoutViewModel() { TimeSlotList = new List<TimeSlot>(), MotionOneOut = false, MotionTwoOut = false });
        }

        /// <summary>
        /// Return the turnout / results (if the Captcha test is passed)
        /// </summary>
        [HttpPost, CaptchaVerify("Captcha is not valid")]
        public ActionResult Index(DummyInputModel input)
        {
            TurnoutViewModel tvModel = new TurnoutViewModel() { TimeSlotList = new List<TimeSlot>(), MotionOneOut = false, MotionTwoOut = false };

            // Proceed, if the Captcha test is passed 
            if (ModelState.IsValid)
            {
                // The voter turnout data are cached in a json file - no need to read fromt the database every time
                var cachedTurnout = HttpContext.Server.MapPath("~/Content/turnout.json");

                if (System.IO.File.Exists(cachedTurnout))
                {
                    // Read from the cache file
                    var cachedTimeSlotList = JsonConvert.DeserializeObject<List<TimeSlot>>(System.IO.File.ReadAllText(cachedTurnout));

                    var maxTo = cachedTimeSlotList.Max(ts => ts.To);

                    // Pass the cached data to the view model if the cache file was written in the last two hours (120 minutes)
                    if ((DateTime.Now - maxTo).TotalMinutes < 120)
                        tvModel.TimeSlotList = cachedTimeSlotList;
                }

                // Calculate the voter turnout / results if no cache data could be used (cached data discarded if older than 120 minutes)
                if (tvModel.TimeSlotList.Count() <= 0)
                {
                    DateTime from = vb.VotingOpenFrom;

                    // Aggregate numbers of voters by every 2 hours

                    // Generate a list of 2-hour timeslots
                    while (true)
                    {
                        DateTime to = from.AddHours(2);

                        if (to > DateTime.Now)
                            break;

                        TimeSlot timeSlot = new TimeSlot() { From = from, To = to };
                        tvModel.TimeSlotList.Add(timeSlot);
                        from = to;
                    }

                    // Get voter counts of the timeslots on the list
                    int cum = 0;
                    foreach (var ts in tvModel.TimeSlotList)
                    {
                        ts.Number = vb.db.Votes.Count(v => ts.From <= v.DateTime && ts.To > v.DateTime);
                        cum += ts.Number;
                        ts.CumulativeNumber = cum;
                    }

                    tvModel.TimeSlotList = tvModel.TimeSlotList.OrderByDescending(ts => ts.From).ToList();

                    // Write the voter turnout data to the cache
                    System.IO.File.WriteAllText(cachedTurnout, JsonConvert.SerializeObject(tvModel.TimeSlotList));
                }

                // If it is the time to announce the results of motion 1, assign the result data of motion 1 to the view model.
                if (DateTime.Now > announcementOne)
                {

                    tvModel.MotionOneOut = true;

                    // The adult group votes on motion 1 
                    tvModel.MotionOneAdult = new MotionResult() {
                        A = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "A"),
                        B = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "B"),
                        C = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "C"),
                        Blank = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "-")
                    };

                    // The teen group votes on motion 1 
                    tvModel.MotionOneTeen = new MotionResult()
                    {
                        A = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "A"),
                        B = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "B"),
                        C = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "C"),
                        Blank = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "-")
                    };

                    // The total votes on motion 1 
                    tvModel.MotionOneTotal = new MotionResult()
                    {
                        A = vb.db.Votes.Count(v => v.Motion_One == "A"),
                        B = vb.db.Votes.Count(v => v.Motion_One == "B"),
                        C = vb.db.Votes.Count(v => v.Motion_One == "C"),
                        Blank = vb.db.Votes.Count(v => v.Motion_One == "-")
                    };                    
                }

                // If it is the time to announce the results of motion 2, assign the result data of motion 2 to the view model.
                if (DateTime.Now > announcementTwo)
                {
                    tvModel.MotionTwoOut = true;

                    // The adult group votes on motion 2
                    tvModel.MotionTwoAdult = new MotionResult()
                    {
                        A = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "A"),
                        B = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "B"),
                        C = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "C"),
                        Blank = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "-")
                    };

                    // The teen group votes on motion 2
                    tvModel.MotionTwoTeen = new MotionResult()
                    {
                        A = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "A"),
                        B = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "B"),
                        C = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "C"),
                        Blank = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "-")
                    };

                    // The total votes on motion 2
                    tvModel.MotionTwoTotal = new MotionResult()
                    {
                        A = vb.db.Votes.Count(v => v.Motion_Two == "A"),
                        B = vb.db.Votes.Count(v => v.Motion_Two == "B"),
                        C = vb.db.Votes.Count(v => v.Motion_Two == "C"),
                        Blank = vb.db.Votes.Count(v => v.Motion_Two == "-")
                    };
                }
            }

            return View(tvModel);
        }

        /// <summary>
        /// Results of the 2014 Civil Refernedum in text.  (For internal use: Quick preparation of text for publicity materials.)
        /// </summary>
        public string VotesInText()
        {
            TurnoutViewModel tvModel = new TurnoutViewModel() { TimeSlotList = new List<TimeSlot>(), MotionOneOut = false, MotionTwoOut = false };

            tvModel.MotionOneOut = true;

            tvModel.MotionOneAdult = new MotionResult()
            {
                A = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "A"),
                B = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "B"),
                C = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "C"),
                Blank = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_One == "-")
            };

            tvModel.MotionOneTeen = new MotionResult()
            {
                A = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "A"),
                B = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "B"),
                C = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "C"),
                Blank = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_One == "-")
            };

            tvModel.MotionOneTotal = new MotionResult()
            {
                A = vb.db.Votes.Count(v => v.Motion_One == "A"),
                B = vb.db.Votes.Count(v => v.Motion_One == "B"),
                C = vb.db.Votes.Count(v => v.Motion_One == "C"),
                Blank = vb.db.Votes.Count(v => v.Motion_One == "-")
            };

            tvModel.MotionTwoOut = true;

            tvModel.MotionTwoAdult = new MotionResult()
            {
                A = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "A"),
                B = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "B"),
                C = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "C"),
                Blank = vb.db.Votes.Count(v => v.Category == "A" && v.Motion_Two == "-")
            };

            tvModel.MotionTwoTeen = new MotionResult()
            {
                A = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "A"),
                B = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "B"),
                C = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "C"),
                Blank = vb.db.Votes.Count(v => v.Category == "T" && v.Motion_Two == "-")
            };

            tvModel.MotionTwoTotal = new MotionResult()
            {
                A = vb.db.Votes.Count(v => v.Motion_Two == "A"),
                B = vb.db.Votes.Count(v => v.Motion_Two == "B"),
                C = vb.db.Votes.Count(v => v.Motion_Two == "C"),
                Blank = vb.db.Votes.Count(v => v.Motion_Two == "-")
            };

            string result = string.Empty;

            result += "命題一: 你是否贊成2019年澳門行政長官由普選產生？\r\nMotion One: Should the Chief Executive of the Macau be elected by universal suffrage in 2019?\r\n";

            result += string.Format("18歲或以上組別 Group 18+： A.是 Yes) {0:#,##} ; B.否 No) {1:#,##} ; C.棄權 Abstention) {2:#,##} ; 白票 Blank) {3:#,##}.\r\n", tvModel.MotionOneAdult.A, tvModel.MotionOneAdult.B, tvModel.MotionOneAdult.C, tvModel.MotionOneAdult.Blank);
            result += string.Format("16-17歲組別 Group 16-17： A.是 Yes) {0:#,##} ; B.否 No) {1:#,##} ; C.棄權 Abstention) {2:#,##} ; 白票 Blank) {3:#,##}.\r\n", tvModel.MotionOneTeen.A, tvModel.MotionOneTeen.B, tvModel.MotionOneTeen.C, tvModel.MotionOneTeen.Blank);
            result += string.Format("總計 Total： A.是 Yes) {0:#,##} ; B.否 No) {1:#,##} ; C.棄權 Abstention) {2:#,##} ; 白票 Blank) {3:#,##}.\r\n", tvModel.MotionOneTotal.A, tvModel.MotionOneTotal.B, tvModel.MotionOneTotal.C, tvModel.MotionOneTotal.Blank);

            result += "\r\n\r\n命題二: 你是否信任2014澳門行政長官選舉唯一候選人崔世安成為行政長官？\r\nMotion Two: Do you have confidence in the sole candidate in the Chief Executive Election 2014 Chui Sai On Fernando becoming the Chief Executive?\r\n";

            result += string.Format("18歲或以上組別 Group 18+： A.是 Yes) {0:#,##} ; B.否 No) {1:#,##} ; C.棄權 Abstention) {2:#,##} ; 白票 Blank) {3:#,##}.\r\n", tvModel.MotionTwoAdult.A, tvModel.MotionTwoAdult.B, tvModel.MotionTwoAdult.C, tvModel.MotionTwoAdult.Blank);
            result += string.Format("16-17歲組別 Group 16-17： A.是 Yes) {0:#,##} ; B.否 No) {1:#,##} ; C.棄權 Abstention) {2:#,##} ; 白票 Blank) {3:#,##}.\r\n", tvModel.MotionTwoTeen.A, tvModel.MotionTwoTeen.B, tvModel.MotionTwoTeen.C, tvModel.MotionTwoTeen.Blank);
            result += string.Format("總計 Total： A.是 Yes) {0:#,##} ; B.否 No) {1:#,##} ; C.棄權 Abstention) {2:#,##} ; 白票 Blank) {3:#,##}.\r\n", tvModel.MotionTwoTotal.A, tvModel.MotionTwoTotal.B, tvModel.MotionTwoTotal.C, tvModel.MotionTwoTotal.Blank);


            return result;
        }

        /// <summary>
        /// Offer to download the ballot records in text.  (Voters' data are destructed as promised in the privacy statement and will never be recovered.)   
        /// </summary>
        public ActionResult Download()
        {
            // Download only available after the vote is closed
            if (DateTime.Now > announcementOne)
            {
                // Both motion 1 and motion 2 may be released
                if (DateTime.Now > announcementTwo)
                {
                    var resultTxtPhysicalPath = HttpContext.Server.MapPath("~/Content/AllVotes.txt");

                    // Produce the record file if it does not exist
                    if (!System.IO.File.Exists(resultTxtPhysicalPath))
                    {
                        StreamWriter writer = new StreamWriter(resultTxtPhysicalPath, false, System.Text.Encoding.UTF8);

                        writer.WriteLine("投票時間 Date and Time\t命題一 Motion One\t命題二 Motion Two\t組別 Group");

                        // Only the voting time, the choices and the voter group are exported
                        foreach (var vote in vb.db.Votes.OrderBy(v => v.DateTime))
                        {
                            writer.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss}\t{1}\t{2}\t{3}", vote.DateTime, vote.Motion_One, vote.Motion_Two, (vote.Category == "A") ? "18+" : "16-17"));
                        }

                        writer.Close();
                    }

                    // Offer the file
                    return File(resultTxtPhysicalPath, "text/plain");
                }
                else
                {
                    // Only motion 1 may be released
                    var resultTxtPhysicalPath = HttpContext.Server.MapPath("~/Content/AllVotes_M1Only.txt");

                    // Produce the record file if it does not exist
                    if (!System.IO.File.Exists(resultTxtPhysicalPath))
                    {
                        StreamWriter writer = new StreamWriter(resultTxtPhysicalPath, false, System.Text.Encoding.UTF8);

                        writer.WriteLine("命題二結果將於2014年9月2日公佈 The result of motion 2 will be published on/after 02 September 2014\r\n");
                        writer.WriteLine("投票時間 Date and Time\t命題一 Motion One\t組別 Group");

                        // Only the voting time, the choice and the voter group are exported
                        foreach (var vote in vb.db.Votes.OrderBy(v => v.DateTime))
                        {
                            writer.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss}\t{1}\t{2}", vote.DateTime, vote.Motion_One, (vote.Category == "A") ? "18+" : "16-17"));
                        }

                        writer.Close();
                    }

                    // Offer the file
                    return File(resultTxtPhysicalPath, "text/plain");
                }
            }

            return RedirectToAction("Index");
        }

    }
}