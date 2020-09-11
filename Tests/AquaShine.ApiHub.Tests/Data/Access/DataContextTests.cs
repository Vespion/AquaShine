using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Access;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace AquaShine.ApiHub.Tests.Data.Access
{
    public class DataContextTests
    {
        private readonly Entrant _entrant = new Entrant
        {
            Email = "jonathon.spice@gmail.com",
            Address = new Address
            {
                City = "Sheffield",
                Region = "South Yorkshire",
                PostalCode = "S61rt",
                Address1 = "5 Eastgate",
                Country = "GB"
            },
            BioGender = Gender.Female,
            EventbriteId = "1428494963",
            RowKey = 1.ToString("X"),
            PartitionKey = "A",
            Name = "Jonathon Spice",
            Details = new Submission
            {
                Locked = false,
                Show = false,
                Verified = false
            }
        };

        private readonly DataContext _context;
        private readonly CloudTableClient client;

        public DataContextTests()
        {
            var connString =
                @"AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
            var account = CloudStorageAccount.Parse(connString);
            client = account.CreateCloudTableClient();
            client.GetTableReference("TEST-entrants").DeleteIfExists();
            _context = new DataContext(client, Microsoft.Azure.Storage.CloudStorageAccount.Parse(connString));
        }

        [Fact]
        public async Task CreatesSuccessfully()
        {
            await _context.Create(_entrant);
        }

        [Fact]
        public Task CreateThrowsWhenNull()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(() => _context.Create(null));
        }

        [Fact]
        public async Task ReadResponseEqualToInput()
        {
            await _context.Create(_entrant);
            var readBack = await _context.FindById(_entrant.RowKey);
            _entrant.ETag = null;
            _entrant.Timestamp = DateTimeOffset.MinValue;
            readBack.ETag = null;
            readBack.Timestamp = DateTimeOffset.MinValue;
            Assert.Equal(JsonSerializer.Serialize(_entrant), JsonSerializer.Serialize(readBack));
        }

        [Fact]
        public Task ReadThrowsWhenNegative()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(() => _context.FindById(null));
        }

        [Fact]
        public async Task ModelCanBeMerged()
        {
            await _context.Create(_entrant);
            _entrant.Name = "NEW_NAME";
            var storeEntrant = await _context.MergeWithStore(_entrant);
            Assert.Equal(_entrant.Name, storeEntrant.Name);
            var loadedEntrant = await _context.FindById(_entrant.RowKey);
            Assert.Equal(_entrant.Name, loadedEntrant?.Name);
        }

        [Fact]
        public async Task FindResponseByEventbriteId()
        {
            for (var i = 0; i <= 14; i++)
            {
                _entrant.RowKey = i.ToString("X");
                _entrant.EventbriteId += i.ToString();
                await _context.Create(_entrant);
            }

            var readBack = await _context.FindByEventbriteId(_entrant.EventbriteId);
            _entrant.ETag = null;
            _entrant.Timestamp = DateTimeOffset.MinValue;
            readBack.ETag = null;
            readBack.Timestamp = DateTimeOffset.MinValue;
            Assert.Equal(JsonSerializer.Serialize(_entrant), JsonSerializer.Serialize(readBack));
        }

        [Fact]
        public Task EventbriteReadThrowsWhenNull()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(() => _context.FindByEventbriteId(null));
        }

        [Fact]
        public async Task FetchOrderedListOfEntrantsBySubmission()
        {
            var list = new List<Entrant>();
            for (var i = 0; i <= 8; i++)
            {
                var clonedEntrant = _entrant.Clone();
                clonedEntrant.RowKey = i.ToString("X");
                clonedEntrant.EventbriteId += i.ToString();
                clonedEntrant.Details = new Submission
                {
                    Locked = true,
                    Show = false,
                    TimeToComplete = TimeSpan.FromHours(2.5 + i),
                    Verified = i % 2 != 0,
                    VerifyingImgUrl = new Uri("https://example.com/")
                };
                list.Add(clonedEntrant);
            }

            list.Shuffle();

            foreach (var entrant in list)
            {
                await _context.Create(entrant);
            }
            var readBack = await _context.FetchSubmissionOrderedList(5, 0, null);
            foreach (var entrant in readBack)
            {
                var listEntrant = list.Find(x => x.RowKey == entrant.RowKey);
                Assert.NotNull(listEntrant);
                listEntrant.ETag = null;
                listEntrant.Timestamp = DateTimeOffset.MinValue;
                entrant.ETag = null;
                entrant.Timestamp = DateTimeOffset.MinValue;
                Assert.Equal(JsonSerializer.Serialize(listEntrant), JsonSerializer.Serialize(entrant));
            }
        }

        [Fact]
        public async Task FetchOrderedListOfEntrantsBySubmissionVerifiedOnly()
        {
            var list = new List<Entrant>();
            for (var i = 0; i <= 8; i++)
            {
                var clonedEntrant = _entrant.Clone();
                clonedEntrant.RowKey = i.ToString("X");
                clonedEntrant.EventbriteId += i.ToString();
                clonedEntrant.Details = new Submission
                {
                    Locked = true,
                    Show = false,
                    TimeToComplete = TimeSpan.FromHours(2.5 + i),
                    Verified = i % 2 != 0,
                    VerifyingImgUrl = new Uri("https://example.com/")
                };
                list.Add(clonedEntrant);
            }

            list.Shuffle();

            foreach (var entrant in list)
            {
                await _context.Create(entrant);
            }
            var readBack = await _context.FetchSubmissionOrderedList(5, 0, true);
            foreach (var entrant in readBack)
            {
                Assert.True(entrant.Details?.Verified);
                var listEntrant = list.Find(x => x.RowKey == entrant.RowKey);
                Assert.NotNull(listEntrant);
                listEntrant.ETag = null;
                listEntrant.Timestamp = DateTimeOffset.MinValue;
                entrant.ETag = null;
                entrant.Timestamp = DateTimeOffset.MinValue;
                Assert.Equal(JsonSerializer.Serialize(listEntrant), JsonSerializer.Serialize(entrant));
            }
        }

        [Fact]
        public async Task FetchOrderedListOfEntrantsBySubmissionUnverifiedOnly()
        {
            var list = new List<Entrant>();
            for (var i = 0; i <= 8; i++)
            {
                var clonedEntrant = _entrant.Clone();
                clonedEntrant.RowKey = i.ToString("X");
                clonedEntrant.EventbriteId += i.ToString();
                clonedEntrant.Details = new Submission
                {
                    Locked = true,
                    Show = false,
                    TimeToComplete = TimeSpan.FromHours(2.5 + i),
                    Verified = i % 2 != 0,
                    VerifyingImgUrl = new Uri("https://example.com/")
                };
                list.Add(clonedEntrant);
            }

            list.Shuffle();

            foreach (var entrant in list)
            {
                await _context.Create(entrant);
            }
            var readBack = await _context.FetchSubmissionOrderedList(5, 0, false);
            foreach (var entrant in readBack)
            {
                Assert.False(entrant.Details?.Verified);
                var listEntrant = list.Find(x => x.RowKey == entrant.RowKey);
                Assert.NotNull(listEntrant);
                listEntrant.ETag = null;
                listEntrant.Timestamp = DateTimeOffset.MinValue;
                entrant.ETag = null;
                entrant.Timestamp = DateTimeOffset.MinValue;
                Assert.Equal(JsonSerializer.Serialize(listEntrant), JsonSerializer.Serialize(entrant));
            }
        }

        [Fact]
        public async Task FetchUnverifiedList()
        {
            var list = new List<Entrant>();
            for (var i = 0; i <= 6; i++)
            {
                var clonedEntrant = _entrant.Clone();
                clonedEntrant.RowKey = i.ToString("X");
                clonedEntrant.EventbriteId += i.ToString();
                clonedEntrant.Details = new Submission
                {
                    Locked = true,
                    Show = false,
                    TimeToComplete = TimeSpan.FromHours(2.5 + i),
                    Verified = i % 2 != 0,
                    VerifyingImgUrl = new Uri("https://example.com/")
                };
                list.Add(clonedEntrant);
            }
            foreach (var entrant in list)
            {
                await _context.Create(entrant);
            }
            var readBack = await _context.FetchList(6, 0, false);
            foreach (var entrant in readBack)
            {
                Assert.False(entrant.Details?.Verified);
            }
        }

        [Fact]
        public async Task FetchVerifiedList()
        {
            var list = new List<Entrant>();
            for (var i = 0; i <= 6; i++)
            {
                var clonedEntrant = _entrant.Clone();
                clonedEntrant.RowKey = i.ToString("X");
                clonedEntrant.EventbriteId += i.ToString();
                clonedEntrant.Details = new Submission
                {
                    Locked = true,
                    Show = false,
                    TimeToComplete = TimeSpan.FromHours(2.5 + i),
                    Verified = i % 2 != 0,
                    VerifyingImgUrl = new Uri("https://example.com/")
                };
                list.Add(clonedEntrant);
            }
            foreach (var entrant in list)
            {
                await _context.Create(entrant);
            }
            var readBack = await _context.FetchList(6, 0, true);
            foreach (var entrant in readBack)
            {
                Assert.True(entrant.Details?.Verified);
            }
        }

        [Fact]
        public async Task FetchList()
        {
            var list = new List<Entrant>();
            for (var i = 0; i <= 6; i++)
            {
                var clonedEntrant = _entrant.Clone();
                clonedEntrant.RowKey = i.ToString("X");
                clonedEntrant.EventbriteId += i.ToString();
                clonedEntrant.Details = new Submission
                {
                    Locked = true,
                    Show = false,
                    TimeToComplete = TimeSpan.FromHours(2.5 + i),
                    Verified = i % 2 != 0,
                    VerifyingImgUrl = new Uri("https://example.com/")
                };
                list.Add(clonedEntrant);
            }
            foreach (var entrant in list)
            {
                await _context.Create(entrant);
            }
            var readBack = await _context.FetchList(6, 0);
            foreach (var entrant in readBack)
            {
                var listEntrant = list.Find(x => x.RowKey == entrant.RowKey);
                Assert.NotNull(listEntrant);
                listEntrant.ETag = null;
                listEntrant.Timestamp = DateTimeOffset.MinValue;
                entrant.ETag = null;
                entrant.Timestamp = DateTimeOffset.MinValue;
                Assert.Equal(JsonSerializer.Serialize(listEntrant), JsonSerializer.Serialize(entrant));
            }
        }
    }
}
