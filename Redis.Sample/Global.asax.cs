using System;
using System.Configuration;
using SignalR;
using SignalR.Redis;

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

            var keys = new[] { "topic1", "topic2" };

            GlobalHost.DependencyResolver.UseRedis(server, Int32.Parse(port), password, keys);
        }
    }
}