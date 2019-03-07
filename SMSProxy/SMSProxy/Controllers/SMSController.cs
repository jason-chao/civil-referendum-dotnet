using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using System.Web;
using System.Collections;
using System.IO;
using System.Text;

namespace SMSProxy.Controllers
{
    ///<summary>
    /// This is a proxy HTTP service for the external SMS service provider.
    /// The origin of SMS requests has to be exposed to the carrier.  For security consideration, text messages are sent through this proxy to obfuscate the services of the online vote.
    ///</summary>
    public class SMSController : ApiController
    {
        ///<summary>
        /// Format the phone nubmer and pass it onto a carrier
        ///</summary>
        [AcceptVerbs("GET", "POST")]
        public IHttpActionResult Send(string Phone, string Message)
        {
            string smsProvider = System.Configuration.ConfigurationManager.AppSettings["SMS_Provider"];

            try
            {
                Convert.ToInt64(Phone);
            }
            catch
            {
                return Ok("Error: Not a phone number");
            }

            if (!Phone.StartsWith("8536"))
            {
                if (Phone.Length == 8)
                {
                    if (Phone.StartsWith("6"))
                        Phone = "853" + Phone;
                    else
                        return Ok("Error: Invalid phone format");
                }
            }
            else if (Phone.StartsWith("8536"))
            {
                if (Phone.Length != 11)
                    return Ok("Error: Invalid phone format");
            }

            if (smsProvider == "Carrier")
            {
                return Ok("Carrier: " + sendViaTheCarrier(Phone, Message));
            }

            return Ok("Error: No message was sent");
        }

        ///<summary>
        /// Send a SMS text message by firing a HTTP GET request to the carrier
        ///</summary>
        private string sendViaTheCarrier(string Phone, string Message)
        {            
            WebClient client = new WebClient();
            string host = System.Configuration.ConfigurationManager.AppSettings["Carrier_Host"];
            string username = System.Configuration.ConfigurationManager.AppSettings["Carrier_Username"];
            string password = System.Configuration.ConfigurationManager.AppSettings["Carrier_Password"];
            string from = System.Configuration.ConfigurationManager.AppSettings["Carrier_From"];

            return client.DownloadString(string.Format("https://{0}/servlet/SendSMS?username={1}&password={2}&from={3}&to={4}&text={5}&dcs=8&locale=utf-8", host, username, password, from, Phone, Message));
        }

    }
}
