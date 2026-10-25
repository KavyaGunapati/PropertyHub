using AutoMapper;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Models.DTOs;

namespace PropertyHub.WebAPI.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Property
            CreateMap<PropertyImage, PropertyImageResponse>();

            CreateMap<Property, PropertyResponse>()
                .ForMember(d => d.AgentName, o => o.MapFrom(s => s.Agent.UserName))
                .ForMember(d => d.Images, o => o.MapFrom(s => s.Images.OrderBy(i => i.DisplayOrder)));

            CreateMap<CreatePropertyRequest, Property>()
                .ForMember(d => d.Status, o => o.MapFrom(_ => PropertyStatus.Draft))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AgentId, o => o.Ignore())
                .ForMember(d => d.Agent, o => o.Ignore())
                .ForMember(d => d.Images, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore());

            CreateMap<UpdatePropertyRequest, Property>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AgentId, o => o.Ignore())
                .ForMember(d => d.Agent, o => o.Ignore())
                .ForMember(d => d.Images, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore());

            // Used to reuse the create-request validator when updating
            CreateMap<UpdatePropertyRequest, CreatePropertyRequest>();

            // Admin
            CreateMap<Property, AdminPropertyResponse>()
                .ForMember(d => d.AgentName, o => o.MapFrom(s => s.Agent.UserName))
                .ForMember(d => d.PublishedAt, o => o.MapFrom(s => s.CreatedAt));

            // Inquiry
            CreateMap<Inquiry, InquiryResponse>()
                .ForMember(d => d.PropertyTitle, o => o.MapFrom(s => s.Property.Title))
                .ForMember(d => d.BuyerName, o => o.MapFrom(s => s.Buyer.UserName));

            CreateMap<CreateInquiryRequest, Inquiry>()
                .ForMember(d => d.Message, o => o.MapFrom(s => s.Message.Trim()))
                .ForMember(d => d.Status, o => o.MapFrom(_ => InquiryStatus.New))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PropertyId, o => o.Ignore())
                .ForMember(d => d.Property, o => o.Ignore())
                .ForMember(d => d.BuyerId, o => o.Ignore())
                .ForMember(d => d.Buyer, o => o.Ignore())
                .ForMember(d => d.Response, o => o.Ignore())
                .ForMember(d => d.RespondedAt, o => o.Ignore());

            // Review
            CreateMap<Review, ReviewResponse>()
                .ForMember(d => d.ReviewerName, o => o.MapFrom(s => s.User.UserName));

            CreateMap<CreateReviewRequest, Review>()
                .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment.Trim()))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PropertyId, o => o.Ignore())
                .ForMember(d => d.Property, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.User, o => o.Ignore());

            // Favorite
            CreateMap<Favorite, FavoriteResponse>()
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Property.Title))
                .ForMember(d => d.City, o => o.MapFrom(s => s.Property.City))
                .ForMember(d => d.State, o => o.MapFrom(s => s.Property.State))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.Property.Price))
                .ForMember(d => d.AddedAt, o => o.MapFrom(s => s.CreatedAt));
        }
    }
}
