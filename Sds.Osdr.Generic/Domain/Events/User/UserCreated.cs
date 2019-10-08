using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Users
{
    public class UserCreated : IUserEvent
	{
        public readonly string DisplayName;
        public readonly string FirstName;
        public readonly string LastName;
        public readonly string LoginName;
        public readonly string Email;
        public readonly string Avatar;
        public readonly string[] Role;

        public UserCreated(Guid id, Guid userId, string firstName, string lastName, string displayName, string loginName, string email, string avatar, string[] role)
        {
            Id = id;
            UserId = userId;
            DisplayName = displayName;
            FirstName = firstName;
            LastName = lastName;
            LoginName = loginName;
            Email = email;
            Avatar = avatar;
            Role = role;
        }

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
    }
}
