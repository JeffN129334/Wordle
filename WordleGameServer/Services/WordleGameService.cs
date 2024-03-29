using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
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
        private static int totalGamesPlayed = 0;
        private static int totalGamesWon = 0;
        private static List<int> lettersInWrongSpotPerTurn = new List<int>();

        public WordleGameService(ILogger<WordleGameService> logger)
        {
            _logger = logger;
        }

        //While there are still more word requests being sent, send the word to the WordServer,
        //Respond to the client with a string containing info about the word
        public override async Task Play(IAsyncStreamReader<PlayRequest> requestStream, IServerStreamWriter<PlayResponse> responseStream, ServerCallContext context)
        {
            totalGamesPlayed++;
            lettersInWrongSpotPerTurn = new List<int>(new int[6]); // Reset for the new game
            int turn = 0;

            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                string guess = requestStream.Current.Word.ToLower();
                string correctWord = WordServiceClient.GetWord(); //Get the correct word
                bool isValid = WordServiceClient.ValidateWord(guess); //Check if the request word is valid

                // Initialize hint response and matches dictionary
                char[] results = new char[guess.Length];
                Dictionary<char, int> matchesInCorrectWord = correctWord.GroupBy(c => c).ToDictionary(grp => grp.Key, grp => grp.Count());
                Dictionary<char, int> matchesInGuess = new Dictionary<char, int>();

                if (!isValid)
                {
                    await responseStream.WriteAsync(new PlayResponse { Message = "Invalid word" });
                    continue;
                }

                for (int i = 0; i < guess.Length; i++)
                {
                    char letter = guess[i];
                    if (correctWord[i] == letter)
                    {
                        results[i] = '*'; // Correct position
                        if (!matchesInGuess.ContainsKey(letter)) matchesInGuess[letter] = 0;
                        matchesInGuess[letter]++;
                    }
                    else
                    {
                        results[i] = 'x'; // Presume incorrect until proven otherwise
                    }
                }

                //Checks letter if it is found elsewhere in correct word
                for (int i = 0; i < guess.Length; i++)
                {
                    char letter = guess[i];
                    if (results[i] != '*' && correctWord.Contains(letter))
                    {
                        int correctCount = matchesInCorrectWord.ContainsKey(letter) ? matchesInCorrectWord[letter] : 0;
                        int guessCount = matchesInGuess.ContainsKey(letter) ? matchesInGuess[letter] : 0;
                        if (guessCount < correctCount)
                        {
                            results[i] = '?'; // Correct letter, wrong position
                            if (!matchesInGuess.ContainsKey(letter)) matchesInGuess[letter] = 0;
                            matchesInGuess[letter]++;
                        }
                    }
                }

                bool isRight = correctWord == guess; // Check if the request word is correct
                string hintResponse = new string(results);
                string responseBody = $"Valid: {isValid}\nCorrect: {isRight}\nHint: {hintResponse}";

                await responseStream.WriteAsync(new PlayResponse { Message = responseBody });

                int lettersInWrongSpot = results.Count(r => r == '?');
                if (turn < lettersInWrongSpotPerTurn.Count)
                {
                    lettersInWrongSpotPerTurn[turn] = lettersInWrongSpot;
                }

                turn++;

                // End the game if the correct word is guessed
                if (isRight)
                {
                    totalGamesWon++;
                    break;
                }
            }
        }

        //Accept an empty request and return a placeholder string
        //TODO: Replace this filler code with a method that actually returns information about the game statistics (will probably need to modify dailywordle.proto)
        public override Task<GetStatsResponse> GetStats(Empty request, ServerCallContext context)
        {
            double winPercentage = totalGamesPlayed > 0 ? (double)totalGamesWon / totalGamesPlayed * 100 : 0;
            string guessDistribution = string.Join(",", lettersInWrongSpotPerTurn);

            string responseBody = $"Players: {totalGamesPlayed}\nWinners: {winPercentage:0.00}%\nGuess Distribution: {guessDistribution}";
            GetStatsResponse response = new() { Message = responseBody };
            return Task.FromResult(response);
        }
    }
}
