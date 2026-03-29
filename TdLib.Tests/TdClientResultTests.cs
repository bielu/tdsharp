// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using System.Threading.Tasks;
using TdLib.TdApi;
using Xunit;

namespace TdLib.Tests
{
    public class TdClientResultTests
    {
        [Fact]
        public async Task Execute_WhenEmptyCallPassed_ReturnsOk()
        {
            using (var tdCLient = new TdClient())
            {

                var client = new TestCallEmptyClient();

                client.Initialise(tdCLient);
                var result = await client.TestCallEmptyAsync();

                Assert.NotNull(result);
            }
        }

        [Fact]
        public async Task Execute_WhenStringPassed_ReturnsTheSameString()
        {
               using (var tdCLient = new TdClient())
            {

                var client = new TestCallStringClient();
                client.Initialise(tdCLient);

                var arg = "test";
                var result = await client.TestCallStringAsync(arg);

                Assert.NotNull(result);
                Assert.NotNull(result.Value);
                Assert.Equal(arg, result.Value);
            }
        }

        [Fact]
        public async Task Execute_WhenByteArrayPassed_ReturnsTheSameByteArray()
        {
            using (var tdCLient = new TdClient())
            {

                var client = new TestCallBytesClient();
                client.Initialise(tdCLient);
                var arg = new byte[] {1, 2, 3};
                var result = await client.TestCallBytesAsync(arg);

                Assert.NotNull(result);
                Assert.NotNull(result.Value);
                Assert.Equal(arg, result.Value);
            }
        }

        [Fact]
        public async Task Execute_WhenIntArrayPassed_ReturnsTheSameArray()
        {
            using (var tdCLient = new TdClient())
            {

                var client = new TestCallVectorIntClient();
                client.Initialise(tdCLient);
                var arg = new [] {1, 2, 3};
                var result = await client.TestCallVectorIntAsync(arg);

                Assert.NotNull(result);
                Assert.NotNull(result.Value);
                Assert.Equal(arg, result.Value);
            }
        }

        [Fact]
        public async Task Execute_WhenStringArrayPassed_ReturnsTheSameArray()
        {
            using (var tdCLient = new TdClient())
            {

                var client = new TestCallVectorStringClient();
                client.Initialise(tdCLient);
                var arg = new[] {"foo", "bar"};
                var result = await client.TestCallVectorStringAsync(arg);

                Assert.NotNull(result);
                Assert.NotNull(result.Value);
                Assert.Equal(arg, result.Value);
            }
        }
    }
}
