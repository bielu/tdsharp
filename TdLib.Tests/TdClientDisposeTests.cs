// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Threading.Tasks;
using TdLib.TdApi;
using Xunit;

namespace TdLib.Tests
{
    public class TdClientDisposeTests
    {
        [Fact]
        public async Task Dispose_WhenCalled_DoesNotThrowException()
        {
            var tdCLient = new TdClient();

            var client = new TestCallEmptyClient();
            client.Initialise(tdCLient);
            // do some stuff
            await client.TestCallEmptyAsync();

            // dispose client from other thread
            await Task.Run(() => tdCLient.Dispose());
        }

        [Fact]
        public async Task Dispose_WhenCalledTwice_DoesNotThrowException()
        {
            var tdCLient = new TdClient();

            var client = new TestCallEmptyClient();

            client.Initialise(tdCLient);
            // do some stuff
            await client.TestCallEmptyAsync();

            // dispose client from other thread
            await Task.Run(() => tdCLient.Dispose());

            // and then from main thread
            tdCLient.Dispose();
        }

        [Fact]
        public async Task Dispose_WhenCalledOnDifferentClients_DoesNotThrowException()
        {
            using (var tdclient1 = new TdClient())
            using (var tdclient2 = new TdClient())
            using (var tdclient3 = new TdClient())
            {
                var client1 = new TestCallEmptyClient();

                client1.Initialise(tdclient1);
                var client2 = new TestCallEmptyClient();

                client2.Initialise(tdclient2);
                var client3 = new TestCallEmptyClient();

                client3.Initialise(tdclient3);
                // do some stuff
                await client1.TestCallEmptyAsync();
                await client2.TestCallEmptyAsync();
                await client3.TestCallEmptyAsync();
            }
        }
    }
}
