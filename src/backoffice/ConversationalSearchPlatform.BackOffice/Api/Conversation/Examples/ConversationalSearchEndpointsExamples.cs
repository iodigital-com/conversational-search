using ConversationalSearchPlatform.BackOffice.Models.Conversations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace ConversationalSearchPlatform.BackOffice.Api.Conversation.Examples;

public static class ConversationalSearchEndpointsExamples
{
    public class ConversationReferencedResponseExample : IExamplesProvider<ConversationReferencedResponse>
    {
        public ConversationReferencedResponse GetExamples()
        {
            return new ConversationReferencedResponse(new ConversationResponse(
                    Guid.NewGuid(),
                    "  The Pedestrian Protection System (PPS) is a feature in the 2022 Polestar that helps mitigate a pedestrian's impact in certain frontal collisions. The sensors, which are active at a speed of approximately 25-50 km/h (15-30 mph), detect collisions with objects that have properties similar to those of a human leg. When the PPS is activated due to a collision, the bonnet of the vehicle is raised and pushed back slightly, and an automatic alarm is sent via Polestar Connect. It's important to note, however, that the system may also be activated if the vehicle collides with an object that sends a similar signal to the sensors as a pedestrian [1][3].\n\nPlease remember to contact Polestar Customer Support if the front of your vehicle sustains any damage, to ensure that the system is still functioning properly [4]. \n\nAlso, never modify or repair the system yourself, as this could cause it to malfunction. All repairs or modifications should be handled by Polestar Customer Support [7].\n\nHere is an image of the symbol for the Pedestrian Protection System: <img src=\"https://www.volvocars.com/images/support/imgac1e169a5f170656c0a8015261d76982_1_--_--_VOICEpnghigh.png\" alt=\"Pedestrian Protection System symbol\"> [1].)",
                    LanguageDto.English),
                new List<ConversationReferenceResponse>
                {
                    new(1,
                        "https://www.polestar.com/uk/manual/polestar-2/2022/article/Pedestrian-Protection-System",
                        ConversationReferenceTypeDto.Official)
                }
            );
        }
    }

    public class StartConversationRequestSuccess : IExamplesProvider<StartConversationRequest>
    {
        public StartConversationRequest GetExamples()
        {
            return new StartConversationRequest();
        }
    }

    public class StartConversationResponseSuccess : IExamplesProvider<StartConversationResponse>
    {
        public StartConversationResponse GetExamples()
        {
            return new StartConversationResponse(Guid.NewGuid());
        }
    }

    public class SuccessExample : IExamplesProvider<ConversationRequest>
    {

        public ConversationRequest GetExamples()
        {
            return new ConversationRequest(Guid.NewGuid(),
                "I would like to know where the high voltage cables in a Polestar 2 Electric reside ",
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

    public class ConversationSimulationResponseExample : IExamplesProvider<ConversationSimulationResponse>
    {
        public ConversationSimulationResponse GetExamples() =>
            new("A prompt here");
    }
}