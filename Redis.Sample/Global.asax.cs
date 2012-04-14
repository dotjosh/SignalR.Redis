using System;
using SignalR.Redis;
using System.Configuration;

namespace Redis.Sample
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Hook up redis
            string server = ConfigurationManager.AppSettings["redis.server"];
            string port = ConfigurationManager.AppSettings["redis.port"];
            string password = ConfigurationManager.AppSettings["redis.password"];

            SignalR.Global.DependencyResolver.UseRedis(server, Int32.Parse(port), password, "SignalR.Redis.Sample");
        }
    }
}