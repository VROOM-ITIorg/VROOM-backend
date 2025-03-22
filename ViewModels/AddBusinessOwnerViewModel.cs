using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels
{
    public class AddBusinessOwnerViewModel
    {
        [Required(ErrorMessage = "Please Provide valid Product Name")]
        public int UserID { get; set; }
        [Required(ErrorMessage = "Please Provide valid Product Name")]
        public string BankAccount { get; set; }
        [Required(ErrorMessage = "Please Provide valid Product Name")]
        public string BusinessType { get; set; }

    }
}
