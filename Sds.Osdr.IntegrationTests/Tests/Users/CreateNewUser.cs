using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Serilog;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace Sds.Osdr.IntegrationTests
{
    public class NewUserFixture
    {
        public Guid NewUserId { get; }

        public NewUserFixture(OsdrTestHarness harness)
        {
            NewUserId = harness.CreateUser("Johnny Doe", "Johnny", "Doe", "johnny", "johnny@your-company.com", null, harness.JohnId, {"client_admin"}).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreateNewUser : OsdrTest, IClassFixture<NewUserFixture>
    {
        private Guid NewUserId { get; }

        public CreateNewUser(OsdrTestHarness fixture, ITestOutputHelper output, NewUserFixture initFixture) : base(fixture, output)
        {
            NewUserId = initFixture.NewUserId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task CreateUser_JohnDoe_RegisterNewUser()
        {
            var user = await Session.Get<User>(NewUserId);

            user.Should().NotBeNull();
            user.Should().BeEquivalentTo(new
            {
                Id = NewUserId,
                CreatedBy = JohnId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = JohnId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                FirstName = "Johnny",
                LastName = "Doe",
                DisplayName = "Johnny Doe",
                LoginName = "johnny",
                Email = "johnny@your-company.com",
                Role = new string[] {"client_admin"},
            }, options => options
                .ExcludingMissingMembers()
            );
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task CreateUser_JohnDoe_UserEntity()
        {
            var user = await Session.Get<User>(NewUserId);
            user.Should().NotBeNull();

            var userView = Users.Find(new BsonDocument("_id", NewUserId)).FirstOrDefault() as IDictionary<string, object>;
            userView.Should().NotBeNull();

            userView.Should().EntityShouldBeEquivalentTo(user);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Users)]
        public async Task CreateUser_JohnDoe_UserNode()
        {
            var user = await Session.Get<User>(NewUserId);
            user.Should().NotBeNull();

            var userNode = Nodes.Find(new BsonDocument("_id", NewUserId)).FirstOrDefault() as IDictionary<string, object>;
            userNode.Should().NotBeNull();

            userNode.Should().NodeShouldBeEquivalentTo(user);
        }
    }
}
