using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;

using ReferendumPublic.Models;
using ReferendumData;

using CaptchaMvc.Attributes;

namespace ReferendumPublic.Controllers
{
    /// <summary>
    /// The web controller for the online voting channel of the 2014 Civil Referendum
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// The core voting logic
        /// </summary>
        VotingBusiness vb = new VotingBusiness();

        /// <summary>
        /// Return the static landing page
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Return the static thank you page
        /// </summary>
        public ActionResult ThankYou()
        {
            return View();
        }

        /// <summary>
        /// Return the static privacy statement page
        /// </summary>
        public ActionResult Statement()
        {
            return View();
        }

        /// <summary>
        /// Return the static contact information page
        /// </summary>       
        public ActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// Online voting step 1: Voter ID data input
        /// </summary> 
        [HttpGet]
        public ActionResult StepOne()
        {
            // Let the voter to input ID data if the vote is still open
            if (vb.IsNowDuringVotingPeriod)
            {
                ViewBag.ErrorMessages = new List<string>();
                return View();
            }

            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = string.Format("{0} {1:yyyy-MM-dd HH:mm:ss} - {2:yyyy-MM-dd HH:mm:ss}", CRResources.Resources.VotingPeriodIs, vb.VotingOpenFrom, vb.VotingOpenTill), SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }
   
        /// <summary>
        /// Online voting step 1: Recevie voter ID data input (Captcha validation required)
        /// </summary> 
        [HttpPost, CaptchaVerify("Captcha is not valid")]
        public ActionResult StepOne(StepOneInputModel userInput)
        {
            // List of validation errors
            List<string> errorMessages = new List<string>();

            // If all required fields are filled
            if (ModelState.IsValid)
            {
                // Create a Voting Attempt
                Attempt attempt = new Attempt();
                bool lengthCheck = false;

                try
                {
                    // Transpose user input to data fields of a Voting Attempt
                    attempt.ID_Number = DataUtilities.GetSaltedHash(userInput.BIRPart1);
                    attempt.ID_Number_CD = userInput.BIRPart1CD;
                    attempt.Date_of_Issue = userInput.BIRPart2.Substring(0, 6);
                    attempt.Date_of_Issue_CD = userInput.BIRPart2[6].ToString();
                    attempt.Date_of_Birth = userInput.BIRPart3.Substring(0, 6);
                    attempt.Date_of_Birth_CD = userInput.BIRPart3[6].ToString();
                    attempt.Phone = userInput.Phone;
                    attempt.Created = DateTime.Now;
                    attempt.Attempt_ID = vb.GenerateOnlineAttemptID();

                    lengthCheck = true;
                }
                catch // Incorrect data length in substringing operation would result in an exception.  In his case, lengthCheck will always be False.
                {
                    errorMessages.Add(CRResources.Resources.ErrorMsg_InvalidLength);
                }

                // If there is no data length issue (no exception rasied above)
                if (lengthCheck)
                {
                    // Check the format validity of Macau ID card number, ID card issue date, date of birth and phone number
                    var idResult = DataUtilities.IsIDCardNumberValid(userInput.BIRPart1Whole);
                    var issueDateResult = DataUtilities.IsDateValid(userInput.BIRPart2);
                    var dobResult = DataUtilities.IsDateValid(userInput.BIRPart3);
                    var phoneResult = DataUtilities.IsPhoneValid(userInput.Phone);
                    
                    // Proceed if Macau ID number, ID card issue date and date of birth are valid (in correct format)
                    if ((idResult == DataUtilities.GeneralValidationResult.Validated) &&
                        (issueDateResult == DataUtilities.GeneralValidationResult.Validated) &&
                        (dobResult == DataUtilities.GeneralValidationResult.Validated))
                    {
                        // If the format of the phone number is correct
                        if (phoneResult)
                        {
                            // Write the hashed Macau ID number to the Voting Attempt
                            string hashedID = DataUtilities.GetSaltedHash(userInput.BIRPart1);

                            // The time one day (24 hours) earlier
                            DateTime attemptsConcernedFrom = DateTime.Now.AddDays(-1);

                            // Check the Voting Attempts of the same Macau ID number and of the same phone number in the last 24 hours
                            var sameIDAttemptsQuery = vb.db.Attempts.Where(a => a.ID_Number == hashedID && a.Created > attemptsConcernedFrom);
                            var phoneAttemptsQuery = vb.db.Attempts.Where(a => a.Phone == attempt.Phone && a.Created > attemptsConcernedFrom);

                            // Refuse the voter if more than 5 attempts in the last 24 hours
                            // In other words, allow the voter (by Macau ID number and phone number) to proceed if there are no more than 5 attempts in the last 24 hours 
                            if ((sameIDAttemptsQuery.Count() <= 5)&&(phoneAttemptsQuery.Count()<=5))
                            {
                                // Check whether a vote has not cast a vote and is eligible to vote
                                var mayVoteResult = vb.CheckMayVote(userInput.BIRPart1, DataUtilities.UserInputStringToDate(userInput.BIRPart3), attempt.Phone);

                                // If voter is eligible to vote and has not voted
                                if (mayVoteResult == VotingBusiness.MayVoteResult.MayVote)
                                {
                                    // Proceed to SMS verification
                                    attempt.SMS_Code = DataUtilities.GenerateSMSVerificationCode();
                                    attempt.Uniqueness_Verification_Status_ID = vb.GetCode("Passed", "VerifyVoter").Code_ID;
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "+Voter verified"));

                                    // Write the Voting Attempt to the database
                                    vb.db.Attempts.Add(attempt);
                                    vb.db.SaveChanges();

                                    // Send the SMS verification code to voter's phone number
                                    SendSMS(attempt.Phone, string.Format("{0} ({1})", attempt.SMS_Code, CRResources.Resources.SMS_CR_Code));

                                    return RedirectToAction("StepTwo", new { id = attempt.Attempt_ID });
                                }
                                else if ((mayVoteResult == VotingBusiness.MayVoteResult.VotedAlready)||(mayVoteResult == VotingBusiness.MayVoteResult.MayOverridePreviousVote))
                                {
                                    // Refuse the voter if a vote has been cast under the same Macau ID number
                                    // Also, on the online voting channel, overriding a prevoius vote is NOT supported
                                    attempt.Uniqueness_Verification_Status_ID = vb.GetCode("TooMany", "VerifyVoter").Code_ID;
                                    errorMessages.Add(CRResources.Resources.ErrorMsg_AlreadyVoted);
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Voter voted already"));
                                }
                                else if (mayVoteResult == VotingBusiness.MayVoteResult.Underaged)
                                {
                                    // Refuse the voter if not of an eligible age to vote (below 16)
                                    attempt.Uniqueness_Verification_Status_ID = vb.GetCode("IDCard", "VerifyVoter").Code_ID;
                                    errorMessages.Add(CRResources.Resources.ErrorMsg_Underaged);
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Underaged"));
                                }
                            }
                            else
                            {
                                // Refuse the voter if the same Macau ID number or the same phone number has been used to create a Voting Attempt (as to receive SMS verification code)
                                attempt.Uniqueness_Verification_Status_ID = vb.GetCode("TooMany", "VerifyVoter").Code_ID;
                                errorMessages.Add(CRResources.Resources.ErrorMsg_TooManyTrials);
                                attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Too many trials for same ID card number or phone number"));
                            }
                        }
                        else
                        {
                            // Refuse the voter if the phone number is not in a correct format
                            attempt.Uniqueness_Verification_Status_ID = vb.GetCode("Phone", "VerifyVoter").Code_ID;
                            errorMessages.Add(CRResources.Resources.ErrorMsg_InvalidPhone);
                            attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Invalid phone number"));
                        }
                    }
                    else
                    {
                        // Refuse the voter if an ICAO check digit is incorrect
                        attempt.Uniqueness_Verification_Status_ID = vb.GetCode("IDCard", "VerifyVoter").Code_ID;
                        errorMessages.Add(CRResources.Resources.ErrorMsg_InvalidIDInfo);
                        attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Check digit or format not validated"));
                    }

                    vb.db.Attempts.Add(attempt);
                    vb.db.SaveChanges();
                }
            }
            else
            {
                errorMessages.Add(CRResources.Resources.ErrorMsg_Captcha);
            }

            ViewBag.ErrorMessages = errorMessages;

            return View();
        }

        /// <summary>
        /// Online voting step 2: SMS code input
        /// </summary> 
        [HttpGet]
        public ActionResult StepTwo(string id)
        {
            ViewBag.Attempt_ID = id;

            // Proceed only if there is a Voting Attempt of the id in the argument 
            if (vb.db.Attempts.Any(a => a.Attempt_ID == id))
            {
                var attempt = vb.db.Attempts.First(a => a.Attempt_ID == id);

                if ((attempt.Uniqueness_Verification_Status.Name == "Passed")&&(!string.IsNullOrEmpty(attempt.SMS_Code)))
                {
                    // Proceed only if SMS verification is NOT done
                    if (!attempt.Phone_Verification_Status_ID.HasValue)
                    {
                        ViewBag.Phone = attempt.Phone;
                        return View();
                    } 
                }
            }

            // Otherwise, it may be an internal error or a hacking attempt.
            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        /// <summary>
        /// Online voting step 2: Validation of SMS code input
        /// </summary> 
        [HttpPost]
        public ActionResult StepTwo(StepTwoInputModel input)
        {
            var errorMessageList = new List<string>();

            if (vb.db.Attempts.Any(a => a.Attempt_ID == input.Trial_ID))
            {
                var attempt = vb.db.Attempts.First(a => a.Attempt_ID == input.Trial_ID);

                if ((attempt.Uniqueness_Verification_Status.Name == "Passed")&&(!attempt.Phone_Verification_Status_ID.HasValue))
                {
                    if (!string.IsNullOrEmpty(input.SMS_Code))
                    {
                        // The SMS code expires in one hour.
                        // Proceed only if the code is still valid 
                        if (attempt.Created > DateTime.Now.AddHours(-1))
                        {
                            // Proceed only if no more than 5 wrong inputs of SMS code on this Voting Attempt
                            if (attempt.Attempt_Record.Count(ar => ar.Action == "StepTwo" && ar.Data.StartsWith("-")) <= 5)
                            {
                                // Check if the SMS code is correct
                                if (attempt.SMS_Code == input.SMS_Code.Trim())
                                {
                                    // Update the Voting Attempt record on the database
                                    attempt.Phone_Verification_Status_ID = vb.GetCode("Passed", "VerifyPhone").Code_ID;
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "+SMS code verified"));
                                    vb.db.SaveChanges();

                                    // Present the ballot paper to the voter
                                    return RedirectToAction("BallotPaper", new { id = attempt.Attempt_ID });
                                }
                                else
                                {   // Refuse the voter if the SMS code is wrong
                                    errorMessageList.Add(@CRResources.Resources.Invalid_SMS_Code);
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Invalid SMS code"));
                                }
                            }
                            else
                            {
                                // Refuse the voter if there were 5 or more times of providing a wrong SMS code on this Voting Attempt
                                errorMessageList.Add(@CRResources.Resources.TooMany_SMS_Trials);
                                attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Too many SMS trials"));
                            }
                        }
                        else
                        {
                            // Refuse the voter if the SMS code was sent to the phone more than an hour ago
                            errorMessageList.Add(@CRResources.Resources.Expired_SMS_Code);
                            attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Expired SMS code"));
                        }
                    }
                    else
                    {
                        // Refuse the voter if the SMS code is empty
                        errorMessageList.Add(@CRResources.Resources.Invalid_SMS_Code);
                        attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Invalid SMS code"));
                    }
                }
                else
                {
                    // The Voting Attempt record could not be fetched by the id - an internal error or hacking attempt.
                    errorMessageList.Add(@CRResources.Resources.Invalid_Operation);
                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Invalid operation"));
                }

                ViewBag.Attempt_ID = attempt.Attempt_ID;
                ViewBag.Phone = attempt.Phone;
                ViewBag.ErrorMessages = errorMessageList;
                vb.db.SaveChanges();

                return View();
            }

            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        /// <summary>
        /// Online voting step 3: Present the the ballot paper
        /// </summary> 
        [HttpGet]
        public ActionResult BallotPaper(string id)
        {
            // Proceed only if there is a Voting Attempt of the id in the argument
            if (vb.db.Attempts.Any(a => a.Attempt_ID == id))
            {
                var attempt = vb.db.Attempts.First(a => a.Attempt_ID == id);

                // Proceed only if SMS verification is done
                if ((attempt.Uniqueness_Verification_Status.Name == "Passed")&&(attempt.Phone_Verification_Status.Name == "Passed")&&(!attempt.Voter_ID.HasValue))
                {
                    ViewBag.Attempt_ID = id;
                    return View();
                }
            }

            // Otherwise, it may be an internal error or a hacking attempt.
            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        /// <summary>
        /// Online voting step 3: Receive the the ballot paper
        /// </summary>
        [HttpPost]
        public ActionResult CastTheBallot(BallotPaperInputModel userInput)
        { 
            // It is important to repeat the checks from step 1 before creating a final vote record on the database

            // Check if the vote is still open
            if (vb.IsNowDuringVotingPeriod)
            {
                // Check if the Voting Attempt of the id sent from the voter session exists
                if (vb.db.Attempts.Any(a => a.Attempt_ID == userInput.Trial_ID))
                {
                    // Fetch the Voting Attept record
                    var attempt = vb.db.Attempts.First(a => a.Attempt_ID == userInput.Trial_ID);

                    // Check the results of validations done in step 1 and step 2
                    if ((attempt.Uniqueness_Verification_Status.Name == "Passed") && (attempt.Phone_Verification_Status.Name == "Passed") && (!attempt.Voter_ID.HasValue))
                    {
                        // Check if the voter has not vote at this moment and is eligilble to vote
                        var mayVoteResult = vb.CheckMayVote(attempt.ID_Number, DataUtilities.UserInputStringToDate(attempt.Date_of_Birth), attempt.Phone, true);

                        // Proceed if all checks are passed
                        // Important note: On this (online voting) channel, VotingBusiness.MayVoteResult.MayOverridePreviousVote is considerd NOT eligilble to vote.
                        if (mayVoteResult == VotingBusiness.MayVoteResult.MayVote)
                        {
                            // Record the vote
                            var votingResult = vb.Vote(attempt.ID_Number, DataUtilities.UserInputStringToDate(attempt.Date_of_Birth), attempt.Phone, userInput.MotionOne[0].ToString(), userInput.MotionTwo[0].ToString(), false, true);
                            
                            if (votingResult.Voted)
                            {
                                // Redirect to the thank you page
                                return RedirectToAction("ThankYou");
                            }
                        }
                    }
                }
            }

            // In all other cases of unsuccessful vote, show the error page.
            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        /// <summary>
        /// Create a Voting Attempt record
        /// </summary> 
        private Attempt_Record CreateAttemptRecord(string Action, string Data)
        {
            return new Attempt_Record() { Action = Action, Data = Data, DateTime = DateTime.Now, IP_Address = Request.UserHostAddress, User_Agent = Request.UserAgent  };
        }

        /// <summary>
        /// Send a SMS verification code to a phone number
        /// </summary> 
        private string SendSMS(string Phone, string Message)
        {
            try
            {
                WebClient client = new WebClient();
                return client.DownloadString(string.Format("{0}Phone={1}&Message={2}",System.Configuration.ConfigurationManager.AppSettings["SMSUrl"], Phone, Message));
            }
            catch { }

            return string.Empty;
        }


        /// <summary>
        /// Switch to Chinese interface
        /// </summary>
        public ActionResult Chinese()
        {
            CultureAttribute.SavePreferredCulture(HttpContext.Response, "zh-MO");
            return RedirectToAction("Statement", "Home");
        }

        /// <summary>
        /// Switch to English interface
        /// </summary>
        public ActionResult English()
        {
            CultureAttribute.SavePreferredCulture(HttpContext.Response, "en-GB");
            return RedirectToAction("Statement", "Home");
        }

        /// <summary>
        /// Switch to Portuguse interface
        /// </summary>
        public ActionResult Portuguese()
        {
            CultureAttribute.SavePreferredCulture(HttpContext.Response, "pt-PT");
            return RedirectToAction("Statement", "Home");
        }

    }
}