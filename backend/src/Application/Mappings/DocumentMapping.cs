using Domain.Entities;
using Domain.Enums;
using Mapster;
using Application.DTOs.Document;

namespace Application.Mappings;

public class DocumentMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to DocumentResponse
        config.NewConfig<Document, DocumentResponse>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.FileSizeFormatted, src => FormatFileSize(src.FileSize))
            .Map(dest => dest.UploadedByUser, src => $"{src.UploadedBy.FirstName} {src.UploadedBy.LastName}");

        // Entity to DocumentListResponse
        config.NewConfig<Document, DocumentListResponse>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.FileSizeFormatted, src => FormatFileSize(src.FileSize));

        // CreateDocumentRequest to Entity
        config.NewConfig<CreateDocumentRequest, Document>()
            .Map(dest => dest.Type, src => Enum.Parse<DocumentType>(src.Type))
            .Map(dest => dest.UploadedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Customer)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.Ticket)
            .Ignore(dest => dest.UploadedBy);

        // UpdateDocumentRequest to Entity
        config.NewConfig<UpdateDocumentRequest, Document>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.FileName)
            .Ignore(dest => dest.FilePath)
            .Ignore(dest => dest.FileType)
            .Ignore(dest => dest.FileSize)
            .Ignore(dest => dest.Type)
            .Ignore(dest => dest.UploadedAt)
            .Ignore(dest => dest.CustomerId)
            .Ignore(dest => dest.InstallationId)
            .Ignore(dest => dest.TicketId)
            .Ignore(dest => dest.UploadedByUserId)
            .Ignore(dest => dest.Customer)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.Ticket)
            .Ignore(dest => dest.UploadedBy);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}