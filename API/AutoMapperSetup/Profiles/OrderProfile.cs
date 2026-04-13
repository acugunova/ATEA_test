using AutoMapper;
using ATEA_test.API.Models;

namespace ATEA_test.API.AutoMapperSetup.Profiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, Receipt>()
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.PaymentConfirmation, opt => opt.Ignore());
        }
    }
}
