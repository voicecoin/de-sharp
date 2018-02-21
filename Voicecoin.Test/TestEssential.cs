using System;
using System.Collections.Generic;
using System.Text;

namespace Voicecoin.Test
{
    public class TestEssential
    {
        protected string dialogApiKey = "";
        protected string sessionId;
        protected string awsAccessKey = "";
        protected string awsSecretKey = "";
        protected string recordsBaseDir = @"C:\Voicecoin.WebStarter\wwwroot";

        public TestEssential()
        {
            sessionId = Guid.NewGuid().ToString();
        }
    }
}
