using APBD9.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lab9.controllers;
[ApiController]
[Route("api[controller]")]
public class TripsController : ControllerBase
{
    private readonly MasterContext _masterContext;
    public TripsController(MasterContext masterContext)
    {
        _masterContext = masterContext;
    }
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await _masterContext.Trips.Select(e => new
        {
            Name = e.Name,
        }).ToListAsync();
        return Ok(trips);
    }
}