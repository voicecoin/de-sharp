using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Voicecoin.AiBot;

namespace Voicecoin.Test
{
    [TestClass]
    public class ConversationTest : TestEssential
    {
        [TestMethod]
        public void Test()
        {
            var aIResponse = new IntentClassifer("d018bf12a8a8419797fe3965637389b0").TextRequest(sessionId, "What's your name?");
        }
    }
}
