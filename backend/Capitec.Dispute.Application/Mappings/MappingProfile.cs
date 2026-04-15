using AutoMapper;
using Capitec.Dispute.Domain.Entities;
using Capitec.Dispute.Application.DTOs;

namespace Capitec.Dispute.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber))
            .ForMember(dest => dest.IsMfaEnabled, opt => opt.MapFrom(src => src.IsMfaEnabled));

        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        CreateMap<CreateTransactionDto, Transaction>();

        // Dispute mappings
        CreateMap<Capitec.Dispute.Domain.Entities.Dispute, DisputeDto>()
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Transaction, opt => opt.MapFrom(src => src.Transaction));

        CreateMap<CreateDisputeRequestDto, Capitec.Dispute.Domain.Entities.Dispute>();

        // Dispute Status History mappings
        CreateMap<DisputeStatusHistory, DisputeStatusHistoryDto>()
            .ForMember(dest => dest.OldStatus, opt => opt.MapFrom(src => src.OldStatus.ToString()))
            .ForMember(dest => dest.NewStatus, opt => opt.MapFrom(src => src.NewStatus.ToString()))
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.ChangedByEmployee != null 
                ? $"{src.ChangedByEmployee.FirstName} {src.ChangedByEmployee.LastName}" 
                : "System"));
    }
}