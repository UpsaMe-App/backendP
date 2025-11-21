using System;

namespace UpsaMe_API.Models
{
    public class UserConnection
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public DateTime ConnectedAtUtc { get; set; }
        public DateTime LastActivityUtc { get; set; }

        public bool IsOnline =>
            (DateTime.UtcNow - LastActivityUtc).TotalMinutes < 5;
    }
}