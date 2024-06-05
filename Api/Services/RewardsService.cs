using GpsUtil.Location;
using System.Data;
using System.Runtime.InteropServices;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;
    private static int count = 0;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral = rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    public async Task CalculateRewards(User user)
    {
        List<VisitedLocation> userLocations = user.VisitedLocations;
        var attractions = await _gpsUtil.GetAttractions();

        for (int i = 0; i < userLocations.Count; i++)
        {
            for (int j = 0; j < attractions.Count; j++)
            {
                if (NearAttraction(userLocations[i], attractions[j]) && IsNotRewarded(user, attractions[j]))
                {
                    user.AddUserReward(new UserReward(userLocations[i], attractions[j], GetRewardPoints(attractions[j], user)));
                }
            }
        }
    }

    private bool IsNotRewarded(User user, Attraction attraction)
    {
        for (int k = 0; k < user.UserRewards.Count; k++)
        {
            if (user.UserRewards[k].Attraction.AttractionName == attraction.AttractionName)
            {
                return false;
            }
        }
        return true;
    }

    public async Task CalculateRewardsImprove(User user)
    {
        List<VisitedLocation> userLocations = user.VisitedLocations;
        var attractions = await _gpsUtil.GetAttractions();
        var userRewardedAttractions = new HashSet<string>(user.UserRewards.Select(r => r.Attraction.AttractionName));
        var nbUserLocations = userLocations.Count;
        var nbAttractions = attractions.Count;
        var tempReward = new List<UserReward>();
        var attractionNearLocation = new Dictionary<VisitedLocation, List<Attraction>>();

        for (int i = 0; i < userLocations.Count; i++)
        {
            var location = userLocations[i];
            attractionNearLocation[userLocations[i]] = attractions.Where(attraction => NearAttraction(location, attraction)).ToList();
        }
        for (int i = 0; i < userLocations.Count; i++)
        {
            for (int j = 0; j < attractionNearLocation[userLocations[i]].Count; j++)
            {
                user.AddUserReward(new UserReward(userLocations[i], attractionNearLocation[userLocations[i]][j], GetRewardPoints(attractions[j], user)));
            }
        }
    }

    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    public int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}
