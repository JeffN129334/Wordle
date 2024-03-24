using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Xml.Schema;

namespace WordServer.Services
{
    //Implementation of the protobuf interface
    public class WordService : DailyWord.DailyWordBase
    {
        private readonly ILogger<WordService> _logger;
        private readonly string[] _words;

        //Constructor which instantiates the _words array with valid words using a helper method
        public WordService(ILogger<WordService> logger)
        {
            _logger = logger;
            _words = GetWords("wordle.json");
        }

        //Gets a daily word using a helper method, returns a response with a string (no request object here, just empty request)
        public override Task<WordResponse> GetWord(Empty request, ServerCallContext context)
        {
            WordResponse response = new() {Word = _words[RandomizeUsingCurrentDate()] };
            return Task.FromResult(response);
        }

        //Checks if the request word is present in the valid words list, returns a response with a bool
        public override Task<ValidationResponse> ValidateWord(ValidationRequest request, ServerCallContext context)
        {
            bool isValid = _words.Contains(request.Word.ToLower());
            ValidationResponse response = new() { Valid = isValid };
            return Task.FromResult(response);
        }

        //---------------------------Helper Methods---------------------------
        //Load the words from the json file into an array
        private string[] GetWords(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<string[]>(jsonString)!;
        }

        //Use the current date to generate a unique integer,
        //Then use it as a seed to generate a random number which will be used as an index for the daily word
        private int RandomizeUsingCurrentDate()
        {
            DateTime currentDate = DateTime.Now;
            string seedAsString = currentDate.Year.ToString() + currentDate.Month.ToString() + currentDate.Day.ToString();
            int seed = int.Parse(seedAsString);

            //Use the % operator to ensure the random number is not bigger than the length of the words list
            int index = new Random(seed).Next() % _words.Length;
            return index;
        }
    }
}
