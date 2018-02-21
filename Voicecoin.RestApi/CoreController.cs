using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voicecoin.RestApi
{
    [Produces("application/json", "application/xml")]
    [Route("[controller]/[action]")]
    public class CoreController : ControllerBase
    {
        protected Database dc { get; set; }

        public CoreController()
        {
            dc = new DefaultDataContextLoader().GetDefaultDc();
        }

        protected String CurrentUserId
        {
            get
            {
                return this.User.Claims.FirstOrDefault(x => x.Type.Equals("UserId"))?.Value;
            }
        }
    }
}
