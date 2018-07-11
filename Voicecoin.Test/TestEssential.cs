using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Voicecoin.Test
{
    public class TestEssential
    {
        protected Database dc;
        protected string sessionId;
        protected string contentRoot = @"C:\Voicecoin.WebStarter";

        public TestEssential()
        {
            sessionId = Guid.NewGuid().ToString();
        }
    }
}
