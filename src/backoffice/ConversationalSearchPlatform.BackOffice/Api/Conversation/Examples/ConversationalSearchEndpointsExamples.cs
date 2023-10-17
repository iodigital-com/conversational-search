using ConversationalSearchPlatform.BackOffice.Models.Conversations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace ConversationalSearchPlatform.BackOffice.Api.Conversation.Examples;

public static class ConversationalSearchEndpointsExamples
{

    public class SuccessExample : IExamplesProvider<ConversationRequest>
    {

        public ConversationRequest GetExamples()
        {
            return new ConversationRequest("I would like to know where the high voltage cables in a Polestar 2 Electric reside ",
                new Dictionary<string, string>()
                {
                    {
                        "make", "Polestar"
                    },
                    {
                        "model", "Polestar 2"
                    },
                    {
                        "year", "2023"
                    }
                },
                LanguageDto.English);
        }
    }

    public class Error404Example : IExamplesProvider<ProblemDetails>
    {
        public ProblemDetails GetExamples()
        {
            return new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Status = 404,
                Title = "Not Found",
            };
        }
    }
    
    public class Error500Example : IExamplesProvider<ProblemDetails>
    {
        public ProblemDetails GetExamples()
        {
            return new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Status = 500,
                Title = "An error occurred while processing your request.",
            };
        }
    }
    
    public class Error400Example : IExamplesProvider<ProblemDetails>
    {
        public ProblemDetails GetExamples()
        {
            return new ProblemDetails
            {
                Type = "https://tools.ietf.org/doc/html/rfc9110#section-15.5.1",
                Status = 400,
                Title = "Bad request",
            };
        }
    }
    
}