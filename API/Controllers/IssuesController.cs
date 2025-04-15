using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewModels;
using VROOM.Models;
using VROOM.Repository;
using VROOM.Services;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : Controller
    {
        private readonly IssueService issueService;
        public IssuesController(IssueService _issueService)
        {
            issueService = _issueService;
        }




        [HttpPost("report")]
        [Authorize(Roles = "Rider")] 
        public async Task<IActionResult> ReportIssue([FromBody] IssuesViewModel issue)
        {
            var result = await issueService.ReportIssue(issue);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Value);
        }












        //[HttpPost]
        //public async Task<IActionResult> ReportingIssue(IssuesViewModel issues)
        //{

        //    var result = await issueService.ReportIssue(issues);


        //    if (!result.IsSuccess)
        //    {

        //        return BadRequest(result.Error);
        //    }


        //    return Ok(result.Value);
        //}


    }
}
