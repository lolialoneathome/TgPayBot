using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sender.Services
{
    public class FakeSenderService : ISenderService
    {
        public FakeSenderService() {
            Console.WriteLine("ctor");
        }

        public async Task Process(CancellationToken cancellation)
        {
            Console.WriteLine("Process, ok");
            await Task.FromResult(true);
        }
    }
}
