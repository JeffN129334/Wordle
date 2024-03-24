using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using WordleGameServer;

namespace WordleGameClient
{
    public class Program
    {
        //TODO: Add a nicer user interface
        static async Task Main(string[] args)
        {
            //Connect to the WordleGameServer
            var channel = GrpcChannel.ForAddress("https://localhost:7103");
            var wordle = new DailyWordle.DailyWordleClient(channel);


            using (var call = wordle.Play())
            {
                //Create an empty request and a blank string
                PlayRequest playRequest = new PlayRequest() { Word = "" };
                string guess;

                Console.WriteLine("Type in a guess, or hit enter to quit!");
                do
                {
                    //Get input from user (if they just hit enter then exit the loop)
                    guess = Console.ReadLine()!;
                    if (guess != "")
                    {
                        //Populate the request with the user input and send it to the server
                        playRequest.Word = guess;
                        await call.RequestStream.WriteAsync(playRequest);

                        //Read the response from the server, and print it to the console
                        await call.ResponseStream.MoveNext();
                        PlayResponse playResponse = call.ResponseStream.Current;
                        Console.WriteLine(playResponse.Message);
                    }
                    else
                    {
                        //When the user is done then close the stream
                        await call.RequestStream.CompleteAsync();
                    }
                } while (guess != "");
            }

            //Display user stats (which is currently a placeholder string)
            Console.WriteLine("User Stats: ");
            GetStatsResponse statRes = wordle.GetStats(new Empty());
            Console.WriteLine(statRes.Message);


            Console.WriteLine("\nAll done. Press a key to exit.");
            Console.ReadKey();
        }
    }
}