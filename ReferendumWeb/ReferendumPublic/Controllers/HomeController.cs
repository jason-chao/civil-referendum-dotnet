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
    public class HomeController : Controller
    {
        VotingBusiness vb = new VotingBusiness();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ThankYou()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CastTheBallot(BallotPaperInputModel userInput)
        {            
            if (vb.IsNowDuringVotingPeriod)
            {
                if (vb.db.Attempts.Any(a => a.Attempt_ID == userInput.Trial_ID))
                {
                    var attempt = vb.db.Attempts.First(a => a.Attempt_ID == userInput.Trial_ID);

                    if ((attempt.Uniqueness_Verification_Status.Name == "Passed") && (attempt.Phone_Verification_Status.Name == "Passed") && (!attempt.Voter_ID.HasValue))
                    {
                        var mayVoteResult = vb.CheckMayVote(attempt.ID_Number, DataUtilities.UserInputStringToDate(attempt.Date_of_Birth), attempt.Phone, true);

                        if (mayVoteResult == VotingBusiness.MayVoteResult.MayVote)
                        {
                            var votingResult = vb.Vote(attempt.ID_Number, DataUtilities.UserInputStringToDate(attempt.Date_of_Birth), attempt.Phone, userInput.MotionOne[0].ToString(), userInput.MotionTwo[0].ToString(), false, true);

                            if (votingResult.Voted)
                            {
                                return RedirectToAction("ThankYou");
                            }
                        }
                    }
                }
            }

            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        [HttpGet]
        public ActionResult BallotPaper(string id)
        {            
            if (vb.db.Attempts.Any(a => a.Attempt_ID == id))
            {
                var attempt = vb.db.Attempts.First(a => a.Attempt_ID == id);

                if ((attempt.Uniqueness_Verification_Status.Name == "Passed")&&(attempt.Phone_Verification_Status.Name == "Passed")&&(!attempt.Voter_ID.HasValue))
                {
                    ViewBag.Attempt_ID = id;
                    return View();
                }
            }

            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        [HttpGet]
        public ActionResult StepTwo(string id)
        {
            ViewBag.Attempt_ID = id;

            if (vb.db.Attempts.Any(a => a.Attempt_ID == id))
            {
                var attempt = vb.db.Attempts.First(a => a.Attempt_ID == id);

                if ((attempt.Uniqueness_Verification_Status.Name == "Passed")&&(!string.IsNullOrEmpty(attempt.SMS_Code)))
                {
                    if (!attempt.Phone_Verification_Status_ID.HasValue)
                    {
                        ViewBag.Phone = attempt.Phone;
                        return View();
                    }
                }
            }

            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = CRResources.Resources.ContactSupport, SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

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
                        if (attempt.Created > DateTime.Now.AddHours(-1))
                        {
                            if (attempt.Attempt_Record.Count(ar => ar.Action == "StepTwo" && ar.Data.StartsWith("-")) <= 5)
                            {
                                if (attempt.SMS_Code == input.SMS_Code.Trim())
                                {
                                    attempt.Phone_Verification_Status_ID = vb.GetCode("Passed", "VerifyPhone").Code_ID;
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "+SMS code verified"));
                                    vb.db.SaveChanges();

                                    return RedirectToAction("BallotPaper", new { id = attempt.Attempt_ID });
                                }
                                else
                                {
                                    errorMessageList.Add(@CRResources.Resources.Invalid_SMS_Code);
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Invalid SMS code"));
                                }
                            }
                            else
                            {
                                errorMessageList.Add(@CRResources.Resources.TooMany_SMS_Trials);
                                attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Too many SMS trials"));
                            }
                        }
                        else
                        {
                            errorMessageList.Add(@CRResources.Resources.Expired_SMS_Code);
                            attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Expired SMS code"));
                        }
                    }
                    else
                    {
                        errorMessageList.Add(@CRResources.Resources.Invalid_SMS_Code);
                        attempt.Attempt_Record.Add(CreateAttemptRecord("StepTwo", "-Invalid SMS code"));
                    }
                }
                else
                {
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

        [HttpPost, CaptchaVerify("Captcha is not valid")]
        public ActionResult StepOne(StepOneInputModel userInput)
        {
            List<string> errorMessages = new List<string>();

            if (ModelState.IsValid)
            {
                Attempt attempt = new Attempt();
                bool lengthCheck = false;

                try
                {
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
                catch
                {
                    errorMessages.Add(CRResources.Resources.ErrorMsg_InvalidLength);
                }

                if (lengthCheck)
                {
                    var idResult = DataUtilities.IsIDCardNumberValid(userInput.BIRPart1Whole);
                    var issueDateResult = DataUtilities.IsDateValid(userInput.BIRPart2);
                    var dobResult = DataUtilities.IsDateValid(userInput.BIRPart3);
                    var phoneResult = DataUtilities.IsPhoneValid(userInput.Phone);
                    
                    if ((idResult == DataUtilities.GeneralValidationResult.Validated) &&
                        (issueDateResult == DataUtilities.GeneralValidationResult.Validated) &&
                        (dobResult == DataUtilities.GeneralValidationResult.Validated))
                    {
                        if (phoneResult)
                        {
                            string hashedID = DataUtilities.GetSaltedHash(userInput.BIRPart1);
                            DateTime attemptsConcernedFrom = DateTime.Now.AddDays(-1);
                            var sameIDAttemptsQuery = vb.db.Attempts.Where(a => a.ID_Number == hashedID && a.Created > attemptsConcernedFrom);
                            var phoneAttemptsQuery = vb.db.Attempts.Where(a => a.Phone == attempt.Phone && a.Created > attemptsConcernedFrom);

                            if ((sameIDAttemptsQuery.Count() <= 5)&&(phoneAttemptsQuery.Count()<=5))
                            {
                                var mayVoteResult = vb.CheckMayVote(userInput.BIRPart1, DataUtilities.UserInputStringToDate(userInput.BIRPart3), attempt.Phone);

                                if (mayVoteResult == VotingBusiness.MayVoteResult.MayVote)
                                {
                                    attempt.SMS_Code = DataUtilities.GenerateSMSVerificationCode();
                                    attempt.Uniqueness_Verification_Status_ID = vb.GetCode("Passed", "VerifyVoter").Code_ID;
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "+Voter verified"));
                                    vb.db.Attempts.Add(attempt);
                                    vb.db.SaveChanges();

                                    SendSMS(attempt.Phone, string.Format("{0} ({1})", attempt.SMS_Code, CRResources.Resources.SMS_CR_Code));

                                    return RedirectToAction("StepTwo", new { id = attempt.Attempt_ID });
                                }
                                else if ((mayVoteResult == VotingBusiness.MayVoteResult.VotedAlready)||(mayVoteResult == VotingBusiness.MayVoteResult.MayOverridePreviousVote))
                                {
                                    attempt.Uniqueness_Verification_Status_ID = vb.GetCode("TooMany", "VerifyVoter").Code_ID;
                                    errorMessages.Add(CRResources.Resources.ErrorMsg_AlreadyVoted);
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Voter voted already"));
                                }
                                else if (mayVoteResult == VotingBusiness.MayVoteResult.Underaged)
                                {
                                    attempt.Uniqueness_Verification_Status_ID = vb.GetCode("IDCard", "VerifyVoter").Code_ID;
                                    errorMessages.Add(CRResources.Resources.ErrorMsg_Underaged);
                                    attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Underaged"));
                                }
                            }
                            else
                            {
                                attempt.Uniqueness_Verification_Status_ID = vb.GetCode("TooMany", "VerifyVoter").Code_ID;
                                errorMessages.Add(CRResources.Resources.ErrorMsg_TooManyTrials);
                                attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Too many trials for same ID card number or phone number"));
                            }
                        }
                        else
                        {
                            attempt.Uniqueness_Verification_Status_ID = vb.GetCode("Phone", "VerifyVoter").Code_ID;
                            errorMessages.Add(CRResources.Resources.ErrorMsg_InvalidPhone);
                            attempt.Attempt_Record.Add(CreateAttemptRecord("StepOne", "-Invalid phone number"));
                        }
                    }
                    else
                    {
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

        private Attempt_Record CreateAttemptRecord(string Action, string Data)
        {
            return new Attempt_Record() { Action = Action, Data = Data, DateTime = DateTime.Now, IP_Address = Request.UserHostAddress, User_Agent = Request.UserAgent  };
        }

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

        [HttpGet]
        public ActionResult StepOne()
        {
            if (vb.IsNowDuringVotingPeriod)
            {
                ViewBag.ErrorMessages = new List<string>();
                return View();
            }

            var msgModel = new MessageViewModel() { Message_Type = MessageViewModel.MessageType.Error, Message_Title = CRResources.Resources.Invalid_Operation, Message = string.Format("{0} {1:yyyy-MM-dd HH:mm:ss} - {2:yyyy-MM-dd HH:mm:ss}", CRResources.Resources.VotingPeriodIs, vb.VotingOpenFrom, vb.VotingOpenTill), SuggestedAction_GoHome = true };
            return View("Message", msgModel);
        }

        public ActionResult Statement()
        {
            return View();
        }

        public ActionResult Chinese()
        {
            CultureAttribute.SavePreferredCulture(HttpContext.Response, "zh-MO");
            return RedirectToAction("Statement", "Home");
        }

        public ActionResult English()
        {
            CultureAttribute.SavePreferredCulture(HttpContext.Response, "en-GB");
            return RedirectToAction("Statement", "Home");
        }

        public ActionResult Portuguese()
        {
            CultureAttribute.SavePreferredCulture(HttpContext.Response, "pt-PT");
            return RedirectToAction("Statement", "Home");
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}