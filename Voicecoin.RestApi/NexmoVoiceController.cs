using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Voicecoin.RestApi
{
    public class NexmoVoiceController : CoreController
    {
        private IHostingEnvironment env;

        public NexmoVoiceController(IHostingEnvironment hostingEnvironment)
        {
            env = hostingEnvironment;
        }

        [Route("/ncco")]
        public object NCCOHandler(string message)
        {
            string ncco = System.IO.File.ReadAllText(env.ContentRootPath + "\\App_Data\\ncco.json");
            return JsonConvert.DeserializeObject(ncco);
        }
    }
}
