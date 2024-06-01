using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;
    private readonly IRewardsService _rewardsService;

    public TourGuideController(ITourGuideService tourGuideService, IRewardsService rewardsService)
    {
        _tourGuideService = tourGuideService;
        _rewardsService = rewardsService;
    }

    [HttpGet("getLocation")]
    public ActionResult<VisitedLocation> GetLocation([FromQuery] string userName)
    {
        var location = _tourGuideService.GetUserLocation(GetUser(userName));
        return Ok(location);
    }

    [HttpGet("getNearbyAttractions")]
    public async Task<ActionResult<List<Attraction>>> GetNearbyAttractions([FromQuery] string userName)
    {
        List<object> list = new List<object>();
        var visitedLocation = await _tourGuideService.GetUserLocation(GetUser(userName));
        var attractions = await _tourGuideService.GetNearByAttractions(visitedLocation);
        foreach(var attraction in attractions)
        {
            list.Add(new { 
                AttractionName = attraction.AttractionName,
                Latitude = attraction.Latitude,
                Longitude = attraction.Longitude,
                Distance = _rewardsService.GetDistance(attraction, visitedLocation.Location),
                Reward = _rewardsService.GetRewardPoints(attraction, GetUser(userName))
            });
        }
        return Ok(list);
    }

    [HttpGet("getRewards")]
    public ActionResult<List<UserReward>> GetRewards([FromQuery] string userName)
    {
        var rewards = _tourGuideService.GetUserRewards(GetUser(userName));
        return Ok(rewards);
    }

    [HttpGet("getTripDeals")]
    public ActionResult<List<Provider>> GetTripDeals([FromQuery] string userName)
    {
        var deals = _tourGuideService.GetTripDeals(GetUser(userName));
        return Ok(deals);
    }

    [HttpGet("getAllUsers")]
    public ActionResult<List<User>> GetAllUsers()
    {
        var users = _tourGuideService.GetAllUsers();
        return Ok(users);
    }

    private User GetUser(string userName)
    {
        return _tourGuideService.GetUser(userName);
    }
}
