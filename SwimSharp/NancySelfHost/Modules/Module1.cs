using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using NancySelfHost.IOC;
using System;
using System.Threading.Tasks;

namespace NancySelfHost.Modules
{
    public enum MemberState
    {
        Alive = 1,
        Dead = 2,
        Suspect = 3
    }

    public class StateWrapper {

    }


    public class ResourceModule : NancyModule
    {
        private ISmtpService smtpService;

        public ResourceModule(ISmtpService smtpService) : base("/products")
        {
            this.smtpService = smtpService;

            // would capture routes to /products/list sent as a synchronous GET request
            Get("/list", parameters => {
                return Response.AsJson<Person>( new Person { Age = 1, Name = "finbar" });
            });


            Get("/listasync", async (paramaters) => {
                var person = await Task.FromResult<Person>(new Person { Age = 1, Name = "finbar" });
                return Response.AsJson<Person>(person);
            });

            Post("/postpersonasync", async (args, ct) => {

                Person postedPeron = this.Bind<Person>(); //model binding!

                return Response.AsJson<Person>(postedPeron);
            });

            Post("/enumIn", async (args, ct) => {

                var body = Request.Body.AsString();
                var memberState = Enum.Parse<MemberState>(body);

                return Response.AsText(memberState.ToString());
            });
        }
    }

    public class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}
