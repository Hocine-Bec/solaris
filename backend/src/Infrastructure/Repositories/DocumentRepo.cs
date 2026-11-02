using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class DocumentRepo(AppDbContext context) : GenericRepo<Document>(context), IDocumentRepo
{
}