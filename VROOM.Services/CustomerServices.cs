using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.User;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;

namespace VROOM.Services
{
    public class CustomerServices
    {
        private CustomerRepository customerRepository;
        private UserRepository userRepository;
        private UserService userService;

        public CustomerServices(CustomerRepository _customerRepository, UserRepository _userRepository, UserService _userService)
        {
            customerRepository = _customerRepository;
            userRepository = _userRepository;
            userService = _userService;
        }
        public async Task<Customer> CheckForCustomer(CustomerAddViewModel CustomerAddVM)
        {
            var isThereCustomer =  await customerRepository.GetByUsernameAsync(CustomerAddVM.Username);

            // if there are a customer we will return him but if not we will create one
            if (isThereCustomer == null)
            {
               
                // here we will create a customer
                return await userService.AddNewCustomerAsync(CustomerAddVM);
            }
            else
            {
                return isThereCustomer;
            }
        }
    }
}
