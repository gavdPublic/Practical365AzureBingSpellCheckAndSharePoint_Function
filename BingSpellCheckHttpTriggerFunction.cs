using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Prac365.AzureBingSpellCheck
{
    public static class BingSpellCheckHttpTriggerFunction
    {
        [FunctionName("BingSpellCheckHttpTriggerFunction")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] 
             HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("* AzureBingSpellCheck Function processing one request");

            // Using the Body to get the input parameters
            dynamic bodyData = await req.Content.ReadAsAsync<object>();
            string StringToCheck    = bodyData.strcheck;
            string Market           = bodyData.market;
            string Mode             = bodyData.mode;
            log.LogInformation("* Input BodyString: " + StringToCheck + " - " + Market + " - " + Mode);

            AzureSpellCheckResults myReturn = CheckSpelling(StringToCheck, Market, Mode).Result;
            string myModifiedText = ModifyText(StringToCheck, myReturn);
            log.LogInformation("* Output: " + myModifiedText);

            return req.CreateResponse(HttpStatusCode.OK, myModifiedText);
        }

        static async Task<AzureSpellCheckResults> CheckSpelling(
            string StringToCheck, string Market, string Mode)
        {
            string servEntryPoint = "https://api.cognitive.microsoft.com/bing/v7.0/spellcheck?";
            string servKey = "1c7b01f6f5...8400";

            HttpClient servClient = new HttpClient();
            servClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", servKey);

            HttpResponseMessage servResponse = new HttpResponseMessage();
            string servUri = servEntryPoint + "mkt=" + Market + "&mode=" + Mode;

            List<KeyValuePair<string, string>> servInput = new List<KeyValuePair<string, string>>();
            servInput.Add(new KeyValuePair<string, string>("text", StringToCheck));

            using (FormUrlEncodedContent servContent = new FormUrlEncodedContent(servInput))
            {
                servContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                servResponse = await servClient.PostAsync(servUri, servContent);
            }

            string servResult = await servResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AzureSpellCheckResults>(servResult);
        }

        static string ModifyText(string InputString, AzureSpellCheckResults SpellingResult)
        {
            foreach (Flaggedtoken oneWord in SpellingResult.flaggedTokens)
            {
                InputString = InputString.Replace(oneWord.token, oneWord.suggestions[0].suggestion);
            }

            return InputString;
        }
    }

    public class AzureSpellCheckResults
    {
        public string _type { get; set; }
        public Flaggedtoken[] flaggedTokens { get; set; }
    }

    public class Flaggedtoken
    {
        public int offset { get; set; }
        public string token { get; set; }
        public string type { get; set; }
        public Suggestion[] suggestions { get; set; }
    }

    public class Suggestion
    {
        public string suggestion { get; set; }
        public float score { get; set; }
    }
}
