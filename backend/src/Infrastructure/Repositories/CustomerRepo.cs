using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class CustomerRepo(AppDbContext context) : GenericRepo<Customer>(context), ICustomerRepo
{
}