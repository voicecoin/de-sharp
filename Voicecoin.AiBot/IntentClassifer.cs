using ApiAiSDK;
using ApiAiSDK.Model;
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

        public AIResponse TextRequest(String sessionId, String userSay, bool writeLog = true)
        {
            if (writeLog)
            {
                Console.WriteLine($"{DateTime.UtcNow} User: {userSay}");
            }

            string apiAiKey = dialogApiKey;

            var config = new AIConfiguration(apiAiKey, SupportedLanguage.English);
            config.SessionId = sessionId;
            ApiAi apiAi = new ApiAi(config);

            var response = apiAi.TextRequest(userSay);

            if (writeLog)
            {
                Console.WriteLine($"{DateTime.UtcNow} Contexts: {String.Join(',', response.Result.Contexts.Select(x => x.Name))} [{response.Result.Metadata.IntentName}]");

                Console.WriteLine($"{DateTime.UtcNow} Bot: {response.Result.Fulfillment.Speech}");
            }

            return response;
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
