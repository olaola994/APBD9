using APBD9.Data;
using APBD9.Models;
using APBD9.Models.DTOs;
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
    public async Task<IActionResult> GetTrips(int pageNumber = 1, int pageSize = 10)
    {
        var trips = await _masterContext.Trips
            .OrderBy(t => t.Name).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(
                t => new TripsDTO
                {
                    Name = t.Name,
                    Description = t.Description,
                    DateFrom = t.DateFrom,
                    DateTo = t.DateTo,
                    MaxPeople = t.MaxPeople,
                    Countries = t.IdCountries.Select(c => new CountryDTO
                    {
                        Name = c.Name
                    }).ToList(),
                    Clients = t.ClientTrips.Select(c => new ClientsDTO
                    {
                        FirstName = c.IdClientNavigation.FirstName,
                        LastName = c.IdClientNavigation.LastName
                    }).ToList()
                }).ToListAsync();
        return Ok(trips);
    }

    [HttpDelete("{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var doesClientExist = await _masterContext.Clients.FindAsync(idClient);
        if (doesClientExist != null)
        {
            var doesClientHasTrips = await _masterContext.Clients.AnyAsync(c => c.IdClient == idClient);
            if (doesClientHasTrips != null)
            {
                _masterContext.Clients.Remove(doesClientExist);
                await _masterContext.SaveChangesAsync();
            }
            else
            {
                return BadRequest($"Client with ID {idClient} is registered for one or more trips and cannot be deleted.");
            }
        }
        else
        {
            return NotFound($"Client with ID {idClient} not found.");
        }
        return Ok($"Client with ID {idClient} has been deleted successfully.");
    }

    [HttpPost("{idTrip}/clients")]
    public async Task<IActionResult> RegisterClientToTrip(AddClientToTripDTO addClientToTripDto)
        {
            var doesClientWithPeselExist = await _masterContext.Clients.FirstOrDefaultAsync(c => c.Pesel == addClientToTripDto.Pesel);
            if (doesClientWithPeselExist != null)
            {
                return Conflict($"Client with PESEL {addClientToTripDto.Pesel} already exists.");
            }
            
            var trip = await _masterContext.Trips.FirstOrDefaultAsync(t => t.IdTrip == addClientToTripDto.IdTrip);
            if (trip == null)
            {
                return BadRequest($"Trip ID {addClientToTripDto.IdTrip} does not exist.");
            }
            if (trip.DateFrom <= DateTime.Now)
            {
                return BadRequest($"Trip ID {addClientToTripDto.IdTrip} has already started.");
            }
            
            var isClientRegistered = await _masterContext.ClientTrips.AnyAsync(ct => ct.IdClient == doesClientWithPeselExist.IdClient && ct.IdTrip == addClientToTripDto.IdTrip);
            if (isClientRegistered)
            {
                return Conflict($"Client with PESEL {addClientToTripDto.Pesel} is already registered for trip ID {addClientToTripDto.IdTrip}.");
            }
            
            var newClient = new Client
            {
                FirstName = addClientToTripDto.FirstName,
                LastName = addClientToTripDto.LastName,
                Email = addClientToTripDto.Email,
                Telephone = addClientToTripDto.Telephone,
                Pesel = addClientToTripDto.Pesel
            };

            var newClientTrip = new ClientTrip
            {
                IdClientNavigation = newClient,
                IdTrip = addClientToTripDto.IdTrip,
                RegisteredAt = DateTime.Now,
                PaymentDate = addClientToTripDto.PaymentDate
            };

            _masterContext.Clients.Add(newClient);
            _masterContext.ClientTrips.Add(newClientTrip);
            await _masterContext.SaveChangesAsync();

            return Ok("Client successfully registered for the trip.");
        }
    
}