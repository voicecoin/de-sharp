using ApiAiSDK;
using ApiAiSDK.Model;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Voicecoin.AiBot
{
    public class IntentClassifer
    {
        private string dialogApiKey;

        public IntentClassifer(string dialogApiKey)
        {
            this.dialogApiKey = dialogApiKey;
        }

        public AIResponse TextRequest(String sessionId, String userSay)
        {
            ConsoleLogger.WriteLine("Customer", $"{userSay}");

            string apiAiKey = dialogApiKey;

            var config = new AIConfiguration(apiAiKey, SupportedLanguage.English);
            config.SessionId = sessionId;
            ApiAi apiAi = new ApiAi(config);

            var aIResponse = apiAi.TextRequest(userSay);

            //Console.WriteLine($"{DateTime.UtcNow} Contexts: {String.Join(',', response.Result.Contexts.Select(x => x.Name))} [{response.Result.Metadata.IntentName}]");

            /*for (int messageIndex = 0; messageIndex < aIResponse.Result.Fulfillment.Messages.Count; messageIndex++)
            {
                var message = JObject.FromObject(aIResponse.Result.Fulfillment.Messages[messageIndex]);
                string type = message["type"].ToString();

                if (type == "0")
                {
                    string speech = message["speech"].ToString();
                    ConsoleLogger.WriteLine("Voice Browser", $"{speech}");
                }
            }*/

            return aIResponse;
        }

        public string ReplaceTokens4Response(JObject jObject, string text)
        {
            var reg = new Regex(@"@\w{3,}");

            reg.Matches(text).ToList().ForEach(token => {
                text = text.Replace(token.Value, jObject[token.Value.Substring(1)].ToString());
            });

            return text;
        }
    }
}
