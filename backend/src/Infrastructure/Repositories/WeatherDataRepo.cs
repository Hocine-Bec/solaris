using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class WeatherDataRepo(AppDbContext context) : GenericRepo<WeatherData>(context), IWeatherDataRepo
{
}