using System;
using System.Collections.Concurrent;
using Discord;

namespace CountryBot.Utilities;

public interface IThrottleService
{
    TimeSpan GetThrottleReset(ThrottleBy throttleBy, int requestsLimit, int intervalSeconds, IUser us, IGuild guild,
        string command = null);
    bool CheckThrottle(ThrottleBy throttleBy, int limit, int intervalSeconds, IUser user,
        IGuild guild, string command = null);
}

public class ThrottleService : IThrottleService
{
    class ThrottleInfo
    {
        public DateTime FirstRequestTime { get; set; }
        public int RequestCount { get; set; }
    }

    class ThrottleKey
    {
        public ulong ObjectId { get; }
        public string Command { get; }

        public ThrottleKey(ulong objectId, string command)
        {
            ObjectId = objectId;
            Command = command;
        }

        protected bool Equals(ThrottleKey other)
        {
            return ObjectId == other.ObjectId && Command == other.Command;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ThrottleKey)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ObjectId, Command);
        }
    }
    private ConcurrentDictionary<ThrottleKey, ThrottleInfo> UserThrottles { get; set; } = new();
    private ConcurrentDictionary<ThrottleKey, ThrottleInfo> GuildThrottles { get; set; } = new();
    public TimeSpan GetThrottleReset(ThrottleBy throttleBy, int requestsLimit, int intervalSeconds, IUser user,
        IGuild guild, string command)
    {
        ConcurrentDictionary<ThrottleKey, ThrottleInfo> throttles;
        var interval = TimeSpan.FromSeconds(intervalSeconds);
        ulong throttleObjectId;
        switch (throttleBy)
        {
            case ThrottleBy.User:
                throttles = UserThrottles;
                throttleObjectId = user.Id;
                break;
            case ThrottleBy.Guild:
                throttles = GuildThrottles;
                throttleObjectId = guild.Id;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(throttleBy), throttleBy, null);
        }

        var throttleKey = new ThrottleKey(throttleObjectId, command);
        if (throttles.TryGetValue(throttleKey, out var throttleInfo))
        {
            return interval - (DateTime.Now - throttleInfo.FirstRequestTime);
        }
        else
        {
            return TimeSpan.Zero;
        }
    }

    public bool CheckThrottle(ThrottleBy throttleBy, int requestsLimit, int intervalSeconds,
        IUser user, IGuild guild, string command)
    {
        ConcurrentDictionary<ThrottleKey, ThrottleInfo> throttles;
        var interval = TimeSpan.FromSeconds(intervalSeconds);
        ulong throttleObjectId;
        switch (throttleBy)
        {
            case ThrottleBy.User:
                throttles = UserThrottles;
                throttleObjectId = user.Id;
                break;
            case ThrottleBy.Guild:
                throttles = GuildThrottles;
                throttleObjectId = guild.Id;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(throttleBy), throttleBy, null);
        }
        var throttleKey = new ThrottleKey(throttleObjectId, command);
        if (throttles.TryGetValue(throttleKey, out var throttleInfo))
        {
            var checkThrottle = ValidateThrottle(requestsLimit, throttleInfo, interval, throttles, throttleKey);

            return checkThrottle;
        }
        else
        {
            if (!throttles.TryAdd(throttleKey, new ThrottleInfo()
                {
                    RequestCount = 1,
                    FirstRequestTime = DateTime.Now
                }))
            {
                throttleInfo = throttles[throttleKey];
                return ValidateThrottle(requestsLimit, throttleInfo, interval, throttles, throttleKey);
            }

            return true;
        }
    }

    private bool ValidateThrottle(int requestsLimit, ThrottleInfo throttleInfo, TimeSpan interval,
        ConcurrentDictionary<ThrottleKey, ThrottleInfo> throttles, ThrottleKey throttleKey)
    {
        if (DateTime.Now - throttleInfo.FirstRequestTime > interval)
        {
            if (!throttles.TryUpdate(throttleKey, new ThrottleInfo()
                {
                    FirstRequestTime = DateTime.Now,
                    RequestCount = 1
                }, throttleInfo))
            {
                return ValidateThrottle(requestsLimit, throttles[throttleKey], interval, throttles,
                    throttleKey);
            }
            return true;
        }
        else
        {
            if (throttleInfo.RequestCount + 1 <= requestsLimit)
            {
                if (!throttles.TryUpdate(throttleKey, new ThrottleInfo
                    {
                        FirstRequestTime = throttleInfo.FirstRequestTime,
                        RequestCount = throttleInfo.RequestCount + 1
                    }, throttleInfo))
                {
                    return ValidateThrottle(requestsLimit, throttles[throttleKey], interval, throttles,
                        throttleKey);
                }
                return true;
            }

            return false;
        }
    }
}