using InsuranceDecisionIntelligence.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceDecisionIntelligence.Api.Common
{
    public static class ResultExtensions
    {
        public static IActionResult ToHttpResponse(this Result result)
        {
            return result.IsSuccess 
                ? new NoContentResult() 
                : result.Error!.ToErrorResponse();
        }

        public static IActionResult ToHttpResponse<T>(this Result<T> result)
        {
            return result.Match(
                onSuccess: value => new OkObjectResult(new { Success = true, Data = value }),
                onFailure: error => error.ToErrorResponse()
            );
        }

        public static IActionResult ToErrorResponse(this Error error)
        {
            return error.Code switch
            {
                "NOT_FOUND" => new NotFoundObjectResult(new { Success = false, Error = error }),
                "VALIDATION_ERROR" => new BadRequestObjectResult(new { Success = false, Error = error }),
                "UNAUTHORIZED" => new UnauthorizedObjectResult(new { Success = false, Error = error }),
                "FORBIDDEN" => new ForbidResult(),
                "CONFLICT" => new ConflictObjectResult(new { Success = false, Error = error }),
                _ => new ObjectResult(new { Success = false, Error = error }) { StatusCode = 500 }
            };
        }
    }
}
