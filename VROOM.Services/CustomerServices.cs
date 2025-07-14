// VROOM.Services/CustomerServices.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.Feedback;
using ViewModels.User;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;

namespace VROOM.Services
{
    public class CustomerServices
    {
        private readonly CustomerRepository customerRepository;
        private readonly UserRepository userRepository;
        private readonly UserService userService;
        private readonly FeedbackRepository feedbackRepository;

        public CustomerServices(CustomerRepository _customerRepository, UserRepository _userRepository, UserService _userService, FeedbackRepository _feedbackRepository)
        {
            customerRepository = _customerRepository;
            userRepository = _userRepository;
            userService = _userService;
            feedbackRepository = _feedbackRepository;
        }

        public async Task<Customer> CheckForCustomer(CustomerAddViewModel CustomerAddVM)
        {
            var isThereCustomer = await customerRepository.GetByUsernameAsync(CustomerAddVM.Username);

            if (isThereCustomer == null)
            {
                return await userService.AddNewCustomerAsync(CustomerAddVM);
            }
            else
            {
                return isThereCustomer;
            }
        }

        public async Task<bool> AddFeedbackAsync(string customerId, FeedbackRequest feedbackRequest)
        {
            return await feedbackRepository.AddFeedbackAsync(customerId, feedbackRequest);
        }
    }
}