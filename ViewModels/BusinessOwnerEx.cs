using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourNamespace.Models;

namespace ViewModels
{
    public static class BusinessOwnerEx
    {
        public static BusinessOwner ToModel(this AddBusinessOwnerViewModel viewModel)
        {
            return new BusinessOwner
            {
                UserID = viewModel.UserID,
                BankAccount = viewModel.BankAccount,
                BusinessType = viewModel.BusinessType,
            };
        }

        public static BusinessOwnerDetailsViewModel ToDetailsViewModel(this BusinessOwner businessOwner)
        {
            return new BusinessOwnerDetailsViewModel
            {
                BusinessType = businessOwner.BusinessType,
                UserID = businessOwner.UserID,
                BankAccount = businessOwner.BankAccount,
            };
        }

    }
}
