//using AutoMapper;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using VROOM.ViewModels;
//using VROOM.Models;
//namespace VROOM.Services.Mapping
//{
//    public class MappingProfile : Profile
//    {
//        public MappingProfile()
//        {
//            CreateMap<Rider, RiderViewModel>()
//              .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
//              .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
//              .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
//              .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ImagePath));


//            // Entity → RiderDTO
//            CreateMap<Rider, RiderDTO>()
//             .ForMember(dest => dest.VehicleType, opt => opt.MapFrom(src => src.VehicleType))
//             .ForMember(dest => dest.VehicleStatus, opt => opt.MapFrom(src => src.VehicleStatus))
//             .ForMember(dest => dest.ExperienceLevel, opt => opt.MapFrom(src => src.ExperienceLevel))
//             .ForMember(dest => dest.Location, opt => opt.MapFrom(src => new LocationDto
//             {
//                 Lat = src.Lat,
//                 Lang = src.Lang,
//                 Area = src.Area
//             }));

//            CreateMap<RiderDTO, Rider>()
//        .ForMember(dest => dest.VehicleType, opt => opt.MapFrom(src => src.VehicleType))
//        .ForMember(dest => dest.VehicleStatus, opt => opt.MapFrom(src => src.VehicleStatus))
//        .ForMember(dest => dest.ExperienceLevel, opt => opt.MapFrom(src => src.ExperienceLevel))
//        .ForMember(dest => dest.Location, opt => opt.MapFrom(src => new Location
//        {
//            Lat = src.Location.Lat,
//            Lang = src.Location.Lang,
//            Area = src.Location.Area
//        }));

//            // Map from Rider Entity to RiderVM (ViewModel)
//            CreateMap<Rider, RiderVM>()
//                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User.FullName))
//                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
//                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

//            // Map from RiderRegisterRequest to Rider Entity (for registration)
//            CreateMap<RiderRegisterRequest, Rider>()
//                .ForMember(dest => dest.User.Name, opt => opt.MapFrom(src => src.Name))
//                .ForMember(dest => dest.User.Email, opt => opt.MapFrom(src => src.Email))
//                .ForMember(dest => dest.VehicleType, opt => opt.MapFrom(src => src.VehicleType))
//                .ForMember(dest => dest.VehicleStatus, opt => opt.MapFrom(src => src.VehicleStatus))
//                .ForMember(dest => dest.ExperienceLevel, opt => opt.MapFrom(src => src.ExperienceLevel))
//                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => new Location
//                {
//                    Lat = src.Location.Lat,
//                    Lang = src.Location.Lang,
//                    Area = src.Location.Area
//                }));

//            // Map LocationDto to Location (for detailed location mapping)
//            CreateMap<LocationDto, Location>()
//                .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Lat))
//                .ForMember(dest => dest.Lang, opt => opt.MapFrom(src => src.Lang))
//                .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.Area));
//        }
//    }
//    }
//}
