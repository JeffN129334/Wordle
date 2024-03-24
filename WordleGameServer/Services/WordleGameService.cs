using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Xml.Schema;
using WordServer.Clients;

namespace WordleGameServer.Services
{
    public class WordleGameService : DailyWordle.DailyWordleBase
    {
        private readonly ILogger<WordleGameService> _logger;

        public WordleGameService(ILogger<WordleGameService> logger)
        {
            _logger = logger;
        }

        //While there are still more word requests being sent, send the word to the WordServer,
        //Respond to the client with a string containing info about the word
        //TODO: Replace this filler code with the actual logic for the wordle game (will probably need to modify dailywordle.proto)
        public override async Task Play(IAsyncStreamReader<PlayRequest> requestStream, IServerStreamWriter<PlayResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                string correctWord = WordServiceClient.GetWord();                                                          //Get the correct word
                bool isValid = WordServiceClient.ValidateWord(requestStream.Current.Word.ToLower()); //Check if the request word is valid
                bool isRight = correctWord == requestStream.Current.Word.ToLower();                             //Check if the request word is correct
                

                string responseBody = $"Valid: {isValid}\nCorrect: {isRight}\n(Hint: {correctWord})";

                PlayResponse response = new PlayResponse() {Message = responseBody };

                await responseStream.WriteAsync(response);
            }
        }

        //Accept an empty request and return a placeholder string
        //TODO: Replace this filler code with a method that actually returns information about the game statistics (will probably need to modify dailywordle.proto)
        public override Task<GetStatsResponse> GetStats(Empty request, ServerCallContext context)
        {
            GetStatsResponse response = new() { Message = "Not yet implemented" };
            return Task.FromResult(response);
        }
    }
}
