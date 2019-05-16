using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiSelfHosted
{
    public interface IWriter
    {
        void Write();
    }

    public class FooWriter : IWriter
    {
        public void Write()
        {

        }
    }
}
