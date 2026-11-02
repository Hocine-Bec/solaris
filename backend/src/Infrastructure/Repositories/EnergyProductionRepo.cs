using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class EnergyProductionRepo(AppDbContext context) : GenericRepo<EnergyProduction>(context), IEnergyProductionRepo
{
}